using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class InternalStateEditor(
    CustomizeService customizations,
    HumanModelList humans,
    ItemManager items,
    GPoseService gPose,
    ICondition condition)
{
    /// <summary> Change the model id. If the actor is changed from a human to another human, customize and equipData are unused. </summary>
    /// <remarks> We currently only allow changing things to humans, not humans to monsters. </remarks>
    public bool ChangeModelId(ActorState state, uint modelId, in CustomizeArray customize, nint equipData, StateSource source,
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
            state.Sources[MetaIndex.ModelId]     = source;
            state.Sources[MetaIndex.HatState]    = source;
            state.Sources[MetaIndex.WeaponState] = source;
            state.Sources[MetaIndex.VisorState]  = source;
            foreach (var slot in EquipSlotExtensions.FullSlots)
            {
                state.Sources[slot, true]  = source;
                state.Sources[slot, false] = source;
            }

            state.Sources[CustomizeIndex.Clan]   = source;
            state.Sources[CustomizeIndex.Gender] = source;
            var set = customizations.Manager.GetSet(state.ModelData.Customize.Clan, state.ModelData.Customize.Gender);
            foreach (var index in Enum.GetValues<CustomizeIndex>().Where(set.IsAvailable))
                state.Sources[index] = source;
        }
        else
        {
            if (!state.AllowsRedraw(condition))
                return false;

            state.ModelData.LoadNonHuman(modelId, customize, equipData);
            state.Sources[MetaIndex.ModelId] = source;
        }

        return true;
    }

    /// <summary> Change a customization value. </summary>
    public bool ChangeCustomize(ActorState state, CustomizeIndex idx, CustomizeValue value, StateSource source,
        out CustomizeValue old, uint key = 0)
    {
        old = state.ModelData.Customize[idx];
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.Customize[idx] = value;
        state.Sources[idx]             = source;
        return true;
    }

    /// <summary> Change an entire customization array according to functions. </summary>
    public bool ChangeHumanCustomize(ActorState state, in CustomizeArray customizeInput, CustomizeFlag applyWhich,
        Func<CustomizeIndex, StateSource> source, out CustomizeArray old, out CustomizeFlag changed, uint key = 0)
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
                state.Sources[type] = source(type);
        }

        return true;
    }

    /// <summary> Change an entire customization array according to functions. </summary>
    public bool ChangeHumanCustomize(ActorState state, in CustomizeArray customizeInput, Func<CustomizeIndex, bool> applyWhich,
        Func<CustomizeIndex, StateSource> source, out CustomizeArray old, out CustomizeFlag changed, uint key = 0)
    {
        var apply = Enum.GetValues<CustomizeIndex>().Where(applyWhich).Aggregate((CustomizeFlag)0, (current, type) => current | type.ToFlag());
        return ChangeHumanCustomize(state, customizeInput, apply, source, out old, out changed, key);
    }

    /// <summary> Change a single piece of equipment without stain. </summary>
    public bool ChangeItem(ActorState state, EquipSlot slot, EquipItem item, StateSource source, out EquipItem oldItem, uint key = 0)
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
                    ChangeItem(state, slot, old, state.Sources[slot, false], out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state.Sources[slot, false] = source;
        return true;
    }

    /// <summary> Change a single piece of equipment including stain. </summary>
    public bool ChangeEquip(ActorState state, EquipSlot slot, EquipItem item, StainId stain, StateSource source, out EquipItem oldItem,
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
                    ChangeEquip(state, slot, old, oldS, state.Sources[slot, false], out _, out _, key);
            });
        }

        state.ModelData.SetItem(slot, item);
        state.ModelData.SetStain(slot, stain);
        state.Sources[slot, false] = source;
        state.Sources[slot, true]  = source;
        return true;
    }

    /// <summary> Change only the stain of an equipment piece. </summary>
    public bool ChangeStain(ActorState state, EquipSlot slot, StainId stain, StateSource source, out StainId oldStain, uint key = 0)
    {
        oldStain = state.ModelData.Stain(slot);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetStain(slot, stain);
        state.Sources[slot, true] = source;
        return true;
    }

    /// <summary> Change the crest of an equipment piece. </summary>
    public bool ChangeCrest(ActorState state, CrestFlag slot, bool crest, StateSource source, out bool oldCrest, uint key = 0)
    {
        oldCrest = state.ModelData.Crest(slot);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetCrest(slot, crest);
        state.Sources[slot] = source;
        return true;
    }

    /// <summary> Change the customize flags of a character. </summary>
    public bool ChangeParameter(ActorState state, CustomizeParameterFlag flag, CustomizeParameterValue value, StateSource source,
        out CustomizeParameterValue oldValue, uint key = 0)
    {
        oldValue = state.ModelData.Parameters[flag];
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.Parameters.Set(flag, value);
        state.Sources[flag] = source;

        return true;
    }

    /// <summary> Change the value of a single material color table entry. </summary>
    public bool ChangeMaterialValue(ActorState state, MaterialValueIndex index, in MaterialValueState newValue, StateSource source,
        out ColorRow? oldValue, uint key = 0)
    {
        // We already have an existing value.
        if (state.Materials.TryGetValue(index, out var old))
        {
            oldValue = old.Model;
            if (!state.CanUnlock(key))
                return false;

            // Remove if overwritten by a game value.
            if (source is StateSource.Game)
            {
                state.Materials.RemoveValue(index);
                return true;
            }

            // Update if edited.
            state.Materials.UpdateValue(index, newValue, out _);
            return true;
        }

        // We do not have an existing value.
        oldValue = null;
        // Do not do anything if locked or if the game value updates, because then we do not need to add an entry.
        if (!state.CanUnlock(key) || source is StateSource.Game)
            return false;

        // Only add an entry if it is different from the game value.
        return state.Materials.TryAddValue(index, newValue);
    }

    /// <summary> Reset the value of a single material color table entry. </summary>
    public bool ResetMaterialValue(ActorState state, MaterialValueIndex index, uint key = 0)
        => state.CanUnlock(key) && state.Materials.RemoveValue(index);

    public bool ChangeMetaState(ActorState state, MetaIndex index, bool value, StateSource source, out bool oldValue,
        uint key = 0)
    {
        oldValue = state.ModelData.GetMeta(index);
        if (!state.CanUnlock(key))
            return false;

        state.ModelData.SetMeta(index, value);
        state.Sources[index] = source;
        return true;
    }
}
