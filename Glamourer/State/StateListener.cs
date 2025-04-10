﻿using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.GameData;
using Penumbra.GameData.DataContainers;
using Glamourer.Designs;
using Penumbra.GameData.Interop;
using Glamourer.Api.Enums;

namespace Glamourer.State;

/// <summary>
/// This class handles all game events that could cause a drawn model to change,
/// it always updates the base state for existing states,
/// and either discards the changes or updates the model state too.
/// </summary>
public class StateListener : IDisposable
{
    private readonly Configuration             _config;
    private readonly ActorManager              _actors;
    private readonly ActorObjectManager        _objects;
    private readonly StateManager              _manager;
    private readonly StateApplier              _applier;
    private readonly ItemManager               _items;
    private readonly CustomizeService          _customizations;
    private readonly PenumbraService           _penumbra;
    private readonly EquipSlotUpdating         _equipSlotUpdating;
    private readonly BonusSlotUpdating         _bonusSlotUpdating;
    private readonly GearsetDataLoaded         _gearsetDataLoaded;
    private readonly WeaponLoading             _weaponLoading;
    private readonly HeadGearVisibilityChanged _headGearVisibility;
    private readonly VisorStateChanged         _visorState;
    private readonly WeaponVisibilityChanged   _weaponVisibility;
    private readonly StateFinalized            _stateFinalized;
    private readonly AutoDesignApplier         _autoDesignApplier;
    private readonly FunModule                 _funModule;
    private readonly HumanModelList            _humans;
    private readonly MovedEquipment            _movedEquipment;
    private readonly GPoseService              _gPose;
    private readonly ChangeCustomizeService    _changeCustomizeService;
    private readonly CrestService              _crestService;
    private readonly ICondition                _condition;

    private readonly Dictionary<Actor, CharacterWeapon> _fistOffhands = [];

    private ActorIdentifier _creatingIdentifier = ActorIdentifier.Invalid;
    private bool            _isPlayerNpc;
    private ActorState?     _creatingState;
    private ActorState?     _customizeState;

    public StateListener(StateManager manager, ItemManager items, PenumbraService penumbra, ActorManager actors, Configuration config,
        EquipSlotUpdating equipSlotUpdating, GearsetDataLoaded gearsetDataLoaded, WeaponLoading weaponLoading, VisorStateChanged visorState,
        WeaponVisibilityChanged weaponVisibility, HeadGearVisibilityChanged headGearVisibility, AutoDesignApplier autoDesignApplier,
        FunModule funModule, HumanModelList humans, StateApplier applier, MovedEquipment movedEquipment, ActorObjectManager objects,
        GPoseService gPose, ChangeCustomizeService changeCustomizeService, CustomizeService customizations, ICondition condition,
        CrestService crestService, BonusSlotUpdating bonusSlotUpdating, StateFinalized stateFinalized)
    {
        _manager                = manager;
        _items                  = items;
        _penumbra               = penumbra;
        _actors                 = actors;
        _config                 = config;
        _equipSlotUpdating      = equipSlotUpdating;
        _gearsetDataLoaded      = gearsetDataLoaded;
        _weaponLoading          = weaponLoading;
        _visorState             = visorState;
        _weaponVisibility       = weaponVisibility;
        _headGearVisibility     = headGearVisibility;
        _autoDesignApplier      = autoDesignApplier;
        _funModule              = funModule;
        _humans                 = humans;
        _applier                = applier;
        _movedEquipment         = movedEquipment;
        _objects                = objects;
        _gPose                  = gPose;
        _changeCustomizeService = changeCustomizeService;
        _customizations         = customizations;
        _condition              = condition;
        _crestService           = crestService;
        _bonusSlotUpdating      = bonusSlotUpdating;
        _stateFinalized         = stateFinalized;
        Subscribe();
    }

    void IDisposable.Dispose()
        => Unsubscribe();

    /// <summary> The result of updating the base state of an ActorState. </summary>
    private enum UpdateState
    {
        /// <summary> The base state is the same as prior state. </summary>
        NoChange,

