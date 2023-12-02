using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Glamourer.Structs;

namespace Glamourer.State;

/// <summary>
/// This class handles all game events that could cause a drawn model to change,
/// it always updates the base state for existing states,
/// and either discards the changes or updates the model state too.
/// </summary>
public class StateListener : IDisposable
{
    private readonly Configuration             _config;
    private readonly ActorService              _actors;
    private readonly ObjectManager             _objects;
    private readonly StateManager              _manager;
    private readonly StateApplier              _applier;
    private readonly ItemManager               _items;
    private readonly CustomizationService      _customizations;
    private readonly PenumbraService           _penumbra;
    private readonly SlotUpdating              _slotUpdating;
    private readonly WeaponLoading             _weaponLoading;
    private readonly HeadGearVisibilityChanged _headGearVisibility;
    private readonly VisorStateChanged         _visorState;
    private readonly WeaponVisibilityChanged   _weaponVisibility;
    private readonly AutoDesignApplier         _autoDesignApplier;
    private readonly FunModule                 _funModule;
    private readonly HumanModelList            _humans;
    private readonly MovedEquipment            _movedEquipment;
    private readonly GPoseService              _gPose;
    private readonly ChangeCustomizeService    _changeCustomizeService;
    private readonly CrestService              _crestService;
    private readonly ICondition                _condition;

    private ActorIdentifier _creatingIdentifier = ActorIdentifier.Invalid;
    private ActorState?     _creatingState;
    private CharacterWeapon _lastFistOffhand = CharacterWeapon.Empty;

