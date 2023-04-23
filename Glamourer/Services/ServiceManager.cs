using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.State;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;

namespace Glamourer.Services;

public static class ServiceManager
{
    public static ServiceProvider CreateProvider(DalamudPluginInterface pi, Logger log)
    {
        var services = new ServiceCollection()
            .AddSingleton(log)
            .AddDalamud(pi)
            .AddMeta()
            .AddConfig()
            .AddPenumbra()
            .AddInterop()
            .AddGameData()
            .AddDesigns()
            .AddInterface()
            .AddApi();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    private static IServiceCollection AddDalamud(this IServiceCollection services, DalamudPluginInterface pi)
    {
        new DalamudServices(pi).AddServices(services);
        return services;
    }

    private static IServiceCollection AddMeta(this IServiceCollection services)
        => services.AddSingleton<FilenameService>()
            .AddSingleton<SaveService>()
            .AddSingleton<FrameworkManager>()
            .AddSingleton<ChatService>();

    private static IServiceCollection AddConfig(this IServiceCollection services)
        => services.AddSingleton<Configuration>()
            .AddSingleton<BackupService>();

    private static IServiceCollection AddPenumbra(this IServiceCollection services)
        => services.AddSingleton<PenumbraAttach>();

    private static IServiceCollection AddGameData(this IServiceCollection services)
        => services.AddSingleton<IdentifierService>()
            .AddSingleton<ActorService>()
            .AddSingleton<ItemService>()
            .AddSingleton<ItemManager>()
            .AddSingleton<CustomizationService>()
            .AddSingleton<JobService>();

    private static IServiceCollection AddInterop(this IServiceCollection services)
        => services.AddSingleton<Interop.Interop>()
            .AddSingleton<ObjectManager>();

    private static IServiceCollection AddDesigns(this IServiceCollection services)
        => services.AddSingleton<Design.Manager>()
            .AddSingleton<DesignFileSystem>()
            .AddSingleton<ActiveDesign.Manager>()
            .AddSingleton<FixedDesignManager>();

    private static IServiceCollection AddInterface(this IServiceCollection services)
        => services.AddSingleton<Interface>()
            .AddSingleton<GlamourerWindowSystem>();

    private static IServiceCollection AddApi(this IServiceCollection services)
        => services.AddSingleton<CommandService>()
            .AddSingleton<Glamourer.GlamourerIpc>();
}
