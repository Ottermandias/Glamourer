using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcTab(NpcSelector selector, NpcPanel panel) : ITab<MainTabType>
{
    public ReadOnlySpan<byte> Label
        => "NPCs"u8;

    public MainTabType Identifier
        => MainTabType.Npcs;

    public void DrawContent()
    {
        selector.Draw(200 * Im.Style.GlobalScale);
        Im.Line.Same();
        panel.Draw();
    }
}
