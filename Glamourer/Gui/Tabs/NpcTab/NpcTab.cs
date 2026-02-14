using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcTab : TwoPanelLayout, ITab<MainTabType>
{
    public NpcTab(NpcFilter filter, NpcSelector selector, NpcPanel panel, NpcHeader header)
    {
        LeftHeader  = new FilterHeader<NpcCacheItem>(filter, new StringU8("Filter..."u8));
        LeftPanel   = selector;
        LeftFooter  = NopHeaderFooter.Instance;
        RightHeader = header;
        RightPanel  = panel;
        RightFooter = NopHeaderFooter.Instance;
    }

    public override ReadOnlySpan<byte> Label
        => "NPCs"u8;

    public MainTabType Identifier
        => MainTabType.Npcs;

    public void DrawContent()
        => Draw(TwoPanelWidth.IndeterminateRelative);
}
