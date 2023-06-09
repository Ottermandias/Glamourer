using System;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace Glamourer.Gui;

public class GlamourerWindowSystem : IDisposable
{
    private readonly WindowSystem _windowSystem = new("Glamourer");
    private readonly UiBuilder    _uiBuilder;
    private readonly Interface    _ui;

    public GlamourerWindowSystem(UiBuilder uiBuilder, Interface ui)
    {
        _uiBuilder = uiBuilder;
        _ui        = ui;
        _windowSystem.AddWindow(ui);
        _uiBuilder.Draw         += _windowSystem.Draw;
        _uiBuilder.OpenConfigUi += _ui.Toggle;
    }

    public void Dispose()
    {
        _uiBuilder.Draw         -= _windowSystem.Draw;
        _uiBuilder.OpenConfigUi -= _ui.Toggle;
    }

    public void Toggle()
        => _ui.Toggle();
}
