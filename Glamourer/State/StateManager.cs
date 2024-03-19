using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Interop.Material;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.GameData.Interop;

namespace Glamourer.State;

public sealed class StateManager(
    ActorManager _actors,
    ItemManager items,
    StateChanged @event,
    StateApplier applier,
    InternalStateEditor editor,
    HumanModelList _humans,
    IClientState _clientState,
    Configuration config,
    JobChangeState jobChange,
    DesignMerger merger,
    ModSettingApplier modApplier,
    GPoseService gPose)
    : StateEditor(editor, applier, @event, jobChange, config, items, merger, modApplier, gPose),
        IReadOnlyDictionary<ActorIdentifier, ActorState>
{
    private readonly Dictionary<ActorIdentifier, ActorState> _states = [];

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
        => GetOrCreate(actor.GetIdentifier(_actors), actor, out state);

    /// <summary> Try to obtain or create a new state for an existing actor. Returns false if no state could be created. </summary>
    public unsafe bool GetOrCreate(ActorIdentifier identifier, Actor actor, [NotNullWhen(true)] out ActorState? state)
    {
        if (TryGetValue(identifier, out state))
            return true;

        if (!actor.Valid)
            return false;

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
            ret.SetDefaultEquipment(Items);
            return ret;
        }

        var model = actor.Model;

        // Model ID is only unambiguously contained in the game object.
        // The draw object only has the object type.
        // TODO reverse search model data to get model id from model.
        if (!_humans.IsHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId))
        {
            ret.LoadNonHuman((uint)actor.AsCharacter->CharacterData.ModelCharaId, *(CustomizeArray*)&actor.AsCharacter->DrawData.CustomizeData,
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
            var headItem = Items.Identify(EquipSlot.Head, head.Set, head.Variant);
            ret.SetItem(EquipSlot.Head, headItem);
            ret.SetStain(EquipSlot.Head, head.Stain);

            // The other slots can be used from the draw object.
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                var armor = model.GetArmor(slot);
                var item  = Items.Identify(slot, armor.Set, armor.Variant);
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
            ret.Customize = *actor.Customize;

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var armor = actor.GetArmor(slot);
                var item  = Items.Identify(slot, armor.Set, armor.Variant);
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
        var mainItem = Items.Identify(EquipSlot.MainHand, main.Skeleton, main.Weapon, main.Variant);
        var offItem  = Items.Identify(EquipSlot.OffHand,  off.Skeleton,  off.Weapon,  off.Variant, mainItem.Type);
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
        ret.Parameters = model.GetParameterData();

        return ret;
    }

    /// <summary> This is hardcoded in the game. </summary>
    private void FistWeaponHack(ref DesignData ret, ref CharacterWeapon mainhand, ref CharacterWeapon offhand)
    {
        if (mainhand.Skeleton.Id is < 1601 or >= 1651)
            return;

        var gauntlets = Items.Identify(EquipSlot.Hands, offhand.Skeleton, (Variant)offhand.Weapon.Id);
        offhand.Skeleton = (PrimaryId)(mainhand.Skeleton.Id + 50);
        offhand.Variant  = mainhand.Variant;
        offhand.Weapon   = mainhand.Weapon;
        ret.SetItem(EquipSlot.Hands, gauntlets);
        ret.SetStain(EquipSlot.Hands, mainhand.Stain);
    }

    /// <summary> Turn an actor human. </summary>
    public void TurnHuman(ActorState state, StateSource source, uint key = 0)
        => ChangeModelId(state, 0, CustomizeArray.Default, nint.Zero, source, key);

    public void ResetState(ActorState state, StateSource source, uint key = 0)
    {
        if (!state.Unlock(key))
            return;

        var redraw = state.ModelData.ModelId != state.BaseData.ModelId
         || !state.ModelData.IsHuman
         || CustomizeArray.Compare(state.ModelData.Customize, state.BaseData.Customize).RequiresRedraw();

        state.ModelData = state.BaseData;
        state.ModelData.SetIsWet(false);
        foreach (var index in Enum.GetValues<CustomizeIndex>())
            state.Sources[index] = StateSource.Game;

        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            state.Sources[slot, true]  = StateSource.Game;
            state.Sources[slot, false] = StateSource.Game;
        }

        foreach (var type in Enum.GetValues<MetaIndex>())
            state.Sources[type] = StateSource.Game;

        foreach (var slot in CrestExtensions.AllRelevantSet)
            state.Sources[slot] = StateSource.Game;

        foreach (var flag in CustomizeParameterExtensions.AllFlags)
            state.Sources[flag] = StateSource.Game;

        state.Materials.Clear();

        var actors = ActorData.Invalid;
        if (source is not StateSource.Game)
            actors = Applier.ApplyAll(state, redraw, true);

        Glamourer.Log.Verbose(
            $"Reset entire state of {state.Identifier.Incognito(null)} to game base. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Reset, source, state, actors, null);
    }

    public void ResetAdvancedState(ActorState state, StateSource source, uint key = 0)
    {
        if (!state.Unlock(key) || !state.ModelData.IsHuman)
            return;

        state.ModelData.Parameters = state.BaseData.Parameters;

        foreach (var flag in CustomizeParameterExtensions.AllFlags)
            state.Sources[flag] = StateSource.Game;

        var actors = ActorData.Invalid;
        if (source is not StateSource.Game)
        {
            actors = Applier.ChangeParameters(state, CustomizeParameterExtensions.All, true);
            foreach (var (idx, mat) in state.Materials.Values)
                Applier.ChangeMaterialValue(actors, MaterialValueIndex.FromKey(idx), mat.Game, true);
        }

        state.Materials.Clear();

        Glamourer.Log.Verbose(
            $"Reset advanced customization and dye state of {state.Identifier.Incognito(null)} to game base. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Reset, source, state, actors, null);
    }

    public void ResetCustomize(ActorState state, StateSource source, uint key = 0)
    {
        if (!state.Unlock(key) || !state.ModelData.IsHuman)
            return;

        foreach (var flag in CustomizationExtensions.All)
            state.Sources[flag] = StateSource.Game;

        state.ModelData = state.BaseData;
        var actors = ActorData.Invalid;
        if (source is not StateSource.Game)
            actors = Applier.ChangeCustomize(state, true);
        Glamourer.Log.Verbose(
            $"Reset customization state of {state.Identifier.Incognito(null)} to game base. [Affecting {actors.ToLazyString("nothing")}.]");
    }

    public void ResetEquip(ActorState state, StateSource source, uint key = 0)
    {
        if (!state.Unlock(key))
            return;

        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            state.Sources[slot, true]  = StateSource.Game;
            state.Sources[slot, false] = StateSource.Game;
            if (source is not StateSource.Game)
            {
                state.ModelData.SetItem(slot, state.BaseData.Item(slot));
                state.ModelData.SetStain(slot, state.BaseData.Stain(slot));
            }
        }

        var actors = ActorData.Invalid;
        if (source is not StateSource.Game)
        {
            actors = Applier.ChangeArmor(state, EquipSlotExtensions.EqdpSlots[0], true);
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Skip(1))
            {
                Applier.ChangeArmor(actors, slot, state.ModelData.Armor(slot), !state.Sources[slot, false].IsIpc(),
                    state.ModelData.IsHatVisible());
            }

            var mainhandActors = state.ModelData.MainhandType != state.BaseData.MainhandType ? actors.OnlyGPose() : actors;
            Applier.ChangeMainhand(mainhandActors, state.ModelData.Item(EquipSlot.MainHand), state.ModelData.Stain(EquipSlot.MainHand));
            var offhandActors = state.ModelData.OffhandType != state.BaseData.OffhandType ? actors.OnlyGPose() : actors;
            Applier.ChangeOffhand(offhandActors, state.ModelData.Item(EquipSlot.OffHand), state.ModelData.Stain(EquipSlot.OffHand));
        }

        Glamourer.Log.Verbose(
            $"Reset equipment state of {state.Identifier.Incognito(null)} to game base. [Affecting {actors.ToLazyString("nothing")}.]");
    }

    public void ResetStateFixed(ActorState state, bool respectManualPalettes, uint key = 0)
    {
        if (!state.Unlock(key))
            return;

        foreach (var index in Enum.GetValues<CustomizeIndex>().Where(i => state.Sources[i] is StateSource.Fixed))
        {
            state.Sources[index]             = StateSource.Game;
            state.ModelData.Customize[index] = state.BaseData.Customize[index];
        }

        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            if (state.Sources[slot, true] is StateSource.Fixed)
            {
                state.Sources[slot, true] = StateSource.Game;
                state.ModelData.SetStain(slot, state.BaseData.Stain(slot));
            }

            if (state.Sources[slot, false] is StateSource.Fixed)
            {
                state.Sources[slot, false] = StateSource.Game;
                state.ModelData.SetItem(slot, state.BaseData.Item(slot));
            }
        }

        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            if (state.Sources[slot] is StateSource.Fixed)
            {
                state.Sources[slot] = StateSource.Game;
                state.ModelData.SetCrest(slot, state.BaseData.Crest(slot));
            }
        }

        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            switch (state.Sources[flag])
            {
                case StateSource.Fixed:
                case StateSource.Manual when !respectManualPalettes:
                    state.Sources[flag]              = StateSource.Game;
                    state.ModelData.Parameters[flag] = state.BaseData.Parameters[flag];
                    break;
            }
        }

        foreach (var meta in MetaExtensions.AllRelevant.Where(f => state.Sources[f] is StateSource.Fixed))
        {
            state.Sources[meta] = StateSource.Game;
            state.ModelData.SetMeta(meta, state.BaseData.GetMeta(meta));
        }

        foreach (var (index, value) in state.Materials.Values.ToList())
        {
            switch (value.Source)
            {
                case StateSource.Fixed:
                case StateSource.Manual when !respectManualPalettes:
                    state.Materials.RemoveValue(index);
                    break;
            }
        }
    }

    public void ReapplyState(Actor actor, StateSource source)
    {
        if (!GetOrCreate(actor, out var state))
            return;

        ReapplyState(actor, state, source);
    }

    public void ReapplyState(Actor actor, ActorState state, StateSource source)
    {
        var data = Applier.ApplyAll(state,
            !actor.Model.IsHuman || CustomizeArray.Compare(actor.Model.GetCustomize(), state.ModelData.Customize).RequiresRedraw(), false);
        StateChanged.Invoke(StateChanged.Type.Reapply, source, state, data, null);
    }

    public void DeleteState(ActorIdentifier identifier)
        => _states.Remove(identifier);
}
