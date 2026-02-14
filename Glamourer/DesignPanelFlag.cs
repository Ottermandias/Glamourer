using Luna.Generators;
using ImSharp;

namespace Glamourer;

[Flags]
[NamedEnum(Utf16: false)]
public enum DesignPanelFlag : uint
{
    [Name("Customization")]
    Customization = 0x0001,

    [Name("Equipment")]
    Equipment = 0x0002,

    [Name("Advanced Customization")]
    AdvancedCustomizations = 0x0004,

    [Name("Advanced Dyes")]
    AdvancedDyes = 0x0008,

    [Name("Appearance Details")]
    AppearanceDetails = 0x0010,

    [Name("Design Details")]
    DesignDetails = 0x0020,

    [Name("Mod Associations")]
    ModAssociations = 0x0040,

    [Name("Design Links")]
    DesignLinks = 0x0080,

    [Name("Application Rules")]
    ApplicationRules = 0x0100,

    [Name("Debug Data")]
    DebugData = 0x0200,
}

public static partial class DesignPanelFlagExtensions
{
    private static readonly StringU8 Expand                = new("Expand"u8);

    public static Im.HeaderDisposable Header(this DesignPanelFlag flag, Configuration config)
    {
        if (config.HideDesignPanel.HasFlag(flag))
            return default;

        var expand = config.AutoExpandDesignPanel.HasFlag(flag);
        return Im.Tree.HeaderId(flag.ToNameU8(), expand ? TreeNodeFlags.DefaultOpen : TreeNodeFlags.None);
    }

    public static void DrawTable(ReadOnlySpan<byte> label, DesignPanelFlag hidden, DesignPanelFlag expanded, Action<DesignPanelFlag> setterHide,
        Action<DesignPanelFlag> setterExpand)
    {
        var checkBoxWidth = Math.Max(Im.Style.FrameHeight, Expand.CalculateSize().X);
        var test          = DesignPanelFlag.AdvancedCustomizations.ToNameU8();
        var textWidth     = AdvancedCustomizations_Name__GenU8.CalculateSize().X;
        var tableSize = 2 * (textWidth + 2 * checkBoxWidth)
          + 10 * Im.Style.CellPadding.X
          + 2 * Im.Style.WindowPadding.X
          + 2 * Im.Style.FrameBorderThickness;
        using var table = Im.Table.Begin(label, 6, TableFlags.RowBackground | TableFlags.Borders,
            new Vector2(tableSize, 6 * Im.Style.FrameHeight));
        if (!table)
            return;

        var headerColor    = Im.Color.Get(ImGuiColor.TableHeaderBackground);
        var checkBoxOffset = (checkBoxWidth - Im.Style.FrameHeight) / 2;
        table.SetupColumn("Panel##1"u8,  TableColumnFlags.WidthFixed, textWidth);
        table.SetupColumn("Show##1"u8,   TableColumnFlags.WidthFixed, checkBoxWidth);
        table.SetupColumn("Expand##1"u8, TableColumnFlags.WidthFixed, checkBoxWidth);
        table.SetupColumn("Panel##2"u8,  TableColumnFlags.WidthFixed, textWidth);
        table.SetupColumn("Show##2"u8,   TableColumnFlags.WidthFixed, checkBoxWidth);
        table.SetupColumn("Expand##2"u8, TableColumnFlags.WidthFixed, checkBoxWidth);

        table.HeaderRow();
        foreach (var panel in DesignPanelFlag.Values)
        {
            using var id = Im.Id.Push((int)panel);
            table.NextColumn();
            table.SetBackgroundColor(TableBackgroundTarget.Cell, headerColor);
            ImEx.TextFrameAligned(panel.ToNameU8());
            var isShown    = !hidden.HasFlag(panel);
            var isExpanded = expanded.HasFlag(panel);

            table.NextColumn();
            Im.Cursor.X += checkBoxOffset;
            if (Im.Checkbox("##show"u8, ref isShown))
                setterHide.Invoke(isShown ? hidden & ~panel : hidden | panel);
            Im.Tooltip.OnHover(
                "Show this panel and associated functionality in all relevant tabs.\n\nToggling this off does NOT disable any functionality, just the display of it, so hide panels at your own risk."u8);

            table.NextColumn();
            Im.Cursor.X += checkBoxOffset;
            if (Im.Checkbox("##expand"u8, ref isExpanded))
                setterExpand.Invoke(isExpanded ? expanded | panel : expanded & ~panel);
            Im.Tooltip.OnHover("Expand this panel by default in all relevant tabs."u8);
        }
    }
}
