using Glamourer.Config;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class GlamourerColorCombo : FilterComboColors, IDisposable
{
    private readonly DictStain       _stains;
    private readonly FavoriteManager _favorites;
    private readonly Configuration   _config;

    public GlamourerColorCombo(DictStain stains, FavoriteManager favorites, Configuration config)
    {
        _stains                = stains;
        _favorites             = favorites;
        _config                = config;
        ClearFilterOnSelection = !config.KeepItemComboFilter;

        _config.KeepItemComboFilterChanged += OnItemComboFilterChanged;
    }

    protected override float AdditionalSpace
        => AwesomeIcon.Font.CalculateTextSize(LunaStyle.FavoriteIcon.Span).X + 8 * Im.Style.GlobalScale;

    protected override bool DrawItem(in Item item, int globalIndex, bool selected)
    {
        if (globalIndex is 0)
            Im.Dummy(AwesomeIcon.Font.CalculateTextSize(LunaStyle.FavoriteIcon.Span));
        else
            UiHelpers.DrawFavoriteStar(_favorites, item.Id);
        Im.Line.Same(0, 8 * Im.Style.GlobalScale);

        var       buttonWidth = Im.ContentRegion.Available.X;
        var       totalWidth  = Im.ContentRegion.Maximum.X;
        using var style       = ImStyleDouble.ButtonTextAlign.PushX(buttonWidth / 2 / totalWidth);
        return base.DrawItem(item, globalIndex, selected);
    }

    protected override void PreDrawCombo(float width)
    {
        base.PreDrawCombo(width);
        Style.Push(ImGuiColor.Text, CurrentSelection.Color.ContrastColor(), !CurrentSelection.Color.IsTransparent);
    }

    protected override void PostDrawCombo(float width)
    {
        if (!CurrentSelection.Color.IsTransparent)
            Style.PopColor();
        base.PostDrawCombo(width);
    }

    public bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, in Stain current, out Stain newStain, float width)
    {
        // Push the preview color.
        using var color = ImGuiColor.FrameBackground.Push(current.RgbaColor, !current.RgbaColor.IsTransparent)
            .Push(ImGuiColor.FrameBackgroundHovered, current.RgbaColor.Color, !current.RgbaColor.IsTransparent);

        // Set the current selection only for the IsSelected and Gloss checks.
        CurrentSelection = new Item(current.Name, current.RgbaColor, current.RowIndex.Id, current.Gloss);

        // Skip the named preview if it does not fit.
        var name = Im.Font.CalculateSize(current.Name).X <= width && !current.RgbaColor.IsTransparent ? current.Name : StringU8.Empty;
        var ret  = base.Draw(label, name, StringU8.Empty, width, out var newItem);
        if (name.IsEmpty)
            Im.Tooltip.OnHover(CurrentSelection.Name);
        if (ret)
        {
            if (newItem.Id is 0)
                newStain = Stain.None;
            else if (!_stains.TryGetValue(newItem.Id, out newStain))
                return false;

            return true;
        }

        newStain = current;
        return false;
    }

    protected override IEnumerable<Item> GetItems()
        => _stains.Select(kvp => (new Item(kvp.Value.Name, kvp.Value.RgbaColor, kvp.Key.Id, kvp.Value.Gloss), _favorites.Contains(kvp.Key)))
            .OrderBy(p => !p.Item2)
            .Select(p => p.Item1)
            .Prepend(None);

    private void OnItemComboFilterChanged(bool newValue, bool _)
        => ClearFilterOnSelection = !newValue;

    public void Dispose()
        => _config.KeepItemComboFilterChanged -= OnItemComboFilterChanged;
}
