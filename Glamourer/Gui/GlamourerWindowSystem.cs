using Dalamud.Interface;
using Glamourer.Config;
using Glamourer.Gui.Tabs.UnlocksTab;
using Luna;
using WindowSystem = Dalamud.Interface.Windowing.WindowSystem;

namespace Glamourer.Gui;

public sealed class GlamourerWindowSystem : IDisposable, IUiService
{
    private readonly WindowSystem _windowSystem = new("Glamourer");
    private readonly IUiBuilder   _uiBuilder;
    private readonly MainWindow   _ui;

    public GlamourerWindowSystem(IUiBuilder uiBuilder, MainWindow ui, GenericPopupWindow popups,
        Configuration config, UnlocksTab unlocksTab, GlamourerChangelog changelog, DesignQuickBar quick)
    {
        _uiBuilder = uiBuilder;
        _ui        = ui;
        _windowSystem.AddWindow(ui);
        _windowSystem.AddWindow(popups);
        _windowSystem.AddWindow(unlocksTab);
        _windowSystem.AddWindow(changelog.Changelog);
        _windowSystem.AddWindow(quick);
        _uiBuilder.OpenMainUi            += _ui.Toggle;
        _uiBuilder.Draw                  += _windowSystem.Draw;
        _uiBuilder.OpenConfigUi          += _ui.OpenSettings;
        _uiBuilder.DisableCutsceneUiHide =  !config.HideWindowInCutscene;
        _uiBuilder.DisableUserUiHide     =  config.ShowWindowWhenUiHidden;
    }

    public void Dispose()
    {
        _uiBuilder.OpenMainUi   -= _ui.Toggle;
        _uiBuilder.Draw         -= _windowSystem.Draw;
        _uiBuilder.OpenConfigUi -= _ui.OpenSettings;
    }
}
