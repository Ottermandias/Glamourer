using Glamourer.Api.Enums;
using Glamourer.Config;
using Glamourer.Gui.Tabs;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.Gui.Tabs.AutomationTab;
using Glamourer.Gui.Tabs.DebugTab;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Gui.Tabs.NpcTab;
using Glamourer.Gui.Tabs.SettingsTab;
using Glamourer.Gui.Tabs.UnlocksTab;
using Luna;

namespace Glamourer.Gui;

public sealed class MainTabBar : TabBar<MainTabType>, IDisposable
{
    private readonly EphemeralConfig   _config;
    private readonly NavigationService _navigation;
    public readonly  SettingsTab       Settings;

    public MainTabBar(LunaLogger log, EphemeralConfig config, SettingsTab settings, ActorTab actors, DesignTab designs,
        AutomationTab automation, UnlocksTab unlocks, NpcTab npcs, MessagesTab messages, DebugTab debug, NavigationService navigation)
        : base("MainTabBar", log, settings, actors, designs, automation, unlocks, npcs, messages, debug)
    {
        Settings    = settings;
        _navigation = navigation;
        _config     = config;
        TabSelected.Subscribe(OnTabSelected, uint.MinValue);
        NextTab                =  _config.SelectedMainTab;
        _navigation.MainTabBar += Select;
    }

    private void Select(MainTabType tab)
        => NextTab = tab;

    private void OnTabSelected(in MainTabType arguments)
    {
        if (_config.SelectedMainTab == arguments)
            return;

        _config.SelectedMainTab = arguments;
        _config.Save();
    }

    public void Dispose()
        => _navigation.MainTabBar -= Select;
}
