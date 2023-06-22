using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.Structs;
using Lumina.Excel.GeneratedSheets;
using OtterGui.Log;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateManager : IReadOnlyDictionary<ActorIdentifier, ActorState>
{
    private readonly ActorService         _actors;
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly VisorService         _visor;
    private readonly StateChanged         _event;
    private readonly ObjectManager        _objects;
    private readonly StateEditor          _editor;

    private readonly PenumbraService _penumbra;

    private readonly Dictionary<ActorIdentifier, ActorState> _states = new();

    public StateManager(ActorService actors, ItemManager items, CustomizationService customizations, VisorService visor, StateChanged @event,
        PenumbraService penumbra, ObjectManager objects, StateEditor editor)
    {
        _actors         = actors;
        _items          = items;
        _customizations = customizations;
        _visor          = visor;
        _event          = @event;
        _penumbra       = penumbra;
        _objects        = objects;
        _editor         = editor;
    }

    public bool GetOrCreate(Actor actor, [NotNullWhen(true)] out ActorState? state)
        => GetOrCreate(actor.GetIdentifier(_actors.AwaitedService), actor, out state);

    public bool GetOrCreate(ActorIdentifier identifier, Actor actor, [NotNullWhen(true)] out ActorState? state)
    {
        if (TryGetValue(identifier, out state))
            return true;

        try
        {
            var designData = FromActor(actor);
            state = new ActorState(identifier)
            {
                ModelData = designData,
                BaseData  = designData,
            };
            _states.Add(identifier, state);
            return true;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not create new actor data for {identifier}:\n{ex}");
            return false;
        }
    }

    public void UpdateEquip(ActorState state, EquipSlot slot, CharacterArmor armor)
    {
        var current = state.ModelData.Item(slot);
        if (armor.Set.Value != current.ModelId.Value || armor.Variant != current.Variant)
        {
            var item = _items.Identify(slot, armor.Set, armor.Variant);
            state.ModelData.SetItem(slot, item);
        }

        state.ModelData.SetStain(slot, armor.Stain);
    }

    public void UpdateWeapon(ActorState state, EquipSlot slot, CharacterWeapon weapon)
    {
        var current = state.ModelData.Item(slot);
        if (weapon.Set.Value != current.ModelId.Value || weapon.Variant != current.Variant || weapon.Type.Value != current.WeaponType.Value)
        {
            var item = _items.Identify(slot, weapon.Set, weapon.Type, (byte)weapon.Variant,
                slot == EquipSlot.OffHand ? state.ModelData.Item(EquipSlot.MainHand).Type : FullEquipType.Unknown);
            state.ModelData.SetItem(slot, item);
        }

        state.ModelData.SetStain(slot, weapon.Stain);
    }

    public unsafe void Update(ActorState state, Actor actor)
    {
        if (!actor.IsCharacter)
            return;

        if (actor.AsCharacter->ModelCharaId != state.ModelData.ModelId)
            return;

        var model = actor.Model;

        state.ModelData.SetHatVisible(!actor.AsCharacter->DrawData.IsHatHidden);
        state.ModelData.SetIsWet(actor.AsCharacter->IsGPoseWet);
        state.ModelData.SetWeaponVisible(!actor.AsCharacter->DrawData.IsWeaponHidden);

        if (model.IsHuman)
        {
            var head = state.ModelData.IsHatVisible() ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
            UpdateEquip(state, EquipSlot.Head, head);

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
                UpdateEquip(state, slot, model.GetArmor(slot));

            state.ModelData.Customize = model.GetCustomize();
            var (_, _, main, off)     = model.GetWeapons(actor);
            UpdateWeapon(state, EquipSlot.MainHand, main);
            UpdateWeapon(state, EquipSlot.OffHand,  off);
            state.ModelData.SetVisor(_visor.GetVisorState(model));
        }
        else
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
                UpdateEquip(state, slot, actor.GetArmor(slot));

            state.ModelData.Customize = actor.GetCustomize();
            UpdateWeapon(state, EquipSlot.MainHand, actor.GetMainhand());
            UpdateWeapon(state, EquipSlot.OffHand,  actor.GetOffhand());
            state.ModelData.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);
        }
    }

    public IEnumerator<KeyValuePair<ActorIdentifier, ActorState>> GetEnumerator()
        => _states.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _states.Count;

    public bool ContainsKey(ActorIdentifier key)
        => _states.ContainsKey(key);

    public bool TryGetValue(ActorIdentifier key, out ActorState value)
        => _states.TryGetValue(key, out value!);

    public ActorState this[ActorIdentifier key]
        => _states[key];

    public IEnumerable<ActorIdentifier> Keys
        => _states.Keys;

    public IEnumerable<ActorState> Values
        => _states.Values;

    public unsafe DesignData FromActor(Actor actor)
    {
        var ret = new DesignData();
        if (!actor.IsCharacter)
        {
            ret.SetDefaultEquipment(_items);
            return ret;
        }

        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId, *(Customize*)&actor.AsCharacter->DrawData.CustomizeData,
                (byte*)&actor.AsCharacter->DrawData.Head);
            return ret;
        }

        var             model = actor.Model;
        CharacterWeapon main;
        CharacterWeapon off;

        ret.SetHatVisible(!actor.AsCharacter->DrawData.IsHatHidden);
        if (model.IsHuman)
        {
            var head     = ret.IsHatVisible() ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
            var headItem = _items.Identify(EquipSlot.Head, head.Set, head.Variant);
            ret.SetItem(EquipSlot.Head, headItem);
            ret.SetStain(EquipSlot.Head, head.Stain);

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                var armor = model.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            ret.Customize     = model.GetCustomize();
            (_, _, main, off) = model.GetWeapons(actor);
            ret.SetVisor(_visor.GetVisorState(model));
        }
        else
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = actor.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            ret.Customize = actor.GetCustomize();
            main          = actor.GetMainhand();
            off           = actor.GetOffhand();
            ret.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);
        }

        var mainItem = _items.Identify(EquipSlot.MainHand, main.Set, main.Type, (byte)main.Variant);
        var offItem  = _items.Identify(EquipSlot.OffHand,  off.Set,  off.Type,  (byte)off.Variant, mainItem.Type);
        ret.SetItem(EquipSlot.MainHand, mainItem);
        ret.SetStain(EquipSlot.MainHand, main.Stain);
        ret.SetItem(EquipSlot.OffHand, offItem);
        ret.SetStain(EquipSlot.OffHand, off.Stain);

        ret.SetIsWet(actor.AsCharacter->IsGPoseWet);
        ret.SetWeaponVisible(!actor.AsCharacter->DrawData.IsWeaponHidden);
        return ret;
    }

    /// <summary> Change a customization value. </summary>
    public void ChangeCustomize(ActorState state, ActorData data, CustomizeIndex idx, CustomizeValue value, StateChanged.Source source,
        bool force)
    {
        ref var s = ref state[idx];
        if (s is StateChanged.Source.Fixed && source is StateChanged.Source.Game)
            return;

        var oldValue = state.ModelData.Customize[idx];
        if (oldValue == value && !force)
            return;

        state.ModelData.Customize[idx] = value;

        Glamourer.Log.Excessive(
            $"Changed customize {idx.ToDefaultName()} for {state.Identifier} ({string.Join(", ", data.Objects.Select(o => $"0x{o.Address}"))}) from {oldValue.Value} to {value.Value}.");
        _event.Invoke(StateChanged.Type.Customize, source, state, data, (oldValue, value, idx));
    }

    public void ApplyDesign(Design design, ActorState state)
    {
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            switch (design.DoApplyEquip(slot), design.DoApplyStain(slot))
            {
                case (false, false): continue;
                case (true, false):
                    ChangeEquip(state, slot, design.DesignData.Item(slot), StateChanged.Source.Manual);
                    break;
                case (false, true):
                    ChangeStain(state, slot, design.DesignData.Stain(slot), StateChanged.Source.Manual);
                    break;
                case (true, true):
                    ChangeEquip(state, slot, design.DesignData.Item(slot), design.DesignData.Stain(slot), StateChanged.Source.Manual);
                    break;
            }
        }
        if (design.DoApplyHatVisible())
            ChangeHatState(state, design.DesignData.IsHatVisible(), StateChanged.Source.Manual);
        if (design.DoApplyWeaponVisible())
            ChangeWeaponState(state, design.DesignData.IsWeaponVisible(), StateChanged.Source.Manual);
        if (design.DoApplyVisorToggle())
            ChangeVisorState(state, design.DesignData.IsVisorToggled(), StateChanged.Source.Manual);
        if (design.DoApplyWetness())
            ChangeWetness(state, design.DesignData.IsWet());
    }

    public void ResetState(ActorState state)
    {
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            ChangeEquip(state, slot, state.BaseData.Item(slot), state.BaseData.Stain(slot), StateChanged.Source.Game);
            _editor.ChangeArmor(state, objects, slot);
        }
    }

    public void ReapplyState(Actor actor)
    {
        if (!GetOrCreate(actor, out var state))
            return;

        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            _editor.ChangeArmor(state, objects, slot);
    }

    public void ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StateChanged.Source source)
    {
        var old = state.ModelData.Item(slot);
        state.ModelData.SetItem(slot, item);
        state[slot, false] = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeArmor(state, objects, slot);
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} equipment piece in state {state.Identifier} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}). [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Equip, source, state, objects, (old, item, slot));
    }

    public void ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StainId stain, StateChanged.Source source)
    {
        var old      = state.ModelData.Item(slot);
        var oldStain = state.ModelData.Stain(slot);
        state.ModelData.SetItem(slot, item);
        state.ModelData.SetStain(slot, stain);
        state[slot, false] = source;
        state[slot, true]  = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeArmor(state, objects, slot);
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} equipment piece in state {state.Identifier} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}) and its stain from {oldStain.Value} to {stain.Value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Equip, source, state, objects, (old, item, slot));
        _event.Invoke(StateChanged.Type.Stain, source, state, objects, (oldStain, stain, slot));
    }

    public void ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateChanged.Source source)
    {
        var old = state.ModelData.Stain(slot);
        state.ModelData.SetStain(slot, stain);
        state[slot, true] = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeArmor(state, objects, slot);
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} stain in state {state.Identifier} from {old.Value} to {stain.Value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Stain, source, state, objects, (old, stain, slot));
    }

    public void ChangeHatState(ActorState state, bool value, StateChanged.Source source)
    {
        var old = state.ModelData.IsHatVisible();
        state.ModelData.SetHatVisible(value);
        state[ActorState.MetaFlag.HatState] = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeHatState(objects, value);
        Glamourer.Log.Verbose(
            $"Set Head Gear Visibility in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.HatState));
    }

    public void ChangeWeaponState(ActorState state, bool value, StateChanged.Source source)
    {
        var old = state.ModelData.IsWeaponVisible();
        state.ModelData.SetWeaponVisible(value);
        state[ActorState.MetaFlag.WeaponState] = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeWeaponState(objects, value);
        Glamourer.Log.Verbose(
            $"Set Weapon Visibility in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.WeaponState));
    }

    public void ChangeVisorState(ActorState state, bool value, StateChanged.Source source)
    {
        var old = state.ModelData.IsVisorToggled();
        state.ModelData.SetVisor(value);
        state[ActorState.MetaFlag.VisorState] = source;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeVisor(objects, value);
        Glamourer.Log.Verbose(
            $"Set Visor State in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.VisorState));
    }

    public void ChangeWetness(ActorState state, bool value)
    {
        var old = state.ModelData.IsWet();
        state.ModelData.SetIsWet(value);
        state[ActorState.MetaFlag.Wetness] = value ? StateChanged.Source.Manual : StateChanged.Source.Game;
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        _editor.ChangeWetness(objects, value);
        Glamourer.Log.Verbose(
            $"Set Wetness in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, state[ActorState.MetaFlag.Wetness], state, objects, (old, value, ActorState.MetaFlag.Wetness));
    }

    //
    ///// <summary> Change whether to apply a specific customize value. </summary>
    //public void ChangeApplyCustomize(Design design, CustomizeIndex idx, bool value)
    //{
    //    if (!design.SetApplyCustomize(idx, value))
    //        return;
    //
    //    design.LastEdit = DateTimeOffset.UtcNow;
    //    _saveService.QueueSave(design);
    //    Glamourer.Log.Debug($"Set applying of customization {idx.ToDefaultName()} to {value}.");
    //    _event.Invoke(DesignChanged.Type.ApplyCustomize, design, idx);
    //}
    //

    //
    ///// <summary> Change a weapon. </summary>
    //public void ChangeWeapon(Design design, EquipSlot slot, EquipItem item)
    //{
    //    var currentMain = design.DesignData.Item(EquipSlot.MainHand);
    //    var currentOff  = design.DesignData.Item(EquipSlot.OffHand);
    //    switch (slot)
    //    {
    //        case EquipSlot.MainHand:
    //            var newOff = currentOff;
    //            if (item.Type == currentMain.Type)
    //            {
    //                if (_items.ValidateWeapons(item.Id, currentOff.Id, out _, out _).Length != 0)
    //                    return;
    //            }
    //            else
    //            {
    //                var newOffId = FullEquipTypeExtensions.OffhandTypes.Contains(item.Type)
    //                    ? item.Id
    //                    : ItemManager.NothingId(item.Type.Offhand());
    //                if (_items.ValidateWeapons(item.Id, newOffId, out _, out newOff).Length != 0)
    //                    return;
    //            }
    //
    //            design.DesignData.SetItem(EquipSlot.MainHand, item);
    //            design.DesignData.SetItem(EquipSlot.OffHand,  newOff);
    //            design.LastEdit = DateTimeOffset.UtcNow;
    //            _saveService.QueueSave(design);
    //            Glamourer.Log.Debug(
    //                $"Set {EquipSlot.MainHand.ToName()} weapon in design {design.Identifier} from {currentMain.Name} ({currentMain.Id}) to {item.Name} ({item.Id}).");
    //            _event.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, item, newOff));
    //            return;
    //        case EquipSlot.OffHand:
    //            if (item.Type != currentOff.Type)
    //                return;
    //            if (_items.ValidateWeapons(currentMain.Id, item.Id, out _, out _).Length > 0)
    //                return;
    //
    //            if (!design.DesignData.SetItem(EquipSlot.OffHand, item))
    //                return;
    //
    //            design.LastEdit = DateTimeOffset.UtcNow;
    //            _saveService.QueueSave(design);
    //            Glamourer.Log.Debug(
    //                $"Set {EquipSlot.OffHand.ToName()} weapon in design {design.Identifier} from {currentOff.Name} ({currentOff.Id}) to {item.Name} ({item.Id}).");
    //            _event.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, currentMain, item));
    //            return;
    //        default: return;
    //    }
    //}
    //
    ///// <summary> Change whether to apply a specific equipment piece. </summary>
    //public void ChangeApplyEquip(Design design, EquipSlot slot, bool value)
    //{
    //    if (!design.SetApplyEquip(slot, value))
    //        return;
    //
    //    design.LastEdit = DateTimeOffset.UtcNow;
    //    _saveService.QueueSave(design);
    //    Glamourer.Log.Debug($"Set applying of {slot} equipment piece to {value}.");
    //    _event.Invoke(DesignChanged.Type.ApplyEquip, design, slot);
    //}
    //
    ///// <summary> Change the stain for any equipment piece. </summary>
    //public void ChangeStain(Design design, EquipSlot slot, StainId stain)
    //{
    //    if (_items.ValidateStain(stain, out _).Length > 0)
    //        return;
    //
    //    var oldStain = design.DesignData.Stain(slot);
    //    if (!design.DesignData.SetStain(slot, stain))
    //        return;
    //
    //    design.LastEdit = DateTimeOffset.UtcNow;
    //    _saveService.QueueSave(design);
    //    Glamourer.Log.Debug($"Set stain of {slot} equipment piece to {stain.Value}.");
    //    _event.Invoke(DesignChanged.Type.Stain, design, (oldStain, stain, slot));
    //}
    //
    ///// <summary> Change whether to apply a specific stain. </summary>
    //public void ChangeApplyStain(Design design, EquipSlot slot, bool value)
    //{
    //    if (!design.SetApplyStain(slot, value))
    //        return;
    //
    //    design.LastEdit = DateTimeOffset.UtcNow;
    //    _saveService.QueueSave(design);
    //    Glamourer.Log.Debug($"Set applying of stain of {slot} equipment piece to {value}.");
    //    _event.Invoke(DesignChanged.Type.ApplyStain, design, slot);
    //}
}
