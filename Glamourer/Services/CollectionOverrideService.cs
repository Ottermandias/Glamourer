using Dalamud.Interface.Internal.Notifications;
using Glamourer.Interop.Penumbra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Services;

public sealed class CollectionOverrideService : IService, ISavable
{
    public const     int             Version = 2;
    private readonly SaveService     _saveService;
    private readonly ActorManager    _actors;
    private readonly PenumbraService _penumbra;

    public CollectionOverrideService(SaveService saveService, ActorManager actors, PenumbraService penumbra)
    {
        _saveService = saveService;
        _actors      = actors;
        _penumbra    = penumbra;
        Load();
    }

    public unsafe (Guid CollectionId, string Display, bool Overriden) GetCollection(Actor actor, ActorIdentifier identifier = default)
    {
        if (!identifier.IsValid)
            identifier = _actors.FromObject(actor.AsObject, out _, true, true, true);

        return _overrides.FindFirst(p => p.Actor.Matches(identifier), out var ret)
            ? (ret.CollectionId, ret.DisplayName, true)
            : (_penumbra.GetActorCollection(actor, out var name), name, false);
    }

    private readonly List<(ActorIdentifier Actor, Guid CollectionId, string DisplayName)> _overrides = [];

    public IReadOnlyList<(ActorIdentifier Actor, Guid CollectionId, string DisplayName)> Overrides
        => _overrides;

    public string ToFilename(FilenameService fileNames)
        => fileNames.CollectionOverrideFile;

    public void AddOverride(IEnumerable<ActorIdentifier> identifiers, Guid collectionId, string displayName)
    {
        if (collectionId == Guid.Empty)
            return;

        foreach (var id in identifiers.Where(i => i.IsValid))
        {
            _overrides.Add((id, collectionId, displayName));
            Glamourer.Log.Debug($"Added collection override {id.Incognito(null)} -> {collectionId}.");
            _saveService.QueueSave(this);
        }
    }

    public (bool Exists, ActorIdentifier Identifier, Guid CollectionId, string DisplayName) Fetch(int idx)
    {
        var (identifier, id, name) = _overrides[idx];
        var collection = _penumbra.CollectionByIdentifier(id.ToString());
        if (collection == null)
            return (false, identifier, id, name);

        if (collection.Value.Name == name)
            return (true, identifier, id, name);

        _overrides[idx] = (identifier, id, collection.Value.Name);
        Glamourer.Log.Debug($"Updated display name of collection override {idx + 1} ({id}).");
        _saveService.QueueSave(this);
        return (true, identifier, id, collection.Value.Name);
    }

    public void ChangeOverride(int idx, Guid newCollectionId, string newDisplayName)
    {
        if (idx < 0 || idx >= _overrides.Count)
            return;

        if (newCollectionId == Guid.Empty || newDisplayName.Length == 0)
            return;

        var current = _overrides[idx];
        if (current.CollectionId == newCollectionId)
            return;

        _overrides[idx] = current with
        {
            CollectionId = newCollectionId,
            DisplayName = newDisplayName,
        };
        Glamourer.Log.Debug($"Changed collection override {idx + 1} from {current.CollectionId} to {newCollectionId}.");
        _saveService.QueueSave(this);
    }

    public void DeleteOverride(int idx)
    {
        if (idx < 0 || idx >= _overrides.Count)
            return;

        _overrides.RemoveAt(idx);
        Glamourer.Log.Debug($"Removed collection override {idx + 1}.");
        _saveService.QueueSave(this);
    }

    public void MoveOverride(int idxFrom, int idxTo)
    {
        if (!_overrides.Move(idxFrom, idxTo))
            return;

        Glamourer.Log.Debug($"Moved collection override {idxFrom + 1} to {idxTo + 1}.");
        _saveService.QueueSave(this);
    }

    private void Load()
    {
        var file = _saveService.FileNames.CollectionOverrideFile;
        if (!File.Exists(file))
            return;

        try
        {
            var text    = File.ReadAllText(file);
            var jObj    = JObject.Parse(text);
            var version = jObj["Version"]?.ToObject<int>() ?? 0;
            switch (version)
            {
                case 1:
                case 2:
                    if (jObj["Overrides"] is not JArray array)
                    {
                        Glamourer.Log.Error($"Invalid format of collection override file, ignored.");
                        return;
                    }

                    foreach (var token in array.OfType<JObject>())
                    {
                        var collectionIdentifier = token["Collection"]?.ToObject<string>() ?? string.Empty;
                        var identifier           = _actors.FromJson(token);
                        var displayName          = token["DisplayName"]?.ToObject<string>() ?? collectionIdentifier;
                        if (!identifier.IsValid)
                        {
                            Glamourer.Log.Warning(
                                $"Invalid identifier for collection override with collection [{token["Collection"]}], skipped.");
                            continue;
                        }

                        if (!Guid.TryParse(collectionIdentifier, out var collectionId))
                        {
                            if (collectionIdentifier.Length == 0)
                            {
                                Glamourer.Log.Warning($"Empty collection override for identifier {identifier.Incognito(null)}, skipped.");
                                continue;
                            }

                            if (version >= 2)
                            {
                                Glamourer.Log.Warning(
                                    $"Invalid collection override {collectionIdentifier} for identifier {identifier.Incognito(null)}, skipped.");
                                continue;
                            }

                            var collection = _penumbra.CollectionByIdentifier(collectionIdentifier);
                            if (collection == null)
                            {
                                Glamourer.Messager.AddMessage(new Notification(
                                    $"The overridden collection for identifier {identifier.Incognito(null)} with name {collectionIdentifier} could not be found by Penumbra for migration.",
                                    NotificationType.Warning));
                                continue;
                            }

                            Glamourer.Log.Information($"Migrated collection {collectionIdentifier} to {collection.Value.Id}.");
                            collectionId = collection.Value.Id;
                            displayName  = collection.Value.Name;
                        }

                        _overrides.Add((identifier, collectionId, displayName));
                    }

                    break;

                default:
                    Glamourer.Log.Error($"Invalid version {version} of collection override file, ignored.");
                    return;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Error loading collection override file:\n{ex}");
        }
    }

    public void Save(StreamWriter writer)
    {
        var jObj = new JObject()
        {
            ["Version"]   = Version,
            ["Overrides"] = SerializeOverrides(),
        };
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        jObj.WriteTo(j);
        return;

        JArray SerializeOverrides()
        {
            var jArray = new JArray();
            foreach (var (actor, collection, displayName) in _overrides)
            {
                var obj = actor.ToJson();
                obj["Collection"]  = collection;
                obj["DisplayName"] = displayName;
                jArray.Add(obj);
            }

            return jArray;
        }
    }
}
