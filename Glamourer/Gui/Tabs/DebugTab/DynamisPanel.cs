using Luna;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DynamisPanel(DynamisIpc dynamis) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Dynamis Interop"u8;

    public void Draw()
        => dynamis.DrawDebugInfo();

    public bool Disabled
        => false;
}
