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
    private readonly Configuration   _config;
    private readonly ActorService    _actors;
    private readonly StateManager    _manager;
    private readonly ItemManager     _items;
    private readonly PenumbraService _penumbra;
    private readonly SlotUpdating    _slotUpdating;
    private readonly WeaponLoading   _weaponLoading;

    public bool Enabled
    {
        get => _config.Enabled;
        set => Enable(value);
    }

    public StateListener(StateManager manager, ItemManager items, PenumbraService penumbra, ActorService actors, Configuration config,
        SlotUpdating slotUpdating, WeaponLoading weaponLoading)
    {
        _manager       = manager;
        _items         = items;
        _penumbra      = penumbra;
        _actors        = actors;
        _config        = config;
        _slotUpdating  = slotUpdating;
        _weaponLoading = weaponLoading;

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

    private unsafe void OnCreatingCharacterBase(nint actorPtr, string _, nint modelPtr, nint customizePtr, nint equipDataPtr)
    {
        // TODO: Fixed Designs.
        var actor      = (Actor)actorPtr;
        var identifier = actor.GetIdentifier(_actors.AwaitedService);

        if (*(int*)modelPtr != actor.AsCharacter->ModelCharaId)
            return;

        ref var customize = ref *(Customize*)customizePtr;
        if (_manager.TryGetValue(identifier, out var state))
        {
            ApplyCustomize(actor, state, ref customize);
            ApplyEquipment(actor, state, (CharacterArmor*)equipDataPtr);
            if (_config.UseRestrictedGearProtection)
                ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
        }
        else if (_config.UseRestrictedGearProtection && *(uint*)modelPtr == 0)
        {
            ProtectRestrictedGear(equipDataPtr, customize.Race, customize.Gender);
        }
    }

    private void OnSlotUpdating(Model model, EquipSlot slot, Ref<CharacterArmor> armor, Ref<ulong> returnValue)
    {
        // TODO handle hat state
        // TODO handle fixed designs
        var actor     = _penumbra.GameObjectFromDrawObject(model);
        var customize = model.GetCustomize();
        if (actor.Identifier(_actors.AwaitedService, out var identifier)
         && _manager.TryGetValue(identifier, out var state))
            ApplyEquipmentPiece(actor, state, slot, ref armor.Value);

        var (replaced, replacedArmor) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
        if (replaced)
            armor.Assign(replacedArmor);
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
            var oldActorItem = state.ActorData.Item(slot);
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
                    slot == EquipSlot.OffHand ? state.ActorData.Item(EquipSlot.MainHand).Type : FullEquipType.Unknown);
                state.ActorData.SetItem(slot, identified);
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
            var oldActorStain = state.ActorData.Stain(slot);
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
        ref var oldActorCustomize = ref state.ActorData.Customize;
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
        var actorArmor = actor.GetArmor(slot);
        if (armor.Value != actorArmor.Value)
            return;

        var stateArmor = state.ModelData.Item(slot);
        if (armor.Set.Value != stateArmor.ModelId.Value || armor.Variant != stateArmor.Variant)
        {
            var oldActorArmor = state.ActorData.Item(slot);
            if (oldActorArmor.ModelId.Value == actorArmor.Set.Value && oldActorArmor.Variant == actorArmor.Variant)
            {
                armor.Set     = stateArmor.ModelId;
                armor.Variant = stateArmor.Variant;
            }
            else
            {
                var identified = _items.Identify(slot, actorArmor.Set, actorArmor.Variant);
                state.ActorData.SetItem(slot, identified);
                if (state[slot, false] is not StateChanged.Source.Fixed)
                {
                    state.ModelData.SetItem(slot, identified);
                    state[slot, false] = StateChanged.Source.Game;
                }
                else
                {
                    armor.Set     = stateArmor.ModelId;
                    armor.Variant = stateArmor.Variant;
                }
            }
        }

        var stateStain = state.ModelData.Stain(slot);
        if (armor.Stain.Value != stateStain.Value)
        {
            var oldActorStain = state.ActorData.Stain(slot);
            if (oldActorStain.Value == actorArmor.Stain.Value)
            {
                armor.Stain = stateStain;
            }
            else
            {
                state.ActorData.SetStain(slot, actorArmor.Stain);
                if (state[slot, true] is not StateChanged.Source.Fixed)
                {
                    state.ModelData.SetStain(slot, actorArmor.Stain);
                    state[slot, true] = StateChanged.Source.Game;
                }
                else
                {
                    armor.Stain = stateStain;
                }
            }
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
    }

    private void Unsubscribe()
    {
        _penumbra.CreatingCharacterBase -= OnCreatingCharacterBase;
        _slotUpdating.Unsubscribe(OnSlotUpdating);
        _weaponLoading.Unsubscribe(OnWeaponLoading);
    }
}
