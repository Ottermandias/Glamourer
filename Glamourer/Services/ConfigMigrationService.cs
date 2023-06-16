using System;
using System.IO;
using Glamourer.Gui;
using Newtonsoft.Json.Linq;

namespace Glamourer.Services;

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
        if (config.Version >= Configuration.Constants.CurrentVersion || !File.Exists(_saveService.FileNames.ConfigFile))
        {
            AddColors(config, false);
            return;
        }

        _data = JObject.Parse(File.ReadAllText(_saveService.FileNames.ConfigFile));
        MigrateV1To2();
        AddColors(config, true);
    }

    private void MigrateV1To2()
    {
        if (_config.Version > 1)
            return;

        _config.Version = 2;
        var customizationColor = _data["CustomizationColor"]?.ToObject<uint>() ?? ColorId.CustomizationDesign.Data().DefaultColor;
        _config.Colors[ColorId.CustomizationDesign] = customizationColor;
        var stateColor = _data["StateColor"]?.ToObject<uint>() ?? ColorId.StateDesign.Data().DefaultColor;
        _config.Colors[ColorId.StateDesign] = stateColor;
        var equipmentColor = _data["EquipmentColor"]?.ToObject<uint>() ?? ColorId.EquipmentDesign.Data().DefaultColor;
        _config.Colors[ColorId.EquipmentDesign] = equipmentColor;
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
