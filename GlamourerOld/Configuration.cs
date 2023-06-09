using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Glamourer.Services;
using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Glamourer;

public class Configuration : IPluginConfiguration, ISavable
{
    [JsonIgnore]
    private readonly SaveService _saveService;

    public class FixedDesign
    {
        public string Name = string.Empty;
        public string Path = string.Empty;
        public uint   JobGroups;
        public bool   Enabled;
    }

    public int Version { get; set; } = 1;

    public const uint DefaultCustomizationColor = 0xFFC000C0;
    public const uint DefaultStateColor         = 0xFF00C0C0;
    public const uint DefaultEquipmentColor     = 0xFF00C000;

    public bool UseRestrictedGearProtection { get; set; } = true;

    public bool FoldersFirst      { get; set; } = false;
    public bool ColorDesigns      { get; set; } = true;
    public bool ShowLocks         { get; set; } = true;
    public bool ApplyFixedDesigns { get; set; } = true;

    public uint CustomizationColor { get; set; } = DefaultCustomizationColor;
    public uint StateColor         { get; set; } = DefaultStateColor;
    public uint EquipmentColor     { get; set; } = DefaultEquipmentColor;

    public List<FixedDesign> FixedDesigns { get; set; } = new();

    public void Save()
        => _saveService.QueueSave(this);

    public Configuration(SaveService saveService)
    {
        _saveService = saveService;
        Load();
    }

    public void Load()
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Glamourer.Log.Error(
                $"Error parsing Configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.ConfigFile))
            return;

        var text = File.ReadAllText(_saveService.FileNames.ConfigFile);
        JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
        {
            Error = HandleDeserializationError,
        });
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.ConfigFile;

    public void Save(StreamWriter writer)
    {
        using var jWriter    = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        var       serializer = new JsonSerializer { Formatting         = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }
}
