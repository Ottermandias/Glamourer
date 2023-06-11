using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.PlayerWatch;

namespace Glamourer.Gui;

internal partial class Interface
{
    private bool DrawStainSelector(ComboWithFilter<Stain> stainCombo, EquipSlot slot, StainId stainIdx)
    {
        stainCombo.PostPreview = null;
        if (_stains.TryGetValue((byte)stainIdx, out var stain))
        {
            var previewPush = PushColor(stain, ImGuiCol.FrameBg);
            stainCombo.PostPreview = () => ImGui.PopStyleColor(previewPush);
        }

        var change = stainCombo.Draw(string.Empty, out var newStain) && !newStain.RowIndex.Equals(stainIdx);
        if (!change && (byte)stainIdx != 0)
        {
            ImGuiCustom.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change   = true;
                newStain = Stain.None;
            }
        }

        if (!change)
            return false;

        if (_player == null)
            return _inDesignMode && (_selection?.Data.WriteStain(slot, newStain.RowIndex) ?? false);

        Glamourer.RevertableDesigns.Add(_player);
        newStain.Write(_player.Address, slot);
        return true;
    }

    private bool DrawGlobalStainSelector(ComboWithFilter<Stain> stainCombo)
    {
        stainCombo.PostPreview = null;

        var change = stainCombo.Draw(string.Empty, out var newStain);
        ImGui.SameLine();
        ImGui.TextUnformatted("Dye All Slots");
        if (!change)
        {
            ImGuiCustom.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change   = true;
                newStain = Stain.None;
            }
        }

        if (!change)
            return false;

        if (_player == null)
        {
            foreach (var key in GetEquipSlotNames().Keys)
            {
                if (key is EquipSlot.OffHand && _selection?.Data.Equipment.OffHand.Set.Value == 0)
                    continue;

                _selection?.Data.WriteStain(key, newStain.RowIndex);
            }

            return _inDesignMode;
        }

        Glamourer.RevertableDesigns.Add(_player);
        foreach (var key in GetEquipSlotNames().Keys)
            newStain.Write(_player.Address, key);

        return true;
    }

    private bool DrawItemSelector(ComboWithFilter<Item> equipCombo, EquipItem item, EquipSlot slot = EquipSlot.Unknown)
    {
        var currentName = item.Name;
        var change      = equipCombo.Draw(currentName, out var newItem, _itemComboWidth) && newItem.Base.RowId != item.Id;
        if (!change && item.Name != SmallClothes.Name)
        {
            ImGuiCustom.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change  = true;
                newItem = Item.Nothing(slot);
            }
        }

        if (!change)
            return false;

        newItem = new Item(newItem.Base, newItem.Name, slot);
        if (_player == null)
            return _inDesignMode && (_selection?.Data.WriteItem(newItem) ?? false);

        Glamourer.RevertableDesigns.Add(_player);
        newItem.Write(_player.Address);
        return true;
    }

    private static bool DrawCheckbox(CharacterEquipMask flag, ref CharacterEquipMask mask)
    {
        var tmp = (uint)mask;
        var ret = false;
        if (ImGui.CheckboxFlags($"##flag_{(uint)flag}", ref tmp, (uint)flag) && tmp != (uint)mask)
        {
            mask = (CharacterEquipMask)tmp;
            ret  = true;
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Enable writing this slot in this save.");
        return ret;
    }

    private static readonly EquipItem SmallClothes    = new("Nothing", 0, 0, 0, 0, 0, 0, EquipSlot.Unknown);
    private static readonly EquipItem SmallClothesNpc = new("Smallclothes (NPC)", 1, 0, 9903, 0, 1, 0, EquipSlot.Unknown);
    private static readonly EquipItem Unknown         = new("Unknown", 3, 0, 0, 0, 0, 0, EquipSlot.Unknown);

    private EquipItem Identify(SetId set, WeaponType weapon, ushort variant, EquipSlot slot)
    {
        return (uint)set switch
        {
            0    => SmallClothes,
            9903 => SmallClothesNpc,
            _    => ToItem(_identifier.Identify(set, weapon, variant, slot.ToSlot())),
        };
    }

    private static EquipItem ToItem(IEnumerable<EquipItem> items)
    {
        var item = items.FirstOrDefault();
        if (item.Valid)
            return item;

        return Unknown;
    }

    private bool DrawEquipSlot(EquipSlot slot, CharacterArmor equip)
    {
        var (equipCombo, stainCombo) = _combos[slot];

        var ret = DrawStainSelector(stainCombo, slot, equip.Stain);
        ImGui.SameLine();
        var item = Identify(equip.Set, new WeaponType(), equip.Variant, slot);
        if (item.Name == Unknown.Name)
            item = new EquipItem($"Unknown ({item.ModelId.Value}, {item.Variant})", 0, 0, equip.Set, 0, equip.Variant,
                FullEquipType.Unknown, slot);
        ret |= DrawItemSelector(equipCombo, item, slot);

        return ret;
    }

    private bool DrawEquipSlotWithCheck(EquipSlot slot, CharacterArmor equip, CharacterEquipMask flag, ref CharacterEquipMask mask)
    {
        var ret = DrawCheckbox(flag, ref mask);
        ImGui.SameLine();
        ret |= DrawEquipSlot(slot, equip);
        return ret;
    }

    private bool DrawWeapon(EquipSlot slot, CharacterWeapon weapon)
    {
        var (equipCombo, stainCombo) = _combos[slot];

        var ret = DrawStainSelector(stainCombo, slot, weapon.Stain);
        ImGui.SameLine();
        var item = Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
        if (item.Name == Unknown.Name)
            item = new EquipItem($"Unknown ({weapon.Set.Value}, {weapon.Type.Value}, {weapon.Variant})", 0, 0, weapon.Set, weapon.Type, (byte) weapon.Variant, FullEquipType.Unknown, slot);
        ret |= DrawItemSelector(equipCombo, item, slot);

        return ret;
    }

    private bool DrawWeaponWithCheck(EquipSlot slot, CharacterWeapon weapon, CharacterEquipMask flag, ref CharacterEquipMask mask)
    {
        var ret = DrawCheckbox(flag, ref mask);
        ImGui.SameLine();
        ret |= DrawWeapon(slot, weapon);
        return ret;
    }

    private bool DrawEquip(CharacterEquipment equip)
    {
        var ret = false;
        if (ImGui.CollapsingHeader("Character Equipment"))
        {
            ret |= DrawGlobalStainSelector(_globalStainCombo);
            ret |= DrawWeapon(EquipSlot.MainHand, equip.MainHand);
            ret |= DrawWeapon(EquipSlot.OffHand,  equip.OffHand);
            ret |= DrawEquipSlot(EquipSlot.Head,    equip.Head);
            ret |= DrawEquipSlot(EquipSlot.Body,    equip.Body);
            ret |= DrawEquipSlot(EquipSlot.Hands,   equip.Hands);
            ret |= DrawEquipSlot(EquipSlot.Legs,    equip.Legs);
            ret |= DrawEquipSlot(EquipSlot.Feet,    equip.Feet);
            ret |= DrawEquipSlot(EquipSlot.Ears,    equip.Ears);
            ret |= DrawEquipSlot(EquipSlot.Neck,    equip.Neck);
            ret |= DrawEquipSlot(EquipSlot.Wrists,  equip.Wrists);
            ret |= DrawEquipSlot(EquipSlot.RFinger, equip.RFinger);
            ret |= DrawEquipSlot(EquipSlot.LFinger, equip.LFinger);
        }

        return ret;
    }

    private bool DrawEquip(CharacterEquipment equip, ref CharacterEquipMask mask)
    {
        var ret = false;
        if (ImGui.CollapsingHeader("Character Equipment"))
        {
            ret |= DrawGlobalStainSelector(_globalStainCombo);
            ret |= DrawWeaponWithCheck(EquipSlot.MainHand, equip.MainHand, CharacterEquipMask.MainHand, ref mask);
            ret |= DrawWeaponWithCheck(EquipSlot.OffHand,  equip.OffHand,  CharacterEquipMask.OffHand,  ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Head,    equip.Head,    CharacterEquipMask.Head,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Body,    equip.Body,    CharacterEquipMask.Body,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Hands,   equip.Hands,   CharacterEquipMask.Hands,   ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Legs,    equip.Legs,    CharacterEquipMask.Legs,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Feet,    equip.Feet,    CharacterEquipMask.Feet,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Ears,    equip.Ears,    CharacterEquipMask.Ears,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Neck,    equip.Neck,    CharacterEquipMask.Neck,    ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.Wrists,  equip.Wrists,  CharacterEquipMask.Wrists,  ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.RFinger, equip.RFinger, CharacterEquipMask.RFinger, ref mask);
            ret |= DrawEquipSlotWithCheck(EquipSlot.LFinger, equip.LFinger, CharacterEquipMask.LFinger, ref mask);
        }

        return ret;
    }
}
