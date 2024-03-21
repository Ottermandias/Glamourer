using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Services;

namespace Glamourer;

public class Glamourer : IDalamudPlugin
{
    public string Name
        => "Glamourer";

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public static readonly string CommitHash =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";


    public static readonly Logger         Log = new();
    public static          MessageService Messager { get; private set; } = null!;

    private readonly ServiceManager _services;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        try
        {
            _services = StaticServiceManager.CreateProvider(pluginInterface, Log);
            Messager  = _services.GetService<MessageService>();
            _services.EnsureRequiredServices();

            _services.GetService<VisorService>();
            _services.GetService<WeaponService>();
            _services.GetService<ScalingService>();
            _services.GetService<StateListener>();         // Initialize State Listener.
            _services.GetService<GlamourerWindowSystem>(); // initialize ui.
            _services.GetService<CommandService>();        // initialize commands.
            _services.GetService<GlamourerIpc>();          // initialize IPC.
            Log.Information($"Glamourer v{Version} loaded successfully.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }


    public void Dispose()
        => _services?.Dispose();
}
