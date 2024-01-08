using Glamourer.State;
using Penumbra.Api.Enums;

namespace Glamourer.Interop.Penumbra;

public class PenumbraAutoRedraw : IDisposable
{
    private readonly Configuration   _config;
    private readonly PenumbraService _penumbra;
    private readonly StateManager    _state;
    private readonly ObjectManager   _objects;
    private          bool            _enabled;

    public PenumbraAutoRedraw(PenumbraService penumbra, Configuration config, StateManager state, ObjectManager objects)
    {
        _penumbra = penumbra;
        _config   = config;
        _state    = state;
        _objects  = objects;
        if (_config.AutoRedrawEquipOnChanges)
            Enable();
    }

    public void SetState(bool value)
    {
        if (value == _config.AutoRedrawEquipOnChanges)
            return;

        _config.AutoRedrawEquipOnChanges = value;
        _config.Save();
        if (value)
            Enable();
        else
            Disable();
    }

    public void Enable()
    {
        if (_enabled)
            return;

        _penumbra.ModSettingChanged += OnModSettingChange;
        _enabled                    =  true;
    }

    public void Disable()
    {
        if (!_enabled)
            return;

        _penumbra.ModSettingChanged -= OnModSettingChange;
        _enabled                    =  false;
    }

    public void Dispose()
    {
        Disable();
    }

    private void OnModSettingChange(ModSettingChange type, string name, string mod, bool inherited)
    {
        var playerName = _penumbra.GetCurrentPlayerCollection();
        if (playerName == name)
            _state.ReapplyState(_objects.Player);
    }
}
