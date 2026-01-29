using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DebugTab(ServiceManager manager) : ITab<MainTabType>
{
    private readonly Configuration _config = manager.GetService<Configuration>();

    public bool IsVisible
        => _config.DebugMode;

    public ReadOnlySpan<byte> Label
        => "Debug"u8;

    public MainTabType Identifier
        => MainTabType.Debug;

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
        using var child = Im.Child.Begin("MainWindowChild"u8);
        if (!child)
            return;

        if (Im.Tree.Header("General"u8))
            StartTimeTracker.Draw("Timers"u8);

        foreach (var header in _headers)
            header.Draw();
    }
}
