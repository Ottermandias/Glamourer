﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Glamourer.Interop.Penumbra;

using CurrentSettings = ValueTuple<PenumbraApiEc, (bool, int, IDictionary<string, IList<string>>, bool)?>;

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

public readonly record struct ModSettings(IDictionary<string, IList<string>> Settings, int Priority, bool Enabled)
{
    public ModSettings()
        : this(new Dictionary<string, IList<string>>(), 0, false)
    { }

    public static ModSettings Empty
        => new();
}
public readonly record struct Collection(string Name) : IComparable<Collection>
{
    public bool IsAssociable()
    {
        return !Name.IsNullOrEmpty();
    }
    public int CompareTo(Collection other)
    {
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

public unsafe class PenumbraService : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion  = 15;

    private readonly DalamudPluginInterface                                                               _pluginInterface;
    private readonly EventSubscriber<ChangedItemType, uint>                                               _tooltipSubscriber;
    private readonly EventSubscriber<MouseButton, ChangedItemType, uint>                                  _clickSubscriber;
    private readonly EventSubscriber<nint, string, nint, nint, nint>                                      _creatingCharacterBase;
    private readonly EventSubscriber<nint, string, nint>                                                  _createdCharacterBase;
    private readonly EventSubscriber<ModSettingChange, string, string, bool>                              _modSettingChanged;
    private          ActionSubscriber<int, RedrawType>                                                    _redrawSubscriber;
    private          FuncSubscriber<nint, (nint, string)>                                                 _drawObjectInfo;
    private          FuncSubscriber<int, int>                                                             _cutsceneParent;
    private          FuncSubscriber<int, (bool, bool, string)>                                            _objectCollection;
    private          FuncSubscriber<IList<(string, string)>>                                              _getMods;
    private          FuncSubscriber<ApiCollectionType, string>                                            _currentCollection;
    private          FuncSubscriber<IList<string>>                                                        _getAllCollections;
    private          FuncSubscriber<string, string, string, bool, CurrentSettings>                        _getCurrentSettings;
    private          FuncSubscriber<int, string, bool, bool, (PenumbraApiEc, string)>                     _setCurrentCollection;
    private          FuncSubscriber<string, string, string, bool, PenumbraApiEc>                          _setMod;
    private          FuncSubscriber<string, string, string, int, PenumbraApiEc>                           _setModPriority;
    private          FuncSubscriber<string, string, string, string, string, PenumbraApiEc>                _setModSetting;
    private          FuncSubscriber<string, string, string, string, IReadOnlyList<string>, PenumbraApiEc> _setModSettings;

    private readonly EventSubscriber _initializedEvent;
    private readonly EventSubscriber _disposedEvent;

    private readonly PenumbraReloaded _penumbraReloaded;

    public bool Available { get; private set; }

    public PenumbraService(DalamudPluginInterface pi, PenumbraReloaded penumbraReloaded)
    {
        _pluginInterface       = pi;
        _penumbraReloaded      = penumbraReloaded;
        _initializedEvent      = Ipc.Initialized.Subscriber(pi, Reattach);
        _disposedEvent         = Ipc.Disposed.Subscriber(pi, Unattach);
        _tooltipSubscriber     = Ipc.ChangedItemTooltip.Subscriber(pi);
        _clickSubscriber       = Ipc.ChangedItemClick.Subscriber(pi);
        _createdCharacterBase  = Ipc.CreatedCharacterBase.Subscriber(pi);
        _creatingCharacterBase = Ipc.CreatingCharacterBase.Subscriber(pi);
        _modSettingChanged     = Ipc.ModSettingChanged.Subscriber(pi);
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


    public event Action<nint, string, nint, nint, nint> CreatingCharacterBase
    {
        add => _creatingCharacterBase.Event += value;
        remove => _creatingCharacterBase.Event -= value;
    }

    public event Action<nint, string, nint> CreatedCharacterBase
    {
        add => _createdCharacterBase.Event += value;
        remove => _createdCharacterBase.Event -= value;
    }

    public event Action<ModSettingChange, string, string, bool> ModSettingChanged
    {
        add => _modSettingChanged.Event += value;
        remove => _modSettingChanged.Event -= value;
    }

    public IReadOnlyList<(Mod Mod, ModSettings Settings)> GetMods()
    {
        if (!Available)
            return Array.Empty<(Mod Mod, ModSettings Settings)>();

        try
        {
            var allMods    = _getMods.Invoke();
            var collection = _currentCollection.Invoke(ApiCollectionType.Current);
            return allMods
                .Select(m => (m.Item1, m.Item2, _getCurrentSettings.Invoke(collection, m.Item1, m.Item2, true)))
                .Where(t => t.Item3.Item1 is PenumbraApiEc.Success)
                .Select(t => (new Mod(t.Item2, t.Item1),
                    !t.Item3.Item2.HasValue
                        ? ModSettings.Empty
                        : new ModSettings(t.Item3.Item2!.Value.Item3, t.Item3.Item2!.Value.Item2, t.Item3.Item2!.Value.Item1)))
                .OrderByDescending(p => p.Item2.Enabled)
                .ThenBy(p => p.Item1.Name)
                .ThenBy(p => p.Item1.DirectoryName)
                .ThenByDescending(p => p.Item2.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching mods from Penumbra:\n{ex}");
            return Array.Empty<(Mod Mod, ModSettings Settings)>();
        }
    }

    public IReadOnlyList<Collection> GetAllCollections()
    {
        if (!Available)
            return Array.Empty<Collection>();

        try
        {
            var allCollections = _getAllCollections.Invoke();
            return allCollections
                .Select(c => new Collection(c))
                .ToList();
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error fetching collections from Penumbra:\n{ex}");
            return Array.Empty<Collection>();
        }
    }

    public string CurrentCollection
        => Available ? _currentCollection.Invoke(ApiCollectionType.Current) : "<Unavailable>";

    /// <summary>
    /// Try to set all mod settings as desired. Only sets when the mod should be enabled.
    /// If it is disabled, ignore all other settings.
    /// </summary>
    public string SetMod(Mod mod, ModSettings settings)
    {
        if (!Available)
            return "Penumbra is not available.";

        var sb = new StringBuilder();
        try
        {
            var collection = _currentCollection.Invoke(ApiCollectionType.Current);
            var ec         = _setMod.Invoke(collection, mod.DirectoryName, mod.Name, settings.Enabled);
            if (ec is PenumbraApiEc.ModMissing)
                return $"The mod {mod.Name} [{mod.DirectoryName}] could not be found.";

            Debug.Assert(ec is not PenumbraApiEc.CollectionMissing, "Missing collection should not be possible.");

            if (!settings.Enabled)
                return string.Empty;

            ec = _setModPriority.Invoke(collection, mod.DirectoryName, mod.Name, settings.Priority);
            Debug.Assert(ec is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged, "Setting Priority should not be able to fail.");

            foreach (var (setting, list) in settings.Settings)
            {
                ec = list.Count == 1
                    ? _setModSetting.Invoke(collection, mod.DirectoryName, mod.Name, setting, list[0])
                    : _setModSettings.Invoke(collection, mod.DirectoryName, mod.Name, setting, (IReadOnlyList<string>)list);
                switch (ec)
                {
                    case PenumbraApiEc.OptionGroupMissing:
                        sb.AppendLine($"Could not find the option group {setting} in mod {mod.Name}.");
                        break;
                    case PenumbraApiEc.OptionMissing:
                        sb.AppendLine($"Could not find all desired options in the option group {setting} in mod {mod.Name}.");
                        break;
                }

                Debug.Assert(ec is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged,
                    "Missing Mod or Collection should not be possible here.");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return sb.AppendLine(ex.Message).ToString();
        }
    }
    public string SetCollection(Actor actor, Collection collection)
    {
        if (!Available)
            return "Penumbra is not available.";

        var sb = new StringBuilder();
        try
        {
            var ec = _setCurrentCollection.Invoke(actor.AsObject -> ObjectIndex, collection.Name, true, false);
            if (ec.Item1 is PenumbraApiEc.CollectionMissing)
                sb.AppendLine($"The collection {collection.Name}] could not be found.");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return sb.AppendLine(ex.Message).ToString();
        }
    }

    /// <summary> Obtain the name of the collection currently assigned to the player. </summary>
    public string GetCurrentPlayerCollection()
    {
        if (!Available)
            return string.Empty;

        var (valid, _, name) = _objectCollection.Invoke(0);
        return valid ? name : string.Empty;
    }

    /// <summary> Obtain the game object corresponding to a draw object. </summary>
    public Actor GameObjectFromDrawObject(Model drawObject)
        => Available ? _drawObjectInfo.Invoke(drawObject.Address).Item1 : Actor.Null;

    /// <summary> Obtain the parent of a cutscene actor if it is known. </summary>
    public int CutsceneParent(int idx)
        => Available ? _cutsceneParent.Invoke(idx) : -1;

    /// <summary> Try to redraw the given actor. </summary>
    public void RedrawObject(Actor actor, RedrawType settings)
    {
        if (!actor || !Available)
            return;

        try
        {
            _redrawSubscriber.Invoke(actor.AsObject->ObjectIndex, settings);
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Failure redrawing object:\n{e}");
        }
    }

    /// <summary> Reattach to the currently running Penumbra IPC provider. Unattaches before if necessary. </summary>
    public void Reattach()
    {
        try
        {
            Unattach();

            var (breaking, feature) = Ipc.ApiVersions.Subscriber(_pluginInterface).Invoke();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _tooltipSubscriber.Enable();
            _clickSubscriber.Enable();
            _creatingCharacterBase.Enable();
            _createdCharacterBase.Enable();
            _modSettingChanged.Enable();
            _drawObjectInfo     = Ipc.GetDrawObjectInfo.Subscriber(_pluginInterface);
            _cutsceneParent     = Ipc.GetCutsceneParentIndex.Subscriber(_pluginInterface);
            _redrawSubscriber   = Ipc.RedrawObjectByIndex.Subscriber(_pluginInterface);
            _objectCollection   = Ipc.GetCollectionForObject.Subscriber(_pluginInterface);
            _getMods            = Ipc.GetMods.Subscriber(_pluginInterface);
            _currentCollection  = Ipc.GetCollectionForType.Subscriber(_pluginInterface);
            _getCurrentSettings = Ipc.GetCurrentModSettings.Subscriber(_pluginInterface);
            _getAllCollections  = Ipc.GetCollections.Subscriber(_pluginInterface);
            _setCurrentCollection = Ipc.SetCollectionForObject.Subscriber( _pluginInterface);
            _setMod             = Ipc.TrySetMod.Subscriber(_pluginInterface);
            _setModPriority     = Ipc.TrySetModPriority.Subscriber(_pluginInterface);
            _setModSetting      = Ipc.TrySetModSetting.Subscriber(_pluginInterface);
            _setModSettings     = Ipc.TrySetModSettings.Subscriber(_pluginInterface);
            Available = true;
            _penumbraReloaded.Invoke();
            Glamourer.Log.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
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
            Available = false;
            Glamourer.Log.Debug("Glamourer detached from Penumbra.");
        }
    }

    public void Dispose()
    {
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
