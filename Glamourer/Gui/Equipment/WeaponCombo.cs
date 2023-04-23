using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Data;
using Glamourer.Designs;
using Glamourer.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Equipment;

public sealed class WeaponCombo : FilterComboCache<Weapon>
{
    public readonly string Label;
    private         uint   _currentItem;

    public WeaponCombo(DataManager gameData, ItemManager items, FullEquipType type, EquipSlot offhand)
        : base(offhand is EquipSlot.OffHand ? () => GetOff(items, type) : () => GetMain(items, type))
        => Label = GetLabel(gameData, offhand);

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            CurrentSelection = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (CurrentSelection.ItemId != _currentItem)
        {
            CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _currentItem);
            CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
            return base.UpdateCurrentSelected(CurrentSelectionIdx);
        }

        return currentSelected;
    }

    public bool Draw(string previewName, uint previewIdx, float width)
    {
        _currentItem = previewIdx;
        return Draw(Label, previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj  = Items[globalIdx];
        var name = ToString(obj);
        var ret  = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.ModelBase.Value}-{obj.WeaponBase.Value}-{obj.Variant})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].ModelBase.ToString());

    protected override string ToString(Weapon obj)
        => obj.Name;

    private static string GetLabel(DataManager gameData, EquipSlot offhand)
    {
        var sheet = gameData.GetExcelSheet<Addon>()!;
        return offhand is EquipSlot.OffHand
            ? sheet.GetRow(739)?.Text.ToString() ?? "Off Hand"
            : sheet.GetRow(738)?.Text.ToString() ?? "Main Hand";
    }

    private static IReadOnlyList<Weapon> GetMain(ItemManager items, FullEquipType type)
    {
        var list = new List<Weapon>();
        if (type is FullEquipType.Unknown)
            foreach (var t in Enum.GetValues<FullEquipType>().Where(t => t.ToSlot() == EquipSlot.MainHand))
                list.AddRange(items.ItemService.AwaitedService[t].Select(w => new Weapon(w, false)));
        else if (type.ToSlot() is EquipSlot.MainHand)
            list.AddRange(items.ItemService.AwaitedService[type].Select(w => new Weapon(w, false)));
        list.Sort((w1, w2) => string.CompareOrdinal(w1.Name, w2.Name));
        return list;
    }

    private static IReadOnlyList<Weapon> GetOff(ItemManager items, FullEquipType type)
    {
        if (type.ToSlot() == EquipSlot.OffHand)
        {
            var nothing = ItemManager.NothingItem(type);
            if (!items.ItemService.AwaitedService.TryGetValue(type, out var list))
                return new[]
                {
                    nothing,
                };

            return list.Select(w => new Weapon(w, true)).OrderBy(w => w.Name).Prepend(nothing).ToList();
        }
        else if (items.ItemService.AwaitedService.TryGetValue(type, out var list))
        {
            return list.Select(w => new Weapon(w, true)).OrderBy(w => w.Name).ToList();
        }

        return Array.Empty<Weapon>();
    }
}
