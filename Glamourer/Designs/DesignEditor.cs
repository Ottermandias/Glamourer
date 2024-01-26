using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignEditor(
    SaveService saveService,
    DesignChanged designChanged,
    CustomizeService customizations,
    ItemManager items,
    Configuration config)
    : IDesignEditor
{
    protected readonly DesignChanged                DesignChanged  = designChanged;
    protected readonly SaveService                  SaveService    = saveService;
    protected readonly ItemManager                  Items          = items;
    protected readonly CustomizeService             Customizations = customizations;
    protected readonly Configuration                Config         = config;
    protected readonly Dictionary<Guid, DesignData> UndoStore      = [];

    private bool _forceFullItemOff = false;

    /// <summary> Whether an Undo for the given design is possible. </summary>
    public bool CanUndo(Design? design)
        => design != null && UndoStore.ContainsKey(design.Identifier);

    /// <inheritdoc/>
    public void ChangeCustomize(object data, CustomizeIndex idx, CustomizeValue value, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        var oldValue = design.DesignData.Customize[idx];

        switch (idx)
        {
            case CustomizeIndex.Race:
            case CustomizeIndex.BodyType:
                Glamourer.Log.Error("Somehow race or body type was changed in a design. This should not happen.");
                return;
            case CustomizeIndex.Clan:
            {
                var customize = design.DesignData.Customize;
                if (Customizations.ChangeClan(ref customize, (SubRace)value.Value) == 0)
                    return;
                if (!design.SetCustomize(Customizations, customize))
                    return;

                break;
            }
            case CustomizeIndex.Gender:
            {
                var customize = design.DesignData.Customize;
                if (Customizations.ChangeGender(ref customize, (Gender)(value.Value + 1)) == 0)
                    return;
                if (!design.SetCustomize(Customizations, customize))
                    return;

                break;
            }
            default:
                if (!Customizations.IsCustomizationValid(design.DesignData.Customize.Clan, design.DesignData.Customize.Gender,
                        design.DesignData.Customize.Face, idx, value)
                 || !design.GetDesignDataRef().Customize.Set(idx, value))
                    return;

                break;
        }

        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Changed customize {idx.ToDefaultName()} in design {design.Identifier} from {oldValue.Value} to {value.Value}.");
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Customize, design, (oldValue, value, idx));
    }

    /// <inheritdoc/>
    public void ChangeEntireCustomize(object data, in CustomizeArray customize, CustomizeFlag apply, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        var (newCustomize, applied, changed) = Customizations.Combine(design.DesignData.Customize, customize, apply, true);
        if (changed == 0)
            return;

        var oldCustomize = design.DesignData.Customize;
        design.SetCustomize(Customizations, newCustomize);
        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Changed entire customize with resulting flags {applied} and {changed}.");
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.EntireCustomize, design, (oldCustomize, applied, changed));
    }

    /// <inheritdoc/>
    public void ChangeCustomizeParameter(object data, CustomizeParameterFlag flag, CustomizeParameterValue value, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        var old = design.DesignData.Parameters[flag];
        if (!design.GetDesignDataRef().Parameters.Set(flag, value))
            return;

        var @new = design.DesignData.Parameters[flag];
        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Set customize parameter {flag} in design {design.Identifier} from {old} to {@new}.");
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Parameter, design, (old, @new, flag));
    }

    /// <inheritdoc/>
    public void ChangeItem(object data, EquipSlot slot, EquipItem item, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        switch (slot)
        {
            case EquipSlot.MainHand:
            {
                var currentMain = design.DesignData.Item(EquipSlot.MainHand);
                var currentOff  = design.DesignData.Item(EquipSlot.OffHand);
                if (!Items.IsItemValid(EquipSlot.MainHand, item.ItemId, out item))
                    return;

                if (!ChangeMainhandPeriphery(design, currentMain, currentOff, item, out var newOff, out var newGauntlets))
                    return;

                design.LastEdit = DateTimeOffset.UtcNow;
                SaveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.MainHand.ToName()} weapon in design {design.Identifier} from {currentMain.Name} ({currentMain.ItemId}) to {item.Name} ({item.ItemId}).");
                DesignChanged.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, item, newOff, newGauntlets));
                return;
            }
            case EquipSlot.OffHand:
            {
                var currentMain = design.DesignData.Item(EquipSlot.MainHand);
                var currentOff  = design.DesignData.Item(EquipSlot.OffHand);
                if (!Items.IsOffhandValid(currentOff.Type, item.ItemId, out item))
                    return;

                if (!design.GetDesignDataRef().SetItem(EquipSlot.OffHand, item))
                    return;

                design.LastEdit = DateTimeOffset.UtcNow;
                SaveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.OffHand.ToName()} weapon in design {design.Identifier} from {currentOff.Name} ({currentOff.ItemId}) to {item.Name} ({item.ItemId}).");
                DesignChanged.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, currentMain, item));
                return;
            }
            default:
            {
                if (!Items.IsItemValid(slot, item.Id, out item))
                    return;

                var old = design.DesignData.Item(slot);
                if (!design.GetDesignDataRef().SetItem(slot, item))
                    return;

                design.LastEdit = DateTimeOffset.UtcNow;
                Glamourer.Log.Debug(
                    $"Set {slot.ToName()} equipment piece in design {design.Identifier} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}).");
                SaveService.QueueSave(design);
                DesignChanged.Invoke(DesignChanged.Type.Equip, design, (old, item, slot));
                return;
            }
        }
    }

    /// <inheritdoc/>
    public void ChangeStain(object data, EquipSlot slot, StainId stain, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        if (Items.ValidateStain(stain, out var _, false).Length > 0)
            return;

        var oldStain = design.DesignData.Stain(slot);
        if (!design.GetDesignDataRef().SetStain(slot, stain))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set stain of {slot} equipment piece to {stain.Id}.");
        DesignChanged.Invoke(DesignChanged.Type.Stain, design, (oldStain, stain, slot));
    }

    /// <inheritdoc/>
    public void ChangeEquip(object data, EquipSlot slot, EquipItem? item, StainId? stain, ApplySettings _ = default)
    {
        if (item.HasValue)
            ChangeItem(data, slot, item.Value, _);
        if (stain.HasValue)
            ChangeStain(data, slot, stain.Value, _);
    }

    /// <inheritdoc/>
    public void ChangeCrest(object data, CrestFlag slot, bool crest, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        var oldCrest = design.DesignData.Crest(slot);
        if (!design.GetDesignDataRef().SetCrest(slot, crest))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set crest visibility of {slot} equipment piece to {crest}.");
        DesignChanged.Invoke(DesignChanged.Type.Crest, design, (oldCrest, crest, slot));
    }

    /// <inheritdoc/>
    public void ChangeMetaState(object data, MetaIndex metaIndex, bool value, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        if (!design.GetDesignDataRef().SetMeta(metaIndex, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set value of {metaIndex} to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.Other, design, (metaIndex, false, value));
    }

    /// <inheritdoc/>
    public void ApplyDesign(object data, MergedDesign other, ApplySettings _ = default)
        => ApplyDesign(data, other.Design);

    /// <inheritdoc/>
    public void ApplyDesign(object data, DesignBase other, ApplySettings _ = default)
    {
        if (data is not Design design)
            return;

        UndoStore[design.Identifier] = design.DesignData;
        foreach (var index in MetaExtensions.AllRelevant.Where(other.DoApplyMeta))
            design.GetDesignDataRef().SetMeta(index, other.DesignData.GetMeta(index));

        if (!design.DesignData.IsHuman)
            return;

        ChangeEntireCustomize(design, other.DesignData.Customize, other.ApplyCustomize);

        _forceFullItemOff = true;
        foreach (var slot in EquipSlotExtensions.FullSlots)
        {
            ChangeEquip(design, slot,
                other.DoApplyEquip(slot) ? other.DesignData.Item(slot) : null,
                other.DoApplyStain(slot) ? other.DesignData.Stain(slot) : null);
        }

        _forceFullItemOff = false;

        foreach (var slot in Enum.GetValues<CrestFlag>().Where(other.DoApplyCrest))
            ChangeCrest(design, slot, other.DesignData.Crest(slot));

        foreach (var parameter in CustomizeParameterExtensions.AllFlags.Where(other.DoApplyParameter))
            ChangeCustomizeParameter(design, parameter, other.DesignData.Parameters[parameter]);
    }

    /// <summary> Change a mainhand weapon and either fix or apply appropriate offhand and potentially gauntlets. </summary>
    private bool ChangeMainhandPeriphery(Design design, EquipItem currentMain, EquipItem currentOff, EquipItem newMain, out EquipItem? newOff,
        out EquipItem? newGauntlets)
    {
        newOff       = null;
        newGauntlets = null;
        if (newMain.Type != currentMain.Type)
        {
            var defaultOffhand = Items.GetDefaultOffhand(newMain);
            if (!Items.IsOffhandValid(newMain, defaultOffhand.ItemId, out var o))
                return false;

            newOff = o;
        }
        else if (!_forceFullItemOff && Config.ChangeEntireItem)
        {
            var defaultOffhand = Items.GetDefaultOffhand(newMain);
            if (Items.IsOffhandValid(newMain, defaultOffhand.ItemId, out var o))
                newOff = o;

            if (newMain.Type is FullEquipType.Fists && Items.ItemData.Tertiary.TryGetValue(newMain.ItemId, out var g))
                newGauntlets = g;
        }

        if (!design.GetDesignDataRef().SetItem(EquipSlot.MainHand, newMain))
            return false;

        if (newOff.HasValue && !design.GetDesignDataRef().SetItem(EquipSlot.OffHand, newOff.Value))
        {
            design.GetDesignDataRef().SetItem(EquipSlot.MainHand, currentMain);
            return false;
        }

        if (newGauntlets.HasValue && !design.GetDesignDataRef().SetItem(EquipSlot.Hands, newGauntlets.Value))
        {
            design.GetDesignDataRef().SetItem(EquipSlot.MainHand, currentMain);
            design.GetDesignDataRef().SetItem(EquipSlot.OffHand,  currentOff);
            return false;
        }

        return true;
    }
}
