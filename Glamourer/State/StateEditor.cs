using System;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Services;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateEditor
{
    private readonly ItemManager          _items;
    private readonly CustomizationService _customizations;
    private readonly HumanModelList       _humans;
    private readonly GPoseService         _gPose;

    public StateEditor(CustomizationService customizations, HumanModelList humans, ItemManager items, GPoseService gPose)
    {
        _customizations = customizations;
        _humans         = humans;
        _items          = items;
        _gPose          = gPose;
    }

    /// <summary> Change the model id. If the actor is changed from a human to another human, customize and equipData are unused. </summary>
    /// <remarks> We currently only allow changing things to humans, not humans to monsters. </remarks>
    public bool ChangeModelId(ActorState state, uint modelId, in Customize customize, nint equipData, StateChanged.Source source,
        out uint oldModelId, uint key = 0)
    {
        oldModelId = state.ModelData.ModelId;

        // TODO think about this.
        if (modelId != 0)
            return false;

        if (!state.CanUnlock(key))
            return false;

        var oldIsHuman = state.ModelData.IsHuman;
        state.ModelData.IsHuman = _humans.IsHuman(modelId);
        if (state.ModelData.IsHuman)
        {
            if (oldModelId == modelId)
                return true;

            state.ModelData.ModelId = modelId;
            if (oldIsHuman)
                return true;

            // Fix up everything else to make sure the result is a valid human.
            state.ModelData.Customize = Customize.Default;
            state.ModelData.SetDefaultEquipment(_items);
            state.ModelData.SetHatVisible(true);
            state.ModelData.SetWeaponVisible(true);
            state.ModelData.SetVisor(false);
            state[ActorState.MetaIndex.ModelId]     = source;
            state[ActorState.MetaIndex.HatState]    = source;
            state[ActorState.MetaIndex.WeaponState] = source;
            state[ActorState.MetaIndex.VisorState]  = source;
            foreach (var slot in EquipSlotExtensions.FullSlots)
            {
                state[slot, true]  = source;
                state[slot, false] = source;
            }

            state[CustomizeIndex.Clan]   = source;
            state[CustomizeIndex.Gender] = source;
            var set = _customizations.AwaitedService.GetList(state.ModelData.Customize.Clan, state.ModelData.Customize.Gender);
            foreach (var index in Enum.GetValues<CustomizeIndex>().Where(set.IsAvailable))
                state[index] = source;
        }
        else
        {
            state.ModelData.LoadNonHuman(modelId, customize, equipData);
            state[ActorState.MetaIndex.ModelId] = source;
        }

        return true;
    }

    /// <summary> Change a customization value. </summary>
    public bool ChangeCustomize(ActorState state, CustomizeIndex idx, CustomizeValue value, StateChanged.Source source,
        out CustomizeValue old, uint key = 0)
    {
        old = state.ModelData.Customize[idx];
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.Customize[idx] = value;
        state[idx]                     = source;
        return true;
    }

    /// <summary> Change an entire customization array according to flags. </summary>
    public bool ChangeHumanCustomize(ActorState state, in Customize customizeInput, CustomizeFlag applyWhich, StateChanged.Source source,
        out Customize old, out CustomizeFlag changed, uint key = 0)
    {
        old     = state.ModelData.Customize;
        changed = 0;
        if (!state.CanUnlock(key))
            return false;

        (var customize, var applied, changed) = _customizations.Combine(state.ModelData.Customize, customizeInput, applyWhich, true);
        if (changed == 0)
            return false;

        state.ModelData.Customize =  customize;
        applied                   |= changed;
        foreach (var type in Enum.GetValues<CustomizeIndex>())
        {
            if (applied.HasFlag(type.ToFlag()))
                state[type] = source;
        }

        return true;
    }

    /// <summary> Change a single piece of equipment without stain. </summary>
    public bool ChangeItem(ActorState state, EquipSlot slot, EquipItem item, StateChanged.Source source, out EquipItem oldItem, uint key = 0)
    {
        oldItem = state.ModelData.Item(slot);
        if (!state.CanUnlock(key))
            return false;

        // Can not change weapon type from expected type in state.
        if (slot is EquipSlot.MainHand && item.Type != state.BaseData.MainhandType
         || slot is EquipSlot.OffHand && item.Type != state.BaseData.OffhandType)
        {
            if (!_gPose.InGPose)
                return false;

            var old = oldItem;
            _gPose.AddActionOnLeave(() =>
            {
                if (old.Type == state.BaseData.Item(slot).Type)
                    ChangeItem(state, slot, old, state[slot, false], out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state[slot, false] = source;
        return true;
    }

    /// <summary> Change a single piece of equipment including stain. </summary>
    public bool ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StainId stain, StateChanged.Source source, out EquipItem oldItem,
        out StainId oldStain, uint key = 0)
    {
        oldItem  = state.ModelData.Item(slot);
        oldStain = state.ModelData.Stain(slot);
        if (!state.CanUnlock(key))
            return false;

        // Can not change weapon type from expected type in state.
        if (slot is EquipSlot.MainHand && item.Type != state.BaseData.MainhandType
         || slot is EquipSlot.OffHand && item.Type != state.BaseData.OffhandType)
        {
            if (!_gPose.InGPose)
                return false;

            var old  = oldItem;
            var oldS = oldStain;
            _gPose.AddActionOnLeave(() =>
            {
                if (old.Type == state.BaseData.Item(slot).Type)
                    ChangeEquip(state, slot, old, oldS, state[slot, false], out _, out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state.ModelData.SetStain(slot, stain);
        state[slot, false] = source;
        state[slot, true]  = source;
        return true;
    }

    /// <summary> Change only the stain of an equipment piece. </summary>
    public bool ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateChanged.Source source, out StainId oldStain, uint key = 0)
    {
        oldStain = state.ModelData.Stain(slot);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetStain(slot, stain);
        state[slot, true] = source;
        return true;
    }

    public bool ChangeMetaState(ActorState state, ActorState.MetaIndex index, bool value, StateChanged.Source source, out bool oldValue,
        uint key = 0)
    {
        (var setter, oldValue) = index switch
        {
            ActorState.MetaIndex.Wetness    => ((Func<bool, bool>)(v => state.ModelData.SetIsWet(v)), state.ModelData.IsWet()),
            ActorState.MetaIndex.HatState   => ((Func<bool, bool>)(v => state.ModelData.SetHatVisible(v)), state.ModelData.IsHatVisible()),
            ActorState.MetaIndex.VisorState => ((Func<bool, bool>)(v => state.ModelData.SetVisor(v)), state.ModelData.IsVisorToggled()),
            ActorState.MetaIndex.WeaponState => ((Func<bool, bool>)(v => state.ModelData.SetWeaponVisible(v)),
                state.ModelData.IsWeaponVisible()),
            _ => throw new Exception("Invalid MetaIndex."),
        };

        if (!state.CanUnlock(key))
            return false;

        setter(value);
        state[index] = source;
        return true;
    }
}
