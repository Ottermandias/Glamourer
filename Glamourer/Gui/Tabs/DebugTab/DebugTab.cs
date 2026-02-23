using Glamourer.Config;
using Glamourer.Services;
using ImSharp;
using InteropGenerator.Runtime;
using Luna;
using Penumbra.GameData.Files.ShaderStructs;
using Vortice.Direct3D11.Debug;

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
        {
            if (Im.Button("Open Config Directory"u8))
                OpenFileOrFolder(manager.GetService<FilenameService>().ConfigurationDirectory);
            StartTimeTracker.Draw("Timers"u8, manager.GetService<StartTimeTracker>());
        }

        foreach (var header in _headers)
            header.Draw();
    }

    private static void OpenFileOrFolder(string text)
    {
        try
        {
            var process = new ProcessStartInfo(text)
            {
                UseShellExecute = true,
            };
            Process.Start(process);
        }
        catch
        {
            // Ignored
        }
    }
}
