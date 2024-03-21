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
using Glamourer.Gui.Tabs.DebugTab;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Gui.Tabs.NpcTab;
using Glamourer.Gui.Tabs.SettingsTab;
using Glamourer.Gui.Tabs.UnlocksTab;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Glamourer.Unlocks;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Glamourer.Services;

public static class StaticServiceManager
{
    public static ServiceManager CreateProvider(DalamudPluginInterface pi, Logger log)
    {
        EventWrapperBase.ChangeLogger(log);
        var services = new ServiceManager(log)
            .AddExistingService(log)
            .AddMeta()
            .AddInterop()
            .AddEvents()
            .AddData()
            .AddDesigns()
            .AddState()
            .AddUi()
            .AddApi();
        DalamudServices.AddServices(services, pi);
        services.AddIServices(typeof(EquipItem).Assembly);
        services.AddIServices(typeof(Glamourer).Assembly);
        services.AddIServices(typeof(ImRaii).Assembly);
        services.CreateProvider();
        return services;
    }

    private static ServiceManager AddMeta(this ServiceManager services)
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

    private static ServiceManager AddEvents(this ServiceManager services)
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

    private static ServiceManager AddData(this ServiceManager services)
        => services.AddSingleton<ObjectIdentification>()
            .AddSingleton<ItemData>()
            .AddSingleton<ActorManager>()
            .AddSingleton<CustomizeService>()
            .AddSingleton<ItemManager>()
            .AddSingleton<GamePathParser>()
            .AddSingleton<HumanModelList>();

    private static ServiceManager AddInterop(this ServiceManager services)
        => services.AddSingleton<VisorService>()
            .AddSingleton<ChangeCustomizeService>()
            .AddSingleton<MetaService>()
            .AddSingleton<UpdateSlotService>()
            .AddSingleton<WeaponService>()
            .AddSingleton<PenumbraService>()
            .AddSingleton(p => new CutsceneResolver(p.GetRequiredService<PenumbraService>().CutsceneParent))
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

    private static ServiceManager AddDesigns(this ServiceManager services)
        => services.AddSingleton<DesignManager>()
            .AddSingleton<DesignFileSystem>()
            .AddSingleton<AutoDesignManager>()
            .AddSingleton<AutoDesignApplier>()
            .AddSingleton<FixedDesignMigrator>()
            .AddSingleton<DesignConverter>()
            .AddSingleton<DesignColors>();

    private static ServiceManager AddState(this ServiceManager services)
        => services.AddSingleton<StateManager>()
            .AddSingleton<StateApplier>()
            .AddSingleton<InternalStateEditor>()
            .AddSingleton<StateListener>()
            .AddSingleton<FunModule>();

    private static ServiceManager AddUi(this ServiceManager services)
        => services.AddSingleton<DebugTab>()
            .AddSingleton<MessagesTab>()
            .AddSingleton<SettingsTab>()
            .AddSingleton<ActorTab>()
            .AddSingleton<ActorSelector>()
            .AddSingleton<ActorPanel>()
            .AddSingleton<NpcPanel>()
            .AddSingleton<NpcSelector>()
            .AddSingleton<LocalNpcAppearanceData>()
            .AddSingleton<NpcTab>()
            .AddSingleton<MainWindow>()
            .AddSingleton<GenericPopupWindow>()
            .AddSingleton<GlamourerWindowSystem>()
            .AddSingleton<CustomizationDrawer>()
            .AddSingleton<EquipmentDrawer>()
            .AddSingleton<DesignFileSystemSelector>()
            .AddSingleton<MultiDesignPanel>()
            .AddSingleton<DesignPanel>()
            .AddSingleton<DesignTab>()
            .AddSingleton<QuickDesignCombo>()
            .AddSingleton<LinkDesignCombo>()
            .AddSingleton<RandomDesignCombo>()
            .AddSingleton<SpecialDesignCombo>()
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
            .AddSingleton<DesignColorUi>()
            .AddSingleton<NpcCombo>();

    private static ServiceManager AddApi(this ServiceManager services)
        => services.AddSingleton<CommandService>()
            .AddSingleton<GlamourerIpc>();
}
