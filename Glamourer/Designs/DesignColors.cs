using Dalamud.Interface.ImGuiNotification;
using Glamourer.Gui;
using Glamourer.Services;
using ImSharp;
using Luna;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Glamourer.Designs;

public sealed class DesignColors : ISavable, IReadOnlyDictionary<string, Rgba32>, IService
{
    public const           string   AutomaticName       = "Automatic";
    public static readonly StringU8 AutomaticNameU8     = new("Automatic"u8);
    public const           string   MissingColorName    = "Missing Color";
    public static readonly StringU8 MissingColorNameU8  = new("Missing Color"u8);
    public const           uint     MissingColorDefault = 0xFF0000D0;

    private readonly SaveService                _saveService;
    private readonly Dictionary<string, Rgba32> _colors = [];
    public           Rgba32                     MissingColor { get; private set; } = MissingColorDefault;

    public event Action? ColorChanged;

    public DesignColors(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

    public Rgba32 GetColor(Design? design)
    {
        if (design is null)
            return ColorId.NormalDesign.Value();

        if (design.Color.Length is 0)
            return AutoColor(design);

        return TryGetValue(design.Color, out var color) ? color : MissingColor;
    }

    public void SetColor(string key, Rgba32 newColor)
    {
        if (key.Length is 0)
            return;

        if (key is MissingColorName && MissingColor != newColor)
        {
            MissingColor = newColor.Color;
            SaveAndInvoke();
            return;
        }

        if (_colors.TryAdd(key, newColor.Color))
        {
            SaveAndInvoke();
            return;
        }

        _colors.TryGetValue(key, out var color);
        _colors[key] = newColor.Color;

        if (color != newColor)
            SaveAndInvoke();
    }

    private void SaveAndInvoke()
    {
        ColorChanged?.Invoke();
        _saveService.DelaySave(this, TimeSpan.FromSeconds(2));
    }

    public void DeleteColor(string key)
    {
        if (_colors.Remove(key))
            SaveAndInvoke();
    }

    public string ToFilePath(FilenameService fileNames)
        => fileNames.DesignColorFile;

    public void Save(StreamWriter writer)
    {
        var jObj = new JObject
        {
            ["Version"]      = 1,
            ["MissingColor"] = MissingColor.Color,
            ["Definitions"]  = JToken.FromObject(_colors),
        };
        writer.Write(jObj.ToString(Formatting.Indented));
    }

    private void Load()
    {
        _colors.Clear();
        var file = _saveService.FileNames.DesignColorFile;
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
                {
                    var dict = jObj["Definitions"]?.ToObject<Dictionary<string, uint>>() ?? new Dictionary<string, uint>();
                    _colors.EnsureCapacity(dict.Count);
                    foreach (var kvp in dict)
                        _colors.Add(kvp.Key, kvp.Value);
                    MissingColor = jObj["MissingColor"]?.ToObject<uint>() ?? MissingColorDefault;
                    break;
                }
                default: throw new Exception($"Unknown Version {version}");
            }
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not read design color file.", NotificationType.Error);
        }
    }

    public IEnumerator<KeyValuePair<string, Rgba32>> GetEnumerator()
        => _colors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _colors.Count;

    public bool ContainsKey(string key)
        => _colors.ContainsKey(key);

    public bool TryGetValue(string key, out Rgba32 value)
    {
        if (_colors.TryGetValue(key, out value))
        {
            if (value.IsTransparent)
                value = Im.Color.Get(ImGuiColor.Text);
            return true;
        }

        value = Rgba32.Transparent;
        return false;
    }

    public static Rgba32 AutoColor(DesignBase design)
    {
        var customize = design.ApplyCustomizeExcludingBodyType is 0;
        var equip     = design.Application.Equip is 0;
        return (customize, equip) switch
        {
            (true, true)   => ColorId.StateDesign.Value(),
            (true, false)  => ColorId.EquipmentDesign.Value(),
            (false, true)  => ColorId.CustomizationDesign.Value(),
            (false, false) => ColorId.NormalDesign.Value(),
        };
    }

    public Rgba32 this[string key]
        => _colors[key];

    public IEnumerable<string> Keys
        => _colors.Keys;

    public IEnumerable<Rgba32> Values
        => _colors.Values;
}
