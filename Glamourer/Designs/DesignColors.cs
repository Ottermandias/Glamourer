using Dalamud.Interface.ImGuiNotification;
using Glamourer.Gui;
using Glamourer.Services;
using ImSharp;
using Luna;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Glamourer.Designs;

public class DesignColorUi(DesignColors colors, Configuration config)
{
    private string _newName = string.Empty;

    public void Draw()
    {
        using var table = Im.Table.Begin("designColors"u8, 3, TableFlags.RowBackground);
        if (!table)
            return;

        var     changeString = string.Empty;
        Rgba32? changeValue  = null;

        table.SetupColumn("##Delete"u8,   TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("##Select"u8,   TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("Color Name"u8, TableColumnFlags.WidthStretch);

        table.HeaderRow();

        table.NextColumn();
        if (ImEx.Icon.Button(LunaStyle.RefreshIcon, "Revert the color used for missing design colors to its default."u8,
                colors.MissingColor == DesignColors.MissingColorDefault))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = DesignColors.MissingColorDefault;
        }

        table.NextColumn();
        if (DrawColorButton(DesignColors.MissingColorNameU8, colors.MissingColor, out var newColor))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = newColor;
        }

        table.NextColumn();
        Im.Cursor.X += Im.Style.FramePadding.X;
        Im.Text(DesignColors.MissingColorNameU8);
        Im.Tooltip.OnHover("This color is used when the color specified in a design is not available."u8);

        var disabled = !config.DeleteDesignModifier.IsActive();
        foreach (var (idx, (name, color)) in colors.Index())
        {
            using var id = Im.Id.Push(idx);
            table.NextColumn();

            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this color. This does not remove it from designs using it."u8, disabled))
            {
                changeString = name;
                changeValue  = null;
            }

            if (disabled)
                Im.Tooltip.OnHover($"\nHold {config.DeleteDesignModifier} to delete.");

            table.NextColumn();
            if (DrawColorButton(name, color, out newColor))
            {
                changeString = name;
                changeValue  = newColor;
            }

            table.NextColumn();
            Im.Cursor.X += Im.Style.FramePadding.X;
            Im.Text(name);
        }

        table.NextColumn();
        (var tt, disabled) = _newName.Length == 0
            ? ("Specify a name for a new color first.", true)
            : _newName is DesignColors.MissingColorName or DesignColors.AutomaticName
                ? ($"You can not use the name {DesignColors.MissingColorName} or {DesignColors.AutomaticName}, choose a different one.", true)
                : colors.ContainsKey(_newName)
                    ? ($"The color {_newName} already exists, please choose a different name.", true)
                    : ($"Add a new color {_newName} to your list.", false);
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, disabled))
        {
            changeString = _newName;
            changeValue  = 0xFFFFFFFF;
        }

        table.NextColumn();
        table.NextColumn();
        Im.Item.SetNextWidth(Im.ContentRegion.Available.X);
        if (Im.Input.Text("##newDesignColor"u8, ref _newName, "New Color Name..."u8, InputTextFlags.EnterReturnsTrue))
        {
            changeString = _newName;
            changeValue  = 0xFFFFFFFF;
        }

        if (changeString.Length > 0)
        {
            if (!changeValue.HasValue)
                colors.DeleteColor(changeString);
            else
                colors.SetColor(changeString, changeValue.Value);
        }
    }

    public static bool DrawColorButton(Utf8StringHandler<LabelStringHandlerBuffer> tooltip, Rgba32 color, out Rgba32 newColor)
    {
        var ret = Im.Color.Editor(tooltip, ref color, ColorEditorFlags.AlphaPreviewHalf | ColorEditorFlags.NoInputs);
        Im.Tooltip.OnHover(ref tooltip);
        newColor = color;
        return ret;
    }
}

public class DesignColors : ISavable, IReadOnlyDictionary<string, Rgba32>
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

    public string ToFilename(FilenameService fileNames)
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
