using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui;
using Glamourer.Gui.Tabs;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
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
            .AddInterop()
            .AddEvents()
            .AddData()
            .AddDesigns()
            .AddUi()
            .AddApi();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    private static IServiceCollection AddDalamud(this IServiceCollection services, DalamudPluginInterface pi)
    {
        new DalamudServices(pi).AddServices(services);
        return services;
    }

    private static IServiceCollection AddMeta(this IServiceCollection services)
        => services.AddSingleton<ChatService>()
            .AddSingleton<FilenameService>()
            .AddSingleton<BackupService>()
            .AddSingleton<FrameworkManager>()
            .AddSingleton<SaveService>()
            .AddSingleton<ConfigMigrationService>()
            .AddSingleton<Configuration>();

    private static IServiceCollection AddEvents(this IServiceCollection services)
        => services.AddSingleton<VisorStateChanged>()
            .AddSingleton<UpdatedSlot>()
            .AddSingleton<DesignChanged>();

    private static IServiceCollection AddData(this IServiceCollection services)
        => services.AddSingleton<IdentifierService>()
            .AddSingleton<ItemService>()
            .AddSingleton<ActorService>()
            .AddSingleton<CustomizationService>()
            .AddSingleton<ItemManager>();

    private static IServiceCollection AddInterop(this IServiceCollection services)
        => services.AddSingleton<VisorService>()
            .AddSingleton<ChangeCustomizeService>()
            .AddSingleton<UpdateSlotService>()
            .AddSingleton<WeaponService>()
            .AddSingleton<PenumbraService>()
            .AddSingleton<ObjectManager>();

    private static IServiceCollection AddDesigns(this IServiceCollection services)
        => services.AddSingleton<DesignManager>()
            .AddSingleton<DesignFileSystem>();

    private static IServiceCollection AddUi(this IServiceCollection services)
        => services.AddSingleton<DebugTab>()
            .AddSingleton<MainWindow>()
            .AddSingleton<GlamourerWindowSystem>();

    private static IServiceCollection AddApi(this IServiceCollection services)
        => services.AddSingleton<CommandService>();
}
