using System;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace Glamourer.Gui;

public class GlamourerWindowSystem : IDisposable
{
    private readonly WindowSystem               _windowSystem = new("Glamourer");
    private readonly UiBuilder                  _uiBuilder;
    private readonly MainWindow                 _ui;
    private readonly PenumbraChangedItemTooltip _penumbraTooltip;

    public GlamourerWindowSystem(UiBuilder uiBuilder, MainWindow ui, GenericPopupWindow popups, PenumbraChangedItemTooltip penumbraTooltip)
    {
        _uiBuilder       = uiBuilder;
        _ui              = ui;
        _penumbraTooltip = penumbraTooltip;
        _windowSystem.AddWindow(ui);
        _windowSystem.AddWindow(popups);
        _uiBuilder.Draw         += _windowSystem.Draw;
        _uiBuilder.OpenConfigUi += _ui.Toggle;
    }

    public void Dispose()
    {
        _uiBuilder.Draw         -= _windowSystem.Draw;
        _uiBuilder.OpenConfigUi -= _ui.Toggle;
    }
}