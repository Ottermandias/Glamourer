using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Glamourer.Events;
using OtterGui.Classes;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.Penumbra;

public readonly record struct Mod(string Name, string DirectoryName) : IComparable<Mod>
{
    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, StringComparison.Ordinal);
    }
}

public readonly record struct ModSettings(Dictionary<string, List<string>> Settings, int Priority, bool Enabled, bool ForceInherit, bool Remove)
{
    public ModSettings()
        : this(new Dictionary<string, List<string>>(), 0, false, false, false)
    { }

    public static ModSettings Empty
        => new();
}

public class PenumbraService : IDisposable
{
    public const int RequiredPenumbraBreakingVersion     = 5;
    public const int RequiredPenumbraFeatureVersion      = 3;
    public const int RequiredPenumbraFeatureVersionTemp  = 4;
    public const int RequiredPenumbraFeatureVersionTemp2 = 5;

    private const int Key = -1610;

    private readonly IDalamudPluginInterface                               _pluginInterface;
    private readonly Configuration                                         _config;
    private readonly EventSubscriber<ChangedItemType, uint>                _tooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint>   _clickSubscriber;
    private readonly EventSubscriber<nint, Guid, nint, nint, nint>         _creatingCharacterBase;
    private readonly EventSubscriber<nint, Guid, nint>                     _createdCharacterBase;
    private readonly EventSubscriber<ModSettingChange, Guid, string, bool> _modSettingChanged;

    private global::Penumbra.Api.IpcSubscribers.GetCollectionsByIdentifier?          _collectionByIdentifier;
    private global::Penumbra.Api.IpcSubscribers.GetCollections?                      _collections;
    private global::Penumbra.Api.IpcSubscribers.RedrawObject?                        _redraw;
    private global::Penumbra.Api.IpcSubscribers.GetDrawObjectInfo?                   _drawObjectInfo;
    private global::Penumbra.Api.IpcSubscribers.GetCutsceneParentIndex?              _cutsceneParent;
    private global::Penumbra.Api.IpcSubscribers.GetCollectionForObject?              _objectCollection;
    private global::Penumbra.Api.IpcSubscribers.GetModList?                          _getMods;
    private global::Penumbra.Api.IpcSubscribers.GetCollection?                       _currentCollection;
    private global::Penumbra.Api.IpcSubscribers.GetCurrentModSettings?               _getCurrentSettings;
    private global::Penumbra.Api.IpcSubscribers.TryInheritMod?                       _inheritMod;
    private global::Penumbra.Api.IpcSubscribers.TrySetMod?                           _setMod;
    private global::Penumbra.Api.IpcSubscribers.TrySetModPriority?                   _setModPriority;
    private global::Penumbra.Api.IpcSubscribers.TrySetModSetting?                    _setModSetting;
    private global::Penumbra.Api.IpcSubscribers.TrySetModSettings?                   _setModSettings;
    private global::Penumbra.Api.IpcSubscribers.SetTemporaryModSettings?             _setTemporaryModSettings;
    private global::Penumbra.Api.IpcSubscribers.SetTemporaryModSettingsPlayer?       _setTemporaryModSettingsPlayer;
    private global::Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettings?          _removeTemporaryModSettings;
    private global::Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettingsPlayer?    _removeTemporaryModSettingsPlayer;
    private global::Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettings?       _removeAllTemporaryModSettings;
    private global::Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettingsPlayer? _removeAllTemporaryModSettingsPlayer;
    private global::Penumbra.Api.IpcSubscribers.QueryTemporaryModSettings?           _queryTemporaryModSettings;
    private global::Penumbra.Api.IpcSubscribers.OpenMainWindow?                      _openModPage;

    private readonly IDisposable _initializedEvent;
    private readonly IDisposable _disposedEvent;

    private readonly PenumbraReloaded _penumbraReloaded;

    public bool     Available    { get; private set; }
    public int      CurrentMajor { get; private set; }
    public int      CurrentMinor { get; private set; }
    public DateTime AttachTime   { get; private set; }

