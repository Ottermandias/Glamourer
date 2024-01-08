using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Glamourer.Gui.Tabs.UnlocksTab;

namespace Glamourer.Gui;

public class GlamourerWindowSystem : IDisposable
{
    private readonly WindowSystem               _windowSystem = new("Glamourer");
    private readonly UiBuilder                  _uiBuilder;
    private readonly MainWindow                 _ui;
    private readonly PenumbraChangedItemTooltip _penumbraTooltip;

    public GlamourerWindowSystem(UiBuilder uiBuilder, MainWindow ui, GenericPopupWindow popups, PenumbraChangedItemTooltip penumbraTooltip,
        Configuration config, UnlocksTab unlocksTab, GlamourerChangelog changelog, DesignQuickBar quick)
    {
        _uiBuilder       = uiBuilder;
        _ui              = ui;
        _penumbraTooltip = penumbraTooltip;
        _windowSystem.AddWindow(ui);
        _windowSystem.AddWindow(popups);
        _windowSystem.AddWindow(unlocksTab);
        _windowSystem.AddWindow(changelog.Changelog);
        _windowSystem.AddWindow(quick);
        _uiBuilder.Draw                  += _windowSystem.Draw;
        _uiBuilder.OpenConfigUi          += _ui.Toggle;
        _uiBuilder.DisableCutsceneUiHide =  !config.HideWindowInCutscene;
    }

    public void Dispose()
    {
        _uiBuilder.Draw         -= _windowSystem.Draw;
        _uiBuilder.OpenConfigUi -= _ui.Toggle;
    }
}
