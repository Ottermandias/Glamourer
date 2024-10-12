using Glamourer.Designs.History;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Material;
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

    private bool _forceFullItemOff;

    /// <summary> Whether an Undo for the given design is possible. </summary>
    public bool CanUndo(Design? design)
        => design != null && UndoStore.ContainsKey(design.Identifier);

    /// <inheritdoc/>
    public void ChangeCustomize(object data, CustomizeIndex idx, CustomizeValue value, ApplySettings _ = default)
    {
        var design   = (Design)data;
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
        DesignChanged.Invoke(DesignChanged.Type.Customize, design, new CustomizeTransaction(idx, oldValue, value));
    }

    /// <inheritdoc/>
    public void ChangeEntireCustomize(object data, in CustomizeArray customize, CustomizeFlag apply, ApplySettings _ = default)
    {
        var design = (Design)data;
        var (newCustomize, applied, changed) = Customizations.Combine(design.DesignData.Customize, customize, apply, true);
        if (changed == 0)
            return;

        var oldCustomize = design.DesignData.Customize;
        design.SetCustomize(Customizations, newCustomize);
        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Changed entire customize with resulting flags {applied} and {changed}.");
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.EntireCustomize, design, new EntireCustomizeTransaction(changed, oldCustomize, newCustomize));
    }

    /// <inheritdoc/>
    public void ChangeCustomizeParameter(object data, CustomizeParameterFlag flag, CustomizeParameterValue value, ApplySettings _ = default)
    {
        var design = (Design)data;
        var old    = design.DesignData.Parameters[flag];
        if (!design.GetDesignDataRef().Parameters.Set(flag, value))
            return;

        var @new = design.DesignData.Parameters[flag];
        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Set customize parameter {flag} in design {design.Identifier} from {old} to {@new}.");
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Parameter, design, new ParameterTransaction(flag, old, @new));
    }

    /// <inheritdoc/>
    public void ChangeItem(object data, EquipSlot slot, EquipItem item, ApplySettings _ = default)
    {
        var design = (Design)data;
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

                var currentGauntlets = design.DesignData.Item(EquipSlot.Hands);
                design.LastEdit = DateTimeOffset.UtcNow;
                SaveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.MainHand.ToName()} weapon in design {design.Identifier} from {currentMain.Name} ({currentMain.ItemId}) to {item.Name} ({item.ItemId}).");
                DesignChanged.Invoke(DesignChanged.Type.Weapon, design,
                    new WeaponTransaction(currentMain, currentOff, currentGauntlets, item, newOff ?? currentOff,
                        newGauntlets ?? currentGauntlets));
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

                var currentGauntlets = design.DesignData.Item(EquipSlot.Hands);
                design.LastEdit = DateTimeOffset.UtcNow;
                SaveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.OffHand.ToName()} weapon in design {design.Identifier} from {currentOff.Name} ({currentOff.ItemId}) to {item.Name} ({item.ItemId}).");
                DesignChanged.Invoke(DesignChanged.Type.Weapon, design,
                    new WeaponTransaction(currentMain, currentOff, currentGauntlets, currentMain, item, currentGauntlets));
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
                DesignChanged.Invoke(DesignChanged.Type.Equip, design, new EquipTransaction(slot, old, item));
                return;
            }
        }
    }

    /// <inheritdoc/>
    public void ChangeBonusItem(object data, BonusItemFlag slot, EquipItem item, ApplySettings settings = default)
    {
        var design = (Design)data;
        if (item.Type.ToBonus() != slot)
            return;

        var oldItem = design.DesignData.BonusItem(slot);
        if (!design.GetDesignDataRef().SetBonusItem(slot, item))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set {slot} bonus item to {item}.");
        DesignChanged.Invoke(DesignChanged.Type.BonusItem, design, new BonusItemTransaction(slot, oldItem, item));
    }

    /// <inheritdoc/>
    public void ChangeStains(object data, EquipSlot slot, StainIds stains, ApplySettings _ = default)
    {
        var design = (Design)data;
        if (Items.ValidateStain(stains, out var _, false).Length > 0)
            return;

        var oldStain = design.DesignData.Stain(slot);
        if (!design.GetDesignDataRef().SetStain(slot, stains))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set stain of {slot} equipment piece to {stains}.");
        DesignChanged.Invoke(DesignChanged.Type.Stains, design, new StainTransaction(slot, oldStain, stains));
    }

    /// <inheritdoc/>
    public void ChangeEquip(object data, EquipSlot slot, EquipItem? item, StainIds? stains, ApplySettings _ = default)
    {
        if (item.HasValue)
            ChangeItem(data, slot, item.Value, _);
        if (stains.HasValue)
            ChangeStains(data, slot, stains.Value, _);
    }

    /// <inheritdoc/>
    public void ChangeCrest(object data, CrestFlag slot, bool crest, ApplySettings _ = default)
    {
        var design   = (Design)data;
        var oldCrest = design.DesignData.Crest(slot);
        if (!design.GetDesignDataRef().SetCrest(slot, crest))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set crest visibility of {slot} equipment piece to {crest}.");
        DesignChanged.Invoke(DesignChanged.Type.Crest, design, new CrestTransaction(slot, oldCrest, crest));
    }

    /// <inheritdoc/>
    public void ChangeMetaState(object data, MetaIndex metaIndex, bool value, ApplySettings _ = default)
    {
        var design = (Design)data;
        if (!design.GetDesignDataRef().SetMeta(metaIndex, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set value of {metaIndex} to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.Other, design, new MetaTransaction(metaIndex, !value, value));
    }

    public void ChangeMaterialRevert(Design design, MaterialValueIndex index, bool revert)
    {
        var materials = design.GetMaterialDataRef();
        if (!materials.TryGetValue(index, out var oldValue))
            return;

        materials.AddOrUpdateValue(index, oldValue with { Revert = revert });
        Glamourer.Log.Debug($"Changed advanced dye value for {index} to {(revert ? "Revert." : "no longer Revert.")}");
        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.MaterialRevert, design, new MaterialRevertTransaction(index, !revert, revert));
    }

    public void ChangeMaterialValue(Design design, MaterialValueIndex index, ColorRow? row)
    {
        var materials = design.GetMaterialDataRef();
        if (materials.TryGetValue(index, out var oldValue))
        {
            if (!row.HasValue)
            {
                materials.RemoveValue(index);
                Glamourer.Log.Debug($"Removed advanced dye value for {index}.");
            }
            else if (!row.Value.NearEqual(oldValue.Value))
            {
                materials.UpdateValue(index, new MaterialValueDesign(row.Value, oldValue.Enabled, oldValue.Revert), out _);
                Glamourer.Log.Debug($"Updated advanced dye value for {index} to new value.");
            }
            else
            {
                return;
            }
        }
        else
        {
            if (!row.HasValue)
                return;
            if (!materials.TryAddValue(index, new MaterialValueDesign(row.Value, true, false)))
                return;

            Glamourer.Log.Debug($"Added new advanced dye value for {index}.");
        }

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.DelaySave(design);
        DesignChanged.Invoke(DesignChanged.Type.Material, design, new MaterialTransaction(index, oldValue.Value, row));
    }

    public void ChangeApplyMaterialValue(Design design, MaterialValueIndex index, bool value)
    {
        var materials = design.GetMaterialDataRef();
        if (!materials.TryGetValue(index, out var oldValue) || oldValue.Enabled == value)
            return;

        materials.AddOrUpdateValue(index, oldValue with { Enabled = value });
        Glamourer.Log.Debug($"Changed application of advanced dye for {index} to {value}.");
        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        DesignChanged.Invoke(DesignChanged.Type.ApplyMaterial, design, new ApplicationTransaction(index, !value, value));
    }


    /// <inheritdoc/>
    public void ApplyDesign(object data, MergedDesign other, ApplySettings settings = default)
        => ApplyDesign(data, other.Design, settings);

    /// <inheritdoc/>
    public void ApplyDesign(object data, DesignBase other, ApplySettings _ = default)
    {
        var design = (Design)data;
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

        foreach (var slot in BonusExtensions.AllFlags)
        {
            if (other.DoApplyBonusItem(slot))
                ChangeBonusItem(design, slot, other.DesignData.BonusItem(slot));
        }

        foreach (var slot in Enum.GetValues<CrestFlag>().Where(other.DoApplyCrest))
            ChangeCrest(design, slot, other.DesignData.Crest(slot));

        foreach (var parameter in CustomizeParameterExtensions.AllFlags.Where(other.DoApplyParameter))
            ChangeCustomizeParameter(design, parameter, other.DesignData.Parameters[parameter]);

        foreach (var (key, value) in other.Materials)
        {
            if (!value.Enabled)
                continue;

            design.GetMaterialDataRef().AddOrUpdateValue(MaterialValueIndex.FromKey(key), value);
        }
    }

    /// <summary> Change a mainhand weapon and either fix or apply appropriate offhand and potentially gauntlets. </summary>
    private bool ChangeMainhandPeriphery(DesignBase design, EquipItem currentMain, EquipItem currentOff, EquipItem newMain,
        out EquipItem? newOff,
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
        else if (!_forceFullItemOff && Config.ChangeEntireItem && newMain.Type is not FullEquipType.Sword) // Skip applying shields.
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
