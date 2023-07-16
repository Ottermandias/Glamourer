using System;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
    private readonly StateManager              _manager;
    private readonly ItemManager               _items;
    private readonly PenumbraService           _penumbra;
    private readonly SlotUpdating              _slotUpdating;
    private readonly WeaponLoading             _weaponLoading;
    private readonly HeadGearVisibilityChanged _headGearVisibility;
    private readonly VisorStateChanged         _visorState;
    private readonly WeaponVisibilityChanged   _weaponVisibility;
    private readonly AutoDesignApplier         _autoDesignApplier;
    private readonly FunModule                 _funModule;
    private readonly HumanModelList            _humans;

    public bool Enabled
    {
        get => _config.Enabled;
        set => Enable(value);
    }

    public StateListener(StateManager manager, ItemManager items, PenumbraService penumbra, ActorService actors, Configuration config,
        SlotUpdating slotUpdating, WeaponLoading weaponLoading, VisorStateChanged visorState, WeaponVisibilityChanged weaponVisibility,
        HeadGearVisibilityChanged headGearVisibility, AutoDesignApplier autoDesignApplier, FunModule funModule, HumanModelList humans)
    {
        _manager            = manager;
        _items              = items;
        _penumbra           = penumbra;
        _actors             = actors;
        _config             = config;
        _slotUpdating       = slotUpdating;
        _weaponLoading      = weaponLoading;
        _visorState         = visorState;
        _weaponVisibility   = weaponVisibility;
        _headGearVisibility = headGearVisibility;
        _autoDesignApplier  = autoDesignApplier;
        _funModule          = funModule;
        _humans             = humans;

        if (Enabled)
            Subscribe();
    }

    public void Enable(bool value)
    {
        if (value == Enabled)
            return;

        _config.Enabled = value;
        _config.Save();

        if (value)
            Subscribe();
        else
            Unsubscribe();
    }

    void IDisposable.Dispose()
    {
        if (Enabled)
            Unsubscribe();
    }

    /// <summary> The result of updating the base state of an ActorState. </summary>
    private enum UpdateState
    {
        /// <summary> The base state is the same as prior state. </summary>
        NoChange,

        /// <summary> The game requests an update to a state that does not agree with the actor state. </summary>
        Transformed,

        /// <summary> The base state changed compared to prior state. </summary>
        Change,
    }

    /// <summary>
    /// Invoked when a new draw object is created from a game object.
    /// We need to update all state: Model ID, Customize and Equipment.
    /// Weapons and meta flags are updated independently.
    /// We also need to apply fixed designs here.
    /// </summary>
    private unsafe void OnCreatingCharacterBase(nint actorPtr, string _, nint modelPtr, nint customizePtr, nint equipDataPtr)
    {
        var actor      = (Actor)actorPtr;
        var identifier = actor.GetIdentifier(_actors.AwaitedService);

        ref var modelId   = ref *(uint*)modelPtr;
        ref var customize = ref *(Customize*)customizePtr;
        if (_autoDesignApplier.Reduce(actor, identifier, out var state))
        {
            switch (UpdateBaseData(actor, state, modelId, customizePtr, equipDataPtr))
            {
                // TODO handle right
                case UpdateState.Change:      break;
                case UpdateState.Transformed: break;
                case UpdateState.NoChange:

                    modelId = state.ModelData.ModelId;
                    switch (UpdateBaseData(actor, state, customize))
                    {
                        case UpdateState.Transformed: break;
                        case UpdateState.Change:      break;
                        case UpdateState.NoChange:
                            customize = state.ModelData.Customize;
                            break;
                    }

                    foreach (var slot in EquipSlotExtensions.EqdpSlots)
                        HandleEquipSlot(actor, state, slot, ref ((CharacterArmor*)equipDataPtr)[slot.ToIndex()]);

                    break;
            }

            state.TempUnlock();
        }

        _funModule.ApplyFun(actor, new Span<CharacterArmor>((void*)equipDataPtr, 10), ref customize);
        if (modelId == 0)
            ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
    }

    /// <summary>
    /// A draw model loads a new equipment piece.
    /// Update base data, apply or update model data, and protect against restricted gear.
    /// </summary>
    private void OnSlotUpdating(Model model, EquipSlot slot, Ref<CharacterArmor> armor, Ref<ulong> returnValue)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (actor.Identifier(_actors.AwaitedService, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
            HandleEquipSlot(actor, state, slot, ref armor.Value);

        _funModule.ApplyFun(actor, ref armor.Value, slot);
        if (!_config.UseRestrictedGearProtection)
            return;

        var customize = model.GetCustomize();
        (_, armor.Value) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
    }

    /// <summary>
    /// A game object loads a new weapon.
    /// Update base data, apply or update model data.
    /// Verify consistent weapon types.
    /// </summary>
    private void OnWeaponLoading(Actor actor, EquipSlot slot, Ref<CharacterWeapon> weapon)
    {
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
                {
                    state.ModelData.SetItem(slot, state.BaseData.Item(slot));
                    state[slot, false] = StateChanged.Source.Game;
                }
                else
                {
                    apply = true;
                }

                if (state[slot, false] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                {
                    state.ModelData.SetStain(slot, state.BaseData.Stain(slot));
                    state[slot, true] = StateChanged.Source.Game;
                }
                else
                {
                    apply = true;
                }

                break;
            case UpdateState.NoChange:
                apply = true;
                break;
        }

        if (apply)
        {
            // Only allow overwriting identical weapons
            var newWeapon = state.ModelData.Weapon(slot);
            if (baseType is FullEquipType.Unknown || baseType == state.ModelData.Item(slot).Type)
                actorWeapon = newWeapon;
            else if (actorWeapon.Set.Value != 0)
                actorWeapon = actorWeapon.With(newWeapon.Stain);
        }
    }

    /// <summary> Update base data for a single changed equipment slot. </summary>
    private UpdateState UpdateBaseData(Actor actor, ActorState state, EquipSlot slot, CharacterArmor armor)
    {
        var actorArmor = actor.GetArmor(slot);
        // The actor armor does not correspond to the model armor, thus the actor is transformed.
        // This also prevents it from changing values due to hat state.
        if (actorArmor.Value != armor.Value)
            return UpdateState.Transformed;

        var baseData = state.BaseData.Armor(slot);
        var change   = UpdateState.NoChange;
        if (baseData.Stain != armor.Stain)
        {
            state.BaseData.SetStain(slot, armor.Stain);
            change = UpdateState.Change;
        }

        if (baseData.Set.Value != armor.Set.Value || baseData.Variant != armor.Variant)
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
            // Transformed also handles invisible hat state.
            case UpdateState.Transformed: break;
            // Base data changed equipment while actors were not there.
            // Update model state if not on fixed design.
            case UpdateState.Change:
                var apply = false;
                if (state[slot, false] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                {
                    state.ModelData.SetItem(slot, state.BaseData.Item(slot));
                    state[slot, false] = StateChanged.Source.Game;
                }
                else
                {
                    apply = true;
                }

                if (state[slot, true] is not StateChanged.Source.Fixed and not StateChanged.Source.Ipc)
                {
                    state.ModelData.SetStain(slot, state.BaseData.Stain(slot));
                    state[slot, true] = StateChanged.Source.Game;
                }
                else
                {
                    apply = true;
                }

                if (apply)
                    armor = state.ModelData.Armor(slot);

                break;
            // Use current model data.
            case UpdateState.NoChange:
                armor = state.ModelData.Armor(slot);
                break;
        }
    }

    /// <summary> Update base data for a single changed weapon slot. </summary>
    private UpdateState UpdateBaseData(Actor _, ActorState state, EquipSlot slot, CharacterWeapon weapon)
    {
        var baseData = state.BaseData.Weapon(slot);
        var change   = UpdateState.NoChange;

        if (baseData.Stain != weapon.Stain)
        {
            state.BaseData.SetStain(slot, weapon.Stain);
            change = UpdateState.Change;
        }

        if (baseData.Set.Value != weapon.Set.Value || baseData.Type.Value != weapon.Type.Value || baseData.Variant != weapon.Variant)
        {
            var item = _items.Identify(slot, weapon.Set, weapon.Type, (byte)weapon.Variant,
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
            state.BaseData = _manager.FromActor(actor, false);
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
    private UpdateState UpdateBaseData(Actor actor, ActorState state, Customize customize)
    {
        // Customize array does not agree between game object and draw object => transformation.
        if (!actor.GetCustomize().Equals(customize))
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
        // Find appropriate actor and state.
        // We do not need to handle fixed designs,
        // since a fixed design would already have established state-tracking.
        var actor = _penumbra.GameObjectFromDrawObject(model);
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
        _slotUpdating.Subscribe(OnSlotUpdating, SlotUpdating.Priority.StateListener);
        _weaponLoading.Subscribe(OnWeaponLoading, WeaponLoading.Priority.StateListener);
        _visorState.Subscribe(OnVisorChange, VisorStateChanged.Priority.StateListener);
        _headGearVisibility.Subscribe(OnHeadGearVisibilityChange, HeadGearVisibilityChanged.Priority.StateListener);
        _weaponVisibility.Subscribe(OnWeaponVisibilityChange, WeaponVisibilityChanged.Priority.StateListener);
    }

    private void Unsubscribe()
    {
        _penumbra.CreatingCharacterBase -= OnCreatingCharacterBase;
        _slotUpdating.Unsubscribe(OnSlotUpdating);
        _weaponLoading.Unsubscribe(OnWeaponLoading);
        _visorState.Unsubscribe(OnVisorChange);
        _headGearVisibility.Unsubscribe(OnHeadGearVisibilityChange);
        _weaponVisibility.Unsubscribe(OnWeaponVisibilityChange);
    }
}
