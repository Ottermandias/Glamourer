using System;
using System.Collections.Generic;
using System.Linq;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class WeaponCombo : FilterComboCache<EquipItem>
{
    public readonly string Label;
    private         ItemId _currentItemId;
    private         float  _innerWidth;

    public WeaponCombo(ItemManager items, FullEquipType type)
        : base(() => GetWeapons(items, type))
    {
        Label         = GetLabel(type);
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
        if (CurrentSelection.ItemId == _currentItemId)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _currentItemId);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    protected override float GetFilterWidth()
        => _innerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    public bool Draw(string previewName, ItemId previewId, float width, float innerWidth)
    {
        _currentItemId = previewId;
        _innerWidth    = innerWidth;
        return Draw($"##{Label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj  = Items[globalIdx];
        var name = ToString(obj);
        var ret  = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.ModelId.Id}-{obj.WeaponType.Id}-{obj.Variant})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].ModelId.Id.ToString());

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(FullEquipType type)
        => type is FullEquipType.Unknown ? "Mainhand" : type.ToName();

    private static IReadOnlyList<EquipItem> GetWeapons(ItemManager items, FullEquipType type)
    {
        if (type is FullEquipType.Unknown)
        {
            var enumerable = Array.Empty<EquipItem>().AsEnumerable();
            foreach (var t in Enum.GetValues<FullEquipType>().Where(e => e.ToSlot() is EquipSlot.MainHand))
            {
                if (items.ItemService.AwaitedService.TryGetValue(t, out var l))
                    enumerable = enumerable.Concat(l);
            }

            return enumerable.OrderBy(e => e.Name).ToList();
        }

        if (!items.ItemService.AwaitedService.TryGetValue(type, out var list))
            return Array.Empty<EquipItem>();

        if (type.AllowsNothing())
            return list.OrderBy(e => e.Name).Prepend(ItemManager.NothingItem(type)).ToList();

        return list.OrderBy(e => e.Name).ToList();
    }
}
