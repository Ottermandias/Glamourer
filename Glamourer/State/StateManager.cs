using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Plugin.Services;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.Structs;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateManager(ActorService _actors, ItemManager _items, StateChanged _event, StateApplier _applier, StateEditor _editor,
        HumanModelList _humans, ICondition _condition, IClientState _clientState)
    : IReadOnlyDictionary<ActorIdentifier, ActorState>
{
    private readonly Dictionary<ActorIdentifier, ActorState> _states = new();

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
                ModelData     = FromActor(actor, true,  false),
                BaseData      = FromActor(actor, false, false),
                LastJob       = (byte)(actor.IsCharacter ? actor.AsCharacter->CharacterData.ClassJob : 0),
                LastTerritory = _clientState.TerritoryType,
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
    public unsafe DesignData FromActor(Actor actor, bool useModel, bool ignoreHatState)
    {
        var ret = new DesignData();
        // If the given actor is not a character, just return a default character.
        if (!actor.IsCharacter)
        {
            ret.SetDefaultEquipment(_items);
            return ret;
        }

        var model = actor.Model;

        // Model ID is only unambiguously contained in the game object.
        // The draw object only has the object type.
        // TODO reverse search model data to get model id from model.
        if (!_humans.IsHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId))
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId, *(Customize*)&actor.AsCharacter->DrawData.CustomizeData,
                (nint)(&actor.AsCharacter->DrawData.Head));
            return ret;
        }

        ret.ModelId = (uint)actor.AsCharacter->CharacterData.ModelCharaId;
        ret.IsHuman = true;

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
            var head     = ret.IsHatVisible() || ignoreHatState ? model.GetArmor(EquipSlot.Head) : actor.GetArmor(EquipSlot.Head);
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
            ret.SetVisor(VisorService.GetVisorState(model));

            foreach (var slot in CrestExtensions.AllRelevantSet)
                ret.SetCrest(slot, CrestService.GetModelCrest(actor, slot));
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
            FistWeaponHack(ref ret, ref main, ref off);
            ret.SetVisor(actor.AsCharacter->DrawData.IsVisorToggled);

            foreach (var slot in CrestExtensions.AllRelevantSet)
                ret.SetCrest(slot, actor.GetCrest(slot));
        }

        // Set the weapons regardless of source.
        var mainItem = _items.Identify(EquipSlot.MainHand, main.Set, main.Type, main.Variant);
        var offItem  = _items.Identify(EquipSlot.OffHand,  off.Set,  off.Type,  off.Variant, mainItem.Type);
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

    /// <summary> This is hardcoded in the game. </summary>
    private void FistWeaponHack(ref DesignData ret, ref CharacterWeapon mainhand, ref CharacterWeapon offhand)
    {
        if (mainhand.Set.Id is < 1601 or >= 1651)
            return;

        var gauntlets = _items.Identify(EquipSlot.Hands, offhand.Set, (Variant)offhand.Type.Id);
        offhand.Set     = (SetId)(mainhand.Set.Id + 50);
        offhand.Variant = mainhand.Variant;
        offhand.Type    = mainhand.Type;
        ret.SetItem(EquipSlot.Hands, gauntlets);
        ret.SetStain(EquipSlot.Hands, mainhand.Stain);
    }

    #region Change Values

    /// <summary> Turn an actor human. </summary>
    public void TurnHuman(ActorState state, StateChanged.Source source, uint key = 0)
        => ChangeModelId(state, 0, Customize.Default, nint.Zero, source, key);

    /// <summary> Turn an actor to. </summary>
    public void ChangeModelId(ActorState state, uint modelId, Customize customize, nint equipData, StateChanged.Source source,
        uint key = 0)
    {
        if (!_editor.ChangeModelId(state, modelId, customize, equipData, source, out var old, key))
            return;

        var actors = _applier.ForceRedraw(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set model id in state {state.Identifier.Incognito(null)} from {old} to {modelId}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Model, source, state, actors, (old, modelId));
    }

    /// <summary> Change a customization value. </summary>
    public void ChangeCustomize(ActorState state, CustomizeIndex idx, CustomizeValue value, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeCustomize(state, idx, value, source, out var old, key))
            return;

        var actors = _applier.ChangeCustomize(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set {idx.ToDefaultName()} customizations in state {state.Identifier.Incognito(null)} from {old.Value} to {value.Value}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Customize, source, state, actors, (old, value, idx));
    }

    /// <summary> Change an entire customization array according to flags. </summary>
    public void ChangeCustomize(ActorState state, in Customize customizeInput, CustomizeFlag apply, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeHumanCustomize(state, customizeInput, apply, source, out var old, out var applied, key))
            return;

        var actors = _applier.ChangeCustomize(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set {applied} customizations in state {state.Identifier.Incognito(null)} from {old} to {customizeInput}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.EntireCustomize, source, state, actors, (old, applied));
    }

    /// <summary> Change a single piece of equipment without stain. </summary>
    /// <remarks> Do not use this in the same frame as ChangeStain, use <see cref="ChangeEquip(ActorState,EquipSlot,EquipItem,StainId,StateChanged.Source,uint)"/> instead. </remarks>
    public void ChangeItem(ActorState state, EquipSlot slot, EquipItem item, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeItem(state, slot, item, source, out var old, key))
            return;

        var type = slot.ToIndex() < 10 ? StateChanged.Type.Equip : StateChanged.Type.Weapon;
        var actors = type is StateChanged.Type.Equip
            ? _applier.ChangeArmor(state, slot, source is StateChanged.Source.Manual or StateChanged.Source.Ipc)
            : _applier.ChangeWeapon(state, slot, source is StateChanged.Source.Manual or StateChanged.Source.Ipc,
                item.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}). [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(type, source, state, actors, (old, item, slot));
    }

    /// <summary> Change a single piece of equipment including stain. </summary>
    public void ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StainId stain, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeEquip(state, slot, item, stain, source, out var old, out var oldStain, key))
            return;

        var type = slot.ToIndex() < 10 ? StateChanged.Type.Equip : StateChanged.Type.Weapon;
        var actors = type is StateChanged.Type.Equip
            ? _applier.ChangeArmor(state, slot, source is StateChanged.Source.Manual or StateChanged.Source.Ipc)
            : _applier.ChangeWeapon(state, slot, source is StateChanged.Source.Manual or StateChanged.Source.Ipc,
                item.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}) and its stain from {oldStain.Id} to {stain.Id}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(type,                    source, state, actors, (old, item, slot));
        _event.Invoke(StateChanged.Type.Stain, source, state, actors, (oldStain, stain, slot));
    }

    /// <summary> Change only the stain of an equipment piece. </summary>
    /// <remarks> Do not use this in the same frame as ChangeEquip, use <see cref="ChangeEquip(ActorState,EquipSlot,EquipItem,StainId,StateChanged.Source,uint)"/> instead. </remarks>
    public void ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeStain(state, slot, stain, source, out var old, key))
            return;

        var actors = _applier.ChangeStain(state, slot, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} stain in state {state.Identifier.Incognito(null)} from {old.Id} to {stain.Id}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Stain, source, state, actors, (old, stain, slot));
    }

    /// <summary> Change the crest of an equipment piece. </summary>
    public void ChangeCrest(ActorState state, CrestFlag slot, bool crest, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeCrest(state, slot, crest, source, out var old, key))
            return;

        var actors = _applier.ChangeCrests(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set {slot.ToLabel()} crest in state {state.Identifier.Incognito(null)} from {old} to {crest}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Crest, source, state, actors, (old, crest, slot));
    }

    /// <summary> Change hat visibility. </summary>
    public void ChangeHatState(ActorState state, bool value, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeMetaState(state, ActorState.MetaIndex.HatState, value, source, out var old, key))
            return;

        var actors = _applier.ChangeHatState(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set Head Gear Visibility in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, actors, (old, value, ActorState.MetaIndex.HatState));
    }

    /// <summary> Change weapon visibility. </summary>
    public void ChangeWeaponState(ActorState state, bool value, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeMetaState(state, ActorState.MetaIndex.WeaponState, value, source, out var old, key))
            return;

        var actors = _applier.ChangeWeaponState(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set Weapon Visibility in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, actors, (old, value, ActorState.MetaIndex.WeaponState));
    }

    /// <summary> Change visor state. </summary>
    public void ChangeVisorState(ActorState state, bool value, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeMetaState(state, ActorState.MetaIndex.VisorState, value, source, out var old, key))
            return;

        var actors = _applier.ChangeVisor(state, source is StateChanged.Source.Manual or StateChanged.Source.Ipc);
        Glamourer.Log.Verbose(
            $"Set Visor State in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, source, state, actors, (old, value, ActorState.MetaIndex.VisorState));
    }

    /// <summary> Set GPose Wetness. </summary>
    public void ChangeWetness(ActorState state, bool value, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeMetaState(state, ActorState.MetaIndex.Wetness, value, source, out var old, key))
            return;

        var actors = _applier.ChangeWetness(state, true);
        Glamourer.Log.Verbose(
            $"Set Wetness in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Other, state[ActorState.MetaIndex.Wetness], state, actors, (old, value, ActorState.MetaIndex.Wetness));
    }

    #endregion

    public void ApplyDesign(DesignBase design, ActorState state, StateChanged.Source source, uint key = 0)
    {
        if (!_editor.ChangeModelId(state, design.DesignData.ModelId, design.DesignData.Customize, design.GetDesignDataRef().GetEquipmentPtr(),
                source,
                out var oldModelId, key))
            return;

        var redraw = oldModelId != design.DesignData.ModelId || !design.DesignData.IsHuman;
        if (design.DoApplyWetness())
            _editor.ChangeMetaState(state, ActorState.MetaIndex.Wetness, design.DesignData.IsWet(), source, out _, key);

        if (state.ModelData.IsHuman)
        {
            if (design.DoApplyHatVisible())
                _editor.ChangeMetaState(state, ActorState.MetaIndex.HatState, design.DesignData.IsHatVisible(), source, out _, key);
            if (design.DoApplyWeaponVisible())
                _editor.ChangeMetaState(state, ActorState.MetaIndex.WeaponState, design.DesignData.IsWeaponVisible(), source, out _, key);
            if (design.DoApplyVisorToggle())
                _editor.ChangeMetaState(state, ActorState.MetaIndex.VisorState, design.DesignData.IsVisorToggled(), source, out _, key);

            var flags = state.AllowsRedraw(_condition)
                ? design.ApplyCustomize
                : design.ApplyCustomize & ~CustomizeFlagExtensions.RedrawRequired;
            _editor.ChangeHumanCustomize(state, design.DesignData.Customize, flags, source, out _, out var applied, key);
            redraw |= applied.RequiresRedraw();

            foreach (var slot in EquipSlotExtensions.FullSlots)
                HandleEquip(slot, design.DoApplyEquip(slot), design.DoApplyStain(slot));

            foreach (var slot in CrestExtensions.AllRelevantSet.Where(design.DoApplyCrest))
                _editor.ChangeCrest(state, slot, design.DesignData.Crest(slot), source, out _, key);
        }

        var actors = ApplyAll(state, redraw, false);
        Glamourer.Log.Verbose(
            $"Applied design to {state.Identifier.Incognito(null)}. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Design, state[ActorState.MetaIndex.Wetness], state, actors, design);
        return;

        void HandleEquip(EquipSlot slot, bool applyPiece, bool applyStain)
        {
            var unused = (applyPiece, applyStain) switch
            {
                (false, false) => false,
                (true, false)  => _editor.ChangeItem(state, slot, design.DesignData.Item(slot), source, out _, key),
                (false, true)  => _editor.ChangeStain(state, slot, design.DesignData.Stain(slot), source, out _, key),
                (true, true) => _editor.ChangeEquip(state, slot, design.DesignData.Item(slot), design.DesignData.Stain(slot), source, out _,
                    out _, key),
            };
        }
    }

    private ActorData ApplyAll(ActorState state, bool redraw, bool withLock)
    {
        var actors = _applier.ChangeWetness(state, true);
        if (redraw)
        {
            if (withLock)
                state.TempLock();
            _applier.ForceRedraw(actors);
        }
        else
        {
            _applier.ChangeCustomize(actors, state.ModelData.Customize);
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                _applier.ChangeArmor(actors, slot, state.ModelData.Armor(slot), state[slot, false] is not StateChanged.Source.Ipc,
                    state.ModelData.IsHatVisible());
            }

            var mainhandActors = state.ModelData.MainhandType != state.BaseData.MainhandType ? actors.OnlyGPose() : actors;
            _applier.ChangeMainhand(mainhandActors, state.ModelData.Item(EquipSlot.MainHand), state.ModelData.Stain(EquipSlot.MainHand));
            var offhandActors = state.ModelData.OffhandType != state.BaseData.OffhandType ? actors.OnlyGPose() : actors;
            _applier.ChangeOffhand(offhandActors, state.ModelData.Item(EquipSlot.OffHand), state.ModelData.Stain(EquipSlot.OffHand));
        }

        if (state.ModelData.IsHuman)
        {
            _applier.ChangeHatState(actors, state.ModelData.IsHatVisible());
            _applier.ChangeWeaponState(actors, state.ModelData.IsWeaponVisible());
            _applier.ChangeVisor(actors, state.ModelData.IsVisorToggled());
            _applier.ChangeCrests(actors, state.ModelData.CrestVisibility);
        }

        return actors;
    }

    public void ResetState(ActorState state, StateChanged.Source source, uint key = 0)
    {
        if (!state.Unlock(key))
            return;

        var redraw = state.ModelData.ModelId != state.BaseData.ModelId
         || !state.ModelData.IsHuman
         || Customize.Compare(state.ModelData.Customize, state.BaseData.Customize).RequiresRedraw();
        state.ModelData = state.BaseData;
        state.ModelData.SetIsWet(false);
        foreach (var index in Enum.GetValues<CustomizeIndex>())
            state[index] = StateChanged.Source.Game;

        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            state[slot, true]  = StateChanged.Source.Game;
            state[slot, false] = StateChanged.Source.Game;
        }
        
        foreach (var type in Enum.GetValues<ActorState.MetaIndex>())
            state[type] = StateChanged.Source.Game;

        foreach (var slot in CrestExtensions.AllRelevantSet)
            state[slot] = StateChanged.Source.Game;

        var actors = ActorData.Invalid;
        if (source is StateChanged.Source.Manual or StateChanged.Source.Ipc)
            actors = ApplyAll(state, redraw, true);
        Glamourer.Log.Verbose(
            $"Reset entire state of {state.Identifier.Incognito(null)} to game base. [Affecting {actors.ToLazyString("nothing")}.]");
        _event.Invoke(StateChanged.Type.Reset, StateChanged.Source.Manual, state, actors, null);
    }

    public void ResetStateFixed(ActorState state, uint key = 0)
    {
        if (!state.Unlock(key))
            return;

        foreach (var index in Enum.GetValues<CustomizeIndex>().Where(i => state[i] is StateChanged.Source.Fixed))
        {
            state[index]                     = StateChanged.Source.Game;
            state.ModelData.Customize[index] = state.BaseData.Customize[index];
        }

        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            if (state[slot, true] is StateChanged.Source.Fixed)
            {
                state[slot, true] = StateChanged.Source.Game;
                state.ModelData.SetStain(slot, state.BaseData.Stain(slot));
            }

            if (state[slot, false] is StateChanged.Source.Fixed)
            {
                state[slot, false] = StateChanged.Source.Game;
                state.ModelData.SetItem(slot, state.BaseData.Item(slot));
            }
        }

        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            if (state[slot] is StateChanged.Source.Fixed)
            {
                state[slot] = StateChanged.Source.Game;
                state.ModelData.SetCrest(slot, state.BaseData.Crest(slot));
            }
        }

        if (state[ActorState.MetaIndex.HatState] is StateChanged.Source.Fixed)
        {
            state[ActorState.MetaIndex.HatState] = StateChanged.Source.Game;
            state.ModelData.SetHatVisible(state.BaseData.IsHatVisible());
        }

        if (state[ActorState.MetaIndex.VisorState] is StateChanged.Source.Fixed)
        {
            state[ActorState.MetaIndex.VisorState] = StateChanged.Source.Game;
            state.ModelData.SetVisor(state.BaseData.IsVisorToggled());
        }

        if (state[ActorState.MetaIndex.WeaponState] is StateChanged.Source.Fixed)
        {
            state[ActorState.MetaIndex.WeaponState] = StateChanged.Source.Game;
            state.ModelData.SetWeaponVisible(state.BaseData.IsWeaponVisible());
        }

        if (state[ActorState.MetaIndex.Wetness] is StateChanged.Source.Fixed)
        {
            state[ActorState.MetaIndex.Wetness] = StateChanged.Source.Game;
            state.ModelData.SetIsWet(state.BaseData.IsWet());
        }
    }

    public void ReapplyState(Actor actor)
    {
        if (!GetOrCreate(actor, out var state))
            return;

        ApplyAll(state, !actor.Model.IsHuman || Customize.Compare(actor.Model.GetCustomize(), state.ModelData.Customize).RequiresRedraw(),
            false);
    }

    public void DeleteState(ActorIdentifier identifier)
        => _states.Remove(identifier);
}
