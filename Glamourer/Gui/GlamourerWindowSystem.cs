using Dalamud.Interface;
using Glamourer.Config;
using Glamourer.Gui.Tabs.AutomationTab;
using Glamourer.Gui.Tabs.UnlocksTab;
using Luna;

namespace Glamourer.Gui;

public sealed class GlamourerWindowSystem : IDisposable, IUiService
{
    private readonly WindowSystem _windowSystem;
    private readonly IUiBuilder   _uiBuilder;
    private readonly MainWindow   _ui;

    public GlamourerWindowSystem(IUiBuilder uiBuilder, MainWindow ui, Configuration config, UnlocksTab unlocksTab, GlamourerChangelog changelog,
        DesignQuickBar quick, AutomationTestWindow automationTest)
    {
        _uiBuilder    = uiBuilder;
        _ui           = ui;
        _windowSystem = WindowSystem.Create(uiBuilder, "Glamourer");
        _windowSystem.AddWindow(ui);
        _windowSystem.AddWindow(unlocksTab);
        _windowSystem.AddWindow(changelog.Changelog);
        _windowSystem.AddWindow(quick);
        _windowSystem.AddWindow(automationTest);
        _uiBuilder.OpenMainUi            += _ui.Toggle;
        _uiBuilder.OpenConfigUi          += _ui.OpenSettings;
        _uiBuilder.DisableCutsceneUiHide =  !config.HideWindowInCutscene;
        _uiBuilder.DisableUserUiHide     =  config.ShowWindowWhenUiHidden;
    }

    public void Dispose()
    {
        _uiBuilder.OpenMainUi   -= _ui.Toggle;
        _uiBuilder.OpenConfigUi -= _ui.OpenSettings;
        _windowSystem.Dispose();
    }
}
