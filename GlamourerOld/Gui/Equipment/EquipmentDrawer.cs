using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Data;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Widgets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public class EquipmentDrawer
{
    private readonly ItemManager                                         _items;
    private readonly FilterComboColors                                   _stainCombo;
    private readonly StainData                                           _stainData;
    private readonly ItemCombo[]                                         _itemCombo;
    private readonly Dictionary<(FullEquipType, EquipSlot), WeaponCombo> _weaponCombo;

    public EquipmentDrawer(DataManager gameData, ItemManager items)
    {
        _items     = items;
        _stainData = items.Stains;
        _stainCombo = new FilterComboColors(140,
            _stainData.Data.Prepend(new KeyValuePair<byte, (string Name, uint Dye, bool Gloss)>(0, ("None", 0, false))));
        _itemCombo   = EquipSlotExtensions.EqdpSlots.Select(e => new ItemCombo(gameData, items, e)).ToArray();
        _weaponCombo = new Dictionary<(FullEquipType, EquipSlot), WeaponCombo>(FullEquipTypeExtensions.WeaponTypes.Count * 2);
        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.ToSlot() is EquipSlot.MainHand)
                _weaponCombo.TryAdd((type, EquipSlot.MainHand), new WeaponCombo(gameData, items, type, EquipSlot.MainHand));
            else if (type.ToSlot() is EquipSlot.OffHand)
                _weaponCombo.TryAdd((type, EquipSlot.OffHand), new WeaponCombo(gameData, items, type, EquipSlot.OffHand));

            var offhand = type.Offhand();
            if (offhand is not FullEquipType.Unknown && !_weaponCombo.ContainsKey((offhand, EquipSlot.OffHand)))
                _weaponCombo.TryAdd((offhand, EquipSlot.OffHand), new WeaponCombo(gameData, items, type, EquipSlot.OffHand));
        }

        _weaponCombo.Add((FullEquipType.Unknown, EquipSlot.MainHand), new WeaponCombo(gameData, items, FullEquipType.Unknown, EquipSlot.MainHand));
    }

    private string VerifyRestrictedGear(Item gear, EquipSlot slot, Gender gender, Race race)
    {
        if (slot.IsAccessory())
            return gear.Name;

        var (changed, _) = _items.ResolveRestrictedGear(gear.Model, slot, race, gender);
        if (changed)
            return gear.Name + " (Restricted)";

        return gear.Name;
    }

    public bool DrawArmor(Item current, EquipSlot slot, out Item armor, Gender gender = Gender.Unknown, Race race = Race.Unknown)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory(), $"Called {nameof(DrawArmor)} on {slot}.");
        var combo = _itemCombo[slot.ToIndex()];
        armor = current;
        var change = combo.Draw(VerifyRestrictedGear(armor, slot, gender, race), armor.ItemId, 320 * ImGuiHelpers.GlobalScale);
        if (armor.ModelBase.Value != 0)
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change = true;
                armor  = ItemManager.NothingItem(slot);
            }
            else if (change)
            {
                armor = combo.CurrentSelection.WithStain(armor.Stain);
            }
        }
        else if (change)
        {
            armor = combo.CurrentSelection.WithStain(armor.Stain);
        }

        return change;
    }

    public bool DrawStain(StainId current, EquipSlot slot, out Stain stain)
    {
        var found = _stainData.TryGetValue(current, out stain);
        if (!_stainCombo.Draw($"##stain{slot}", stain.RgbaColor, stain.Name, found))
            return false;

        return _stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain);
    }

    public bool DrawMainhand(Weapon current, bool drawAll, out Weapon weapon)
    {
        weapon = current;
        if (!_weaponCombo.TryGetValue((drawAll ? FullEquipType.Unknown : current.Type, EquipSlot.MainHand), out var combo))
            return false;

        if (!combo.Draw(weapon.Name, weapon.ItemId, 320 * ImGuiHelpers.GlobalScale))
            return false;

        weapon = combo.CurrentSelection.WithStain(current.Stain);
        return true;
    }

    public bool DrawOffhand(Weapon current, FullEquipType mainType, out Weapon weapon)
    {
        weapon = current;
        var offType = mainType.Offhand();
        if (offType == FullEquipType.Unknown)
            return false;

        if (!_weaponCombo.TryGetValue((offType, EquipSlot.OffHand), out var combo))
            return false;

        var change = combo.Draw(weapon.Name, weapon.ItemId, 320 * ImGuiHelpers.GlobalScale);
        if (offType.ToSlot() is EquipSlot.OffHand && weapon.ModelBase.Value != 0)
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change = true;
                weapon = ItemManager.NothingItem(offType);
            }
        }
        else if (change)
        {
            weapon = combo.CurrentSelection.WithStain(current.Stain);
        }

        return change;
    }

    public bool DrawApply(Design design, EquipSlot slot, out bool enabled)
        => DrawCheckbox($"##apply{slot}", design.DoApplyEquip(slot), out enabled);

    public bool DrawApplyStain(Design design, EquipSlot slot, out bool enabled)
        => DrawCheckbox($"##applyStain{slot}", design.DoApplyStain(slot), out enabled);

    private static bool DrawCheckbox(string label, bool value, out bool on)
    {
        var ret = ImGuiUtil.Checkbox(label, string.Empty, value, v => value = v);
        on = value;
        return ret;
    }

    public bool DrawVisor(Design design, out bool on)
        => DrawCheckbox("##visorToggled", design.Visor.ForcedValue, out on);

    public bool DrawVisor(ActiveDesign design, out bool on)
        => DrawCheckbox("##visorToggled", design.IsVisorToggled, out on);

    public bool DrawHat(Design design, out bool on)
        => DrawCheckbox("##hatVisible", design.Hat.ForcedValue, out on);

    public bool DrawHat(ActiveDesign design, out bool on)
        => DrawCheckbox("##hatVisible", design.IsHatVisible, out on);

    public bool DrawWeapon(Design design, out bool on)
        => DrawCheckbox("##weaponVisible", design.Weapon.ForcedValue, out on);

    public bool DrawWeapon(ActiveDesign design, out bool on)
        => DrawCheckbox("##weaponVisible", design.IsWeaponVisible, out on);

    public bool DrawWetness(Design design, out bool on)
        => DrawCheckbox("##wetness", design.Wetness.ForcedValue, out on);

    public bool DrawWetness(ActiveDesign design, out bool on)
        => DrawCheckbox("##wetnessVisible", design.IsWet, out on);

    public bool DrawApplyVisor(Design design, out bool on)
        => DrawCheckbox("##applyVisor", design.Visor.Enabled, out on);

    public bool DrawApplyWetness(Design design, out bool on)
        => DrawCheckbox("##applyWetness", design.Wetness.Enabled, out on);

    public bool DrawApplyHatState(Design design, out bool on)
        => DrawCheckbox("##applyHatState", design.Hat.Enabled, out on);

    public bool DrawApplyWeaponState(Design design, out bool on)
        => DrawCheckbox("##applyWeaponState", design.Weapon.Enabled, out on);
}
