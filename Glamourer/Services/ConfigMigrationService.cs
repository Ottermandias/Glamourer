using System;
using System.IO;
using System.Linq;
using Glamourer.Automation;
using Glamourer.Gui;
using Newtonsoft.Json.Linq;

namespace Glamourer.Services;

public class ConfigMigrationService
{
    private readonly SaveService         _saveService;
    private readonly FixedDesignMigrator _fixedDesignMigrator;
    private readonly BackupService       _backupService;

    private Configuration _config = null!;
    private JObject       _data   = null!;

    public ConfigMigrationService(SaveService saveService, FixedDesignMigrator fixedDesignMigrator, BackupService backupService)
    {
        _saveService         = saveService;
        _fixedDesignMigrator = fixedDesignMigrator;
        _backupService       = backupService;
    }

    public void Migrate(Configuration config)
    {
        _config = config; 
        if (config.Version >= Configuration.Constants.CurrentVersion || !File.Exists(_saveService.FileNames.ConfigFile))
        {
            AddColors(config, false);
            return;
        }

        _data = JObject.Parse(File.ReadAllText(_saveService.FileNames.ConfigFile));
        MigrateV1To2();
        MigrateV2To4();
        AddColors(config, true);
    }

    private void MigrateV1To2()
    {
        if (_config.Version > 1)
            return;

        _backupService.CreateMigrationBackup("pre_v1_to_v2_migration");
        _fixedDesignMigrator.Migrate(_data["FixedDesigns"]);
        _config.Version = 2;
        var customizationColor = _data["CustomizationColor"]?.ToObject<uint>() ?? ColorId.CustomizationDesign.Data().DefaultColor;
        _config.Colors[ColorId.CustomizationDesign] = customizationColor;
        var stateColor = _data["StateColor"]?.ToObject<uint>() ?? ColorId.StateDesign.Data().DefaultColor;
        _config.Colors[ColorId.StateDesign] = stateColor;
        var equipmentColor = _data["EquipmentColor"]?.ToObject<uint>() ?? ColorId.EquipmentDesign.Data().DefaultColor;
        _config.Colors[ColorId.EquipmentDesign] = equipmentColor;
    }

    private void MigrateV2To4()
    {
        if (_config.Version > 4)
            return;
    
        _config.Version = 4;
        _config.Codes   = _config.Codes.DistinctBy(c => c.Code).ToList();
    }

    private static void AddColors(Configuration config, bool forceSave)
    {
        var save = false;
        foreach (var color in Enum.GetValues<ColorId>())
            save |= config.Colors.TryAdd(color, color.Data().DefaultColor);

        if (save || forceSave)
            config.Save();
        Colors.SetColors(config);
    }
}
