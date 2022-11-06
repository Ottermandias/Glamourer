using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Item = Glamourer.Structs.Item;

namespace Glamourer.Gui.Equipment;

public partial class EquipmentDrawer
{
    public const int ItemComboWidth = 320;

    private sealed class ItemCombo : FilterComboBase<Item>
    {
        public readonly string    Label;
        public readonly EquipSlot Slot;

        public  Lumina.Excel.GeneratedSheets.Item? LastItem;
        private CharacterArmor                     _lastArmor;
        private string                             _lastPreview = string.Empty;
        private int                                _lastIndex;

        public ItemCombo(EquipSlot slot)
            : base(GetItems(slot), false)
        {
            Label = GetLabel(slot);
            Slot  = slot;
        }

        protected override string ToString(Item obj)
            => obj.Name;

        private static string GetLabel(EquipSlot slot)
        {
            var sheet = Dalamud.GameData.GetExcelSheet<Addon>()!;

            return slot switch
            {
                EquipSlot.Head    => sheet.GetRow(740)?.Text.ToString() ?? "Head",
                EquipSlot.Body    => sheet.GetRow(741)?.Text.ToString() ?? "Body",
                EquipSlot.Hands   => sheet.GetRow(742)?.Text.ToString() ?? "Hands",
                EquipSlot.Legs    => sheet.GetRow(744)?.Text.ToString() ?? "Legs",
                EquipSlot.Feet    => sheet.GetRow(745)?.Text.ToString() ?? "Feet",
                EquipSlot.Ears    => sheet.GetRow(746)?.Text.ToString() ?? "Ears",
                EquipSlot.Neck    => sheet.GetRow(747)?.Text.ToString() ?? "Neck",
                EquipSlot.Wrists  => sheet.GetRow(748)?.Text.ToString() ?? "Wrists",
                EquipSlot.RFinger => sheet.GetRow(749)?.Text.ToString() ?? "Right Ring",
                EquipSlot.LFinger => sheet.GetRow(750)?.Text.ToString() ?? "Left Ring",
                _                 => string.Empty,
            };
        }

        public bool Draw(CharacterArmor armor, out int newIdx)
        {
            UpdateItem(armor);
            newIdx = _lastIndex;
            return Draw(Label, _lastPreview, ref newIdx, ItemComboWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight());
        }

        private void UpdateItem(CharacterArmor armor)
        {
            if (armor.Equals(_lastArmor))
                return;

            _lastArmor   = armor;
            LastItem     = Identify(armor.Set, 0, armor.Variant, Slot);
            _lastIndex   = Items.IndexOf(i => i.Base.RowId == LastItem.RowId);
            _lastPreview = _lastIndex >= 0 ? Items[_lastIndex].Name : LastItem.Name.ToString();
        }

        private static IReadOnlyList<Item> GetItems(EquipSlot slot)
            => GameData.ItemsBySlot(Dalamud.GameData).TryGetValue(slot, out var list) ? list : Array.Empty<Item>();
    }

    private sealed class WeaponCombo : FilterComboBase<Item>
    {
        public readonly string    Label;
        public readonly EquipSlot Slot;

        public  Lumina.Excel.GeneratedSheets.Item? LastItem;
        private CharacterWeapon                    _lastWeapon  = new(ulong.MaxValue);
        private string                             _lastPreview = string.Empty;
        private int                                _lastIndex;
        public  WeaponCategory                     LastCategory { get; private set; }
        private bool                               _drawAll;

        public WeaponCombo(EquipSlot slot)
            : base(GetItems(slot), false)
        {
            Label = GetLabel(slot);
            Slot  = slot;
        }

        protected override string ToString(Item obj)
            => obj.Name;

        private static string GetLabel(EquipSlot slot)
        {
            var sheet = Dalamud.GameData.GetExcelSheet<Addon>()!;
            return slot switch
            {
                EquipSlot.MainHand => sheet.GetRow(738)?.Text.ToString() ?? "Main Hand",
                EquipSlot.OffHand  => sheet.GetRow(739)?.Text.ToString() ?? "Off Hand",
                _                  => string.Empty,
            };
        }

        public bool Draw(CharacterWeapon weapon, out int newIdx, bool drawAll)
        {
            if (drawAll != _drawAll)
            {
                _drawAll = drawAll;
                ResetFilter();
            }

            UpdateItem(weapon);
            UpdateCategory((WeaponCategory?)LastItem!.ItemUICategory?.Row ?? WeaponCategory.Unknown);
            newIdx = _lastIndex;
            return Draw(Label, _lastPreview, ref newIdx, ItemComboWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight());
        }

