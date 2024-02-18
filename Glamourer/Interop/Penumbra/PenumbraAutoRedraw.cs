using Glamourer.State;
using Penumbra.Api.Enums;

namespace Glamourer.Interop.Penumbra;

public class PenumbraAutoRedraw : IDisposable
{
    private readonly Configuration   _config;
    private readonly PenumbraService _penumbra;
    private readonly StateManager    _state;
    private readonly ObjectManager   _objects;

    public PenumbraAutoRedraw(PenumbraService penumbra, Configuration config, StateManager state, ObjectManager objects)
    {
        _penumbra                   =  penumbra;
        _config                     =  config;
        _state                      =  state;
        _objects                    =  objects;
        _penumbra.ModSettingChanged += OnModSettingChange;
    }

    public void Dispose()
        => _penumbra.ModSettingChanged -= OnModSettingChange;

    private void OnModSettingChange(ModSettingChange type, string name, string mod, bool inherited)
    {
        if (!_config.AutoRedrawEquipOnChanges && type is not ModSettingChange.TemporaryMod)
            return;

        var playerName = _penumbra.GetCurrentPlayerCollection();
        if (playerName == name)
            _state.ReapplyState(_objects.Player, StateSource.IpcManual);
    }
}