        /// <summary> The game requests an update to a state that does not agree with the actor state. </summary>
        Transformed,

        /// <summary> The base state changed compared to prior state. </summary>
        Change,

        /// <summary> Special case for hat stuff. </summary>
        HatHack,
    }

    /// <summary>
    /// Invoked when a new draw object is created from a game object.
    /// We need to update all state: Model ID, Customize and Equipment.
    /// Weapons and meta flags are updated independently.
    /// We also need to apply fixed designs here.
    /// </summary>
    private unsafe void OnCreatingCharacterBase(nint actorPtr, Guid _, nint modelPtr, nint customizePtr, nint equipDataPtr)
    {
        var actor = (Actor)actorPtr;
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        _creatingIdentifier = actor.GetIdentifier(_actors);
        ref var modelId   = ref *(uint*)modelPtr;
        ref var customize = ref *(CustomizeArray*)customizePtr;
        if (_autoDesignApplier.Reduce(actor, _creatingIdentifier, out _creatingState))
        {
            _isPlayerNpc = _creatingIdentifier.Type is IdentifierType.Player
             && actor.IsCharacter
             && actor.AsCharacter->GetObjectKind() is ObjectKind.EventNpc;
            switch (UpdateBaseData(actor, _creatingState, modelId, customizePtr, equipDataPtr))
            {
                // TODO handle right
                case UpdateState.Change:      break;
                case UpdateState.Transformed: break;
                case UpdateState.NoChange:

                    modelId = _creatingState.ModelData.ModelId;
                    UpdateCustomize(actor, _creatingState, ref customize, true);
                    foreach (var slot in EquipSlotExtensions.EqdpSlots)
                        HandleEquipSlot(actor, _creatingState, slot, ref ((CharacterArmor*)equipDataPtr)[slot.ToIndex()]);

                    break;
            }

            _creatingState.TempUnlock();
        }

        _funModule.ApplyFunOnLoad(actor, new Span<CharacterArmor>((void*)equipDataPtr, 10), ref customize);
        if (modelId == 0 && _creatingState is not { IsLocked: true })
            ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
    }