        public bool Draw(CharacterWeapon weapon, WeaponCategory category, out int newIdx)
        {
            if (_drawAll)
            {
                _drawAll = false;
                ResetFilter();
            }

            UpdateItem(weapon);
            UpdateCategory(category);
            newIdx = _lastIndex;
            return Draw(Label, _lastPreview, ref newIdx, ItemComboWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight());
        }

        //protected override bool DrawSelectable(int globalIdx, bool selected)
        //{
        //    using var _ = ImRaii.Group();
        //    
        //}

        protected override bool IsVisible(int globalIndex, LowerString filter)
        {
            var item = Items[globalIndex];
            return (_drawAll || item.WeaponCategory == LastCategory) && filter.IsContained(item.Name);
        }

        private void UpdateItem(CharacterWeapon weapon)
        {
            if (weapon.Equals(_lastWeapon))
                return;

            _lastWeapon  = weapon;
            LastItem     = Identify(weapon.Set, weapon.Type, weapon.Variant, Slot);
            _lastIndex   = LastItem.RowId == 0 ? -1 : Items.IndexOf(i => i.Base.RowId == LastItem.RowId);
            _lastPreview = _lastIndex >= 0 ? Items[_lastIndex].Name : LastItem.Name.ToString();
        }

        private void UpdateCategory(WeaponCategory category)
        {
            if (category == LastCategory)
                return;

            LastCategory = category;
            ResetFilter();
        }

        private static IReadOnlyList<Item> GetItems(EquipSlot slot)
            => GameData.ItemsBySlot(Dalamud.GameData).TryGetValue(EquipSlot.MainHand, out var list) ? list : Array.Empty<Item>();
    }

    private static readonly IObjectIdentifier        Identifier;
    private static readonly IReadOnlyList<ItemCombo> ItemCombos;
    private static readonly WeaponCombo              MainHandCombo;
    private static readonly WeaponCombo              OffHandCombo;

    private void DrawItemSelector()
    {
        var combo   = ItemCombos[(int)_currentSlotIdx];
        var change  = combo.Draw(_currentArmor, out var idx);
        var newItem = change ? ToArmor(combo.Items[idx], _currentArmor.Stain) : CharacterArmor.Empty;
        if (!change && !ReferenceEquals(combo.LastItem, SmallClothes))
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                change = true;
        }

        if (!change)
            return;

