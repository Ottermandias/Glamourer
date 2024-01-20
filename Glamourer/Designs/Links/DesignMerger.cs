using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Unlocks;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Links;

using WeaponDict = Dictionary<FullEquipType, (EquipItem, StateChanged.Source)>;

public sealed class MergedDesign
{
    public MergedDesign(DesignManager designManager)
    {
        Design                 = designManager.CreateTemporary();
        Design.ApplyEquip      = 0;
        Design.ApplyCustomize  = 0;
        Design.ApplyCrest      = 0;
        Design.ApplyParameters = 0;
        Design.SetApplyWetness(false);
        Design.SetApplyVisorToggle(false);
        Design.SetApplyWeaponVisible(false);
        Design.SetApplyHatVisible(false);
    }

    public readonly DesignBase  Design;
    public readonly WeaponDict  Weapons = new(4);
    public readonly StateSource Source  = new();

    public StateChanged.Source GetSource(EquipSlot slot, bool stain, StateChanged.Source actualSource)
        => GetSource(Source[slot, stain], actualSource);

    public StateChanged.Source GetSource(CrestFlag slot, StateChanged.Source actualSource)
        => GetSource(Source[slot], actualSource);

    public StateChanged.Source GetSource(CustomizeIndex type, StateChanged.Source actualSource)
        => GetSource(Source[type], actualSource);

    public StateChanged.Source GetSource(MetaIndex index, StateChanged.Source actualSource)
        => GetSource(Source[index], actualSource);

    public StateChanged.Source GetSource(CustomizeParameterFlag flag, StateChanged.Source actualSource)
        => GetSource(Source[flag], actualSource);

    public static StateChanged.Source GetSource(StateChanged.Source given, StateChanged.Source actualSource)
        => given is StateChanged.Source.Game ? StateChanged.Source.Game : actualSource;
}

