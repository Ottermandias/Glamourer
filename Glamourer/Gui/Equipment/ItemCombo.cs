using System.Collections.Generic;
using System.Linq;
using Dalamud.Data;
using Glamourer.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class ItemCombo : FilterComboCache<EquipItem>
{
    private readonly TextureService _textures;

    public readonly string Label;
    private         uint   _currentItem;

    public ItemCombo(DataManager gameData, ItemManager items, EquipSlot slot, TextureService textures)
        : base(() => GetItems(items, slot))
    {
        _textures     = textures;
        Label         = GetLabel(gameData, slot);
        _currentItem  = ItemManager.NothingId(slot);
        SearchByParts = true;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            CurrentSelection = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (CurrentSelection.ItemId == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _currentItem);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    public bool Draw(string previewName, uint previewIdx, float width)
    {
        _currentItem = previewIdx;
        return Draw($"##{Label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj  = Items[globalIdx];
        var name = ToString(obj);
        var ret  = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.ModelId.Value}-{obj.Variant})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].ModelId.Value.ToString());

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(DataManager gameData, EquipSlot slot)
    {
        var sheet = gameData.GetExcelSheet<Addon>()!;

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

    private static IReadOnlyList<EquipItem> GetItems(ItemManager items, EquipSlot slot)
    {
        var nothing = ItemManager.NothingItem(slot);
        if (!items.ItemService.AwaitedService.TryGetValue(slot.ToEquipType(), out var list))
            return new[]
            {
                nothing,
            };

        var enumerable = list.AsEnumerable();
        if (slot.IsEquipment())
            enumerable = enumerable.Append(ItemManager.SmallClothesItem(slot));
        return enumerable.OrderBy(i => i.Name).Prepend(nothing).ToList();
    }
}
