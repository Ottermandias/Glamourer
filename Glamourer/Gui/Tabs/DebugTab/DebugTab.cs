using Dalamud.Bindings.ImGui;
using Luna;
using OtterGui.Raii;
using ITab = OtterGui.Widgets.ITab;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class DebugTab(ServiceManager manager) : ITab
{
    private readonly Configuration _config = manager.GetService<Configuration>();

    public bool IsVisible
        => _config.DebugMode;

    public ReadOnlySpan<byte> Label
        => "Debug"u8;

    private readonly DebugTabHeader[] _headers =
    [
        DebugTabHeader.CreateInterop(manager.Provider!),
        DebugTabHeader.CreateGameData(manager.Provider!),
        DebugTabHeader.CreateDesigns(manager.Provider!),
        DebugTabHeader.CreateState(manager.Provider!),
        DebugTabHeader.CreateUnlocks(manager.Provider!),
    ];

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        if (ImGui.CollapsingHeader("General"))
        {
            StartTimeTracker.Draw("Timers"u8);
        }

        foreach (var header in _headers)
            header.Draw();
    }
}
