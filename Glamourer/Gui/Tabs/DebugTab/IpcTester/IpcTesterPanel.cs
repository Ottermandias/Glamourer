using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Glamourer.Api.IpcSubscribers;
using ImGuiNET;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab.IpcTester;

public class IpcTesterPanel(
    IDalamudPluginInterface pluginInterface,
    DesignIpcTester designs,
    ItemsIpcTester items,
    StateIpcTester state,
    IFramework framework) : IGameDataDrawer
{
    public string Label
        => "IPC Tester";

    public bool Disabled
        => false;

    private DateTime _lastUpdate;
    private bool     _subscribed = false;

    public void Draw()
    {
        try
        {
            _lastUpdate = framework.LastUpdateUTC.AddSeconds(1);
            Subscribe();
            ImGui.TextUnformatted(ApiVersion.Label);
            var (major, minor) = new ApiVersion(pluginInterface).Invoke();
            ImGui.SameLine();
            ImGui.TextUnformatted($"({major}.{minor:D4})");

            designs.Draw();
            items.Draw();
            state.Draw();
        }
        catch (Exception e)
        {
            Glamourer.Log.Error($"Error during IPC Tests:\n{e}");
        }
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        Glamourer.Log.Debug("[IPCTester] Subscribed to IPC events for IPC tester.");
        state.GPoseChanged.Enable();
        state.StateChanged.Enable();
        state.StateFinalized.Enable();
        framework.Update += CheckUnsubscribe;
        _subscribed      =  true;
    }

    private void CheckUnsubscribe(IFramework framework1)
    {
        if (_lastUpdate > framework.LastUpdateUTC)
            return;

        Unsubscribe();
        framework.Update -= CheckUnsubscribe;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        Glamourer.Log.Debug("[IPCTester] Unsubscribed from IPC events for IPC tester.");
        _subscribed = false;
        state.GPoseChanged.Disable();
        state.StateChanged.Disable();
        state.StateFinalized.Disable();
    }
}
