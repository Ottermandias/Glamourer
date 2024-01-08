using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorTab : ITab
{
    private readonly ActorSelector _selector;
    private readonly ActorPanel    _panel;

    public ReadOnlySpan<byte> Label
        => "Actors"u8;

    public void DrawContent()
    {
        _selector.Draw(200 * ImGuiHelpers.GlobalScale);
        ImGui.SameLine();
        _panel.Draw();
    }

    public ActorTab(ActorSelector selector, ActorPanel panel)
    {
        _selector = selector;
        _panel    = panel;
    }
}
