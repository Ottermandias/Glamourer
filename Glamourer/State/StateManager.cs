using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateManager : IReadOnlyDictionary<ActorIdentifier, ActorState>, IDisposable
{
    private readonly ActorService         _actors;
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly VisorService         _visor;
    private readonly StateChanged         _event;

    private readonly PenumbraService _penumbra;
    private readonly UpdatedSlot     _updatedSlot;

    private readonly Dictionary<ActorIdentifier, ActorState> _states = new();

    public StateManager(ActorService actors, ItemManager items, CustomizationService customizations, VisorService visor, StateChanged @event,
        UpdatedSlot updatedSlot, PenumbraService penumbra)
    {
        _actors         = actors;
        _items          = items;
        _customizations = customizations;
        _visor          = visor;
        _event          = @event;
        _updatedSlot    = updatedSlot;
        _penumbra       = penumbra;
        _updatedSlot.Subscribe(OnSlotUpdated, UpdatedSlot.Priority.StateManager);
    }

    public void Dispose()
    {
        _updatedSlot.Unsubscribe(OnSlotUpdated);
    }

    private unsafe void OnSlotUpdated(Model model, EquipSlot slot, Ref<CharacterArmor> armor, Ref<ulong> returnValue)
    {
        var actor     = _penumbra.GameObjectFromDrawObject(model);
        var customize = model.GetCustomize();
        if (!actor.AsCharacter->DrawData.IsHatHidden && actor.Identifier(_actors.AwaitedService, out var identifier) && _states.TryGetValue(identifier, out var state))
        {
            ref var armorState = ref state[slot, false];
            ref var stainState = ref state[slot, true];
            if (armorState != StateChanged.Source.Fixed)
            {
                armorState = StateChanged.Source.Game;
                var current = state.Data.Item(slot);
                if (current.ModelId.Value != armor.Value.Set.Value || current.Variant != armor.Value.Variant)
                {
                    var item = _items.Identify(slot, armor.Value.Set, armor.Value.Variant);
                    state.Data.SetItem(slot, item);
                }
            }

            if (stainState != StateChanged.Source.Fixed)
            {
                stainState = StateChanged.Source.Game;
                state.Data.SetStain(slot, armor.Value.Stain);
            }
        }

        var (replaced, replacedArmor) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
        if (replaced)
            armor.Assign(replacedArmor);
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
            _states.Add(identifier, new ActorState(identifier) { Data = designData });
            return true;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not create new actor data for {identifier}:\n{ex}");
            return false;
        }
    }

    public unsafe void Update(ref DesignData data, Actor actor)
    {
        if (!actor.IsCharacter)
            return;

        if (actor.AsCharacter->ModelCharaId != data.ModelId)
            return;

        var model = actor.Model;

        static bool EqualArmor(CharacterArmor armor, EquipItem item)
            => armor.Set.Value == item.ModelId.Value && armor.Variant == item.Variant;

        static bool EqualWeapon(CharacterWeapon weapon, EquipItem item)
            => weapon.Set.Value == item.ModelId.Value && weapon.Type.Value == item.WeaponType.Value && weapon.Variant == item.Variant;

        data.SetHatVisible(!actor.AsCharacter->DrawData.IsHatHidden);
        data.SetIsWet(actor.AsCharacter->IsGPoseWet);
        data.SetWeaponVisible(!actor.AsCharacter->DrawData.IsWeaponHidden);

        CharacterWeapon main;
        CharacterWeapon off;
        if (model.IsHuman)
        {
            var head = data.IsHatVisible() ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
            data.SetStain(EquipSlot.Head, head.Stain);
            if (!EqualArmor(head, data.Item(EquipSlot.Head)))
            {
                var headItem = _items.Identify(EquipSlot.Head, head.Set, head.Variant);
                data.SetItem(EquipSlot.Head, headItem);
            }

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                var armor = model.GetArmor(slot);
                data.SetStain(slot, armor.Stain);
                if (EqualArmor(armor, data.Item(slot)))
                    continue;

                var item = _items.Identify(slot, armor.Set, armor.Variant);
                data.SetItem(slot, item);
            }

            data.Customize    = model.GetCustomize();
            (_, _, main, off) = model.GetWeapons(actor);
            data.SetVisor(_visor.GetVisorState(model));
        }
        else
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = actor.GetArmor(slot);
                data.SetStain(slot, armor.Stain);
                if (EqualArmor(armor, data.Item(slot)))
                    continue;

                var item = _items.Identify(slot, armor.Set, armor.Variant);
                data.SetItem(slot, item);
            }

            data.Customize = actor.GetCustomize();
            main           = actor.GetMainhand();
            off            = actor.GetOffhand();
            data.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);
        }

        data.SetStain(EquipSlot.MainHand, main.Stain);
        data.SetStain(EquipSlot.OffHand,  off.Stain);
        if (!EqualWeapon(main, data.Item(EquipSlot.MainHand)))
        {
            var mainItem = _items.Identify(EquipSlot.MainHand, main.Set, main.Type, (byte)main.Variant);
            data.SetItem(EquipSlot.MainHand, mainItem);
        }

        if (!EqualWeapon(off, data.Item(EquipSlot.OffHand)))
        {
            var offItem = _items.Identify(EquipSlot.OffHand, off.Set, off.Type, (byte)off.Variant, data.Item(EquipSlot.MainHand).Type);
            data.SetItem(EquipSlot.OffHand, offItem);
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

        if (actor.AsCharacter->ModelCharaId != 0)
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->ModelCharaId, *(Customize*)&actor.AsCharacter->DrawData.CustomizeData,
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

        var oldValue = state.Data.Customize[idx];
        if (oldValue == value && !force)
            return;

        state.Data.Customize[idx] = value;

        Glamourer.Log.Excessive(
            $"Changed customize {idx.ToDefaultName()} for {state.Identifier} ({string.Join(", ", data.Objects.Select(o => $"0x{o.Address}"))}) from {oldValue.Value} to {value.Value}.");
        _event.Invoke(StateChanged.Type.Customize, source, state, data, (oldValue, value, idx));
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
    ///// <summary> Change a non-weapon equipment piece. </summary>
    //public void ChangeEquip(Design design, EquipSlot slot, EquipItem item)
    //{
    //    if (_items.ValidateItem(slot, item.Id, out item).Length > 0)
    //        return;
    //
    //    var old = design.DesignData.Item(slot);
    //    if (!design.DesignData.SetItem(slot, item))
    //        return;
    //
    //    Glamourer.Log.Debug(
    //        $"Set {slot.ToName()} equipment piece in design {design.Identifier} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}).");
    //    _saveService.QueueSave(design);
    //    _event.Invoke(DesignChanged.Type.Equip, design, (old, item, slot));
    //}
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
