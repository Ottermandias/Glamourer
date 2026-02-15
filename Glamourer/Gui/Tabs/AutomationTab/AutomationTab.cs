using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class AutomationTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly Configuration _config;

    public AutomationTab(AutomationFilter filter, SetSelector selector, SetPanel panel, AutomationButtons buttons, AutomationHeader header,
        Configuration config)
    {
        _config    = config;
        LeftHeader = new FilterHeader<AutomationCacheItem>(filter, new StringU8("Filter..."u8));
        LeftPanel  = selector;
        LeftFooter = buttons;

        RightHeader = header;
        RightPanel  = panel;
        RightFooter = NopHeaderFooter.Instance;
    }

    public bool IsVisible
        => _config.EnableAutoDesigns;

    public override ReadOnlySpan<byte> Label
        => "Automation"u8;

    public MainTabType Identifier
        => MainTabType.Automation;

    public void DrawContent()
    {
        Draw(TwoPanelWidth.IndeterminateRelative);
    }
}
