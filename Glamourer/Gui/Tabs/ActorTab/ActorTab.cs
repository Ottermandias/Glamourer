using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorTab(ActorSelector selector, ActorPanel panel) : ITab<MainTabType>
{
    public ReadOnlySpan<byte> Label
        => "Actors"u8;

    public MainTabType Identifier
        => MainTabType.Actors;

    public void DrawContent()
    {
        selector.Draw(200 * Im.Style.GlobalScale);
        Im.Line.Same();
        panel.Draw();
    }
}
