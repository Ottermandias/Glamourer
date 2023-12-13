using System.Reflection;
using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;

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

    private readonly ServiceProvider _services;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        try
        {
            _services = ServiceManager.CreateProvider(pluginInterface, Log);
            Messager  = _services.GetRequiredService<MessageService>();
            _services.GetRequiredService<VisorService>();
            _services.GetRequiredService<WeaponService>();
            _services.GetRequiredService<ScalingService>();
            _services.GetRequiredService<StateListener>();         // Initialize State Listener.
            _services.GetRequiredService<GlamourerWindowSystem>(); // initialize ui.
            _services.GetRequiredService<CommandService>();        // initialize commands.
            _services.GetRequiredService<GlamourerIpc>();          // initialize IPC.
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
