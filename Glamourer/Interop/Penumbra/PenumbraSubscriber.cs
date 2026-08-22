global using ModIdentifier = (string Identifier, string Name);
global using SettingPresetData = (System.Collections.Generic.Dictionary<(System.Guid Identifier, string? Name),
        (System.Collections.Generic.Dictionary<(System.Guid Identifier, string? Name), byte> Options, bool DisableAllUnknown)> Settings, int
    _priority, short Version, bool _hasPriority, byte _state);
using Dalamud.Plugin;
using Glamourer.Events;
using Glamourer.State;
using Luna;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.Wrappers;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Penumbra;

public sealed class PenumbraSubscriber(MainLogger log, IDalamudPluginInterface pluginInterface, PenumbraReloaded reloaded)
    : BasePenumbraSubscriber(log, pluginInterface, 5, 17), IApiService
{
    public const int RequiredPenumbraBreakingVersion = 5;
    public const int RequiredPenumbraFeatureVersion  = 13;

    private const int    KeyFixed   = -1610;
    private const string NameFixed  = "Glamourer (Automation)";
    private const int    KeyManual  = -6160;
    private const string NameManual = "Glamourer (Manually)";

    public readonly GameStateWrapper         GameState   = new();
    public readonly ModManagerWrapper        Mods        = new();
    public readonly CollectionManagerWrapper Collections = new();
    public          PenumbraPcpService       Pcp { get; private set; } = null!;
    public          PenumbraUiService        Ui  { get; private set; } = null!;

    public (Guid Identifier, string Name, int Index) CurrentCollection
        => Collections.TypeCollectionId(ApiCollectionType.Current)!.Value;

    public void RemoveAllTemporarySettings(int index, StateSource state)
        => Collections.RemoveAllTemporarySettingsObject(index, state.IsFixed() ? KeyFixed : KeyManual);

    public void RemoveAllTemporarySettings(Guid collection, StateSource state)
        => Collections.RemoveAllTemporarySettings(collection, state.IsFixed() ? KeyFixed : KeyManual);

    public void RemoveAllTemporarySettings(bool fix, bool manual)
    {
        if (!Available)
            return;

        foreach (var collection in Collections.EnumerateNames())
        {
            if (fix)
                RemoveAllTemporarySettings(collection.Identifier, StateSource.Fixed);
            if (manual)
                RemoveAllTemporarySettings(collection.Identifier, StateSource.Manual);
        }
    }

    public unsafe Actor GameObjectFromDrawObject(Model drawObject)
    {
        Actor gameObject = GameState.GameObjectFromDrawObject(drawObject.AsDrawObject);
        if (gameObject.Valid)
            return gameObject;

        return GameState.LastGameObject;
    }

    protected override void PluginInitialized()
    {
        GameState.Reconnect(PluginInterface, 1);
        Mods.Reconnect(PluginInterface, 1);
        Collections.Reconnect(PluginInterface, 1);
        Ui.Attach();
        reloaded.Invoke();
    }

    protected override void Initialize()
    {
        Ui  = new PenumbraUiService(PluginInterface);
        Pcp = new PenumbraPcpService(PluginInterface);
    }

    protected override void InternalDispose()
    {
        RemoveAllTemporarySettings(true, true);
        GameState.Dispose();
        Collections.Dispose();
        Mods.Dispose();
        Ui?.Dispose();
        Pcp?.Dispose();
    }

    protected override void PluginDisposed()
    {
        Mods.Disconnect();
        Collections.Disconnect();
        GameState.Disconnect();
        Ui.Detach();
    }
}
