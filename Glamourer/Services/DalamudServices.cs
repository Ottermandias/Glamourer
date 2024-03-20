using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.DragDrop;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Services;

namespace Glamourer.Services;

public class DalamudServices
{
    public static void AddServices(ServiceManager services, DalamudPluginInterface pi)
    {
        services.AddExistingService(pi);
        services.AddExistingService(pi.UiBuilder);
        services.AddDalamudService<ICommandManager>(pi);
        services.AddDalamudService<IDataManager>(pi);
        services.AddDalamudService<IClientState>(pi);
        services.AddDalamudService<ICondition>(pi);
        services.AddDalamudService<IGameGui>(pi);
        services.AddDalamudService<IChatGui>(pi);
        services.AddDalamudService<IFramework>(pi);
        services.AddDalamudService<ITargetManager>(pi);
        services.AddDalamudService<IObjectTable>(pi);
        services.AddDalamudService<IKeyState>(pi);
        services.AddDalamudService<IDragDropManager>(pi);
        services.AddDalamudService<ITextureProvider>(pi);
        services.AddDalamudService<IPluginLog>(pi);
        services.AddDalamudService<IGameInteropProvider>(pi);
        services.AddDalamudService<INotificationManager>(pi);
        services.AddDalamudService<IContextMenu>(pi);
    }
}