    public PenumbraService(IDalamudPluginInterface pi, PenumbraReloaded penumbraReloaded, Configuration config)
    {
        _pluginInterface       = pi;
        _penumbraReloaded      = penumbraReloaded;
        _config                = config;
        _initializedEvent      = global::Penumbra.Api.IpcSubscribers.Initialized.Subscriber(pi, Reattach);
        _disposedEvent         = global::Penumbra.Api.IpcSubscribers.Disposed.Subscriber(pi, Unattach);
        _tooltipSubscriber     = global::Penumbra.Api.IpcSubscribers.ChangedItemTooltip.Subscriber(pi);
        _clickSubscriber       = global::Penumbra.Api.IpcSubscribers.ChangedItemClicked.Subscriber(pi);
        _createdCharacterBase  = global::Penumbra.Api.IpcSubscribers.CreatedCharacterBase.Subscriber(pi);
        _creatingCharacterBase = global::Penumbra.Api.IpcSubscribers.CreatingCharacterBase.Subscriber(pi);
        _modSettingChanged     = global::Penumbra.Api.IpcSubscribers.ModSettingChanged.Subscriber(pi);
        Reattach();
    }

    public event Action<MouseButton, ChangedItemType, uint> Click
    {
        add => _clickSubscriber.Event += value;
        remove => _clickSubscriber.Event -= value;
    }

    public event Action<ChangedItemType, uint> Tooltip
    {
        add => _tooltipSubscriber.Event += value;
        remove => _tooltipSubscriber.Event -= value;
    }


    public event Action<nint, Guid, nint, nint, nint> CreatingCharacterBase
    {
        add => _creatingCharacterBase.Event += value;
        remove => _creatingCharacterBase.Event -= value;
    }

    public event Action<nint, Guid, nint> CreatedCharacterBase
    {
        add => _createdCharacterBase.Event += value;
        remove => _createdCharacterBase.Event -= value;
    }

    public event Action<ModSettingChange, Guid, string, bool> ModSettingChanged
    {
        add => _modSettingChanged.Event += value;
        remove => _modSettingChanged.Event -= value;
    }

    public Dictionary<Guid, string> GetCollections()
        => Available ? _collections!.Invoke() : [];

    public ModSettings GetModSettings(in Mod mod, out string source)
    {
        source = string.Empty; 
        if (!Available)
            return ModSettings.Empty;

        try
        {
            var collection = _currentCollection!.Invoke(ApiCollectionType.Current);
            if (_queryTemporaryModSettings != null)
            {
                var tempEc = _queryTemporaryModSettings.Invoke(collection!.Value.Id, mod.DirectoryName, out var tempTuple, out source);
                if (tempEc is PenumbraApiEc.Success && tempTuple != null)
                    return new ModSettings(tempTuple.Value.Settings, tempTuple.Value.Priority, tempTuple.Value.Enabled,
                        tempTuple.Value.ForceInherit, false);
            }

            var (ec, tuple) = _getCurrentSettings!.Invoke(collection!.Value.Id, mod.DirectoryName);
            if (ec is not PenumbraApiEc.Success)
                return ModSettings.Empty;

            return tuple.HasValue
                ? new ModSettings(tuple.Value.Item3, tuple.Value.Item2, tuple.Value.Item1, false, false)
                : ModSettings.Empty;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching mod settings for {mod.DirectoryName} from Penumbra:\n{ex}");
            return ModSettings.Empty;
        }
    }

    public (Guid Id, string Name)? CollectionByIdentifier(string identifier)
    {
        if (!Available)
            return null;

        var ret = _collectionByIdentifier!.Invoke(identifier);
        if (ret.Count == 0)
            return null;

        return ret[0];
    }

