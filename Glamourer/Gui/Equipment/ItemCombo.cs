using Dalamud.Plugin.Services;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class ItemCombo : FilterComboCache<EquipItem>
{
    private readonly FavoriteManager _favorites;
    public readonly  string          Label;
    private          ItemId          _currentItem;
    private          float           _innerWidth;

    public PrimaryId CustomSetId   { get; private set; }
    public Variant   CustomVariant { get; private set; }

    public ItemCombo(IDataManager gameData, ItemManager items, EquipSlot slot, Logger log, FavoriteManager favorites)
        : base(() => GetItems(favorites, items, slot), MouseWheelType.Control, log)
    {
        _favorites    = favorites;
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

    public bool Draw(string previewName, ItemId previewIdx, float width, float innerWidth)
    {
        _innerWidth   = innerWidth;
        _currentItem  = previewIdx;
        CustomVariant = 0;
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
        ImUtf8.TextRightAligned($"({obj.PrimaryId.Id}-{obj.Variant.Id})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || Items[globalIndex].ModelString.StartsWith(filter.Lower);

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(IDataManager gameData, EquipSlot slot)
    {
        var sheet = gameData.GetExcelSheet<Addon>();

        return slot switch
        {
            EquipSlot.Head    => sheet.TryGetRow(740, out var text) ? text.Text.ToString() : "Head",
            EquipSlot.Body    => sheet.TryGetRow(741, out var text) ? text.Text.ToString() : "Body",
            EquipSlot.Hands   => sheet.TryGetRow(742, out var text) ? text.Text.ToString() : "Hands",
            EquipSlot.Legs    => sheet.TryGetRow(744, out var text) ? text.Text.ToString() : "Legs",
            EquipSlot.Feet    => sheet.TryGetRow(745, out var text) ? text.Text.ToString() : "Feet",
            EquipSlot.Ears    => sheet.TryGetRow(746, out var text) ? text.Text.ToString() : "Ears",
            EquipSlot.Neck    => sheet.TryGetRow(747, out var text) ? text.Text.ToString() : "Neck",
            EquipSlot.Wrists  => sheet.TryGetRow(748, out var text) ? text.Text.ToString() : "Wrists",
            EquipSlot.RFinger => sheet.TryGetRow(749, out var text) ? text.Text.ToString() : "Right Ring",
            EquipSlot.LFinger => sheet.TryGetRow(750, out var text) ? text.Text.ToString() : "Left Ring",
            _                 => string.Empty,
        };
    }

    private static List<EquipItem> GetItems(FavoriteManager favorites, ItemManager items, EquipSlot slot)
    {
        var nothing = ItemManager.NothingItem(slot);
        if (!items.ItemData.ByType.TryGetValue(slot.ToEquipType(), out var list))
            return [nothing];

        var enumerable = list.AsEnumerable();
        if (slot.IsEquipment())
            enumerable = enumerable.Append(ItemManager.SmallClothesItem(slot));
        return enumerable.OrderByDescending(favorites.Contains).ThenBy(i => i.Name).Prepend(nothing).ToList();
    }

    protected override void OnClosePopup()
    {
        // If holding control while the popup closes, try to parse the input as a full pair of set id and variant, and set a custom item for that.
        if (!ImGui.GetIO().KeyCtrl)
            return;

        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;

        CustomSetId   = setId;
        CustomVariant = variant;
    }
}
