using Dalamud.Bindings.ImGui;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Widgets;

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
            manager.Timers.Draw("Timers");
        }

        foreach (var header in _headers)
            header.Draw();
    }
}
