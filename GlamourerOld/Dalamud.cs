using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Glamourer;

public class DalamudServices
{
    public DalamudServices(DalamudPluginInterface pi)
    {
        pi.Inject(this);
    }

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton(PluginInterface);
        services.AddSingleton(Commands);
        services.AddSingleton(GameData);
        services.AddSingleton(ClientState);
        services.AddSingleton(GameGui);
        services.AddSingleton(Chat);
        services.AddSingleton(Framework);
        services.AddSingleton(Targets);
        services.AddSingleton(Objects);
        services.AddSingleton(KeyState);
        services.AddSingleton(this);
        services.AddSingleton(PluginInterface.UiBuilder);
    }

    // @formatter:off
    [PluginService][RequiredVersion("1.0")] public DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public CommandManager         Commands        { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public DataManager            GameData        { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public ClientState            ClientState     { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public GameGui                GameGui         { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public ChatGui                Chat            { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public Framework              Framework       { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public TargetManager          Targets         { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public ObjectTable            Objects         { get; private set; } = null!;
    [PluginService][RequiredVersion("1.0")] public KeyState               KeyState        { get; private set; } = null!;
    // @formatter:on
}
