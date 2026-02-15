using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationFilter : TextFilterBase<AutomationCacheItem>, IUiService
{
    private bool? _setStateFilter;

    protected override string ToFilterString(in AutomationCacheItem item, int globalIndex)
        => item.Name.Utf16;

    public override bool WouldBeVisible(in AutomationCacheItem item, int globalIndex)
        => _setStateFilter switch
            {
                true  => item.Set.Enabled,
                false => !item.Set.Enabled,
                null  => true,
            }
         && base.WouldBeVisible(in item, globalIndex);

    public override bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
    {
        availableRegion.X -= Im.Style.FrameHeight;
        var ret = base.DrawFilter(label, availableRegion);
        Im.Line.NoSpacing();
        using (ImGuiColor.FrameBackground.Push(0x807070FF, _setStateFilter is not null))
        {
            if (ImEx.TriStateCheckbox("##state"u8, ref _setStateFilter, Rgba32.Transparent, ColorParameter.Default, ColorParameter.Default))
            {
                ret = true;
                InvokeEvent();
            }
        }

        if (Im.Item.Hovered())
        {
            if (Im.Item.RightClicked())
            {
                // Ensure that a right-click clears the text filter if it is currently being edited.
                Im.Id.ClearActive();
                Clear();
            }

            using var tt = Im.Tooltip.Begin();
            Im.Text("Filter to only show enabled or disabled sets."u8);
            if (Text.Length is not 0 || _setStateFilter is not null)
                Im.Text("\nRight-click to clear all filters, including the text filter."u8);
        }

        var pos = Im.Item.UpperLeftCorner;
        pos.X -= Im.Style.GlobalScale;
        Im.Window.DrawList.Shape.Line(pos, pos with { Y = Im.Item.LowerRightCorner.Y }, ImGuiColor.Border.Get(), Im.Style.GlobalScale);
        return ret;
    }

    public override void Clear()
    {
        var changes = _setStateFilter is not null;
        _setStateFilter = null;
        if (!Set(string.Empty) && changes)
            InvokeEvent();
    }
}
