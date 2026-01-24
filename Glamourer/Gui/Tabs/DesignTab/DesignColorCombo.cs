using Glamourer.Designs;
using ImSharp;
using OtterGui;
using OtterGui.Widgets;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignColorCombo(DesignColors designColors, bool skipAutomatic) :
    FilterComboCache<string>(skipAutomatic
            ? designColors.Keys.OrderBy(k => k)
            : designColors.Keys.OrderBy(k => k).Prepend(DesignColors.AutomaticName),
        MouseWheelType.Control, Glamourer.Log)
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var       isAutomatic = !skipAutomatic && globalIdx is 0;
        var       key         = Items[globalIdx];
        var       color       = isAutomatic ? 0 : designColors[key];
        using var c           = ImGuiColor.Text.Push(color, !color.IsTransparent);
        var       ret         = base.DrawSelectable(globalIdx, selected);
        if (isAutomatic)
            ImGuiUtil.HoverTooltip(
                "The automatic color uses the colors dependent on the design state, as defined in the regular color definitions.");
        return ret;
    }
}
