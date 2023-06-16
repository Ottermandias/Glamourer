using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Glamourer;

public class Configuration : IPluginConfiguration, ISavable
{
    public bool UseRestrictedGearProtection = true;

    public int Version { get; set; } = 2;

    public Dictionary<ColorId, uint> Colors { get; set; }
        = Enum.GetValues<ColorId>().ToDictionary(c => c, c => c.Data().DefaultColor);

    [JsonIgnore]
    private readonly SaveService _saveService;

    public Configuration(SaveService saveService, ConfigMigrationService migrator)
    {
        _saveService = saveService;
        Load(migrator);
    }

    public void Save()
        => _saveService.QueueSave(this);

    public void Load(ConfigMigrationService migrator)
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Glamourer.Log.Error(
                $"Error parsing Configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.ConfigFile))
            return;

        if (File.Exists(_saveService.FileNames.ConfigFile))
            try
            {
                var text = File.ReadAllText(_saveService.FileNames.ConfigFile);
                JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
                {
                    Error = HandleDeserializationError,
                });
            }
            catch (Exception ex)
            {
                Glamourer.ChatService.NotificationMessage(ex,
                    "Error reading Configuration, reverting to default.\nYou may be able to restore your configuration using the rolling backups in the XIVLauncher/backups/Glamourer directory.",
                    "Error reading Configuration", "Error", NotificationType.Error);
            }

        migrator.Migrate(this);
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

public class ConfigMigrationService
{
    private readonly SaveService _saveService;

    private Configuration _config = null!;
    private JObject       _data   = null!;

    public ConfigMigrationService(SaveService saveService)
        => _saveService = saveService;

    public void Migrate(Configuration config)
    {
        _config = config;
        _data   = JObject.Parse(File.ReadAllText(_saveService.FileNames.ConfigFile));
        MigrateV1To2();
    }

    private void MigrateV1To2()
    {
        if (_config.Version > 1)
            return;

        _config.Version = 2;
        
    }
}

public class ConfigurationOld : IPluginConfiguration, ISavable
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

    public ConfigurationOld(SaveService saveService)
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
