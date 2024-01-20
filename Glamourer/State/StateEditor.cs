using Dalamud.Plugin.Services;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateEditor(CustomizeService customizations, HumanModelList humans, ItemManager items, GPoseService gPose, ICondition condition)
{
    /// <summary> Change the model id. If the actor is changed from a human to another human, customize and equipData are unused. </summary>
    /// <remarks> We currently only allow changing things to humans, not humans to monsters. </remarks>
    public bool ChangeModelId(ActorState state, uint modelId, in CustomizeArray customize, nint equipData, StateChanged.Source source,
        out uint oldModelId, uint key = 0)
    {
        oldModelId = state.ModelData.ModelId;

        // TODO think about this.
        if (modelId != 0)
            return false;

        if (!state.CanUnlock(key))
            return false;

        var oldIsHuman = state.ModelData.IsHuman;
        state.ModelData.IsHuman = humans.IsHuman(modelId);
        if (state.ModelData.IsHuman)
        {
            if (oldModelId == modelId)
                return true;

            state.ModelData.ModelId = modelId;
            if (oldIsHuman)
                return true;

            if (!state.AllowsRedraw(condition))
                return false;

            // Fix up everything else to make sure the result is a valid human.
            state.ModelData.Customize = CustomizeArray.Default;
            state.ModelData.SetDefaultEquipment(items);
            state.ModelData.SetHatVisible(true);
            state.ModelData.SetWeaponVisible(true);
            state.ModelData.SetVisor(false);
            state.Source[MetaIndex.ModelId]     = source;
            state.Source[MetaIndex.HatState]    = source;
            state.Source[MetaIndex.WeaponState] = source;
            state.Source[MetaIndex.VisorState]  = source;
            foreach (var slot in EquipSlotExtensions.FullSlots)
            {
                state.Source[slot, true]  = source;
                state.Source[slot, false] = source;
            }

            state.Source[CustomizeIndex.Clan]   = source;
            state.Source[CustomizeIndex.Gender] = source;
            var set = customizations.Manager.GetSet(state.ModelData.Customize.Clan, state.ModelData.Customize.Gender);
            foreach (var index in Enum.GetValues<CustomizeIndex>().Where(set.IsAvailable))
                state.Source[index] = source;
        }
        else
        {
            if (!state.AllowsRedraw(condition))
                return false;

            state.ModelData.LoadNonHuman(modelId, customize, equipData);
            state.Source[MetaIndex.ModelId] = source;
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
        state.Source[idx]              = source;
        return true;
    }

    /// <summary> Change an entire customization array according to flags. </summary>
    public bool ChangeHumanCustomize(ActorState state, in CustomizeArray customizeInput, CustomizeFlag applyWhich, StateChanged.Source source,
        out CustomizeArray old, out CustomizeFlag changed, uint key = 0)
    {
        old     = state.ModelData.Customize;
        changed = 0;
        if (!state.CanUnlock(key))
            return false;

        (var customize, var applied, changed) = customizations.Combine(state.ModelData.Customize, customizeInput, applyWhich, true);
        if (changed == 0)
            return false;

        state.ModelData.Customize =  customize;
        applied                   |= changed;
        foreach (var type in Enum.GetValues<CustomizeIndex>())
        {
            if (applied.HasFlag(type.ToFlag()))
                state.Source[type] = source;
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
            if (!gPose.InGPose)
                return false;

            var old = oldItem;
            gPose.AddActionOnLeave(() =>
            {
                if (old.Type == state.BaseData.Item(slot).Type)
                    ChangeItem(state, slot, old, state.Source[slot, false], out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state.Source[slot, false] = source;
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
            if (!gPose.InGPose)
                return false;

            var old  = oldItem;
            var oldS = oldStain;
            gPose.AddActionOnLeave(() =>
            {
                if (old.Type == state.BaseData.Item(slot).Type)
                    ChangeEquip(state, slot, old, oldS, state.Source[slot, false], out _, out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state.ModelData.SetStain(slot, stain);
        state.Source[slot, false] = source;
        state.Source[slot, true]  = source;
        return true;
    }

    /// <summary> Change only the stain of an equipment piece. </summary>
    public bool ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateChanged.Source source, out StainId oldStain, uint key = 0)
    {
        oldStain = state.ModelData.Stain(slot);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetStain(slot, stain);
        state.Source[slot, true] = source;
        return true;
    }

    /// <summary> Change the crest of an equipment piece. </summary>
    public bool ChangeCrest(ActorState state, CrestFlag slot, bool crest, StateChanged.Source source, out bool oldCrest, uint key = 0)
    {
        oldCrest = state.ModelData.Crest(slot);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetCrest(slot, crest);
        state.Source[slot] = source;
        return true;
    }

    /// <summary> Change the customize flags of a character. </summary>
    public bool ChangeParameter(ActorState state, CustomizeParameterFlag flag, CustomizeParameterValue value, StateChanged.Source source,
        out CustomizeParameterValue oldValue, uint key = 0)
    {
        oldValue = state.ModelData.Parameters[flag];
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.Parameters.Set(flag, value);
        state.Source[flag] = source;

        return true;
    }

    public bool ChangeMetaState(ActorState state, MetaIndex index, bool value, StateChanged.Source source, out bool oldValue,
        uint key = 0)
    {
        (var setter, oldValue) = index switch
        {
            MetaIndex.Wetness    => ((Func<bool, bool>)(v => state.ModelData.SetIsWet(v)), state.ModelData.IsWet()),
            MetaIndex.HatState   => ((Func<bool, bool>)(v => state.ModelData.SetHatVisible(v)), state.ModelData.IsHatVisible()),
            MetaIndex.VisorState => ((Func<bool, bool>)(v => state.ModelData.SetVisor(v)), state.ModelData.IsVisorToggled()),
            MetaIndex.WeaponState => ((Func<bool, bool>)(v => state.ModelData.SetWeaponVisible(v)),
                state.ModelData.IsWeaponVisible()),
            _ => throw new Exception("Invalid MetaIndex."),
        };

        if (!state.CanUnlock(key))
            return false;

        setter(value);
        state.Source[index] = source;
        return true;
    }
}
