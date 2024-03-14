using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class AutomationTab(SetSelector selector, SetPanel panel, Configuration config) : ITab
{
    public ReadOnlySpan<byte> Label
        => "Automation"u8;

    public bool IsVisible
        => config.EnableAutoDesigns;

    public void DrawContent()
    {
        selector.Draw(GetSetSelectorSize());
        ImGui.SameLine();
        panel.Draw();
    }

    public float GetSetSelectorSize()
        => 200f * ImGuiHelpers.GlobalScale;
}
