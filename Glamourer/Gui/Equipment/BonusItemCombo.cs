using Dalamud.Plugin.Services;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class BonusItemCombo : FilterComboCache<EquipItem>
{
    private readonly FavoriteManager _favorites;
    public readonly  string          Label;
    private          CustomItemId    _currentItem;
    private          float           _innerWidth;

    public PrimaryId CustomSetId   { get; private set; }
    public Variant   CustomVariant { get; private set; }

    public BonusItemCombo(IDataManager gameData, ItemManager items, BonusItemFlag slot, Logger log, FavoriteManager favorites)
        : base(() => GetItems(favorites, items, slot), MouseWheelType.Control, log)
    {
        _favorites    = favorites;
        Label         = GetLabel(gameData, slot);
        _currentItem  = 0;
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
        if (CurrentSelection.Id == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Id == _currentItem);
        CurrentSelection    = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    public bool Draw(string previewName, BonusItemId previewIdx, float width, float innerWidth)
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
            _currentItem        = obj.Id;
            CurrentSelection    = default;
        }

        ImGui.SameLine();
        var ret = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.PrimaryId.Id}-{obj.Variant.Id})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].PrimaryId.Id.ToString());

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(IDataManager gameData, BonusItemFlag slot)
    {
        var sheet = gameData.GetExcelSheet<Addon>()!;

        return slot switch
        {
            BonusItemFlag.Glasses => sheet.TryGetRow(16050, out var text) ? text.Text.ToString() : "Facewear",
            BonusItemFlag.UnkSlot => sheet.TryGetRow(16051, out var text) ? text.Text.ToString() : "Facewear",

            _ => string.Empty,
        };
    }

    private static List<EquipItem> GetItems(FavoriteManager favorites, ItemManager items, BonusItemFlag slot)
    {
        var nothing = EquipItem.BonusItemNothing(slot);
        return items.ItemData.ByType[slot.ToEquipType()].OrderByDescending(favorites.Contains).ThenBy(i => i.Id.Id).Prepend(nothing).ToList();
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
