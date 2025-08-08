using Glamourer.Designs;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using OtterGui.Text.EndObjects;

namespace Glamourer;

[Flags]
public enum DesignPanelFlag : uint
{
    Customization          = 0x0001,
    Equipment              = 0x0002,
    AdvancedCustomizations = 0x0004,
    AdvancedDyes           = 0x0008,
    AppearanceDetails      = 0x0010,
    DesignDetails          = 0x0020,
    ModAssociations        = 0x0040,
    DesignLinks            = 0x0080,
    ApplicationRules       = 0x0100,
    DebugData              = 0x0200,
}

public static class DesignPanelFlagExtensions
{
    public static ReadOnlySpan<byte> ToName(this DesignPanelFlag flag)
        => flag switch
        {
            DesignPanelFlag.Customization          => "Customization"u8,
            DesignPanelFlag.Equipment              => "Equipment"u8,
            DesignPanelFlag.AdvancedCustomizations => "Advanced Customization"u8,
            DesignPanelFlag.AdvancedDyes           => "Advanced Dyes"u8,
            DesignPanelFlag.DesignDetails          => "Design Details"u8,
            DesignPanelFlag.ApplicationRules       => "Application Rules"u8,
            DesignPanelFlag.ModAssociations        => "Mod Associations"u8,
            DesignPanelFlag.DesignLinks            => "Design Links"u8,
            DesignPanelFlag.DebugData              => "Debug Data"u8,
            DesignPanelFlag.AppearanceDetails      => "Appearance Details"u8,
            _                                      => ""u8,
        };

    public static CollapsingHeader Header(this DesignPanelFlag flag, Configuration config)
    {
        if (config.HideDesignPanel.HasFlag(flag))
            return new CollapsingHeader()
            {
                Disposed = true,
            };

        var expand = config.AutoExpandDesignPanel.HasFlag(flag);
        return ImUtf8.CollapsingHeaderId(flag.ToName(), expand ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
    }

    public static void DrawTable(ReadOnlySpan<byte> label, DesignPanelFlag hidden, DesignPanelFlag expanded, Action<DesignPanelFlag> setterHide,
        Action<DesignPanelFlag> setterExpand)
    {
        var       checkBoxWidth = Math.Max(ImGui.GetFrameHeight(), ImUtf8.CalcTextSize("Expand"u8).X);
        var       textWidth     = ImUtf8.CalcTextSize(DesignPanelFlag.AdvancedCustomizations.ToName()).X;
        var       tableSize     = 2 * (textWidth + 2 * checkBoxWidth) + 10 * ImGui.GetStyle().CellPadding.X + 2 * ImGui.GetStyle().WindowPadding.X + 2 * ImGui.GetStyle().FrameBorderSize;
        using var table         = ImUtf8.Table(label, 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders, new Vector2(tableSize, 6 * ImGui.GetFrameHeight()));
        if (!table)
            return;

        var headerColor    = ImGui.GetColorU32(ImGuiCol.TableHeaderBg);
        var checkBoxOffset = (checkBoxWidth - ImGui.GetFrameHeight()) / 2;
        ImUtf8.TableSetupColumn("Panel##1"u8,  ImGuiTableColumnFlags.WidthFixed, textWidth);
        ImUtf8.TableSetupColumn("Show##1"u8,   ImGuiTableColumnFlags.WidthFixed, checkBoxWidth);
        ImUtf8.TableSetupColumn("Expand##1"u8, ImGuiTableColumnFlags.WidthFixed, checkBoxWidth);
        ImUtf8.TableSetupColumn("Panel##2"u8,  ImGuiTableColumnFlags.WidthFixed, textWidth);                                  
        ImUtf8.TableSetupColumn("Show##2"u8,   ImGuiTableColumnFlags.WidthFixed, checkBoxWidth);
        ImUtf8.TableSetupColumn("Expand##2"u8, ImGuiTableColumnFlags.WidthFixed, checkBoxWidth);

        ImGui.TableHeadersRow();
        foreach (var panel in Enum.GetValues<DesignPanelFlag>())
        {
            using var id = ImUtf8.PushId((int)panel);
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, headerColor);
            ImUtf8.TextFrameAligned(panel.ToName());
            var isShown    = !hidden.HasFlag(panel);
            var isExpanded = expanded.HasFlag(panel);

            ImGui.TableNextColumn();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + checkBoxOffset);
            if (ImUtf8.Checkbox("##show"u8, ref isShown))
                setterHide.Invoke(isShown ? hidden & ~panel : hidden | panel);
            ImUtf8.HoverTooltip(
                "Show this panel and associated functionality in all relevant tabs.\n\nToggling this off does NOT disable any functionality, just the display of it, so hide panels at your own risk."u8);

            ImGui.TableNextColumn();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + checkBoxOffset);
            if (ImUtf8.Checkbox("##expand"u8, ref isExpanded))
                setterExpand.Invoke(isExpanded ? expanded | panel : expanded & ~panel);
            ImUtf8.HoverTooltip("Expand this panel by default in all relevant tabs."u8);
        }
    }
}
