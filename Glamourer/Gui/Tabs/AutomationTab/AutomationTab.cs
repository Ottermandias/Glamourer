using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class AutomationTab(SetSelector selector, SetPanel panel, Configuration config) : ITab<MainTabType>
{
    public bool IsVisible
        => config.EnableAutoDesigns;

    public ReadOnlySpan<byte> Label
        => "Automation"u8;

    public MainTabType Identifier
        => MainTabType.Automation;

    public void DrawContent()
    {
        selector.Draw(GetSetSelectorSize());
        Im.Line.Same();
        panel.Draw();
    }

    public float GetSetSelectorSize()
        => 200f * Im.Style.GlobalScale;
}
