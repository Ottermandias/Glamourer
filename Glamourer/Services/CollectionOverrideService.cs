using Glamourer.Interop.Penumbra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Filesystem;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Services;

public sealed class CollectionOverrideService : IService, ISavable
{
    public const     int             Version = 1;
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

    public unsafe (string Collection, bool Overriden) GetCollection(Actor actor, ActorIdentifier identifier = default)
    {
        if (!identifier.IsValid)
            identifier = _actors.FromObject(actor.AsObject, out _, true, true, true);

        return _overrides.FindFirst(p => p.Actor.Matches(identifier), out var ret)
            ? (ret.Collection, true)
            : (_penumbra.GetActorCollection(actor), false);
    }

    private readonly List<(ActorIdentifier Actor, string Collection)> _overrides = [];

    public IReadOnlyList<(ActorIdentifier Actor, string Collection)> Overrides
        => _overrides;

    public string ToFilename(FilenameService fileNames)
        => fileNames.CollectionOverrideFile;

    public void AddOverride(IEnumerable<ActorIdentifier> identifiers, string collection)
    {
        if (collection.Length == 0)
            return;

        foreach (var id in identifiers.Where(i => i.IsValid))
        {
            _overrides.Add((id, collection));
            Glamourer.Log.Debug($"Added collection override {id.Incognito(null)} -> {collection}.");
            _saveService.QueueSave(this);
        }
    }

    public void ChangeOverride(int idx, string newCollection)
    {
        if (idx < 0 || idx >= _overrides.Count || newCollection.Length == 0)
            return;

        var current = _overrides[idx];
        if (current.Collection == newCollection)
            return;

        _overrides[idx] = current with { Collection = newCollection };
        Glamourer.Log.Debug($"Changed collection override {idx + 1} from {current.Collection} to {newCollection}.");
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
                    if (jObj["Overrides"] is not JArray array)
                    {
                        Glamourer.Log.Error($"Invalid format of collection override file, ignored.");
                        return;
                    }

                    foreach (var token in array.OfType<JObject>())
                    {
                        var collection = token["Collection"]?.ToObject<string>() ?? string.Empty;
                        var identifier = _actors.FromJson(token);
                        if (!identifier.IsValid)
                            Glamourer.Log.Warning($"Invalid identifier for collection override with collection [{collection}], skipped.");
                        else if (collection.Length == 0)
                            Glamourer.Log.Warning($"Empty collection override for identifier {identifier.Incognito(null)}, skipped.");
                        else
                            _overrides.Add((identifier, collection));
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
            foreach (var (actor, collection) in _overrides)
            {
                var obj = actor.ToJson();
                obj["Collection"] = collection;
                jArray.Add(obj);
            }

            return jArray;
        }
    }
}
