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
    public static ServiceManager CreateProvider(IDalamudPluginInterface pi, Logger log, Glamourer glamourer)
    {
        var services = new ServiceManager(log)
            .AddExistingService(log)
            .AddSingleton<MessageService>()
            .AddSingleton<ActorObjectManager>()
            .AddSingleton(p => new CutsceneResolver(p.GetRequiredService<PenumbraService>().CutsceneParent))
            .AddExistingService(glamourer);
        services.AddIServices(typeof(EquipItem).Assembly);
        services.AddIServices(typeof(Glamourer).Assembly);
        services.AddIServices(typeof(ServiceManager).Assembly);
        services.AddSingleton<IGlamourerApi>(p => p.GetRequiredService<GlamourerApi>());
        DalamudServices.AddServices(services, pi);

        services.BuildProvider();
        return services;
    }
}
