using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class WeaponCombo : FilterComboCache<EquipItem>
{
    private readonly FavoriteManager _favorites;
    public readonly  string          Label;
    private          ItemId          _currentItem;
    private          float           _innerWidth;

    public WeaponCombo(ItemManager items, FullEquipType type, Logger log, FavoriteManager favorites)
        : base(() => GetWeapons(favorites, items, type), MouseWheelType.Control, log)
    {
        _favorites    = favorites;
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
        if (CurrentSelection.ItemId == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _currentItem);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    public bool Draw(string previewName, ItemId previewIdx, float width, float innerWidth)
    {
        _innerWidth  = innerWidth;
        _currentItem = previewIdx;
        return Draw($"##{Label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override float GetFilterWidth()
        => _innerWidth - 2 * ImGui.GetStyle().FramePadding.X;


    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj  = Items[globalIdx];
        var name = ToString(obj);
        if (UiHelpers.DrawFavoriteStar(_favorites, obj) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            _currentItem        = obj.ItemId;
            CurrentSelection    = default;
        }

        ImGui.SameLine();
        var ret = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImUtf8.TextRightAligned($"({obj.PrimaryId.Id}-{obj.SecondaryId.Id}-{obj.Variant.Id})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || Items[globalIndex].ModelString.StartsWith(filter.Lower);

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(FullEquipType type)
        => type.IsUnknown() ? "Mainhand" : type.ToName();

    private static IReadOnlyList<EquipItem> GetWeapons(FavoriteManager favorites, ItemManager items, FullEquipType type)
    {
        if (type is FullEquipType.Unknown)
        {
            var enumerable = Array.Empty<EquipItem>().AsEnumerable();
            foreach (var t in Enum.GetValues<FullEquipType>().Where(e => e.ToSlot() is EquipSlot.MainHand))
            {
                if (items.ItemData.ByType.TryGetValue(t, out var l))
                    enumerable = enumerable.Concat(l);
            }

            return [.. enumerable.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];
        }

        if (!items.ItemData.ByType.TryGetValue(type, out var list))
            return [];

        if (type.AllowsNothing())
            return [ItemManager.NothingItem(type), .. list.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];

        return [.. list.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];
    }
}
