using Glamourer.Automation;
using Glamourer.Gui;
using Newtonsoft.Json.Linq;

namespace Glamourer.Services;

public class ConfigMigrationService(SaveService saveService, FixedDesignMigrator fixedDesignMigrator, BackupService backupService)
{
    private Configuration _config = null!;
    private JObject       _data   = null!;

    public void Migrate(Configuration config)
    {
        _config = config;
        if (config.Version >= Configuration.Constants.CurrentVersion || !File.Exists(saveService.FileNames.ConfigFile))
        {
            AddColors(config, false);
            return;
        }

        _data = JObject.Parse(File.ReadAllText(saveService.FileNames.ConfigFile));
        MigrateV1To2();
        MigrateV2To4();
        MigrateV4To5();
        MigrateV5To6();
        AddColors(config, true);
    }

    private void MigrateV5To6()
    {
        if (_config.Version > 5)
            return;

        if (_data["ShowRevertAdvancedParametersButton"]?.ToObject<bool>() ?? true)
            _config.QdbButtons |= QdbButtons.RevertAdvanced;
        _config.Version = 6;
    }

    // Ephemeral Config.
    private void MigrateV4To5()
    {
        if (_config.Version > 4)
            return;

        _config.Ephemeral.IncognitoMode      = _data["IncognitoMode"]?.ToObject<bool>() ?? _config.Ephemeral.IncognitoMode;
        _config.Ephemeral.UnlockDetailMode   = _data["UnlockDetailMode"]?.ToObject<bool>() ?? _config.Ephemeral.UnlockDetailMode;
        _config.Ephemeral.ShowDesignQuickBar = _data["ShowDesignQuickBar"]?.ToObject<bool>() ?? _config.Ephemeral.ShowDesignQuickBar;
        _config.Ephemeral.LockDesignQuickBar = _data["LockDesignQuickBar"]?.ToObject<bool>() ?? _config.Ephemeral.LockDesignQuickBar;
        _config.Ephemeral.LockMainWindow     = _data["LockMainWindow"]?.ToObject<bool>() ?? _config.Ephemeral.LockMainWindow;
        _config.Ephemeral.SelectedTab        = _data["SelectedTab"]?.ToObject<MainWindow.TabType>() ?? _config.Ephemeral.SelectedTab;
        _config.Ephemeral.LastSeenVersion    = _data["LastSeenVersion"]?.ToObject<int>() ?? _config.Ephemeral.LastSeenVersion;
        _config.Version                      = 5;
        _config.Ephemeral.Version            = 5;
        _config.Ephemeral.Save();
    }

    private void MigrateV1To2()
    {
        if (_config.Version > 1)
            return;

        backupService.CreateMigrationBackup("pre_v1_to_v2_migration");
        fixedDesignMigrator.Migrate(_data["FixedDesigns"]);
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
