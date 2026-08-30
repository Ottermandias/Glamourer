global using ModIdentifier = (string Identifier, string Name);
global using SettingPresetData = (System.Collections.Generic.Dictionary<(System.Guid Identifier, string? Name),
        (System.Collections.Generic.Dictionary<(System.Guid Identifier, string? Name), byte> Options, bool DisableAllUnknown)> Settings, int
    _priority, short Version, bool _hasPriority, byte _state);
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Glamourer.Config;
using Glamourer.Events;
using Glamourer.State;
using Luna;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Preset;
using Penumbra.Api.Wrappers;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Penumbra;

public sealed class PenumbraSubscriber(MainLogger log, IDalamudPluginInterface pluginInterface, PenumbraReloaded reloaded, Configuration config)
    : BasePenumbraSubscriber(log, pluginInterface, 5, 17), IApiService
{
    private static readonly GameStateWrapper InternalGameState = new();

    public static short ResolveCutscene(ushort index)
        => InternalGameState.HasAdapter ? InternalGameState.ResolveCutsceneActor(index) : (short)index;

    public const int    KeyFixed   = -1610;
    public const string NameFixed  = "Glamourer (Automation)";
    public const int    KeyManual  = -6160;
    public const string NameManual = "Glamourer (Manually)";

    public readonly GameStateWrapper         GameState   = InternalGameState;
    public readonly ModManagerWrapper        Mods        = new();
    public readonly CollectionManagerWrapper Collections = new();
    public          PenumbraPcpService       Pcp { get; private set; } = null!;
    public          PenumbraUiService        Ui  { get; private set; } = null!;

    public CollectionWrapper? Current
    {
        get
        {
            if (!Available)
                return null;

            try
            {
                return Collections.Current;
            }
            catch (ObjectDisposedException)
            {
                // Ignored.
            }
            catch (AdapterMethodMissingException)
            {
                // Ignored.
            }

            return null;
        }
    }

    public (Guid Identifier, string Name, int Index) CurrentCollection
    {
        get
        {
            if (!Available)
                return (Guid.Empty, "", -1);

            try
            {
                return Collections.TypeCollectionId(ApiCollectionType.Current)!.Value;
            }
            catch (ObjectDisposedException)
            {
                // Ignored.
            }
            catch (AdapterMethodMissingException)
            {
                // Ignored.
            }

            return (Guid.Empty, "", -1);
        }
    }

    public void RemoveAllTemporarySettings(int index, StateSource state)
    {
        if (!Available)
            return;

        try
        {
            Collections.RemoveAllTemporarySettingsObject(index, state.IsFixed() ? KeyFixed : KeyManual);
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }
    }

    public void RemoveAllTemporarySettings(Guid collection, StateSource state)
    {
        if (!Available)
            return;

        try
        {
            Collections.RemoveAllTemporarySettings(collection, state.IsFixed() ? KeyFixed : KeyManual);
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }
    }

    public void RemoveAllTemporarySettings(bool fix, bool manual)
    {
        if (!Available)
            return;

        try
        {
            foreach (var collection in Collections.EnumerateNames())
            {
                if (fix)
                    RemoveAllTemporarySettings(collection.Identifier, StateSource.Fixed);
                if (manual)
                    RemoveAllTemporarySettings(collection.Identifier, StateSource.Manual);
            }
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }
    }

    public IEnumerable<(Guid Identifier, string Name, int Index)> EnumerateNames()
    {
        if (!Available)
            return [];

        try
        {
            return Collections.EnumerateNames();
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }

        return [];
    }

    public IEnumerable<ModIdentifier> CheckCurrentChangedItems(string itemName)
    {
        if (!Available)
            return [];

        try
        {
            return Collections.CheckCurrentChangedItems(itemName);
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }

        return [];
    }

    public unsafe Actor GameObjectFromDrawObject(Model drawObject)
    {
        if (!Available)
            return Actor.Null;

        try
        {
            Actor gameObject = GameState.GameObjectFromDrawObject(drawObject.AsDrawObject);
            if (gameObject.Valid)
                return gameObject;

            return GameState.LastGameObject;
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }

        return Actor.Null;
    }

    public void Redraw(int objectIndex, RedrawType type = RedrawType.Redraw)
    {
        if (!Available)
            return;

        try
        {
            GameState.Redraw(objectIndex, type);
        }
        catch (ObjectDisposedException)
        {
            // Ignored.
        }
        catch (AdapterMethodMissingException)
        {
            // Ignored.
        }
    }

    public string SetMod(in ModIdentifier modIdentifier, in SettingPresetData settings, StateSource source, bool respectManual,
        Guid? collectionInput = null, ObjectIndex? index = null)
    {
        if (!Available)
            return "Penumbra is not available.";

        var sb = new StringBuilder();
        try
        {
            using var collection = index.HasValue
                ? Collections.TryGetForObject(index.Value.Index)
                : Collections.GetById(collectionInput ?? CurrentCollection.Identifier);
            if (collection is null)
            {
                sb.Append($"The collection {collection} could not be found.");
                return sb.ToString();
            }

            var modIndex = Mods.IndexByName(modIdentifier);
            if (modIndex < 0)
            {
                sb.Append($"The mod {modIdentifier.Name} [{modIdentifier.Identifier}] could not be found.");
                return sb.ToString();
            }

            if (config.UseTemporarySettings)
            {
                if (HandleRespectManual(modIndex, modIdentifier.Name, collection, respectManual, source, out var temporaryKey,
                        out var temporaryName))
                    return string.Empty;

                collection.ApplyPreset(modIndex, settings, temporaryName, PresetApplyMode.Temporary, temporaryKey);
            }
            else
            {
                collection.ApplyPreset(modIndex, settings, string.Empty, PresetApplyMode.Permanent);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return sb.AppendLine(ex.Message).ToString();
        }
    }

    public SettingPresetData GetModSettings(in ModIdentifier mod, out string source)
    {
        source = string.Empty;
        if (!Available)
            return SettingPresetData.Empty;

        try
        {
            using var collection = Collections.Current!;
            var       index      = Mods.IndexByName(mod);
            if (index < 0)
                return SettingPresetData.Empty;

            var settings = collection.GetPreset(index);
            source = collection.GetTemporarySource(index) ?? string.Empty;
            return settings ?? SettingPresetData.Empty;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching mod settings for {mod.Identifier} from Penumbra:\n{ex}");
            return SettingPresetData.Empty;
        }
    }

    public IReadOnlyList<(ModIdentifier Mod, SettingPresetData Settings, int Count)> GetMods(IReadOnlyList<string> data)
    {
        if (!Available)
            return [];

        try
        {
            using var collection = Collections.Current!;
            var       modCount   = Mods.Count;
            var       ret        = new (ModIdentifier Mod, SettingPresetData Settings, int Count)[modCount];
            foreach (var (index, tuple) in Enumerable.Range(0, modCount)
                         .Select(i => (Mods.NameByIndex(i), collection.GetPreset(i) ?? SettingPresetData.Create(),
                             data.Count(item => Mods.ContainsChangedItem(i, item))))
                         .OrderByDescending(p => p.Item2.State is ModState.Enabled)
                         .ThenByDescending(p => p.Item3)
                         .ThenBy(p => p.Item1.Name)
                         .ThenBy(p => p.Item1.Identifier)
                         .ThenByDescending(p => p.Item2.Priority)
                         .Index())
                ret[index] = tuple;

            return ret;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching mods from Penumbra:\n{ex}");
            return [];
        }
    }

    private static bool HandleRespectManual(int modIndex, string modName, CollectionWrapper collection, bool respectManual, StateSource source,
        out int key, out string name)
    {
        (key, name) = source.IsFixed() ? (KeyFixed, NameFixed) : (KeyManual, NameManual);
        if (!respectManual || key is not KeyFixed)
            return false;

        if (collection.GetTemporarySource(modIndex) is not NameManual)
            return false;

        Glamourer.Log.Debug(
            $"Skipped applying mod settings for [{modName}] through automation because manual settings from Glamourer existed.");
        return true;
    }

    protected override void PluginInitialized()
    {
        if (!GameState.Reconnect(PluginInterface, 1))
            throw new Exception("Unable to connect to GameState adapter.");
        if (!Mods.Reconnect(PluginInterface, 1))
            throw new Exception("Unable to connect to ModManager adapter.");
        if (!Collections.Reconnect(PluginInterface, 1))
            throw new Exception("Unable to connect to CollectionManager adapter.");
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