    public IReadOnlyList<(Mod Mod, ModSettings Settings)> GetMods()
    {
        if (!Available)
            return [];

        try
        {
            var allMods    = _getMods!.Invoke();
            var collection = _currentCollection!.Invoke(ApiCollectionType.Current);
            return allMods
                .Select(m => (m.Key, m.Value, _getCurrentSettings!.Invoke(collection!.Value.Id, m.Key)))
                .Where(t => t.Item3.Item1 is PenumbraApiEc.Success)
                .Select(t => (new Mod(t.Item2, t.Item1),
                    !t.Item3.Item2.HasValue
                        ? ModSettings.Empty
                        : new ModSettings(t.Item3.Item2!.Value.Item3, t.Item3.Item2!.Value.Item2, t.Item3.Item2!.Value.Item1, false, false)))
                .OrderByDescending(p => p.Item2.Enabled)
                .ThenBy(p => p.Item1.Name)
                .ThenBy(p => p.Item1.DirectoryName)
                .ThenByDescending(p => p.Item2.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching mods from Penumbra:\n{ex}");
            return [];
        }
    }

    public void OpenModPage(Mod mod)
    {
        if (!Available)
            return;

        if (_openModPage!.Invoke(TabType.Mods, mod.DirectoryName) == PenumbraApiEc.ModMissing)
            Glamourer.Messager.NotificationMessage($"Could not open the mod {mod.Name}, no fitting mod was found in your Penumbra install.",
                NotificationType.Info, false);
    }

    public (Guid Id, string Name) CurrentCollection
        => Available ? _currentCollection!.Invoke(ApiCollectionType.Current)!.Value : (Guid.Empty, "<Unavailable>");

    /// <summary>
    /// Try to set all mod settings as desired. Only sets when the mod should be enabled.
    /// If it is disabled, ignore all other settings.
    /// </summary>
    public string SetMod(Mod mod, ModSettings settings, Guid? collectionInput = null, ObjectIndex? index = null)
    {
        if (!Available)
            return "Penumbra is not available.";

        var sb = new StringBuilder();
        try
        {
            var collection = collectionInput ?? _currentCollection!.Invoke(ApiCollectionType.Current)!.Value.Id;
            if (_config.UseTemporarySettings && _setTemporaryModSettings != null)
                SetModTemporary(sb, mod, settings, collection, index);
            else
                SetModPermanent(sb, mod, settings, collection);

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return sb.AppendLine(ex.Message).ToString();
        }
    }

    public void RemoveAllTemporarySettings(Guid collection)
        => _removeAllTemporaryModSettings?.Invoke(collection, Key);

    public void RemoveAllTemporarySettings(ObjectIndex index)
        => _removeAllTemporaryModSettingsPlayer?.Invoke(index.Index, Key);

    public void ClearAllTemporarySettings()
    {
        if (!Available || _removeAllTemporaryModSettings == null)
            return;

        var collections = _collections!.Invoke();
        foreach (var collection in collections)
            RemoveAllTemporarySettings(collection.Key);
    }

    private void SetModTemporary(StringBuilder sb, Mod mod, ModSettings settings, Guid collection, ObjectIndex? index)
    {
        var ex = settings.Remove
            ? index.HasValue
                ? _removeTemporaryModSettingsPlayer!.Invoke(index.Value.Index, mod.DirectoryName, Key)
                : _removeTemporaryModSettings!.Invoke(collection, mod.DirectoryName, Key)
            : index.HasValue
                ? _setTemporaryModSettingsPlayer!.Invoke(index.Value.Index, mod.DirectoryName, settings.ForceInherit, settings.Enabled,
                    settings.Priority,
                    settings.Settings.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value), "Glamourer", Key)
                : _setTemporaryModSettings!.Invoke(collection, mod.DirectoryName, settings.ForceInherit, settings.Enabled, settings.Priority,
                    settings.Settings.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value), "Glamourer", Key);
        switch (ex)
        {
            case PenumbraApiEc.InvalidArgument:
                sb.Append($"No actor with index {index!.Value.Index} could be identified.");
                return;
            case PenumbraApiEc.ModMissing:
                sb.Append($"The mod {mod.Name} [{mod.DirectoryName}] could not be found.");
                return;
            case PenumbraApiEc.CollectionMissing:
                sb.Append($"The collection {collection} could not be found.");
                return;
            case PenumbraApiEc.TemporarySettingImpossible:
                sb.Append($"The collection {collection} can not have settings.");
                return;
            case PenumbraApiEc.TemporarySettingDisallowed:
                sb.Append($"The mod {mod.Name} [{mod.DirectoryName}] already has temporary settings with a different key in {collection}.");
                return;
            case PenumbraApiEc.OptionGroupMissing:
            case PenumbraApiEc.OptionMissing:
                sb.Append($"The provided settings for {mod.Name} [{mod.DirectoryName}] did not correspond to its actual options.");
                return;
        }
    }

    private void SetModPermanent(StringBuilder sb, Mod mod, ModSettings settings, Guid collection)
    {
        var ec = settings.ForceInherit
            ? _inheritMod!.Invoke(collection, mod.DirectoryName, true)
            : _setMod!.Invoke(collection, mod.DirectoryName, settings.Enabled);
        switch (ec)
        {
            case PenumbraApiEc.ModMissing:
                sb.Append($"The mod {mod.Name} [{mod.DirectoryName}] could not be found.");
                return;
            case PenumbraApiEc.CollectionMissing:
                sb.Append($"The collection {collection} could not be found.");
                return;
        }

        if (settings.ForceInherit || !settings.Enabled)
            return;

        ec = _setModPriority!.Invoke(collection, mod.DirectoryName, settings.Priority);
        Debug.Assert(ec is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged, "Setting Priority should not be able to fail.");

        foreach (var (setting, list) in settings.Settings)
        {
            ec = list.Count == 1
                ? _setModSetting!.Invoke(collection, mod.DirectoryName, setting, list[0])
                : _setModSettings!.Invoke(collection, mod.DirectoryName, setting, list);
            switch (ec)
            {
                case PenumbraApiEc.OptionGroupMissing:
                    sb.AppendLine($"Could not find the option group {setting} in mod {mod.Name}.");
                    break;
                case PenumbraApiEc.OptionMissing:
                    sb.AppendLine($"Could not find all desired options in the option group {setting} in mod {mod.Name}.");
                    break;
                case PenumbraApiEc.Success:
                case PenumbraApiEc.NothingChanged:
                    break;
                default:
                    sb.AppendLine($"Could not apply options in the option group {setting} in mod {mod.Name} for unknown reason {ec}.");
                    break;
            }
        }
    }


    /// <summary> Obtain the name of the collection currently assigned to the player. </summary>
    public Guid GetCurrentPlayerCollection()
    {
        if (!Available)
            return Guid.Empty;

        var (valid, _, (id, _)) = _objectCollection!.Invoke(0);
        return valid ? id : Guid.Empty;
    }

    /// <summary> Obtain the name of the collection currently assigned to the given actor. </summary>
    public Guid GetActorCollection(Actor actor, out string name)
    {
        if (!Available)
        {
            name = string.Empty;
            return Guid.Empty;
        }

        (var valid, _, (var id, name)) = _objectCollection!.Invoke(actor.Index.Index);
        return valid ? id : Guid.Empty;
    }

    /// <summary> Obtain the game object corresponding to a draw object. </summary>
    public Actor GameObjectFromDrawObject(Model drawObject)
        => Available ? _drawObjectInfo!.Invoke(drawObject.Address).Item1 : Actor.Null;

    /// <summary> Obtain the parent of a cutscene actor if it is known. </summary>
    public short CutsceneParent(ushort idx)
        => (short)(Available ? _cutsceneParent!.Invoke(idx) : -1);

    /// <summary> Try to redraw the given actor. </summary>
    public void RedrawObject(Actor actor, RedrawType settings)
    {
        if (!actor)
            return;

        RedrawObject(actor.Index, settings);
    }

    /// <summary> Try to redraw the given actor. </summary>
    public void RedrawObject(ObjectIndex index, RedrawType settings)
    {
        if (!Available)
            return;

        try
        {
            _redraw!.Invoke(index.Index, settings);
        }
        catch (Exception e)
        {
            Glamourer.Log.Debug($"Failure redrawing object:\n{e}");
        }
    }

    /// <summary> Reattach to the currently running Penumbra IPC provider. Unattaches before if necessary. </summary>
    public void Reattach()
    {
        try
        {
            Unattach();

            AttachTime = DateTime.UtcNow;
            try
            {
                (CurrentMajor, CurrentMinor) = new global::Penumbra.Api.IpcSubscribers.ApiVersion(_pluginInterface).Invoke();
            }
            catch
            {
                try
                {
                    (CurrentMajor, CurrentMinor) = new global::Penumbra.Api.IpcSubscribers.Legacy.ApiVersions(_pluginInterface).Invoke();
                }
                catch
                {
                    CurrentMajor = 0;
                    CurrentMinor = 0;
                    throw;
                }
            }

            if (CurrentMajor != RequiredPenumbraBreakingVersion || CurrentMinor < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {CurrentMajor}.{CurrentMinor:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            _creatingCharacterBase.Enable();
            _createdCharacterBase.Enable();
            _modSettingChanged.Enable();
            _collectionByIdentifier = new global::Penumbra.Api.IpcSubscribers.GetCollectionsByIdentifier(_pluginInterface);
            _collections            = new global::Penumbra.Api.IpcSubscribers.GetCollections(_pluginInterface);
            _redraw                 = new global::Penumbra.Api.IpcSubscribers.RedrawObject(_pluginInterface);
            _drawObjectInfo         = new global::Penumbra.Api.IpcSubscribers.GetDrawObjectInfo(_pluginInterface);
            _cutsceneParent         = new global::Penumbra.Api.IpcSubscribers.GetCutsceneParentIndex(_pluginInterface);
            _objectCollection       = new global::Penumbra.Api.IpcSubscribers.GetCollectionForObject(_pluginInterface);
            _getMods                = new global::Penumbra.Api.IpcSubscribers.GetModList(_pluginInterface);
            _currentCollection      = new global::Penumbra.Api.IpcSubscribers.GetCollection(_pluginInterface);
            _getCurrentSettings     = new global::Penumbra.Api.IpcSubscribers.GetCurrentModSettings(_pluginInterface);
            _inheritMod             = new global::Penumbra.Api.IpcSubscribers.TryInheritMod(_pluginInterface);
            _setMod                 = new global::Penumbra.Api.IpcSubscribers.TrySetMod(_pluginInterface);
            _setModPriority         = new global::Penumbra.Api.IpcSubscribers.TrySetModPriority(_pluginInterface);
            _setModSetting          = new global::Penumbra.Api.IpcSubscribers.TrySetModSetting(_pluginInterface);
            _setModSettings         = new global::Penumbra.Api.IpcSubscribers.TrySetModSettings(_pluginInterface);
            _openModPage            = new global::Penumbra.Api.IpcSubscribers.OpenMainWindow(_pluginInterface);
            if (CurrentMinor >= RequiredPenumbraFeatureVersionTemp)
            {
                _setTemporaryModSettings          = new global::Penumbra.Api.IpcSubscribers.SetTemporaryModSettings(_pluginInterface);
                _setTemporaryModSettingsPlayer    = new global::Penumbra.Api.IpcSubscribers.SetTemporaryModSettingsPlayer(_pluginInterface);
                _removeTemporaryModSettings       = new global::Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettings(_pluginInterface);
                _removeTemporaryModSettingsPlayer = new global::Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettingsPlayer(_pluginInterface);
                _removeAllTemporaryModSettings    = new global::Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettings(_pluginInterface);
                _removeAllTemporaryModSettingsPlayer =
                    new global::Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettingsPlayer(_pluginInterface);
                if (CurrentMinor >= RequiredPenumbraFeatureVersionTemp2)
                    _queryTemporaryModSettings = new global::Penumbra.Api.IpcSubscribers.QueryTemporaryModSettings(_pluginInterface);
            }

            Available = true;
            _penumbraReloaded.Invoke();
            Glamourer.Log.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            Unattach();
            Glamourer.Log.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    /// <summary> Unattach from the currently running Penumbra IPC provider. </summary>
    public void Unattach()
    {
        _tooltipSubscriber.Disable();
        _clickSubscriber.Disable();
        _creatingCharacterBase.Disable();
        _createdCharacterBase.Disable();
        _modSettingChanged.Disable();
        if (Available)
        {
            _collectionByIdentifier              = null;
            _collections                         = null;
            _redraw                              = null;
            _drawObjectInfo                      = null;
            _cutsceneParent                      = null;
            _objectCollection                    = null;
            _getMods                             = null;
            _currentCollection                   = null;
            _getCurrentSettings                  = null;
            _inheritMod                          = null;
            _setMod                              = null;
            _setModPriority                      = null;
            _setModSetting                       = null;
            _setModSettings                      = null;
            _openModPage                         = null;
            _setTemporaryModSettings             = null;
            _setTemporaryModSettingsPlayer       = null;
            _removeTemporaryModSettings          = null;
            _removeTemporaryModSettingsPlayer    = null;
            _removeAllTemporaryModSettings       = null;
            _removeAllTemporaryModSettingsPlayer = null;
            _queryTemporaryModSettings           = null;
            Available                            = false;
            Glamourer.Log.Debug("Glamourer detached from Penumbra.");
        }
    }

    public void Dispose()
    {
        ClearAllTemporarySettings();
        Unattach();
        _tooltipSubscriber.Dispose();
        _clickSubscriber.Dispose();
        _creatingCharacterBase.Dispose();
        _createdCharacterBase.Dispose();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
        _modSettingChanged.Dispose();
    }
}