        _currentArmor = newItem;
        UpdateActors();
    }

    private static CharacterArmor ToArmor(Item item, StainId stain)
    {
        var (id, _, variant) = item.MainModel;
        return new CharacterArmor(id, (byte)variant, stain);
    }

    private static CharacterWeapon ToWeapon(Item item, StainId stain)
    {
        var (id, type, variant) = item.MainModel;
        return new CharacterWeapon(id, type, variant, stain);
    }

    private void DrawMainHandSelector(ref CharacterWeapon mainHand)
    {
        if (!MainHandCombo.Draw(mainHand, out var newIdx, false))
            return;

        mainHand = ToWeapon(MainHandCombo.Items[newIdx], mainHand.Stain);
        foreach (var actor in _actors)
            Glamourer.RedrawManager.LoadWeapon(actor, _currentSlot, mainHand);
    }

    private void DrawOffHandSelector(ref CharacterWeapon offHand, WeaponCategory category)
    {
        var change    = OffHandCombo.Draw(offHand, category, out var newIdx);
        var newWeapon = change ? ToWeapon(OffHandCombo.Items[newIdx], offHand.Stain) : CharacterWeapon.Empty;
        if (!change && !ReferenceEquals(OffHandCombo.LastItem, SmallClothes))
        {
            ImGuiUtil.HoverTooltip("Right-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                change = true;
        }

        if (!change)
            return;

        offHand = newWeapon;
        foreach (var actor in _actors)
            Glamourer.RedrawManager.LoadWeapon(actor, _currentSlot, offHand);
    }
    //private bool DrawEquipSlot(EquipSlot slot, CharacterArmor equip)
    //{
    //    var (equipCombo, stainCombo) = _combos[slot];
    //
    //    var ret = DrawStainSelector(stainCombo, slot, equip.Stain);
    //    ImGui.SameLine();
    //    var item = Identify(equip.Set, new WeaponType(), equip.Variant, slot);
    //    ret |= DrawItemSelector(equipCombo, item, slot);
    //
    //    return ret;
    //}
    //
    //private bool DrawEquipSlotWithCheck(EquipSlot slot, CharacterArmor equip, CharacterEquipMask flag, ref CharacterEquipMask mask)
    //{
    //    var ret = DrawCheckbox(flag, ref mask);
    //    ImGui.SameLine();
    //    ret |= DrawEquipSlot(slot, equip);
    //    return ret;
    //}
    //
    //private bool DrawWeapon(EquipSlot slot, CharacterWeapon weapon)
    //{
    //    var (equipCombo, stainCombo) = _combos[slot];
    //
    //    var ret = DrawStainSelector(stainCombo, slot, weapon.Stain);
    //    ImGui.SameLine();
    //    var item = Identify(weapon.Set, weapon.Type, weapon.Variant, slot);
    //    ret |= DrawItemSelector(equipCombo, item, slot);
    //
    //    return ret;
    //}
    //
    //private bool DrawWeaponWithCheck(EquipSlot slot, CharacterWeapon weapon, CharacterEquipMask flag, ref CharacterEquipMask mask)
    //{
    //    var ret = DrawCheckbox(flag, ref mask);
    //    ImGui.SameLine();
    //    ret |= DrawWeapon(slot, weapon);
    //    return ret;
    //}
    //
    //private bool DrawEquip(CharacterEquipment equip)
    //{
    //    var ret = false;
    //    if (ImGui.CollapsingHeader("Character Equipment"))
    //    {
    //        ret |= DrawWeapon(EquipSlot.MainHand, equip.MainHand);
    //        ret |= DrawWeapon(EquipSlot.OffHand,  equip.OffHand);
    //        ret |= DrawEquipSlot(EquipSlot.Head,    equip.Head);
    //        ret |= DrawEquipSlot(EquipSlot.Body,    equip.Body);
    //        ret |= DrawEquipSlot(EquipSlot.Hands,   equip.Hands);
    //        ret |= DrawEquipSlot(EquipSlot.Legs,    equip.Legs);
    //        ret |= DrawEquipSlot(EquipSlot.Feet,    equip.Feet);
    //        ret |= DrawEquipSlot(EquipSlot.Ears,    equip.Ears);
    //        ret |= DrawEquipSlot(EquipSlot.Neck,    equip.Neck);
    //        ret |= DrawEquipSlot(EquipSlot.Wrists,  equip.Wrists);
    //        ret |= DrawEquipSlot(EquipSlot.RFinger, equip.RFinger);
    //        ret |= DrawEquipSlot(EquipSlot.LFinger, equip.LFinger);
    //    }
    //
    //    return ret;
    //}
    //
    //private bool DrawEquip(CharacterEquipment equip, ref CharacterEquipMask mask)
    //{
    //    var ret = false;
    //    if (ImGui.CollapsingHeader("Character Equipment"))
    //    {
    //        ret |= DrawWeaponWithCheck(EquipSlot.MainHand, equip.MainHand, CharacterEquipMask.MainHand, ref mask);
    //        ret |= DrawWeaponWithCheck(EquipSlot.OffHand,  equip.OffHand,  CharacterEquipMask.OffHand,  ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Head,    equip.Head,    CharacterEquipMask.Head,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Body,    equip.Body,    CharacterEquipMask.Body,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Hands,   equip.Hands,   CharacterEquipMask.Hands,   ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Legs,    equip.Legs,    CharacterEquipMask.Legs,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Feet,    equip.Feet,    CharacterEquipMask.Feet,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Ears,    equip.Ears,    CharacterEquipMask.Ears,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Neck,    equip.Neck,    CharacterEquipMask.Neck,    ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.Wrists,  equip.Wrists,  CharacterEquipMask.Wrists,  ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.RFinger, equip.RFinger, CharacterEquipMask.RFinger, ref mask);
    //        ret |= DrawEquipSlotWithCheck(EquipSlot.LFinger, equip.LFinger, CharacterEquipMask.LFinger, ref mask);
    //    }
    //
    //    return ret;
    //}
    //
    //
    //


    private static readonly Lumina.Excel.GeneratedSheets.Item SmallClothes = new()
    {
        Name  = new SeString("Nothing"),
        RowId = 0,
    };

    private static readonly Lumina.Excel.GeneratedSheets.Item SmallClothesNpc = new()
    {
        Name  = new SeString("Smallclothes (NPC)"),
        RowId = 1,
    };

    private static readonly Lumina.Excel.GeneratedSheets.Item Unknown = new()
    {
        Name  = new SeString("Unknown"),
        RowId = 2,
    };

    private static Lumina.Excel.GeneratedSheets.Item Identify(SetId set, WeaponType weapon, ushort variant, EquipSlot slot)
    {
        return (uint)set switch
        {
            0    => SmallClothes,
            9903 => SmallClothesNpc,
            _    => Identifier.Identify(set, weapon, variant, slot).FirstOrDefault(Unknown),
        };
    }
}
