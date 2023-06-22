using System;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

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

    public bool Enabled
    {
        get => _config.Enabled;
        set => Enable(value);
    }

    public StateListener(StateManager manager, ItemManager items, PenumbraService penumbra, ActorService actors, Configuration config,
        SlotUpdating slotUpdating, WeaponLoading weaponLoading, VisorStateChanged visorState, WeaponVisibilityChanged weaponVisibility,
        HeadGearVisibilityChanged headGearVisibility)
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

    private enum UpdateState
    {
        NoChange,
        Transformed,
        Change,
    }

    private unsafe void OnCreatingCharacterBase(nint actorPtr, string _, nint modelPtr, nint customizePtr, nint equipDataPtr)
    {
        // TODO: Fixed Designs.
        var actor      = (Actor)actorPtr;
        var identifier = actor.GetIdentifier(_actors.AwaitedService);

        var     modelId   = *(uint*)modelPtr;
        ref var customize = ref *(Customize*)customizePtr;
        if (_manager.TryGetValue(identifier, out var state))
            switch (UpdateBaseData(actor, state, modelId, customizePtr, equipDataPtr))
            {
                case UpdateState.Change:      break;
                case UpdateState.Transformed: break;
                case UpdateState.NoChange:
                    UpdateBaseData(actor, state, customize);
                    break;
            }

        if (_config.UseRestrictedGearProtection && modelId == 0)
            ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
    }

    private void OnSlotUpdating(Model model, EquipSlot slot, Ref<CharacterArmor> armor, Ref<ulong> returnValue)
    {
        // TODO handle hat state
        var actor     = _penumbra.GameObjectFromDrawObject(model);
        var customize = model.GetCustomize();
        if (actor.Identifier(_actors.AwaitedService, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
            ApplyEquipmentPiece(actor, state, slot, ref armor.Value);

        if (_config.UseRestrictedGearProtection)
            (_, armor.Value) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
    }

    private void OnWeaponLoading(Actor actor, EquipSlot slot, Ref<CharacterWeapon> weapon)
    {
        if (!actor.Identifier(_actors.AwaitedService, out var identifier)
         || !_manager.TryGetValue(identifier, out var state))
            return;

        ref var actorWeapon = ref weapon.Value;
        var     stateItem   = state.ModelData.Item(slot);
        if (actorWeapon.Set.Value != stateItem.ModelId.Value
         || actorWeapon.Type.Value != stateItem.WeaponType
         || actorWeapon.Variant != stateItem.Variant)
        {
            var oldActorItem = state.BaseData.Item(slot);
            if (oldActorItem.ModelId.Value == actorWeapon.Set.Value
             && oldActorItem.WeaponType.Value == actorWeapon.Type.Value
             && oldActorItem.Variant == actorWeapon.Variant)
            {
                actorWeapon.Set     = stateItem.ModelId;
                actorWeapon.Type    = stateItem.WeaponType;
                actorWeapon.Variant = stateItem.Variant;
            }
            else
            {
                var identified = _items.Identify(slot, actorWeapon.Set, actorWeapon.Type, (byte)actorWeapon.Variant,
                    slot == EquipSlot.OffHand ? state.BaseData.Item(EquipSlot.MainHand).Type : FullEquipType.Unknown);
                state.BaseData.SetItem(slot, identified);
                if (state[slot, false] is not StateChanged.Source.Fixed)
                {
                    state.ModelData.SetItem(slot, identified);
                    state[slot, false] = StateChanged.Source.Game;
                }
                else
                {
                    actorWeapon.Set     = stateItem.ModelId;
                    actorWeapon.Type    = stateItem.Variant;
                    actorWeapon.Variant = stateItem.Variant;
                }
            }
        }

        var stateStain = state.ModelData.Stain(slot);
        if (actorWeapon.Stain.Value != stateStain.Value)
        {
            var oldActorStain = state.BaseData.Stain(slot);
            if (state[slot, true] is not StateChanged.Source.Fixed)
            {
                state.ModelData.SetStain(slot, actorWeapon.Stain);
                state[slot, true] = StateChanged.Source.Game;
            }
            else
            {
                actorWeapon.Stain = stateStain;
            }
        }
    }


    private void ApplyCustomize(Actor actor, ActorState state, ref Customize customize)
    {
        var     actorCustomize    = actor.GetCustomize();
        ref var oldActorCustomize = ref state.BaseData.Customize;
        ref var stateCustomize    = ref state.ModelData.Customize;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var value      = customize[idx];
            var actorValue = actorCustomize[idx];
            if (value.Value != actorValue.Value)
                continue;

            var stateValue = stateCustomize[idx];
            if (value.Value == stateValue.Value)
                continue;

            if (oldActorCustomize[idx].Value == actorValue.Value)
            {
                customize[idx] = stateValue;
            }
            else
            {
                oldActorCustomize[idx] = actorValue;
                if (state[idx] is StateChanged.Source.Fixed)
                {
                    state.ModelData.Customize[idx] = value;
                    state[idx]                     = StateChanged.Source.Game;
                }
                else
                {
                    customize[idx] = stateValue;
                }
            }
        }
    }

    private unsafe void ApplyEquipment(Actor actor, ActorState state, CharacterArmor* equipData)
    {
        // TODO: Handle hat state
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            ApplyEquipmentPiece(actor, state, slot, ref *equipData++);
    }

    private void ApplyEquipmentPiece(Actor actor, ActorState state, EquipSlot slot, ref CharacterArmor armor)
    {
        var changeState = UpdateBaseData(actor, state, slot, armor);
        if (changeState is UpdateState.Transformed)
            return;

        if (changeState is UpdateState.NoChange)
        {
            armor = state.ModelData.Armor(slot);
        }
        else
        {
            var modelArmor = state.ModelData.Armor(slot);
            if (armor.Value == modelArmor.Value)
                return;

            if (state[slot, false] is StateChanged.Source.Fixed)
            {
                armor.Set     = modelArmor.Set;
                armor.Variant = modelArmor.Variant;
            }
            else
            {
                _manager.ChangeEquip(state, slot, state.BaseData.Item(slot), StateChanged.Source.Game);
            }

            if (state[slot, true] is StateChanged.Source.Fixed)
                armor.Stain = modelArmor.Stain;
            else
                _manager.ChangeStain(state, slot, state.BaseData.Stain(slot), StateChanged.Source.Game);
        }
    }

    private unsafe void ProtectRestrictedGear(nint equipDataPtr, Race race, Gender gender)
    {
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

    private UpdateState UpdateBaseData(Actor actor, ActorState state, EquipSlot slot, CharacterWeapon weapon)
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

    private unsafe UpdateState UpdateBaseData(Actor actor, ActorState state, uint modelId, nint customizeData, nint equipData)
    {
        if (modelId != (uint)actor.AsCharacter->CharacterData.ModelCharaId)
            return UpdateState.Transformed;

        if (modelId == state.BaseData.ModelId)
            return UpdateState.NoChange;

        if (modelId == 0)
            state.BaseData.LoadNonHuman(modelId, *(Customize*)customizeData, (byte*)equipData);
        else
            state.BaseData = _manager.FromActor(actor);

        return UpdateState.Change;
    }

    private UpdateState UpdateBaseData(Actor actor, ActorState state, Customize customize)
    {
        if (!actor.GetCustomize().Equals(customize))
            return UpdateState.Transformed;

        if (state.BaseData.Customize.Equals(customize))
            return UpdateState.NoChange;

        state.BaseData.Customize.Load(customize);
        return UpdateState.Change;
    }

    private void OnVisorChange(Model model, Ref<bool> value)
    {
        var actor = _penumbra.GameObjectFromDrawObject(model);
        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        if (state.BaseData.SetVisor(value))
        {
            if (state[ActorState.MetaFlag.VisorState] is StateChanged.Source.Fixed)
                value.Value = state.ModelData.IsVisorToggled();
            else
                _manager.ChangeVisorState(state, value, StateChanged.Source.Game);
        }
        else
        {
            value.Value = state.ModelData.IsVisorToggled();
        }
    }

    private void OnHeadGearVisibilityChange(Actor actor, Ref<bool> value)
    {
        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        if (state.BaseData.SetHatVisible(value))
        {
            if (state[ActorState.MetaFlag.HatState] is StateChanged.Source.Fixed)
                value.Value = state.ModelData.IsHatVisible();
            else
                _manager.ChangeHatState(state, value, StateChanged.Source.Game);
        }
        else
        {
            value.Value = state.ModelData.IsHatVisible();
        }
    }

    private void OnWeaponVisibilityChange(Actor actor, Ref<bool> value)
    {
        if (!actor.Identifier(_actors.AwaitedService, out var identifier))
            return;

        if (!_manager.TryGetValue(identifier, out var state))
            return;

        if (state.BaseData.SetWeaponVisible(value))
        {
            if (state[ActorState.MetaFlag.WeaponState] is StateChanged.Source.Fixed)
                value.Value = state.ModelData.IsWeaponVisible();
            else
                _manager.ChangeWeaponState(state, value, StateChanged.Source.Game);
        }
        else
        {
            value.Value = state.ModelData.IsWeaponVisible();
        }
    }
}