    private void OnCustomizeChange(Model model, ref CustomizeArray customize)
    {
        if (!model.IsHuman)
            return;

        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors, out var identifier)
         || !_manager.TryGetValue(identifier, out _customizeState))
            return;

        UpdateCustomize(actor, _customizeState, ref customize, false);
    }

    private void UpdateCustomize(Actor actor, ActorState state, ref CustomizeArray customize, bool checkTransform)
    {
        switch (UpdateBaseData(actor, state, customize, checkTransform))
        {
            case UpdateState.Transformed: break;
            case UpdateState.Change:
                var model = state.ModelData.Customize;
                if (customize.Gender != model.Gender || customize.Clan != model.Clan)
                {
                    _manager.ChangeEntireCustomize(state, in customize, CustomizeFlagExtensions.All, ApplySettings.Game);
                    return;
                }

                var set = _customizations.Manager.GetSet(model.Clan, model.Gender);
                foreach (var index in CustomizationExtensions.AllBasic)
                {
                    if (!state.Sources[index].IsFixed())
                    {
                        var newValue = customize[index];
                        var oldValue = model[index];
                        if (newValue != oldValue)
                        {
                            if (set.Validate(index, newValue, out _, model.Face))
                                _manager.ChangeCustomize(state, index, newValue, ApplySettings.Game);
                            else
                                customize[index] = oldValue;
                        }
                    }
                    else
                    {
                        customize[index] = model[index];
                    }
                }

                break;
            case UpdateState.NoChange: customize = state.ModelData.Customize; break;
        }
    }

    /// <summary>
    /// A draw model loads a new equipment piece.
    /// Update base data, apply or update model data, and protect against restricted gear.
    /// </summary>
    private void OnEquipSlotUpdating(Model model, EquipSlot slot, ref CharacterArmor armor, ref ulong returnValue)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // If the model comes from IPC it is probably from Mare,
        // then we do not want to use our restricted gear protection
        // since we assume the player has that gear modded to availability.
        var locked = false;
        if (actor.Identifier(_actors, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
        {
            HandleEquipSlot(actor, state, slot, ref armor);
            locked = state.Sources[slot, false] is StateSource.IpcFixed;
        }

        _funModule.ApplyFunToSlot(actor, ref armor, slot);
        if (!_config.UseRestrictedGearProtection || locked)
            return;

        var customize = model.GetCustomize();
        (_, armor) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
    }

    private void OnBonusSlotUpdating(Model model, BonusItemFlag slot, ref CharacterArmor item, ref ulong returnValue)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (actor.Identifier(_actors, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
            switch (UpdateBaseData(actor, state, slot, item))
            {
                // Base data changed equipment while actors were not there.
                // Update model state if not on fixed design.
                case UpdateState.Change:
                    var apply = false;
                    if (!state.Sources[slot].IsFixed())
                        _manager.ChangeBonusItem(state, slot, state.BaseData.BonusItem(slot), ApplySettings.Game);
                    else
                        apply = true;
                    if (apply)
                        item = state.ModelData.BonusItem(slot).Armor();
                    break;
                // Use current model data.
                case UpdateState.NoChange:    item = state.ModelData.BonusItem(slot).Armor(); break;
                case UpdateState.Transformed: break;
            }
    }

    private void OnGearsetDataLoaded(Actor actor, Model model)
    {
        if (!actor.Valid || (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart))
            return;

        // ensure actor and state are valid.
        if (!actor.Identifier(_actors, out var identifier))
            return;

        if (_objects.TryGetValue(identifier, out var actors) && actors.Valid)
            _stateFinalized.Invoke(StateFinalizationType.Gearset, actors);
    }


    private void OnMovedEquipment((EquipSlot, uint, StainIds)[] items)
    {
        var (identifier, objects) = _objects.PlayerData;
        if (!identifier.IsValid || !_manager.TryGetValue(identifier, out var state))
            return;

        foreach (var (slot, item, stain) in items)
        {
            var currentItem = state.BaseData.Item(slot);
            var model = slot is EquipSlot.MainHand or EquipSlot.OffHand
                ? state.ModelData.Weapon(slot)
                : state.ModelData.Armor(slot).ToWeapon(0);
            var current = currentItem.Weapon(state.BaseData.Stain(slot));
            if (model.Value == current.Value || !_items.ItemData.TryGetValue(item, EquipSlot.MainHand, out var changedItem))
                continue;

            var changed = changedItem.Weapon(stain);
            var itemChanged = current.Skeleton == changed.Skeleton
             && current.Variant == changed.Variant
             && current.Weapon == changed.Weapon
             && !state.Sources[slot, false].IsFixed();

            var stainChanged = current.Stains == changed.Stains && !state.Sources[slot, true].IsFixed();

            switch (itemChanged, stainChanged)
            {
                case (true, true):
                    _manager.ChangeEquip(state, slot, currentItem, current.Stains, ApplySettings.Game);
                    if (slot is EquipSlot.MainHand or EquipSlot.OffHand)
                        _applier.ChangeWeapon(objects, slot, currentItem, current.Stains);
                    else
                        _applier.ChangeArmor(objects, slot, current.ToArmor(), !state.Sources[slot, false].IsFixed(),
                            state.ModelData.IsHatVisible());
                    break;
                case (true, false):
                    _manager.ChangeItem(state, slot, currentItem, ApplySettings.Game);
                    if (slot is EquipSlot.MainHand or EquipSlot.OffHand)
                        _applier.ChangeWeapon(objects, slot, currentItem, model.Stains);
                    else
                        _applier.ChangeArmor(objects, slot, current.ToArmor(model.Stains), !state.Sources[slot, false].IsFixed(),
                            state.ModelData.IsHatVisible());
                    break;
                case (false, true):
                    _manager.ChangeStains(state, slot, current.Stains, ApplySettings.Game);
                    _applier.ChangeStain(objects, slot, current.Stains);
                    break;
            }
        }
    }

    /// <summary>
    /// A game object loads a new weapon.
    /// Update base data, apply or update model data.
    /// Verify consistent weapon types.
    /// </summary>
    private void OnWeaponLoading(Actor actor, EquipSlot slot, ref CharacterWeapon weapon)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Fist weapon gauntlet hack.
        if (slot is EquipSlot.OffHand
         && weapon.Variant == 0
         && weapon.Weapon.Id != 0
         && _fistOffhands.TryGetValue(actor, out var lastFistOffhand))
        {
            Glamourer.Log.Verbose($"Applying stored fist weapon offhand {lastFistOffhand} for 0x{actor.Address:X}.");
            weapon = lastFistOffhand;
        }

        if (!actor.Identifier(_actors, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        var apply = false;
        switch (UpdateBaseData(actor, state, slot, weapon))
        {
            // Do nothing. But this usually can not happen because the hooked function also writes to game objects later.
            case UpdateState.Transformed: break;
            case UpdateState.Change:
                if (!state.Sources[slot, false].IsFixed())
                    _manager.ChangeItem(state, slot, state.BaseData.Item(slot), ApplySettings.Game);
                else
                    apply = true;

                if (!state.Sources[slot, true].IsFixed())
                    _manager.ChangeStains(state, slot, state.BaseData.Stain(slot), ApplySettings.Game);
                else
                    apply = true;
                break;
            case UpdateState.NoChange: apply = true; break;
        }

        var baseType  = slot is EquipSlot.OffHand ? state.BaseData.MainhandType.Offhand() : state.BaseData.MainhandType;
        var modelType = state.ModelData.Item(slot).Type;
        if (apply)
        {
            // Only allow overwriting identical weapons
            var canApply = baseType == modelType
             || _gPose.InGPose && actor.IsGPoseOrCutscene;
            var newWeapon = state.ModelData.Weapon(slot);
            if (canApply)
            {
                weapon = newWeapon;
            }
            else
            {
                if (weapon.Skeleton.Id != 0)
                    weapon = weapon.With(newWeapon.Stains);
                // Force unlock if necessary.
                _manager.ChangeItem(state, slot, state.BaseData.Item(slot), ApplySettings.Game with { Key = state.Combination });
            }
        }

        // Fist Weapon Offhand hack.
        if (slot is EquipSlot.MainHand && weapon.Skeleton.Id is > 1600 and < 1651)
        {
            lastFistOffhand = new CharacterWeapon((PrimaryId)(weapon.Skeleton.Id + 50), weapon.Weapon, weapon.Variant,
                weapon.Stains);
            _fistOffhands[actor] = lastFistOffhand;
            Glamourer.Log.Excessive($"Storing fist weapon offhand {lastFistOffhand} for 0x{actor.Address:X}.");
        }

        _funModule.ApplyFunToWeapon(actor, ref weapon, slot);
    }

    /// <summary> Update base data for a single changed equipment slot. </summary>
    private UpdateState UpdateBaseData(Actor actor, ActorState state, EquipSlot slot, CharacterArmor armor)
    {
        var actorArmor = actor.GetArmor(slot);
        var fistWeapon = FistWeaponGauntletHack();

        // The actor armor does not correspond to the model armor, thus the actor is transformed.
        if (actorArmor.Value != armor.Value)
        {
            // Update base data in case hat visibility is off.
            if (slot is EquipSlot.Head && armor.Value == 0)
            {
                if (actor.IsTransformed)
                    return UpdateState.Transformed;

                if (actorArmor.Value != state.BaseData.Armor(EquipSlot.Head).Value)
                {
                    var item = _items.Identify(slot, actorArmor.Set, actorArmor.Variant);
                    state.BaseData.SetItem(EquipSlot.Head, item);
                    state.BaseData.SetStain(EquipSlot.Head, actorArmor.Stains);
                    return UpdateState.Change;
                }

                return UpdateState.HatHack;
            }

            if (!fistWeapon)
                return UpdateState.Transformed;
        }

        var baseData = state.BaseData.Armor(slot);

        var change = UpdateState.NoChange;
        if (baseData.Stains != armor.Stains)
        {
            if (_isPlayerNpc)
                return UpdateState.Transformed;

            state.BaseData.SetStain(slot, armor.Stains);
            change = UpdateState.Change;
        }

        if (baseData.Set.Id != armor.Set.Id || baseData.Variant != armor.Variant && !fistWeapon)
        {
            if (_isPlayerNpc)
                return UpdateState.Transformed;

            var item = _items.Identify(slot, armor.Set, armor.Variant);
            state.BaseData.SetItem(slot, item);
            change = UpdateState.Change;
        }

        return change;

        bool FistWeaponGauntletHack()
        {
            if (slot is not EquipSlot.Hands)
                return false;

            var offhand = actor.GetOffhand();
            return offhand.Variant == 0 && offhand.Weapon.Id != 0 && armor.Set.Id == offhand.Skeleton.Id;
        }
    }

    private UpdateState UpdateBaseData(Actor actor, ActorState state, BonusItemFlag slot, CharacterArmor item)
    {
        var actorItemId = actor.GetBonusItem(slot);
        if (!_items.IsBonusItemValid(slot, actorItemId, out var actorItem))
            return UpdateState.NoChange;

        // The actor item does not correspond to the model item, thus the actor is transformed.
        if (actorItem.PrimaryId != item.Set || actorItem.Variant != item.Variant)
            return UpdateState.Transformed;

        var baseData = state.BaseData.BonusItem(slot);
        var change   = UpdateState.NoChange;
        if (baseData.Id != actorItem.Id || baseData.PrimaryId != item.Set || baseData.Variant != item.Variant)
        {
            if (_isPlayerNpc)
                return UpdateState.Transformed;

            var identified = _items.Identify(slot, item.Set, item.Variant);
            state.BaseData.SetBonusItem(slot, identified);
            change = UpdateState.Change;
        }

        return change;
    }

    /// <summary> Handle a full equip slot update for base data and model data. </summary>
    private void HandleEquipSlot(Actor actor, ActorState state, EquipSlot slot, ref CharacterArmor armor)
    {
        switch (UpdateBaseData(actor, state, slot, armor))
        {
            // Base data changed equipment while actors were not there.
            // Update model state if not on fixed design.
            case UpdateState.Change:
                var apply = false;
                if (!state.Sources[slot, false].IsFixed())
                    _manager.ChangeItem(state, slot, state.BaseData.Item(slot), ApplySettings.Game);
                else
                    apply = true;

                if (!state.Sources[slot, true].IsFixed())
                    _manager.ChangeStains(state, slot, state.BaseData.Stain(slot), ApplySettings.Game);
                else
                    apply = true;

                if (apply)
                    armor = state.ModelData.ArmorWithState(slot);
                break;
            // Use current model data.
            case UpdateState.NoChange:
            case UpdateState.HatHack:
                armor = state.ModelData.ArmorWithState(slot);
                break;
            case UpdateState.Transformed: break;
        }
    }

    private void OnCrestChange(Actor actor, CrestFlag slot, ref bool value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        switch (UpdateBaseCrest(actor, state, slot, value))
        {
            case UpdateState.Change:
                if (!state.Sources[slot].IsFixed())
                    _manager.ChangeCrest(state, slot, state.BaseData.Crest(slot), ApplySettings.Game);
                else
                    value = state.ModelData.Crest(slot);
                break;
            case UpdateState.NoChange:
            case UpdateState.HatHack:
                value = state.ModelData.Crest(slot);
                break;
            case UpdateState.Transformed: break;
        }
    }

    private void OnModelCrestSetup(Model model, CrestFlag slot, ref bool value)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        value = state.ModelData.Crest(slot);
    }

    private static UpdateState UpdateBaseCrest(Actor actor, ActorState state, CrestFlag slot, bool visible)
    {
        if (actor.IsTransformed)
            return UpdateState.Transformed;

        if (state.BaseData.Crest(slot) != visible)
        {
            state.BaseData.SetCrest(slot, visible);
            return UpdateState.Change;
        }

        return UpdateState.NoChange;
    }

    /// <summary> Update base data for a single changed weapon slot. </summary>
    private unsafe UpdateState UpdateBaseData(Actor actor, ActorState state, EquipSlot slot, CharacterWeapon weapon)
    {
        if (actor.AsCharacter->CharacterData.TransformationId != 0)
        {
            var actorWeapon = slot is EquipSlot.MainHand ? actor.GetMainhand() : actor.GetOffhand();
            if (weapon.Value != actorWeapon.Value)
                return UpdateState.Transformed;
        }

        var baseData = state.BaseData.Weapon(slot);
        var change   = UpdateState.NoChange;

        // Fist weapon bug hack
        if (slot is EquipSlot.OffHand && weapon.Value == 0 && actor.GetMainhand().Skeleton.Id is > 1600 and < 1651)
            return UpdateState.NoChange;

        if (baseData.Stains != weapon.Stains)
        {
            state.BaseData.SetStain(slot, weapon.Stains);
            change = UpdateState.Change;
        }

        if (baseData.Skeleton.Id != weapon.Skeleton.Id || baseData.Weapon.Id != weapon.Weapon.Id || baseData.Variant != weapon.Variant)
        {
            if (_isPlayerNpc)
                return UpdateState.Transformed;

            var item = _items.Identify(slot, weapon.Skeleton, weapon.Weapon, weapon.Variant,
                slot is EquipSlot.OffHand ? state.BaseData.MainhandType : FullEquipType.Unknown);
            state.BaseData.SetItem(slot, item);
            change = UpdateState.Change;
        }

        return change;
    }

    /// <summary>
    /// Update the base data starting with the model id.
    /// If the model id changed, and is not a transformation, we need to reload the entire base state from scratch.
    /// Non-Humans are handled differently than humans.
    /// </summary>
    private unsafe UpdateState UpdateBaseData(Actor actor, ActorState state, uint modelId, nint customizeData, nint equipData)
    {
        // Model ID does not agree between game object and new draw object => Transformation.
        if (modelId != (uint)actor.AsCharacter->ModelContainer.ModelCharaId)
            return UpdateState.Transformed;

        // Model ID did not change to stored state.
        if (modelId == state.BaseData.ModelId)
            return UpdateState.NoChange;

        // Model ID did change, reload entire state accordingly.
        // Always use the actor for the base data.
        var isHuman = _humans.IsHuman(modelId);
        if (isHuman)
            state.BaseData = _manager.FromActor(actor, false, false);
        else
            state.BaseData.LoadNonHuman(modelId, *(CustomizeArray*)customizeData, equipData);

        return UpdateState.Change;
    }

    /// <summary>
    /// Update the customize base data of a state.
    /// This should rarely result in changes,
    /// only if we kept track of state of someone who went to the aesthetician,
    /// or if they used other tools to change things.
    /// </summary>
    private unsafe UpdateState UpdateBaseData(Actor actor, ActorState state, CustomizeArray customize, bool checkTransform)
    {
        // Customize array does not agree between game object and draw object => transformation.
        if (checkTransform && !actor.Customize->Equals(customize))
            return UpdateState.Transformed;

        // Check for player NPCs with a different game state.
        if (_isPlayerNpc && !actor.Customize->Equals(state.BaseData.Customize))
            return UpdateState.Transformed;

        // Customize array did not change to stored state.
        if (state.BaseData.Customize.Equals(customize))
            return UpdateState.NoChange; // TODO: handle wrong base data.

        // Update customize base state.
        state.BaseData.Customize = customize;
        return UpdateState.Change;
    }

    /// <summary> Handle visor state changes made by the game. </summary>
    private unsafe void OnVisorChange(Model model, bool game, ref bool value)
    {
        // Skip updates when in customize update.
        if (ChangeCustomizeService.InUpdate.InMethod)
            return;

        // Find appropriate actor and state.
        // We do not need to handle fixed designs,
        // since a fixed design would already have established state-tracking.
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (!actor.IsCharacter)
            return;

        // Only actually change anything if the actor state changed,
        // when equipping headgear the method is called with the current draw object state,
        // which corrupts Glamourer's assumed game state otherwise.
        if (!game && actor.AsCharacter->DrawData.IsVisorToggled != value)
            return;

        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update visor base state.
        if (state.BaseData.SetVisor(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state.Sources[MetaIndex.VisorState].IsFixed())
                value = state.ModelData.IsVisorToggled();
            else
                _manager.ChangeMetaState(state, MetaIndex.VisorState, value, ApplySettings.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value = state.ModelData.IsVisorToggled();
        }
    }

    /// <summary> Handle Hat Visibility changes. These act on the game object. </summary>
    private void OnHeadGearVisibilityChange(Actor actor, ref bool value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Find appropriate state.
        // We do not need to handle fixed designs,
        // if there is no model that caused a fixed design to exist yet,
        // we also do not care about the invisible model.
        if (!actor.Identifier(_actors, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update hat visibility state.
        if (state.BaseData.SetHatVisible(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state.Sources[MetaIndex.HatState].IsFixed())
                value = state.ModelData.IsHatVisible();
            else
                _manager.ChangeMetaState(state, MetaIndex.HatState, value, ApplySettings.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value = state.ModelData.IsHatVisible();
        }
    }

    /// <summary> Handle Weapon Visibility changes. These act on the game object. </summary>
    private void OnWeaponVisibilityChange(Actor actor, ref bool value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Find appropriate state.
        // We do not need to handle fixed designs,
        // if there is no model that caused a fixed design to exist yet,
        // we also do not care about the invisible model.
        if (!actor.Identifier(_actors, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update weapon visibility state.
        if (state.BaseData.SetWeaponVisible(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state.Sources[MetaIndex.WeaponState].IsFixed())
                value = state.ModelData.IsWeaponVisible();
            else
                _manager.ChangeMetaState(state, MetaIndex.WeaponState, value, ApplySettings.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value = state.ModelData.IsWeaponVisible();
        }
    }

    /// <summary> Protect a given equipment data array against restricted gear if enabled. </summary>
    private unsafe void ProtectRestrictedGear(nint equipDataPtr, Race race, Gender gender)
    {
        if (!_config.UseRestrictedGearProtection)
            return;

        var idx = 0;
        var ptr = (CharacterArmor*)equipDataPtr;
        for (var end = ptr + 10; ptr < end; ++ptr)
        {
            var (_, newArmor) =
                _items.RestrictedGear.ResolveRestricted(*ptr, EquipSlotExtensions.EqdpSlots[idx++], race, gender);
            *ptr = newArmor;
        }
    }

    private void Subscribe()
    {
        _penumbra.CreatingCharacterBase += OnCreatingCharacterBase;
        _penumbra.CreatedCharacterBase  += OnCreatedCharacterBase;
        _equipSlotUpdating.Subscribe(OnEquipSlotUpdating, EquipSlotUpdating.Priority.StateListener);
        _bonusSlotUpdating.Subscribe(OnBonusSlotUpdating, BonusSlotUpdating.Priority.StateListener);
        _gearsetDataLoaded.Subscribe(OnGearsetDataLoaded, GearsetDataLoaded.Priority.StateListener);
        _movedEquipment.Subscribe(OnMovedEquipment, MovedEquipment.Priority.StateListener);
        _weaponLoading.Subscribe(OnWeaponLoading, WeaponLoading.Priority.StateListener);
        _visorState.Subscribe(OnVisorChange, VisorStateChanged.Priority.StateListener);
        _headGearVisibility.Subscribe(OnHeadGearVisibilityChange, HeadGearVisibilityChanged.Priority.StateListener);
        _weaponVisibility.Subscribe(OnWeaponVisibilityChange, WeaponVisibilityChanged.Priority.StateListener);
        _changeCustomizeService.Subscribe(OnCustomizeChange, ChangeCustomizeService.Priority.StateListener);
        _crestService.Subscribe(OnCrestChange, CrestService.Priority.StateListener);
        _crestService.ModelCrestSetup += OnModelCrestSetup;
        _changeCustomizeService.Subscribe(OnCustomizeChanged, ChangeCustomizeService.Post.Priority.StateListener);
    }

    private void Unsubscribe()
    {
        _penumbra.CreatingCharacterBase -= OnCreatingCharacterBase;
        _penumbra.CreatedCharacterBase  -= OnCreatedCharacterBase;
        _equipSlotUpdating.Unsubscribe(OnEquipSlotUpdating);
        _bonusSlotUpdating.Unsubscribe(OnBonusSlotUpdating);
        _gearsetDataLoaded.Unsubscribe(OnGearsetDataLoaded);
        _movedEquipment.Unsubscribe(OnMovedEquipment);
        _weaponLoading.Unsubscribe(OnWeaponLoading);
        _visorState.Unsubscribe(OnVisorChange);
        _headGearVisibility.Unsubscribe(OnHeadGearVisibilityChange);
        _weaponVisibility.Unsubscribe(OnWeaponVisibilityChange);
        _changeCustomizeService.Unsubscribe(OnCustomizeChange);
        _crestService.Unsubscribe(OnCrestChange);
        _crestService.ModelCrestSetup -= OnModelCrestSetup;
        _changeCustomizeService.Unsubscribe(OnCustomizeChanged);
    }

    private void OnCreatedCharacterBase(nint gameObject, Guid _, nint drawObject)
    {
        if (_condition[ConditionFlag.CreatingCharacter])
            return;

        if (_creatingState == null)
            return;

        var data = new ActorData(gameObject, _creatingIdentifier.ToName());
        _applier.ChangeMetaState(data, MetaIndex.HatState,    _creatingState.ModelData.IsHatVisible());
        _applier.ChangeMetaState(data, MetaIndex.Wetness,     _creatingState.ModelData.IsWet());
        _applier.ChangeMetaState(data, MetaIndex.WeaponState, _creatingState.ModelData.IsWeaponVisible());

        ApplyParameters(_creatingState, drawObject);
    }

    private void OnCustomizeChanged(Model model)
    {
        if (_customizeState == null)
        {
            var actor = _penumbra.GameObjectFromDrawObject(model);
            if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
                return;

            if (!actor.Identifier(_actors, out var identifier)
             || !_manager.TryGetValue(identifier, out _customizeState))
                return;
        }

        ApplyParameters(_customizeState, model);
        _customizeState = null;
    }

    private void ApplyParameters(ActorState state, Model model)
    {
        if (!model.IsHuman)
            return;

        var data = model.GetParameterData();
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            var newValue = data[flag];
            switch (state.Sources[flag])
            {
                case StateSource.Game:
                    if (state.BaseData.Parameters.Set(flag, newValue))
                        _manager.ChangeCustomizeParameter(state, flag, newValue, ApplySettings.Game);
                    break;
                case StateSource.Manual:
                    if (state.BaseData.Parameters.Set(flag, newValue))
                        _manager.ChangeCustomizeParameter(state, flag, newValue, ApplySettings.Game);
                    else
                        model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
                case StateSource.IpcManual:
                    if (state.BaseData.Parameters.Set(flag, newValue))
                        _manager.ChangeCustomizeParameter(state, flag, newValue, ApplySettings.Game);
                    else
                        model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
                case StateSource.Fixed:
                    state.BaseData.Parameters.Set(flag, newValue);
                    model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
                case StateSource.IpcFixed:
                    state.BaseData.Parameters.Set(flag, newValue);
                    model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
                case StateSource.Pending:
                    state.BaseData.Parameters.Set(flag, newValue);
                    state.Sources[flag] = StateSource.Manual;
                    model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
                case StateSource.IpcPending:
                    state.BaseData.Parameters.Set(flag, newValue);
                    state.Sources[flag] = StateSource.IpcManual;
                    model.ApplySingleParameterData(flag, state.ModelData.Parameters);
                    break;
            }
        }
    }
}
