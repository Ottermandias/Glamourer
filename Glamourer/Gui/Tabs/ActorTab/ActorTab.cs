using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly ActorSelection _selection;

    public ActorTab(ActorSelector selector, ActorPanel panel, ActorFilter filter, SelectPlayerButton selectPlayer,
        SelectTargetButton selectTarget, ActorsHeader header, ActorSelection selection)
    {
        _selection = selection;
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
        Draw(TwoPanelWidth.IndeterminateRelative);
    }

    public MainTabType Identifier
        => MainTabType.Actors;
}
