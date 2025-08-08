using Glamourer.Designs;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignColorCombo(DesignColors _designColors, bool _skipAutomatic) :
    FilterComboCache<string>(_skipAutomatic
            ? _designColors.Keys.OrderBy(k => k)
            : _designColors.Keys.OrderBy(k => k).Prepend(DesignColors.AutomaticName),
        MouseWheelType.Control, Glamourer.Log)
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var       isAutomatic = !_skipAutomatic && globalIdx == 0;
        var       key         = Items[globalIdx];
        var       color       = isAutomatic ? 0 : _designColors[key];
        using var c           = ImRaii.PushColor(ImGuiCol.Text, color, color != 0);
        var       ret         = base.DrawSelectable(globalIdx, selected);
        if (isAutomatic)
            ImGuiUtil.HoverTooltip(
                "The automatic color uses the colors dependent on the design state, as defined in the regular color definitions.");
        return ret;
    }
}