public class DesignMerger(
    DesignManager designManager,
    CustomizeService _customize,
    Configuration _config,
    ItemUnlockManager _itemUnlocks,
    CustomizeUnlockManager _customizeUnlocks)
{
    public MergedDesign Merge(IEnumerable<(DesignBase?, ApplicationType)> designs, in DesignData baseRef, bool respectOwnership)
    {
        var           ret      = new MergedDesign(designManager);
        CustomizeFlag fixFlags = 0;
        respectOwnership &= _config.UnlockedItemMode;
        foreach (var (design, type) in designs)
        {
            if (type is 0)
                continue;

            ref readonly var data   = ref design == null ? ref baseRef : ref design.GetDesignDataRef();
            var              source = design == null ? StateChanged.Source.Game : StateChanged.Source.Manual;

            if (!data.IsHuman)
                continue;

            var (equipFlags, customizeFlags, crestFlags, parameterFlags, applyHat, applyVisor, applyWeapon, applyWet) = type.ApplyWhat(design);
            ReduceMeta(data, applyHat, applyVisor, applyWeapon, applyWet, ret, source);
            ReduceCustomize(data, customizeFlags, ref fixFlags, ret, source, respectOwnership);
            ReduceEquip(data, equipFlags, ret, source, respectOwnership);
            ReduceMainhands(data, equipFlags, ret, source, respectOwnership);
            ReduceOffhands(data, equipFlags, ret, source, respectOwnership);
            ReduceCrests(data, crestFlags, ret, source);
            ReduceParameters(data, parameterFlags, ret, source);
        }

        ApplyFixFlags(ret, fixFlags);
        return ret;
    }


    private static void ReduceMeta(in DesignData design, bool applyHat, bool applyVisor, bool applyWeapon, bool applyWet, MergedDesign ret,
        StateChanged.Source source)
    {
        if (applyHat && !ret.Design.DoApplyHatVisible())
        {
            ret.Design.SetApplyHatVisible(true);
            ret.Design.GetDesignDataRef().SetHatVisible(design.IsHatVisible());
            ret.Source[MetaIndex.HatState] = source;
        }

        if (applyVisor && !ret.Design.DoApplyVisorToggle())
        {
            ret.Design.SetApplyVisorToggle(true);
            ret.Design.GetDesignDataRef().SetVisor(design.IsVisorToggled());
            ret.Source[MetaIndex.VisorState] = source;
        }

        if (applyWeapon && !ret.Design.DoApplyWeaponVisible())
        {
            ret.Design.SetApplyWeaponVisible(true);
            ret.Design.GetDesignDataRef().SetWeaponVisible(design.IsWeaponVisible());
            ret.Source[MetaIndex.WeaponState] = source;
        }

        if (applyWet && !ret.Design.DoApplyWetness())
        {
            ret.Design.SetApplyWetness(true);
            ret.Design.GetDesignDataRef().SetIsWet(design.IsWet());
            ret.Source[MetaIndex.Wetness] = source;
        }
    }

    private static void ReduceCrests(in DesignData design, CrestFlag crestFlags, MergedDesign ret, StateChanged.Source source)
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
            ret.Source[slot] = source;
        }
    }

    private static void ReduceParameters(in DesignData design, CustomizeParameterFlag parameterFlags, MergedDesign ret,
        StateChanged.Source source)
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
            ret.Source[flag] = source;
        }
    }

    private void ReduceEquip(in DesignData design, EquipFlag equipFlags, MergedDesign ret, StateChanged.Source source,
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
                ret.Source[slot, false] = source;
            }

            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                ret.Design.GetDesignDataRef().SetStain(slot, design.Stain(slot));
                ret.Design.SetApplyStain(slot, true);
                ret.Source[slot, true] = source;
            }
        }

        foreach (var slot in EquipSlotExtensions.WeaponSlots)
        {
            var stainFlag = slot.ToStainFlag();
            if (equipFlags.HasFlag(stainFlag))
            {
                ret.Design.GetDesignDataRef().SetStain(slot, design.Stain(slot));
                ret.Design.SetApplyStain(slot, true);
                ret.Source[slot, true] = source;
            }
        }
    }

    private void ReduceMainhands(in DesignData design, EquipFlag equipFlags, MergedDesign ret, StateChanged.Source source, bool respectOwnership)
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

        ret.Weapons.TryAdd(weapon.Type, (weapon, source));
    }

    private void ReduceOffhands(in DesignData design, EquipFlag equipFlags, MergedDesign ret, StateChanged.Source source, bool respectOwnership)
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
            ret.Weapons.TryAdd(weapon.Type, (weapon, source));
    }

    private void ReduceCustomize(in DesignData design, CustomizeFlag customizeFlags, ref CustomizeFlag fixFlags, MergedDesign ret,
        StateChanged.Source source, bool respectOwnership)
    {
        customizeFlags &= ~ret.Design.ApplyCustomizeRaw;
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
            customizeFlags                  &= ~(CustomizeFlag.Clan | CustomizeFlag.Race);
            ret.Source[CustomizeIndex.Clan] =  source;
            ret.Source[CustomizeIndex.Race] =  source;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.Gender))
        {
            fixFlags |= _customize.ChangeGender(ref customize, design.Customize.Gender);
            ret.Design.SetApplyCustomize(CustomizeIndex.Gender, true);
            customizeFlags                    &= ~CustomizeFlag.Gender;
            ret.Source[CustomizeIndex.Gender] =  source;
        }

        if (customizeFlags.HasFlag(CustomizeFlag.Face))
        {
            customize[CustomizeIndex.Face] = design.Customize.Face;
            ret.Design.SetApplyCustomize(CustomizeIndex.Face, true);
            customizeFlags                  &= ~CustomizeFlag.Face;
            ret.Source[CustomizeIndex.Face] =  source;
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
            ret.Source[index] =  source;
            fixFlags          &= ~flag;
        }
    }

    private static void ApplyFixFlags(MergedDesign ret, CustomizeFlag fixFlags)
    {
        if (fixFlags == 0)
            return;

        var source = ret.Design.DoApplyCustomize(CustomizeIndex.Clan)
            ? ret.Source[CustomizeIndex.Clan]
            : ret.Source[CustomizeIndex.Gender];
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var flag = index.ToFlag();
            if (!fixFlags.HasFlag(flag))
                continue;

            ret.Source[index] = source;
            ret.Design.SetApplyCustomize(index, true);
        }
    }
}
