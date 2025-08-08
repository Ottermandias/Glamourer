using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Services;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Glamourer.Gui.Tabs.NpcTab;

public class LocalNpcAppearanceData : ISavable
{
    private readonly DesignColors _colors;

    public record struct Data(string Color = "", bool Favorite = false);

    private readonly Dictionary<ulong, Data> _data = [];

    public LocalNpcAppearanceData(DesignColors colors, SaveService saveService)
    {
        _colors = colors;
        Load(saveService);
        DataChanged += () => saveService.QueueSave(this);
    }

    public bool IsFavorite(in NpcData data)
        => _data.TryGetValue(ToKey(data), out var tuple) && tuple.Favorite;

    public (uint Color, bool Favorite) GetData(in NpcData data)
        => _data.TryGetValue(ToKey(data), out var t)
            ? (GetColor(t.Color,      t.Favorite, data.Kind), t.Favorite)
            : (GetColor(string.Empty, false,      data.Kind), false);

    public string GetColor(in NpcData data)
        => _data.TryGetValue(ToKey(data), out var t) ? t.Color : string.Empty;

    private uint GetColor(string color, bool favorite, ObjectKind kind)
    {
        if (color.Length == 0)
        {
            if (favorite)
                return ColorId.FavoriteStarOn.Value();

            return kind is ObjectKind.BattleNpc
                ? ColorId.BattleNpc.Value()
                : ColorId.EventNpc.Value();
        }

        if (_colors.TryGetValue(color, out var value))
            return value == 0 ? ImGui.GetColorU32(ImGuiCol.Text) : value;

        return _colors.MissingColor;
    }

    public void ToggleFavorite(in NpcData data)
    {
        var key = ToKey(data);
        if (_data.TryGetValue(key, out var t))
        {
            if (t is { Color: "", Favorite: true })
                _data.Remove(key);
            else
                _data[key] = t with { Favorite = !t.Favorite };
        }
        else
        {
            _data[key] = new Data(string.Empty, true);
        }

        DataChanged.Invoke();
    }

    public void SetColor(in NpcData data, string color)
    {
        var key = ToKey(data);
        if (_data.TryGetValue(key, out var t))
        {
            if (!t.Favorite && color.Length == 0)
                _data.Remove(key);
            else
                _data[key] = t with { Color = color };
        }
        else if (color.Length != 0)
        {
            _data[key] = new Data(color);
        }

        DataChanged.Invoke();
    }

    private static ulong ToKey(in NpcData data)
        => (byte)data.Kind | ((ulong)data.Id.Id << 8);

    public event Action DataChanged = null!;

    public string ToFilename(FilenameService fileNames)
        => fileNames.NpcAppearanceFile;

    public void Save(StreamWriter writer)
    {
        var jObj = new JObject()
        {
            ["Version"] = 1,
            ["Data"]    = JToken.FromObject(_data),
        };
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        jObj.WriteTo(j);
    }

    private void Load(SaveService save)
    {
        var file = save.FileNames.NpcAppearanceFile;
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
                    var data = jObj["Data"]?.ToObject<Dictionary<ulong, Data>>() ?? [];
                    _data.EnsureCapacity(data.Count);
                    foreach (var kvp in data)
                        _data.Add(kvp.Key, kvp.Value);
                    return;
                default: throw new Exception("Invalid version {version}.");
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not read local NPC appearance data:\n{ex}");
        }
    }
}
