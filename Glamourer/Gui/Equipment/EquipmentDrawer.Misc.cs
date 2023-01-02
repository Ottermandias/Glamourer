using ImGuiNET;
using OtterGui;

namespace Glamourer.Gui.Equipment;

public partial class EquipmentDrawer
{
    private void DrawCheckbox(ref ApplicationFlags flags)
        => DrawCheckbox("##checkbox", "Enable writing this slot in this save.", ref flags, 0);

    private static void DrawCheckbox(string label, string tooltip, ref ApplicationFlags flags, ApplicationFlags flag)
    {
        var tmp = (uint)flags;
        if (ImGui.CheckboxFlags(label, ref tmp, (uint)flag))
            flags = (ApplicationFlags)tmp;

        ImGuiUtil.HoverTooltip(tooltip);
    }
}
