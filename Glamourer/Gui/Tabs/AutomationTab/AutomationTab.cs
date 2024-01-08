using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class AutomationTab : ITab
{
    private readonly SetSelector _selector;
    private readonly SetPanel    _panel;

    public AutomationTab(SetSelector selector, SetPanel panel)
    {
        _selector = selector;
        _panel    = panel;
    }

    public ReadOnlySpan<byte> Label
        => "Automation"u8;

    public void DrawContent()
    {
        _selector.Draw(GetSetSelectorSize());
        ImGui.SameLine();
        _panel.Draw();
    }

    public float GetSetSelectorSize()
        => 200f * ImGuiHelpers.GlobalScale;
}
