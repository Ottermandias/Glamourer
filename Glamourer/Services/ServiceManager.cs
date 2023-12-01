using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Tabs;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.Gui.Tabs.AutomationTab;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Gui.Tabs.UnlocksTab;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Glamourer.Unlocks;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;
using Penumbra.GameData.Data;

namespace Glamourer.Services;

public static class ServiceManager
{
    public static ServiceProvider CreateProvider(DalamudPluginInterface pi, Logger log)
    {
        EventWrapper.ChangeLogger(log);
        var services = new ServiceCollection()
            .AddSingleton(log)
            .AddDalamud(pi)
            .AddMeta()
            .AddInterop()
            .AddEvents()
            .AddData()
            .AddDesigns()
            .AddState()
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
        => services.AddSingleton<MessageService>()
            .AddSingleton<FilenameService>()
            .AddSingleton<BackupService>()
            .AddSingleton<FrameworkManager>()
            .AddSingleton<SaveService>()
            .AddSingleton<CodeService>()
            .AddSingleton<ConfigMigrationService>()
            .AddSingleton<Configuration>()
            .AddSingleton<EphemeralConfig>()
            .AddSingleton<TextureService>()
            .AddSingleton<FavoriteManager>();

    private static IServiceCollection AddEvents(this IServiceCollection services)
        => services.AddSingleton<VisorStateChanged>()
            .AddSingleton<SlotUpdating>()
            .AddSingleton<DesignChanged>()
            .AddSingleton<AutomationChanged>()
            .AddSingleton<StateChanged>()
            .AddSingleton<WeaponLoading>()
            .AddSingleton<HeadGearVisibilityChanged>()
            .AddSingleton<WeaponVisibilityChanged>()
            .AddSingleton<ObjectUnlocked>()
            .AddSingleton<TabSelected>()
            .AddSingleton<MovedEquipment>()
            .AddSingleton<EquippedGearset>()
            .AddSingleton<GPoseService>()
            .AddSingleton<PenumbraReloaded>();

    private static IServiceCollection AddData(this IServiceCollection services)
        => services.AddSingleton<IdentifierService>()
            .AddSingleton<ItemService>()
            .AddSingleton<ActorService>()
            .AddSingleton<CustomizationService>()
            .AddSingleton<ItemManager>()
            .AddSingleton<HumanModelList>();

    private static IServiceCollection AddInterop(this IServiceCollection services)
        => services.AddSingleton<VisorService>()
            .AddSingleton<ChangeCustomizeService>()
            .AddSingleton<MetaService>()
            .AddSingleton<UpdateSlotService>()
            .AddSingleton<WeaponService>()
            .AddSingleton<PenumbraService>()
            .AddSingleton<ObjectManager>()
            .AddSingleton<PenumbraAutoRedraw>()
            .AddSingleton<JobService>()
            .AddSingleton<CustomizeUnlockManager>()
            .AddSingleton<ItemUnlockManager>()
            .AddSingleton<ImportService>()
            .AddSingleton<CrestService>()
            .AddSingleton<InventoryService>()
            .AddSingleton<ContextMenuService>()
            .AddSingleton<ScalingService>();

    private static IServiceCollection AddDesigns(this IServiceCollection services)
        => services.AddSingleton<DesignManager>()
            .AddSingleton<DesignFileSystem>()
            .AddSingleton<AutoDesignManager>()
            .AddSingleton<AutoDesignApplier>()
            .AddSingleton<FixedDesignMigrator>()
            .AddSingleton<DesignConverter>()
            .AddSingleton<DesignColors>();

    private static IServiceCollection AddState(this IServiceCollection services)
        => services.AddSingleton<StateManager>()
            .AddSingleton<StateApplier>()
            .AddSingleton<StateEditor>()
            .AddSingleton<StateListener>()
            .AddSingleton<FunModule>();

    private static IServiceCollection AddUi(this IServiceCollection services)
        => services.AddSingleton<DebugTab>()
            .AddSingleton<MessagesTab>()
            .AddSingleton<SettingsTab>()
            .AddSingleton<ActorTab>()
            .AddSingleton<ActorSelector>()
            .AddSingleton<ActorPanel>()
            .AddSingleton<MainWindow>()
            .AddSingleton<GenericPopupWindow>()
            .AddSingleton<GlamourerWindowSystem>()
            .AddSingleton<CustomizationDrawer>()
            .AddSingleton<EquipmentDrawer>()
            .AddSingleton<DesignFileSystemSelector>()
            .AddSingleton<MultiDesignPanel>()
            .AddSingleton<DesignPanel>()
            .AddSingleton<DesignTab>()
            .AddSingleton<DesignCombo>()
            .AddSingleton<RevertDesignCombo>()
            .AddSingleton<ModAssociationsTab>()
            .AddSingleton<DesignDetailTab>()
            .AddSingleton<UnlockTable>()
            .AddSingleton<UnlockOverview>()
            .AddSingleton<UnlocksTab>()
            .AddSingleton<PenumbraChangedItemTooltip>()
            .AddSingleton<AutomationTab>()
            .AddSingleton<SetSelector>()
            .AddSingleton<SetPanel>()
            .AddSingleton<IdentifierDrawer>()
            .AddSingleton<GlamourerChangelog>()
            .AddSingleton<DesignQuickBar>()
            .AddSingleton<DesignColorUi>();

    private static IServiceCollection AddApi(this IServiceCollection services)
        => services.AddSingleton<CommandService>()
            .AddSingleton<GlamourerIpc>();
}
