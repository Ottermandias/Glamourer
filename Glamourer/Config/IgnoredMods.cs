using Glamourer.Services;
using Luna;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Glamourer.Config;

public sealed class IgnoredMods : ConfigurationFile<FilenameService>, IReadOnlySet<string>
{
    public override int CurrentVersion
        => 1;

    private readonly HashSet<string> _ignoredMods = [];

    public IgnoredMods(SaveService saveService, MessageService messageService)
        : base(saveService, messageService)
    {
        Load();
    }

    protected override void AddData(JsonTextWriter j)
    {
        j.WritePropertyName("IgnoredMods");
        j.WriteStartArray();
        foreach (var mod in _ignoredMods)
            j.WriteValue(mod);
        j.WriteEndArray();
    }

    protected override void LoadData(JObject j)
    {
        _ignoredMods.Clear();
        if (j["IgnoredMods"] is not JArray arr)
            return;

        foreach (var value in arr.Values<string>().OfType<string>())
            _ignoredMods.Add(value);
    }

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.IgnoredModsFile;

    public IEnumerator<string> GetEnumerator()
        => _ignoredMods.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _ignoredMods.Count;

    public void Add(string mod)
    {
        if (_ignoredMods.Add(mod))
            Save();
    }

    public void Remove(string mod)
    {
        if (_ignoredMods.Remove(mod))
            Save();
    }

    public bool Contains(string item)
        => _ignoredMods.Contains(item);

    public bool IsProperSubsetOf(IEnumerable<string> other)
        => _ignoredMods.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<string> other)
        => _ignoredMods.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<string> other)
        => _ignoredMods.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<string> other)
        => _ignoredMods.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<string> other)
        => _ignoredMods.Overlaps(other);

    public bool SetEquals(IEnumerable<string> other)
        => _ignoredMods.SetEquals(other);
}
