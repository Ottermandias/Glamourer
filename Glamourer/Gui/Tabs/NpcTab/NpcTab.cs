using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.NpcTab;

public class NpcTab(NpcSelector _selector, NpcPanel _panel) : ITab
{
    public ReadOnlySpan<byte> Label
        => "NPCs"u8;

    public void DrawContent()
    {
        _selector.Draw(200 * ImGuiHelpers.GlobalScale);
        Im.Line.Same();
        _panel.Draw();
    }
}
