using Glamourer.Configuration;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly UiConfig _uiConfig;

    public NpcTab(NpcFilter filter, NpcSelector selector, NpcPanel panel, NpcHeader header, UiConfig uiConfig)
    {
        _uiConfig   = uiConfig;
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
        => Draw(_uiConfig.NpcTabScale);

    protected override void SetWidth(float width, ScalingMode mode)
        => _uiConfig.NpcTabScale = new TwoPanelWidth(width, mode);

    protected override float MinimumWidth
        => LeftHeader.MinimumWidth;

    protected override float MaximumWidth
        => Im.Window.Width - 500 * Im.Style.GlobalScale;
}
