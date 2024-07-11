using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Gui;
using Glamourer.Services;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;

namespace Glamourer.Designs;

public class DesignColorUi(DesignColors colors, Configuration config)
{
    private string _newName = string.Empty;

    public void Draw()
    {
        using var table = ImRaii.Table("designColors", 3, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        var   changeString = string.Empty;
        uint? changeValue  = null;

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Delete",   ImGuiTableColumnFlags.WidthFixed, buttonSize.X);
        ImGui.TableSetupColumn("##Select",   ImGuiTableColumnFlags.WidthFixed, buttonSize.X);
        ImGui.TableSetupColumn("Color Name", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableHeadersRow();

        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Recycle.ToIconString(), buttonSize,
                "Revert the color used for missing design colors to its default.", colors.MissingColor == DesignColors.MissingColorDefault,
                true))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = DesignColors.MissingColorDefault;
        }

        ImGui.TableNextColumn();
        if (DrawColorButton(DesignColors.MissingColorName, colors.MissingColor, out var newColor))
        {
            changeString = DesignColors.MissingColorName;
            changeValue  = newColor;
        }

        ImGui.TableNextColumn();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
        ImGui.TextUnformatted(DesignColors.MissingColorName);
        ImGuiUtil.HoverTooltip("This color is used when the color specified in a design is not available.");


        var disabled = !config.DeleteDesignModifier.IsActive();
        var tt       = "Delete this color. This does not remove it from designs using it.";
        if (disabled)
            tt += $"\nHold {config.DeleteDesignModifier} to delete.";

        foreach (var ((name, color), idx) in colors.WithIndex())
        {
            using var id = ImRaii.PushId(idx);
            ImGui.TableNextColumn();

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize, tt, disabled, true))
            {
                changeString = name;
                changeValue  = null;
            }

            ImGui.TableNextColumn();
            if (DrawColorButton(name, color, out newColor))
            {
                changeString = name;
                changeValue  = newColor;
            }

            ImGui.TableNextColumn();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
            ImGui.TextUnformatted(name);
        }

        ImGui.TableNextColumn();
        (tt, disabled) = _newName.Length == 0
            ? ("Specify a name for a new color first.", true)
            : _newName is DesignColors.MissingColorName or DesignColors.AutomaticName
                ? ($"You can not use the name {DesignColors.MissingColorName} or {DesignColors.AutomaticName}, choose a different one.", true)
                : colors.ContainsKey(_newName)
                    ? ($"The color {_newName} already exists, please choose a different name.", true)
                    : ($"Add a new color {_newName} to your list.", false);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), buttonSize, tt, disabled, true))
        {
            changeString = _newName;
            changeValue  = 0xFFFFFFFF;
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##newDesignColor", "New Color Name...", ref _newName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
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

    public static bool DrawColorButton(string tooltip, uint color, out uint newColor)
    {
        var vec = ImGui.ColorConvertU32ToFloat4(color);
        if (!ImGui.ColorEdit4(tooltip, ref vec, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs))
        {
            ImGuiUtil.HoverTooltip(tooltip);
            newColor = color;
            return false;
        }

        ImGuiUtil.HoverTooltip(tooltip);

        newColor = ImGui.ColorConvertFloat4ToU32(vec);
        return newColor != color;
    }
}

public class DesignColors : ISavable, IReadOnlyDictionary<string, uint>
{
    public const string AutomaticName       = "Automatic";
    public const string MissingColorName    = "Missing Color";
    public const uint   MissingColorDefault = 0xFF0000D0;

    private readonly SaveService              _saveService;
    private readonly Dictionary<string, uint> _colors = [];
    public           uint                     MissingColor { get; private set; } = MissingColorDefault;

    public event Action? ColorChanged;

    public DesignColors(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

    public uint GetColor(Design? design)
    {
        if (design == null)
            return ColorId.NormalDesign.Value();

        if (design.Color.Length == 0)
            return AutoColor(design);

        return TryGetValue(design.Color, out var color) ? color : MissingColor;
    }

    public void SetColor(string key, uint newColor)
    {
        if (key.Length == 0)
            return;

        if (key is MissingColorName && MissingColor != newColor)
        {
            MissingColor = newColor;
            SaveAndInvoke();
            return;
        }

        if (_colors.TryAdd(key, newColor))
        {
            SaveAndInvoke();
            return;
        }

        _colors.TryGetValue(key, out var color);
        _colors[key] = newColor;

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
            ["MissingColor"] = MissingColor,
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

    public IEnumerator<KeyValuePair<string, uint>> GetEnumerator()
        => _colors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _colors.Count;

    public bool ContainsKey(string key)
        => _colors.ContainsKey(key);

    public bool TryGetValue(string key, out uint value)
    {
        if (_colors.TryGetValue(key, out value))
        {
            if (value == 0)
                value = ImGui.GetColorU32(ImGuiCol.Text);
            return true;
        }

        return false;
    }

    public static uint AutoColor(DesignBase design)
    {
        var customize = design.ApplyCustomizeExcludingBodyType == 0;
        var equip     = design.ApplyEquip == 0;
        return (customize, equip) switch
        {
            (true, true)   => ColorId.StateDesign.Value(),
            (true, false)  => ColorId.EquipmentDesign.Value(),
            (false, true)  => ColorId.CustomizationDesign.Value(),
            (false, false) => ColorId.NormalDesign.Value(),
        };
    }

    public uint this[string key]
        => _colors[key];

    public IEnumerable<string> Keys
        => _colors.Keys;

    public IEnumerable<uint> Values
        => _colors.Values;
}
