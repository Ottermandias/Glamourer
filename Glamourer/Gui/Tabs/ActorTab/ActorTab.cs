using Glamourer.Config;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly ActorSelection _selection;
    private readonly UiConfig       _uiConfig;

    public ActorTab(ActorSelector selector, ActorPanel panel, ActorFilter filter, SelectPlayerButton selectPlayer,
        SelectTargetButton selectTarget, ActorsHeader header, ActorSelection selection, UiConfig uiConfig)
    {
        _selection = selection;
        _uiConfig  = uiConfig;
        LeftPanel  = selector;
        LeftHeader = new FilterHeader<ActorCacheItem>(filter, new StringU8("Filter..."u8));
        var footer = new ButtonFooter();
        footer.Buttons.AddButton(selectPlayer, 100);
        footer.Buttons.AddButton(selectTarget, 0);
        LeftFooter = footer;

        RightHeader = header;
        RightPanel  = panel;
        RightFooter = NopHeaderFooter.Instance;
    }

    public override ReadOnlySpan<byte> Label
        => "Actors"u8;

    public void DrawContent()
    {
        _selection.Update();
        Draw(_uiConfig.ActorsTabScale);
    }

    protected override void SetWidth(float width, ScalingMode mode)
        => _uiConfig.ActorsTabScale = new TwoPanelWidth(width, mode);

    protected override float MinimumWidth
        => LeftHeader.MinimumWidth;

    protected override float MaximumWidth
        => Im.Window.Width - 500 * Im.Style.GlobalScale;

    public MainTabType Identifier
        => MainTabType.Actors;
}
