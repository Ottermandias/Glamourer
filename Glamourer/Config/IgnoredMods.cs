using System.Text.Json;
using Glamourer.Services;
using Luna;

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

    protected override void AddData(Utf8JsonWriter j)
    {
        j.WritePropertyName("IgnoredMods"u8);
        j.WriteStartArray();
        foreach (var mod in _ignoredMods)
            j.WriteStringValue(mod);
        j.WriteEndArray();
    }

    protected override void LoadData(in JsonElement j)
    {
        _ignoredMods.Clear();
        if (!j.TryReadArray("IgnoredMods"u8, out var ignoredMods))
            return;

        foreach (var value in ignoredMods.EnumerateArray())
        {
            if (value.ValueKind is not JsonValueKind.String)
                continue;

            var mod = value.GetString();
            if (!string.IsNullOrEmpty(mod))
                _ignoredMods.Add(mod);
        }
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
