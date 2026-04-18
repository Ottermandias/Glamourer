using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Api.Api;
using Glamourer.Interop.Penumbra;
using Luna;
using Microsoft.Extensions.DependencyInjection;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public static class StaticServiceManager
{
    public static ServiceManager CreateProvider(IDalamudPluginInterface pi, MainLogger log, Glamourer glamourer)
    {
        var services = new ServiceManager(log)
            .AddDalamudServices(pi)
            .AddExistingService(log)
            .AddSingleton<MessageService>()
            .AddSingleton<ActorObjectManager>()
            .AddSingleton(p => new CutsceneResolver(p.GetRequiredService<CutsceneResolveService>().CutsceneParent))
            .AddExistingService(glamourer);
        services.AddIServices(typeof(EquipItem).Assembly);
        services.AddIServices(typeof(Glamourer).Assembly);
        services.AddIServices(typeof(ServiceManager).Assembly);
        services.AddSingleton<IGlamourerApi>(p => p.GetRequiredService<GlamourerApi>());

        services.BuildProvider();
        return services;
    }
}
