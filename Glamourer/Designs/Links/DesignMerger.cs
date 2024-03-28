using Glamourer.Automation;
using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Unlocks;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Links;

public class DesignMerger(
    DesignManager designManager,
    CustomizeService _customize,
    Configuration _config,
    ItemUnlockManager _itemUnlocks,
    CustomizeUnlockManager _customizeUnlocks) : IService
{
    public MergedDesign Merge(LinkContainer designs, in CustomizeArray currentCustomize, in DesignData baseRef, bool respectOwnership,
        bool modAssociations)
        => Merge(designs.Select(d => ((IDesignStandIn)d.Link, d.Type, JobFlag.All)), currentCustomize, baseRef, respectOwnership, modAssociations);

    public MergedDesign Merge(IEnumerable<(IDesignStandIn, ApplicationType, JobFlag)> designs, in CustomizeArray currentCustomize, in DesignData baseRef,
        bool respectOwnership, bool modAssociations)
    {
        var ret = new MergedDesign(designManager);
        ret.Design.SetCustomize(_customize, currentCustomize);
        var           startBodyType = currentCustomize.BodyType;
        CustomizeFlag fixFlags      = 0;
        respectOwnership &= _config.UnlockedItemMode;
        foreach (var (design, type, jobs) in designs)
        {
            if (type is 0)
                continue;

            ref readonly var data   = ref design.GetDesignData(baseRef);
            var              source = design.AssociatedSource();

            if (!data.IsHuman)
                continue;

            var (equipFlags, customizeFlags, crestFlags, parameterFlags, applyMeta) = type.ApplyWhat(design);
            ReduceMeta(data, applyMeta, ret, source);
            ReduceCustomize(data, customizeFlags, ref fixFlags, ret, source, respectOwnership, startBodyType);
            ReduceEquip(data, equipFlags, ret, source, respectOwnership);
            ReduceMainhands(data, jobs, equipFlags, ret, source, respectOwnership);
            ReduceOffhands(data, jobs, equipFlags, ret, source, respectOwnership);
            ReduceCrests(data, crestFlags, ret, source);
            ReduceParameters(data, parameterFlags, ret, source);
            ReduceMods(design as Design, ret, modAssociations);
            if (type.HasFlag(ApplicationType.GearCustomization))
                ReduceMaterials(design, ret);
        }

        ApplyFixFlags(ret, fixFlags);
        return ret;
    }


    private static void ReduceMaterials(IDesignStandIn designStandIn, MergedDesign ret)
    {
        if (designStandIn is not DesignBase design)
            return;

        var materials = ret.Design.GetMaterialDataRef();
        foreach (var (key, value) in design.Materials.Where(p => p.Item2.Enabled))
            materials.TryAddValue(MaterialValueIndex.FromKey(key), value);
    }

    private static void ReduceMods(Design? design, MergedDesign ret, bool modAssociations)
    {
        if (design == null || !modAssociations)
            return;

        foreach (var (mod, settings) in design.AssociatedMods)
            ret.AssociatedMods.TryAdd(mod, settings);
    }

    private static void ReduceMeta(in DesignData design, MetaFlag applyMeta, MergedDesign ret, StateSource source)
    {
        applyMeta &= ~ret.Design.ApplyMeta;
        if (applyMeta == 0)
            return;

        foreach (var index in MetaExtensions.AllRelevant)
        {
            if (!applyMeta.HasFlag(index.ToFlag()))
                continue;

            ret.Design.SetApplyMeta(index, true);
            ret.Design.GetDesignDataRef().SetMeta(index, design.GetMeta(index));
            ret.Sources[index] = source;
        }
    }

    private static void ReduceCrests(in DesignData design, CrestFlag crestFlags, MergedDesign ret, StateSource source)
    {
        crestFlags &= ~ret.Design.ApplyCrest;
        if (crestFlags == 0)
            return;

        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            if (!crestFlags.HasFlag(slot))
                continue;

            ret.Design.GetDesignDataRef().SetCrest(slot, design.Crest(slot));
            ret.Design.SetApplyCrest(slot, true);
            ret.Sources[slot] = source;
        }
    }

    private static void ReduceParameters(in DesignData design, CustomizeParameterFlag parameterFlags, MergedDesign ret,
        StateSource source)
    {
        parameterFlags &= ~ret.Design.ApplyParameters;
        if (parameterFlags == 0)
            return;

        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            if (!parameterFlags.HasFlag(flag))
                continue;

            ret.Design.GetDesignDataRef().Parameters.Set(flag, design.Parameters[flag]);
            ret.Design.SetApplyParameter(flag, true);
            ret.Sources[flag] = source;
        }
    }

    private void ReduceEquip(in DesignData design, EquipFlag equipFlags, MergedDesign ret, StateSource source,
        bool respectOwnership)
    {
        equipFlags &= ~ret.Design.ApplyEquip;
        if (equipFlags == 0)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var flag = slot.ToFlag();

            if (equipFlags.HasFlag(flag))
            {
                var item = design.Item(slot);
                if (!respectOwnership || _itemUnlocks.IsUnlocked(item.Id, out _))
                    ret.Design.GetDesignDataRef().SetItem(slot, item);
                ret.Design.SetApplyEquip(slot, true);
                ret.Sources[slot, false] = source;
            }

            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                ret.Design.GetDesignDataRef().SetStain(slot, design.Stain(slot));
                ret.Design.SetApplyStain(slot, true);
                ret.Sources[slot, true] = source;
            }
        }

        foreach (var slot in EquipSlotExtensions.WeaponSlots)
        {
            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                ret.Design.GetDesignDataRef().SetStain(slot, design.Stain(slot));
                ret.Design.SetApplyStain(slot, true);
                ret.Sources[slot, true] = source;
            }
        }
    }

    private void ReduceMainhands(in DesignData design, JobFlag allowedJobs, EquipFlag equipFlags, MergedDesign ret, StateSource source,
        bool respectOwnership)
    {
        if (!equipFlags.HasFlag(EquipFlag.Mainhand))
            return;

        var weapon = design.Item(EquipSlot.MainHand);
        if (respectOwnership && !_itemUnlocks.IsUnlocked(weapon.Id, out _))
            return;

        if (!ret.Design.DoApplyEquip(EquipSlot.MainHand))
        {
            ret.Design.SetApplyEquip(EquipSlot.MainHand, true);
            ret.Design.GetDesignDataRef().SetItem(EquipSlot.MainHand, weapon);
        }

        ret.Weapons.TryAdd(weapon.Type, weapon, source, allowedJobs);
    }

    private void ReduceOffhands(in DesignData design, JobFlag allowedJobs, EquipFlag equipFlags, MergedDesign ret, StateSource source, bool respectOwnership)
    {
        if (!equipFlags.HasFlag(EquipFlag.Offhand))
            return;

        var weapon = design.Item(EquipSlot.OffHand);
        if (respectOwnership && !_itemUnlocks.IsUnlocked(weapon.Id, out _))
            return;

        if (!ret.Design.DoApplyEquip(EquipSlot.OffHand))
        {
            ret.Design.SetApplyEquip(EquipSlot.OffHand, true);
            ret.Design.GetDesignDataRef().SetItem(EquipSlot.OffHand, weapon);
        }

        if (weapon.Valid)
            ret.Weapons.TryAdd(weapon.Type, weapon, source, allowedJobs);
    }

    private void ReduceCustomize(in DesignData design, CustomizeFlag customizeFlags, ref CustomizeFlag fixFlags, MergedDesign ret,
        StateSource source, bool respectOwnership, CustomizeValue startBodyType)
    {
        customizeFlags &= ~ret.Design.ApplyCustomizeExcludingBodyType;
        if (ret.Design.DesignData.Customize.BodyType != startBodyType)
            customizeFlags &= ~CustomizeFlag.BodyType;

        if (customizeFlags == 0)
            return;

        // Skip anything not human.
        if (!ret.Design.DesignData.IsHuman || !design.IsHuman)
            return;

        var customize = ret.Design.DesignData.Customize;
        if (customizeFlags.HasFlag(CustomizeFlag.Clan))
        {
            fixFlags |= _customize.ChangeClan(ref customize, design.Customize.Clan);
            ret.Design.SetApplyCustomize(CustomizeIndex.Clan, true);
            ret.Design.SetApplyCustomize(CustomizeIndex.Race, true);
            customizeFlags                   &= ~(CustomizeFlag.Clan | CustomizeFlag.Race);
            ret.Sources[CustomizeIndex.Clan] =  source;
            ret.Sources[CustomizeIndex.Race] =  source;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.Gender))
        {
            fixFlags |= _customize.ChangeGender(ref customize, design.Customize.Gender);
            ret.Design.SetApplyCustomize(CustomizeIndex.Gender, true);
            customizeFlags                     &= ~CustomizeFlag.Gender;
            ret.Sources[CustomizeIndex.Gender] =  source;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.Face))
        {
            customize[CustomizeIndex.Face] = design.Customize.Face;
            ret.Design.SetApplyCustomize(CustomizeIndex.Face, true);
            customizeFlags                   &= ~CustomizeFlag.Face;
            ret.Sources[CustomizeIndex.Face] =  source;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.BodyType))
        {
            customize[CustomizeIndex.BodyType]   =  design.Customize.BodyType;
            customizeFlags                       &= ~CustomizeFlag.BodyType;
            ret.Sources[CustomizeIndex.BodyType] =  source;
        }

        var set  = _customize.Manager.GetSet(customize.Clan, customize.Gender);
        var face = customize.Face;
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var flag = index.ToFlag();
            if (!customizeFlags.HasFlag(flag))
                continue;

            var value = design.Customize[index];
            if (!CustomizeService.IsCustomizationValid(set, face, index, value, out var data))
                continue;

            if (data.HasValue && respectOwnership && !_customizeUnlocks.IsUnlocked(data.Value, out _))
                continue;

            customize[index] = data?.Value ?? value;
            ret.Design.SetApplyCustomize(index, true);
            ret.Sources[index] =  source;
            fixFlags           &= ~flag;
        }

        ret.Design.SetCustomize(_customize, customize);
    }

    private static void ApplyFixFlags(MergedDesign ret, CustomizeFlag fixFlags)
    {
        if (fixFlags == 0)
            return;

        var source = ret.Design.DoApplyCustomize(CustomizeIndex.Clan)
            ? ret.Sources[CustomizeIndex.Clan]
            : ret.Sources[CustomizeIndex.Gender];
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var flag = index.ToFlag();
            if (!fixFlags.HasFlag(flag))
                continue;

            ret.Sources[index] = source;
            ret.Design.SetApplyCustomize(index, true);
        }
    }
}
