using Glamourer.Config;
using Glamourer.Events;
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

public sealed class MainTabBar : TabBar<MainTabType>
{
    private readonly EphemeralConfig _config;
    public readonly  TabSelected     Event;
    public readonly  SettingsTab     Settings;

    public MainTabBar(Logger log, EphemeralConfig config, SettingsTab settings, ActorTab actors, DesignTab designs,
        AutomationTab automation, UnlocksTab unlocks, NpcTab npcs, MessagesTab messages, DebugTab debug, TabSelected @event)
        : base("MainTabBar", log, settings, actors, designs, automation, unlocks, npcs, messages, debug)
    {
        Settings = settings;
        Event    = @event;
        _config  = config;
        TabSelected.Subscribe(OnTabSelected, uint.MinValue);
        Event.Subscribe(OnEvent, Events.TabSelected.Priority.MainWindow);
        NextTab = _config.SelectedMainTab;
    }

    private void OnEvent(in TabSelected.Arguments arguments)
        => NextTab = arguments.Type;

    private void OnTabSelected(in MainTabType arguments)
    {
        if (_config.SelectedMainTab == arguments)
            return;

        _config.SelectedMainTab = arguments;
        _config.Save();
    }
}
