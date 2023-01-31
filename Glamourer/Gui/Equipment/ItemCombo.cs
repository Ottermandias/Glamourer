using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Structs;
using Glamourer.Util;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Item = Glamourer.Designs.Item;

namespace Glamourer.Gui.Equipment;

public sealed class ItemCombo : FilterComboCache<Item>
{
    public readonly  string      Label;
    public readonly  EquipSlot   Slot;

    private uint _lastItemId;
    private uint _previewId;

    public ItemCombo(ItemManager items, EquipSlot slot)
        : base(GetItems(items, slot))
    {
        Label        = GetLabel(slot);
        Slot         = slot;
        _lastItemId = ItemManager.NothingId(slot);
        _previewId  = _lastItemId;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        if (_previewId != _lastItemId)
        {
            _lastItemId         = _previewId;
            CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _lastItemId);
            CurrentSelection    = Items[CurrentSelectionIdx];
        }
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            CurrentSelection = Items[NewSelection.Value];
    }

    public bool Draw(string previewName, uint previewIdx, float width)
    {
        _previewId = previewIdx;
        return Draw(Label, previewName, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var       obj   = Items[globalIdx];
        var       name  = ToString(obj);
        var       ret   = ImGui.Selectable(name, selected);
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.ModelBase.Value}-{obj.Variant})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].ModelBase.ToString());

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

    private static IEnumerable<Item> GetItems(ItemManager items, EquipSlot slot)
    {
        var nothing = ItemManager.NothingItem(slot);
        if (!items.Items.TryGetValue(slot.ToEquipType(), out var list))
            return new[] { nothing };

        return list.Select(i => new Item(i)).Append(ItemManager.SmallClothesItem(slot)).OrderBy(i => i.Name).Prepend(nothing);
    }
}
