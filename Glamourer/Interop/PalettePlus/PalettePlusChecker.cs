using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using OtterGui.Classes;
using OtterGui.Services;

namespace Glamourer.Interop.PalettePlus;

public sealed class PalettePlusChecker : IRequiredService, IDisposable
{
    private readonly Timer _paletteTimer;
    private readonly Configuration _config;
    private readonly IDalamudPluginInterface _pluginInterface;

    public PalettePlusChecker(Configuration config, IDalamudPluginInterface pluginInterface)
    {
        _config = config;
        _pluginInterface = pluginInterface;
        _paletteTimer = new Timer(_ => PalettePlusCheck(), null, TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
        => _paletteTimer.Dispose();

    public void SetAdvancedParameters(bool value)
    {
        _config.UseAdvancedParameters = value;
        PalettePlusCheck();
    }

    private void PalettePlusCheck()
    {
        if (!_config.UseAdvancedParameters)
            return;

        try
        {
            var subscriber = _pluginInterface.GetIpcSubscriber<string>("PalettePlus.ApiVersion");
            subscriber.InvokeFunc();
            Glamourer.Messager.AddMessage(new OtterGui.Classes.Notification(
                "You currently have Palette+ installed. This conflicts with Glamourers advanced options and will cause invalid state.\n\n"
              + "Please uninstall Palette+ and restart your game. Palette+ is deprecated and no longer supported by Mare Synchronos.",
                NotificationType.Warning, 10000));
        }
        catch
        {
            // ignored
        }
    }
}
