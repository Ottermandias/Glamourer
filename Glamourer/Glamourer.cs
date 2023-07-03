using System.Reflection;
using Dalamud.Plugin;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Services;
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


    public static readonly Logger      Log = new();
    public static          ChatService Chat { get; private set; } = null!;


    private readonly ServiceProvider _services;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        try
        {
            _services = ServiceManager.CreateProvider(pluginInterface, Log);
            Chat      = _services.GetRequiredService<ChatService>();
            _services.GetRequiredService<BackupService>();         // call backup service.
            _services.GetRequiredService<GlamourerWindowSystem>(); // initialize ui.
            _services.GetRequiredService<CommandService>();        // initialize commands.
            _services.GetRequiredService<VisorService>();
        }
        catch
        {
            Dispose();
            throw;
        }
    }


    public void Dispose()
    {
        _services?.Dispose();
    }
}
