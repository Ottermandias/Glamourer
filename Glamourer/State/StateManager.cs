using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
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

    private readonly Dictionary<ActorIdentifier, ActorState> _states = new();

    public StateManager(ActorService actors, ItemManager items, CustomizationService customizations, VisorService visor, StateChanged @event,
        ObjectManager objects, StateEditor editor)
    {
        _actors         = actors;
        _items          = items;
        _customizations = customizations;
        _visor          = visor;
        _event          = @event;
        _objects        = objects;
        _editor         = editor;
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

    /// <inheritdoc cref="GetOrCreate(ActorIdentifier, Actor, out ActorState?)"/>
    public bool GetOrCreate(Actor actor, [NotNullWhen(true)] out ActorState? state)
        => GetOrCreate(actor.GetIdentifier(_actors.AwaitedService), actor, out state);

    /// <summary> Try to obtain or create a new state for an existing actor. Returns false if no state could be created. </summary>
    public unsafe bool GetOrCreate(ActorIdentifier identifier, Actor actor, [NotNullWhen(true)] out ActorState? state)
    {
        if (TryGetValue(identifier, out state))
            return true;

        try
        {
            // Initial Creation, use the actors data for the base data, 
            // and the draw objects data for the model data (where possible).
            state = new ActorState(identifier)
            {
                ModelData = FromActor(actor, true),
                BaseData  = FromActor(actor, false),
                LastJob = (byte) (actor.IsCharacter ? actor.AsCharacter->CharacterData.ClassJob : 0),
            };
            // state.Identifier is owned.
            _states.Add(state.Identifier, state);
            return true;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not create new actor data for {identifier}:\n{ex}");
            return false;
        }
    }

    /// <summary>
    /// Create DesignData from a given actor.
    /// This uses the draw object if available and where possible,
    /// and the game object where necessary.
    /// </summary>
    public unsafe DesignData FromActor(Actor actor, bool useModel)
    {
        var ret = new DesignData();
        // If the given actor is not a character, just return a default character.
        if (!actor.IsCharacter)
        {
            ret.SetDefaultEquipment(_items);
            return ret;
        }

        // Model ID is only unambiguously contained in the game object.
        // The draw object only has the object type.
        // TODO reverse search model data to get model id from model.
        if (actor.AsCharacter->CharacterData.ModelCharaId != 0)
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId, *(Customize*)&actor.AsCharacter->DrawData.CustomizeData,
                (byte*)&actor.AsCharacter->DrawData.Head);
            return ret;
        }

        var             model = actor.Model;
        CharacterWeapon main;
        CharacterWeapon off;

        // Hat visibility is only unambiguously contained in the game object.
        // Set it first to know where to get head slot data from.
        ret.SetHatVisible(!actor.AsCharacter->DrawData.IsHatHidden);

        // Use the draw object if it is a human.
        if (useModel && model.IsHuman)
        {
            // Customize can be obtained from the draw object.
            ret.Customize = model.GetCustomize();

            // We can not use the head slot data from the draw object if the hat is hidden.
            var head     = ret.IsHatVisible() ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
            var headItem = _items.Identify(EquipSlot.Head, head.Set, head.Variant);
            ret.SetItem(EquipSlot.Head, headItem);
            ret.SetStain(EquipSlot.Head, head.Stain);

            // The other slots can be used from the draw object.
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                var armor = model.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            // Weapons use the draw objects of the weapons, but require the game object either way.
            (_, _, main, off) = model.GetWeapons(actor);

            // Visor state is a flag on the game object, but we can see the actual state on the draw object.
            ret.SetVisor(_visor.GetVisorState(model));
        }
        else
        {
            // Obtain all data from the game object.
            ret.Customize = actor.GetCustomize();

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = actor.GetArmor(slot);
                var item  = _items.Identify(slot, armor.Set, armor.Variant);
                ret.SetItem(slot, item);
                ret.SetStain(slot, armor.Stain);
            }

            main = actor.GetMainhand();
            off  = actor.GetOffhand();
            ret.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);
        }

        // Set the weapons regardless of source.
        var mainItem = _items.Identify(EquipSlot.MainHand, main.Set, main.Type, (byte)main.Variant);
        var offItem  = _items.Identify(EquipSlot.OffHand,  off.Set,  off.Type,  (byte)off.Variant, mainItem.Type);
        ret.SetItem(EquipSlot.MainHand, mainItem);
        ret.SetStain(EquipSlot.MainHand, main.Stain);
        ret.SetItem(EquipSlot.OffHand, offItem);
        ret.SetStain(EquipSlot.OffHand, off.Stain);

        // Wetness can technically only be set in GPose or via external tools.
        // It is only available in the game object.
        ret.SetIsWet(actor.AsCharacter->IsGPoseWet);

        // Weapon visibility could technically be inferred from the weapon draw objects, 
        // but since we use hat visibility from the game object we can also use weapon visibility from it.
        ret.SetWeaponVisible(!actor.AsCharacter->DrawData.IsWeaponHidden);
        return ret;
    }

    #region Change Values

    /// <summary> Change a customization value. </summary>
    public void ChangeCustomize(ActorState state, CustomizeIndex idx, CustomizeValue value, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.Customize[idx];
        state.ModelData.Customize[idx] = value;
        state[idx]                     = source;

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeCustomize(objects, state.ModelData.Customize);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set {idx.ToDefaultName()} customizations in state {state.Identifier} from {old.Value} to {value.Value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Customize, source, state, objects, (old, value, idx));
    }

    /// <summary> Change an entire customization array according to flags. </summary>
    public void ChangeCustomize(ActorState state, in Customize customizeInput, CustomizeFlag apply, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.Customize;
        var (customize, applied) = _customizations.Combine(state.ModelData.Customize, customizeInput, apply);
        if (applied == 0)
            return;

        state.ModelData.Customize = customize;
        foreach (var type in Enum.GetValues<CustomizeIndex>())
        {
            var flag = type.ToFlag();
            if (applied.HasFlag(flag))
                state[type] = source;
        }

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeCustomize(objects, state.ModelData.Customize);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set {applied} customizations in state {state.Identifier} from {old} to {customize}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Customize, source, state, objects, (old, customize, applied));
    }

    /// <summary> Change a single piece of equipment without stain. </summary>
    /// <remarks> Do not use this in the same frame as ChangeStain, use <see cref="ChangeEquip(ActorState,EquipSlot,EquipItem,StainId,StateChanged.Source)"/> instead. </remarks>
    public void ChangeItem(ActorState state, EquipSlot slot, EquipItem item, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.Item(slot);
        state.ModelData.SetItem(slot, item);
        state[slot, false] = source;
        var type = slot is EquipSlot.MainHand or EquipSlot.OffHand ? StateChanged.Type.Weapon : StateChanged.Type.Equip;

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            if (type == StateChanged.Type.Equip)
                _editor.ChangeArmor(objects, slot, state.ModelData.Armor(slot));
            else
                _editor.ChangeWeapon(objects, slot, state.ModelData.Item(slot), state.ModelData.Stain(slot));

        // Meta.
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}). [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(type, source, state, objects, (old, item, slot));
    }

    /// <summary> Change a single piece of equipment including stain. </summary>
    public void ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StainId stain, StateChanged.Source source)
    {
        // Update state data.
        var old      = state.ModelData.Item(slot);
        var oldStain = state.ModelData.Stain(slot);
        state.ModelData.SetItem(slot, item);
        state.ModelData.SetStain(slot, stain);
        state[slot, false] = source;
        state[slot, true]  = source;
        var type = slot is EquipSlot.MainHand or EquipSlot.OffHand ? StateChanged.Type.Weapon : StateChanged.Type.Equip;

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            if (type == StateChanged.Type.Equip)
                _editor.ChangeArmor(objects, slot, state.ModelData.Armor(slot));
            else
                _editor.ChangeWeapon(objects, slot, state.ModelData.Item(slot), state.ModelData.Stain(slot));

        // Meta.
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}) and its stain from {oldStain.Value} to {stain.Value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(type,                    source, state, objects, (old, item, slot));
        _event.Invoke(StateChanged.Type.Stain, source, state, objects, (oldStain, stain, slot));
    }

    /// <summary> Change only the stain of an equipment piece. </summary>
    /// <remarks>
    /// Do not use this in the same frame as ChangeEquip, use <see cref="ChangeEquip(ActorState,EquipSlot,EquipItem,StainId,StateChanged.Source)"/> instead. </remarks>
    public void ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.Stain(slot);
        state.ModelData.SetStain(slot, stain);
        state[slot, true] = source;

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeStain(objects, slot, stain);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} stain in state {state.Identifier} from {old.Value} to {stain.Value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Stain, source, state, objects, (old, stain, slot));
    }

    /// <summary> Change hat visibility. </summary>
    public void ChangeHatState(ActorState state, bool value, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.IsHatVisible();
        state.ModelData.SetHatVisible(value);
        state[ActorState.MetaFlag.HatState] = source;

        // Update draw objects / game objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeHatState(objects, value);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set Head Gear Visibility in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.HatState));
    }

    /// <summary> Change weapon visibility. </summary>
    public void ChangeWeaponState(ActorState state, bool value, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.IsWeaponVisible();
        state.ModelData.SetWeaponVisible(value);
        state[ActorState.MetaFlag.WeaponState] = source;

        // Update draw objects / game objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeWeaponState(objects, value);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set Weapon Visibility in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.WeaponState));
    }

    /// <summary> Change visor state. </summary>
    public void ChangeVisorState(ActorState state, bool value, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.IsVisorToggled();
        state.ModelData.SetVisor(value);
        state[ActorState.MetaFlag.VisorState] = source;

        // Update draw objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        if (source is StateChanged.Source.Manual)
            _editor.ChangeVisor(objects, value);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set Visor State in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, objects, (old, value, ActorState.MetaFlag.VisorState));
    }

    /// <summary> Set GPose Wetness. </summary>
    public void ChangeWetness(ActorState state, bool value, StateChanged.Source source)
    {
        // Update state data.
        var old = state.ModelData.IsWet();
        state.ModelData.SetIsWet(value);
        state[ActorState.MetaFlag.Wetness] = source;

        // Update draw objects / game objects.
        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        _editor.ChangeWetness(objects, value);

        // Meta.
        Glamourer.Log.Verbose(
            $"Set Wetness in state {state.Identifier} from {old} to {value}. [Affecting {objects.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, state[ActorState.MetaFlag.Wetness], state, objects, (old, value, ActorState.MetaFlag.Wetness));
    }

    #endregion

    public void ApplyDesign(Design design, ActorState state)
    {
        void HandleEquip(EquipSlot slot, bool applyPiece, bool applyStain)
        {
            switch (applyPiece, applyStain)
            {
                case (false, false): break;
                case (true, false):
                    ChangeItem(state, slot, design.DesignData.Item(slot), StateChanged.Source.Manual);
                    break;
                case (false, true):
                    ChangeStain(state, slot, design.DesignData.Stain(slot), StateChanged.Source.Manual);
                    break;
                case (true, true):
                    ChangeEquip(state, slot, design.DesignData.Item(slot), design.DesignData.Stain(slot), StateChanged.Source.Manual);
                    break;
            }
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            HandleEquip(slot, design.DoApplyEquip(slot), design.DoApplyStain(slot));

        HandleEquip(EquipSlot.MainHand,
            design.DoApplyEquip(EquipSlot.MainHand)
         && design.DesignData.Item(EquipSlot.MainHand).Type == state.BaseData.Item(EquipSlot.MainHand).Type,
            design.DoApplyStain(EquipSlot.MainHand));
        HandleEquip(EquipSlot.OffHand,
            design.DoApplyEquip(EquipSlot.OffHand)
         && design.DesignData.Item(EquipSlot.OffHand).Type == state.BaseData.Item(EquipSlot.OffHand).Type,
            design.DoApplyStain(EquipSlot.OffHand));

        if (design.DoApplyHatVisible())
            ChangeHatState(state, design.DesignData.IsHatVisible(), StateChanged.Source.Manual);
        if (design.DoApplyWeaponVisible())
            ChangeWeaponState(state, design.DesignData.IsWeaponVisible(), StateChanged.Source.Manual);
        if (design.DoApplyVisorToggle())
            ChangeVisorState(state, design.DesignData.IsVisorToggled(), StateChanged.Source.Manual);
        if (design.DoApplyWetness())
            ChangeWetness(state, design.DesignData.IsWet(), StateChanged.Source.Manual);
        ChangeCustomize(state, design.DesignData.Customize, design.ApplyCustomize, StateChanged.Source.Manual);
    }

    public void ResetState(ActorState state)
    {
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            ChangeEquip(state, slot, state.BaseData.Item(slot), state.BaseData.Stain(slot), StateChanged.Source.Game);

        ChangeEquip(state, EquipSlot.MainHand, state.BaseData.Item(EquipSlot.MainHand), state.BaseData.Stain(EquipSlot.MainHand),
            StateChanged.Source.Game);
        ChangeEquip(state, EquipSlot.OffHand, state.BaseData.Item(EquipSlot.OffHand), state.BaseData.Stain(EquipSlot.OffHand),
            StateChanged.Source.Game);
        ChangeHatState(state, state.BaseData.IsHatVisible(), StateChanged.Source.Game);
        ChangeVisorState(state, state.BaseData.IsVisorToggled(), StateChanged.Source.Game);
        ChangeWeaponState(state, state.BaseData.IsWeaponVisible(), StateChanged.Source.Game);
        ChangeWetness(state, false, StateChanged.Source.Game);
        ChangeCustomize(state, state.BaseData.Customize, CustomizeFlagExtensions.All, StateChanged.Source.Game);

        _objects.Update();
        var objects = _objects.TryGetValue(state.Identifier, out var d) ? d : ActorData.Invalid;
        foreach (var actor in objects.Objects)
            ReapplyState(actor);
    }

    public void ReapplyState(Actor actor)
    {
        if (!GetOrCreate(actor, out var state))
            return;

        var mdl = actor.Model;
        if (!mdl.IsHuman)
            return;

        var data           = new ActorData(actor, string.Empty);
        var customizeFlags = Customize.Compare(mdl.GetCustomize(), state.ModelData.Customize);

        _editor.ChangeCustomize(data, state.ModelData.Customize);
        if (customizeFlags.RequiresRedraw())
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            _editor.ChangeArmor(data, slot, state.ModelData.Armor(slot));
        _editor.ChangeMainhand(data, state.ModelData.Item(EquipSlot.MainHand), state.ModelData.Stain(EquipSlot.MainHand));
        _editor.ChangeOffhand(data, state.ModelData.Item(EquipSlot.OffHand), state.ModelData.Stain(EquipSlot.OffHand));
        _editor.ChangeWetness(data, false);
        _editor.ChangeWeaponState(data, state.ModelData.IsWeaponVisible());
        _editor.ChangeHatState(data, state.ModelData.IsHatVisible());
        _editor.ChangeVisor(data, state.ModelData.IsVisorToggled());
    }

    public void DeleteState(ActorIdentifier identifier)
        => _states.Remove(identifier);
}
