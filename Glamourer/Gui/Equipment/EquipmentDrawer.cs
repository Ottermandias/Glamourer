using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Data;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Widgets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public class EquipmentDrawer
{
    private readonly ItemManager                            _items;
    private readonly FilterComboColors                      _stainCombo;
    private readonly StainData                              _stainData;
    private readonly ItemCombo[]                            _itemCombo;
    private readonly Dictionary<FullEquipType, WeaponCombo> _weaponCombo;

    public EquipmentDrawer(DataManager gameData, ItemManager items)
    {
        _items     = items;
        _stainData = items.Stains;
        _stainCombo = new FilterComboColors(140,
            _stainData.Data.Prepend(new KeyValuePair<byte, (string Name, uint Dye, bool Gloss)>(0, ("None", 0, false))));
        _itemCombo   = EquipSlotExtensions.EqdpSlots.Select(e => new ItemCombo(gameData, items, e)).ToArray();
        _weaponCombo = new Dictionary<FullEquipType, WeaponCombo>(FullEquipTypeExtensions.WeaponTypes.Count * 2);
        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.ToSlot() is EquipSlot.MainHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type));
            else if (type.ToSlot() is EquipSlot.OffHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type));
        }

        _weaponCombo.Add(FullEquipType.Unknown, new WeaponCombo(items, FullEquipType.Unknown));
    }

    private string VerifyRestrictedGear(EquipItem gear, EquipSlot slot, Gender gender, Race race)
    {
        if (slot.IsAccessory())
            return gear.Name;

        var (changed, _) = _items.ResolveRestrictedGear(gear.Armor(), slot, race, gender);
        if (changed)
            return gear.Name + " (Restricted)";

        return gear.Name;
    }

    public bool DrawArmor(EquipItem current, EquipSlot slot, out EquipItem armor, Gender gender = Gender.Unknown, Race race = Race.Unknown)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory(), $"Called {nameof(DrawArmor)} on {slot}.");
        var combo = _itemCombo[slot.ToIndex()];
        armor = current;
        var change = combo.Draw(VerifyRestrictedGear(armor, slot, gender, race), armor.Id, 320 * ImGuiHelpers.GlobalScale);
        if (armor.ModelId.Value != 0)
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change = true;
                armor  = ItemManager.NothingItem(slot);
            }
            else if (change)
            {
                armor = combo.CurrentSelection;
            }
        }
        else if (change)
        {
            armor = combo.CurrentSelection;
        }

        return change;
    }

    public bool DrawStain(StainId current, EquipSlot slot, out Stain stain)
    {
        var found  = _stainData.TryGetValue(current, out stain);
        var change = _stainCombo.Draw($"##stain{slot}", stain.RgbaColor, stain.Name, found);
        ImGuiUtil.HoverTooltip("Right-click to clear.");
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            stain = Stain.None;
            return true;
        }

        return change && _stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain);
    }

    public bool DrawMainhand(EquipItem current, bool drawAll, out EquipItem weapon)
    {
        weapon = current;
        if (!_weaponCombo.TryGetValue(drawAll ? FullEquipType.Unknown : current.Type, out var combo))
            return false;

        if (!combo.Draw(weapon.Name, weapon.Id, 320 * ImGuiHelpers.GlobalScale))
            return false;

        weapon = combo.CurrentSelection;
        return true;
    }

    public bool DrawOffhand(EquipItem current, FullEquipType mainType, out EquipItem weapon)
    {
        weapon = current;
        var offType = mainType.Offhand();
        if (offType == FullEquipType.Unknown)
            return false;

        if (!_weaponCombo.TryGetValue(offType, out var combo))
            return false;

        var change = combo.Draw(weapon.Name, weapon.Id, 320 * ImGuiHelpers.GlobalScale);
        if (!offType.IsOffhandType() && weapon.ModelId.Value != 0)
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
            weapon = combo.CurrentSelection;
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

    public bool DrawVisor(bool current, out bool on)
        => DrawCheckbox("##visorToggled", current, out on);

    public bool DrawHat(bool current, out bool on)
        => DrawCheckbox("##hatVisible", current, out on);

    public bool DrawWeapon(bool current, out bool on)
        => DrawCheckbox("##weaponVisible", current, out on);

    public bool DrawWetness(bool current, out bool on)
        => DrawCheckbox("##wetness", current, out on);
}