    public StateListener(StateManager manager, ItemManager items, PenumbraService penumbra, ActorService actors, Configuration config,
        SlotUpdating slotUpdating, WeaponLoading weaponLoading, VisorStateChanged visorState, WeaponVisibilityChanged weaponVisibility,
        HeadGearVisibilityChanged headGearVisibility, AutoDesignApplier autoDesignApplier, FunModule funModule, HumanModelList humans,
        StateApplier applier, MovedEquipment movedEquipment, ObjectManager objects, GPoseService gPose,
        ChangeCustomizeService changeCustomizeService, CustomizationService customizations, ICondition condition, CrestService crestService)
    {
        _manager                = manager;
        _items                  = items;
        _penumbra               = penumbra;
        _actors                 = actors;
        _config                 = config;
        _slotUpdating           = slotUpdating;
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
    private unsafe void OnCreatingCharacterBase(nint actorPtr, string _, nint modelPtr, nint customizePtr, nint equipDataPtr)
    {
        var actor = (Actor)actorPtr;
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        _creatingIdentifier = actor.GetIdentifier(_actors.AwaitedService);

        ref var modelId   = ref *(uint*)modelPtr;
        ref var customize = ref *(Customize*)customizePtr;
        if (_autoDesignApplier.Reduce(actor, _creatingIdentifier, out _creatingState))
        {
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

        _funModule.ApplyFun(actor, new Span<CharacterArmor>((void*)equipDataPtr, 10), ref customize);
        if (modelId == 0)
            ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
    }

    private unsafe void OnCustomizeChange(Model model, Ref<Customize> customize)
    {
        if (!model.IsHuman)
            return;

        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors.AwaitedService, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        UpdateCustomize(actor, state, ref customize.Value, false);
    }

    private void UpdateCustomize(Actor actor, ActorState state, ref Customize customize, bool checkTransform)
    {
        switch (UpdateBaseData(actor, state, customize, checkTransform))
        {
            case UpdateState.Transformed: break;
            case UpdateState.Change:
                var model = state.ModelData.Customize;
                if (customize.Gender != model.Gender || customize.Clan != model.Clan)
                {
                    _manager.ChangeCustomize(state, in customize, CustomizeFlagExtensions.AllRelevant, StateChanged.Source.Game);
                    return;
                }

                var set = _customizations.AwaitedService.GetList(model.Clan, model.Gender);
                foreach (var index in CustomizationExtensions.AllBasic)
                {
                    if (state[index] is not StateChanged.Source.Fixed)
                    {
                        var newValue = customize[index];
                        var oldValue = model[index];
                        if (newValue != oldValue)
                        {
                            if (set.Validate(index, newValue, out _, model.Face))
                                _manager.ChangeCustomize(state, index, newValue, StateChanged.Source.Game);
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
            case UpdateState.NoChange:
                customize = state.ModelData.Customize;
                break;
        }
    }

    /// <summary>
    /// A draw model loads a new equipment piece.
    /// Update base data, apply or update model data, and protect against restricted gear.
    /// </summary>
    private void OnSlotUpdating(Model model, EquipSlot slot, Ref<CharacterArmor> armor, Ref<ulong> returnValue)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // If the model comes from IPC it is probably from Mare,
        // then we do not want to use our restricted gear protection
        // since we assume the player has that gear modded to availability.
        var locked = false;
        if (actor.Identifier(_actors.AwaitedService, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
        {
            HandleEquipSlot(actor, state, slot, ref armor.Value);
            locked = state[slot, false] is StateChanged.Source.Ipc;
        }

        _funModule.ApplyFun(actor, ref armor.Value, slot);
        if (!_config.UseRestrictedGearProtection || locked)
            return;

        var customize = model.GetCustomize();
        (_, armor.Value) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
    }

    private void OnMovedEquipment((EquipSlot, uint, StainId)[] items)
    {
        _objects.Update();
        var (identifier, objects) = _objects.PlayerData;
        if (!identifier.IsValid || !_manager.TryGetValue(identifier, out var state))
            return;

        foreach (var (slot, item, stain) in items)
        {
            var currentItem = state.BaseData.Item(slot);
            var model       = state.ModelData.Weapon(slot);
            var current     = currentItem.Weapon(state.BaseData.Stain(slot));
            if (model.Value == current.Value || !_items.ItemService.AwaitedService.TryGetValue(item, EquipSlot.MainHand, out var changedItem))
                continue;

            var changed = changedItem.Weapon(stain);
            if (current.Value == changed.Value && state[slot, false] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
            {
                _manager.ChangeItem(state, slot, currentItem, StateChanged.Source.Game);
                _manager.ChangeStain(state, slot, current.Stain, StateChanged.Source.Game);
                switch (slot)
                {
                    case EquipSlot.MainHand:
                    case EquipSlot.OffHand:
                        _applier.ChangeWeapon(objects, slot, currentItem, stain);
                        break;
                    default:
                        _applier.ChangeArmor(objects, slot, current.ToArmor(), state[slot, false] is not StateChanged.Source.Ipc,
                            state.ModelData.IsHatVisible());
                        break;
                }
            }
        }
    }

    /// <summary>
    /// A game object loads a new weapon.
    /// Update base data, apply or update model data.
    /// Verify consistent weapon types.
    /// </summary>
    private void OnWeaponLoading(Actor actor, EquipSlot slot, Ref<CharacterWeapon> weapon)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Fist weapon gauntlet hack.
        if (slot is EquipSlot.OffHand && weapon.Value.Variant == 0 && weapon.Value.Set.Id != 0 && _lastFistOffhand.Set.Id != 0)
            weapon.Value = _lastFistOffhand;

        if (!actor.Identifier(_actors.AwaitedService, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        ref var actorWeapon = ref weapon.Value;
        var     baseType    = state.BaseData.Item(slot).Type;
        var     apply       = false;
        switch (UpdateBaseData(actor, state, slot, actorWeapon))
        {
            // Do nothing. But this usually can not happen because the hooked function also writes to game objects later.
            case UpdateState.Transformed: break;
            case UpdateState.Change:
                if (state[slot, false] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                    _manager.ChangeItem(state, slot, state.BaseData.Item(slot), StateChanged.Source.Game);
                else
                    apply = true;

                if (state[slot, true] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                    _manager.ChangeStain(state, slot, state.BaseData.Stain(slot), StateChanged.Source.Game);
                else
                    apply = true;
                break;
            case UpdateState.NoChange:
                apply = true;
                break;
        }

        if (apply)
        {
            // Only allow overwriting identical weapons
            var newWeapon = state.ModelData.Weapon(slot);
            if (baseType is FullEquipType.Unknown || baseType == state.ModelData.Item(slot).Type || _gPose.InGPose && actor.IsGPoseOrCutscene)
                actorWeapon = newWeapon;
            else if (actorWeapon.Set.Id != 0)
                actorWeapon = actorWeapon.With(newWeapon.Stain);
        }

        // Fist Weapon Offhand hack.
        if (slot is EquipSlot.MainHand && weapon.Value.Set.Id is > 1600 and < 1651)
            _lastFistOffhand = new CharacterWeapon((SetId)(weapon.Value.Set.Id + 50), weapon.Value.Type, weapon.Value.Variant,
                weapon.Value.Stain);

        _funModule.ApplyFun(actor, ref weapon.Value, slot);
    }

    /// <summary> Update base data for a single changed equipment slot. </summary>
    private UpdateState UpdateBaseData(Actor actor, ActorState state, EquipSlot slot, CharacterArmor armor)
    {
        bool FistWeaponGauntletHack()
        {
            if (slot is not EquipSlot.Hands)
                return false;

            var offhand = actor.GetOffhand();
            return offhand.Variant == 0 && offhand.Set.Id != 0 && armor.Set.Id == offhand.Set.Id;
        }

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
                    state.BaseData.SetStain(EquipSlot.Head, actorArmor.Stain);
                    return UpdateState.Change;
                }

                return UpdateState.HatHack;
            }

            if (!fistWeapon)
                return UpdateState.Transformed;
        }

        var baseData = state.BaseData.Armor(slot);
        var change   = UpdateState.NoChange;
        if (baseData.Stain != armor.Stain)
        {
            state.BaseData.SetStain(slot, armor.Stain);
            change = UpdateState.Change;
        }

        if (baseData.Set.Id != armor.Set.Id || baseData.Variant != armor.Variant && !fistWeapon)
        {
            var item = _items.Identify(slot, armor.Set, armor.Variant);
            state.BaseData.SetItem(slot, item);
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
                if (state[slot, false] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                    _manager.ChangeItem(state, slot, state.BaseData.Item(slot), StateChanged.Source.Game);
                else
                    apply = true;

                if (state[slot, true] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                    _manager.ChangeStain(state, slot, state.BaseData.Stain(slot), StateChanged.Source.Game);
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

    private void OnCrestChange(Actor actor, CrestFlag slot, Ref<bool> value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors.AwaitedService, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        switch (UpdateBaseCrest(actor, state, slot, value.Value))
        {
            case UpdateState.Change:
                if (state[slot] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                    _manager.ChangeCrest(state, slot, state.BaseData.Crest(slot), StateChanged.Source.Game);
                else
                    value.Value = state.ModelData.Crest(slot);
                break;
            case UpdateState.NoChange:
            case UpdateState.HatHack:
                value.Value = state.ModelData.Crest(slot);
                break;
            case UpdateState.Transformed: break;
        }
    }

    private void OnModelCrestSetup(Model model, CrestFlag slot, ref bool value)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors.AwaitedService, out var identifier)
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
        if (slot is EquipSlot.OffHand && weapon.Value == 0 && actor.GetMainhand().Set.Id is > 1600 and < 1651)
            return UpdateState.NoChange;

        if (baseData.Stain != weapon.Stain)
        {
            state.BaseData.SetStain(slot, weapon.Stain);
            change = UpdateState.Change;
        }

        if (baseData.Set.Id != weapon.Set.Id || baseData.Type.Id != weapon.Type.Id || baseData.Variant != weapon.Variant)
        {
            var item = _items.Identify(slot, weapon.Set, weapon.Type, weapon.Variant,
                slot is EquipSlot.OffHand ? state.BaseData.Item(EquipSlot.MainHand).Type : FullEquipType.Unknown);
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
        if (modelId != (uint)actor.AsCharacter->CharacterData.ModelCharaId)
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
            state.BaseData.LoadNonHuman(modelId, *(Customize*)customizeData, equipData);

        return UpdateState.Change;
    }

    /// <summary>
    /// Update the customize base data of a state.
    /// This should rarely result in changes,
    /// only if we kept track of state of someone who went to the aesthetician,
    /// or if they used other tools to change things.
    /// </summary>
    private UpdateState UpdateBaseData(Actor actor, ActorState state, Customize customize, bool checkTransform)
    {
        // Customize array does not agree between game object and draw object => transformation.
        if (checkTransform && !actor.GetCustomize().Equals(customize))
            return UpdateState.Transformed;

        // Customize array did not change to stored state.
        if (state.BaseData.Customize.Equals(customize))
            return UpdateState.NoChange; // TODO: handle wrong base data.

        // Update customize base state.
        state.BaseData.Customize.Load(customize);
        return UpdateState.Change;
    }

    /// <summary> Handle visor state changes made by the game. </summary>
    private void OnVisorChange(Model model, Ref<bool> value)
    {
        // Skip updates when in customize update.
        if (ChangeCustomizeService.InUpdate.InMethod)
            return;

        // Find appropriate actor and state.
        // We do not need to handle fixed designs,
        // since a fixed design would already have established state-tracking.
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update visor base state.
        if (state.BaseData.SetVisor(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state[ActorState.MetaIndex.VisorState] is StateChanged.Source.Fixed or StateChanged.Source.Ipc)
                value.Value = state.ModelData.IsVisorToggled();
            else
                _manager.ChangeVisorState(state, value, StateChanged.Source.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value.Value = state.ModelData.IsVisorToggled();
        }
    }

    /// <summary> Handle Hat Visibility changes. These act on the game object. </summary>
    private void OnHeadGearVisibilityChange(Actor actor, Ref<bool> value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Find appropriate state.
        // We do not need to handle fixed designs,
        // if there is no model that caused a fixed design to exist yet,
        // we also do not care about the invisible model.
        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update hat visibility state.
        if (state.BaseData.SetHatVisible(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state[ActorState.MetaIndex.HatState] is StateChanged.Source.Fixed or StateChanged.Source.Ipc)
                value.Value = state.ModelData.IsHatVisible();
            else
                _manager.ChangeHatState(state, value, StateChanged.Source.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value.Value = state.ModelData.IsHatVisible();
        }
    }

    /// <summary> Handle Weapon Visibility changes. These act on the game object. </summary>
    private void OnWeaponVisibilityChange(Actor actor, Ref<bool> value)
    {
        if (_condition[ConditionFlag.CreatingCharacter] && actor.Index >= ObjectIndex.CutsceneStart)
            return;

        // Find appropriate state.
        // We do not need to handle fixed designs,
        // if there is no model that caused a fixed design to exist yet,
        // we also do not care about the invisible model.
        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        // Update weapon visibility state.
        if (state.BaseData.SetWeaponVisible(value))
        {
            // if base state changed, either overwrite the actual value if we have fixed values,
            // or overwrite the stored model state with the new one.
            if (state[ActorState.MetaIndex.WeaponState] is StateChanged.Source.Fixed or StateChanged.Source.Ipc)
                value.Value = state.ModelData.IsWeaponVisible();
            else
                _manager.ChangeWeaponState(state, value, StateChanged.Source.Game);
        }
        else
        {
            // if base state did not change, overwrite the value with the model state one.
            value.Value = state.ModelData.IsWeaponVisible();
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
        _slotUpdating.Subscribe(OnSlotUpdating, SlotUpdating.Priority.StateListener);
        _movedEquipment.Subscribe(OnMovedEquipment, MovedEquipment.Priority.StateListener);
        _weaponLoading.Subscribe(OnWeaponLoading, WeaponLoading.Priority.StateListener);
        _visorState.Subscribe(OnVisorChange, VisorStateChanged.Priority.StateListener);
        _headGearVisibility.Subscribe(OnHeadGearVisibilityChange, HeadGearVisibilityChanged.Priority.StateListener);
        _weaponVisibility.Subscribe(OnWeaponVisibilityChange, WeaponVisibilityChanged.Priority.StateListener);
        _changeCustomizeService.Subscribe(OnCustomizeChange, ChangeCustomizeService.Priority.StateListener);
        _crestService.Subscribe(OnCrestChange, CrestService.Priority.StateListener);
        _crestService.ModelCrestSetup += OnModelCrestSetup;
    }

    private void Unsubscribe()
    {
        _penumbra.CreatingCharacterBase -= OnCreatingCharacterBase;
        _penumbra.CreatedCharacterBase  -= OnCreatedCharacterBase;
        _slotUpdating.Unsubscribe(OnSlotUpdating);
        _movedEquipment.Unsubscribe(OnMovedEquipment);
        _weaponLoading.Unsubscribe(OnWeaponLoading);
        _visorState.Unsubscribe(OnVisorChange);
        _headGearVisibility.Unsubscribe(OnHeadGearVisibilityChange);
        _weaponVisibility.Unsubscribe(OnWeaponVisibilityChange);
        _changeCustomizeService.Unsubscribe(OnCustomizeChange);
        _crestService.Unsubscribe(OnCrestChange);
        _crestService.ModelCrestSetup -= OnModelCrestSetup;
    }

    private void OnCreatedCharacterBase(nint gameObject, string _, nint drawObject)
    {
        if (_condition[ConditionFlag.CreatingCharacter])
            return;

        if (_creatingState == null)
            return;

        var data = new ActorData(gameObject, _creatingIdentifier.ToName());
        _applier.ChangeHatState(data, _creatingState.ModelData.IsHatVisible());
        _applier.ChangeWeaponState(data, _creatingState.ModelData.IsWeaponVisible());
        _applier.ChangeWetness(data, _creatingState.ModelData.IsWet());
    }
}
