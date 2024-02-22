using Dalamud.Plugin.Services;
using Glamourer.State;
using OtterGui.Services;
using Penumbra.Api.Enums;

namespace Glamourer.Interop.Penumbra;

public class PenumbraAutoRedraw : IDisposable, IRequiredService
{
    private readonly Configuration   _config;
    private readonly PenumbraService _penumbra;
    private readonly StateManager    _state;
    private readonly ObjectManager   _objects;
    private readonly IFramework      _framework;

    public PenumbraAutoRedraw(PenumbraService penumbra, Configuration config, StateManager state, ObjectManager objects, IFramework framework)
    {
        _penumbra                   =  penumbra;
        _config                     =  config;
        _state                      =  state;
        _objects                    =  objects;
        _framework                  =  framework;
        _penumbra.ModSettingChanged += OnModSettingChange;
    }

    public void Dispose()
        => _penumbra.ModSettingChanged -= OnModSettingChange;

    private void OnModSettingChange(ModSettingChange type, string name, string mod, bool inherited)
    {
        if (type is ModSettingChange.TemporaryMod)
            _framework.RunOnFrameworkThread(() =>
            {
                _objects.Update();
                foreach (var (id, state) in _state)
                {
                    if (!_objects.TryGetValue(id, out var actors) || !actors.Valid)
                        continue;

                    var collection = _penumbra.GetActorCollection(actors.Objects[0]);
                    if (collection != name)
                        continue;

                    foreach (var actor in actors.Objects)
                        _state.ReapplyState(actor, state, StateSource.IpcManual);
                    Glamourer.Log.Debug($"Automatically applied mod settings of type {type} to {id.Incognito(null)}.");
                }
            });
        else if (_config.AutoRedrawEquipOnChanges)
            _framework.RunOnFrameworkThread(() =>
            {
                var playerName = _penumbra.GetCurrentPlayerCollection();
                if (playerName == name)
                    _state.ReapplyState(_objects.Player, StateSource.IpcManual);
                Glamourer.Log.Debug(
                    $"Automatically applied mod settings of type {type} to {_objects.PlayerData.Identifier.Incognito(null)} (Local Player).");
            });
    }
}
