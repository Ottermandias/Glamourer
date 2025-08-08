using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorTab(ActorSelector selector, ActorPanel panel) : ITab
{
    public ReadOnlySpan<byte> Label
        => "Actors"u8;

    public void DrawContent()
    {
        selector.Draw(200 * ImGuiHelpers.GlobalScale);
        ImGui.SameLine();
        panel.Draw();
    }
}
