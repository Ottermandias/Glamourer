using System.Linq;
using ImGuiNET;

namespace Glamourer.Gui
{
    public static partial class ImGuiCustom
    {
        public static void HoverTooltip(string text)
        {
            if (text.Any() && ImGui.IsItemHovered())
                ImGui.SetTooltip(text);
        }
    }
}
