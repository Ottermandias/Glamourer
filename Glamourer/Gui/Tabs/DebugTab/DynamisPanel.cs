using OtterGui.Services;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DynamisPanel(DynamisIpc dynamis) : IGameDataDrawer
{
    public string Label
        => "Dynamis Interop";

    public void Draw()
        => dynamis.DrawDebugInfo();

    public bool Disabled
        => false;
}
