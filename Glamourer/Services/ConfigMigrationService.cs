using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Gui;
using ImSharp;
using Luna;
using Newtonsoft.Json.Linq;

namespace Glamourer.Services;

public sealed class ConfigMigrationService(SaveService saveService, FixedDesignMigrator fixedDesignMigrator, BackupService backupService)
    : IRequiredService
{
    private Configuration _config = null!;
    private JObject       _data   = null!;

    public void Migrate(Configuration config)
    {
        _config = config;
        if (config.Version >= Configuration.CurrentVersion || !File.Exists(saveService.FileNames.ConfigurationFile))
        {
            AddColors(config, false);
            return;
        }

        _data = JObject.Parse(File.ReadAllText(saveService.FileNames.ConfigurationFile));
        MigrateV1To2();
        MigrateV2To4();
        MigrateV4To5();
        MigrateV5To6();
        MigrateV6To7();
        MigrateV7To8();
        MigrateV8To9();
        MigrateV9To11();
        AddColors(config, true);
    }

    private void MigrateV9To11()
    {
        if (_config.Version > 10)
            return;

        // Migrate key data.
        if (_data["ToggleQuickDesignBar"] is JObject jObj)
        {
            var hotkey = jObj["Hotkey"]?.Value<ushort>() ?? 0;
            if (jObj["Modifiers"] is JObject modifiers)
            {
                var modifier1 = modifiers["Modifier1"]?["Modifier"]?.Value<ushort>() ?? 0;
                var modifier2 = modifiers["Modifier2"]?["Modifier"]?.Value<ushort>() ?? 0;
                _config.ToggleQuickDesignBar = new ModifiableHotkey((VirtualKey)hotkey, new ModifierHotkey((VirtualKey)modifier1),
                    new ModifierHotkey((VirtualKey)modifier2));
            }
            else
            {
                _config.ToggleQuickDesignBar = new ModifiableHotkey((VirtualKey)hotkey);
            }
        }

        if (_data["DeleteDesignModifier"] is JObject deleteModifier)
        {
            var modifier1 = deleteModifier["Modifier1"]?["Modifier"]?.Value<ushort>() ?? (ushort)VirtualKey.CONTROL;
            var modifier2 = deleteModifier["Modifier2"]?["Modifier"]?.Value<ushort>() ?? (ushort)VirtualKey.SHIFT;
            _config.DeleteDesignModifier = new DoubleModifier((VirtualKey)modifier1, (VirtualKey)modifier2);
        }

        if (_data["IncognitoModifier"] is JObject incognitoModifier)
        {
            var modifier1 = incognitoModifier["Modifier1"]?["Modifier"]?.Value<ushort>() ?? (ushort)VirtualKey.CONTROL;
            var modifier2 = incognitoModifier["Modifier2"]?["Modifier"]?.Value<ushort>() ?? 0;
            _config.IncognitoModifier = new DoubleModifier((VirtualKey)modifier1, (VirtualKey)modifier2);
        }

        // Remove pure 'D' or 'Control + Shift + D' once.
        if (_config.ToggleQuickDesignBar.Equals(new ModifiableHotkey(VirtualKey.D, ModifierHotkey.Control, ModifierHotkey.Shift))
         || _config.ToggleQuickDesignBar.Equals(new ModifiableHotkey(VirtualKey.D)))
            _config.ToggleQuickDesignBar = new ModifiableHotkey(VirtualKey.NO_KEY);

        _config.Version           = 11;
        _config.Ephemeral.Version = 11;
        _config.Save();
        _config.Ephemeral.Save();
    }

    private void MigrateV8To9()
    {
        if (_config.Version > 8)
            return;

        backupService.CreateMigrationBackup("pre_filesystem_update", saveService.FileNames.MigrationDesignFileSystem);
        _config.Version           = 9;
        _config.Ephemeral.Version = 9;
        _config.Save();
        _config.Ephemeral.Save();
    }

    private void MigrateV7To8()
    {
        if (_config.Version > 7)
            return;

        if (_config.QdbButtons.HasFlag(QdbButtons.RevertAdvancedDyes))
            _config.QdbButtons |= QdbButtons.RevertAdvancedCustomization;
        _config.Version = 8;
    }

    private void MigrateV6To7()
    {
        if (_config.Version > 6)
            return;

        // Do not actually change anything in the config, just create a backup before designs are migrated.
        backupService.CreateMigrationBackup("pre_gloss_specular_migration");
        _config.Version = 7;
    }

    private void MigrateV5To6()
    {
        if (_config.Version > 5)
            return;

        if (_data["ShowRevertAdvancedParametersButton"]?.ToObject<bool>() ?? true)
            _config.QdbButtons |= QdbButtons.RevertAdvancedCustomization;
        _config.Version = 6;
    }

    // Ephemeral Configuration.
    private void MigrateV4To5()
    {
        if (_config.Version > 4)
            return;

        _config.Ephemeral.IncognitoMode      = _data["IncognitoMode"]?.ToObject<bool>() ?? _config.Ephemeral.IncognitoMode;
        _config.Ephemeral.UnlockDetailMode   = _data["UnlockDetailMode"]?.ToObject<bool>() ?? _config.Ephemeral.UnlockDetailMode;
        _config.Ephemeral.ShowDesignQuickBar = _data["ShowDesignQuickBar"]?.ToObject<bool>() ?? _config.Ephemeral.ShowDesignQuickBar;
        _config.Ephemeral.LockDesignQuickBar = _data["LockDesignQuickBar"]?.ToObject<bool>() ?? _config.Ephemeral.LockDesignQuickBar;
        _config.Ephemeral.LockMainWindow     = _data["LockMainWindow"]?.ToObject<bool>() ?? _config.Ephemeral.LockMainWindow;
        _config.Ephemeral.SelectedMainTab    = _data["SelectedTab"]?.ToObject<MainTabType>() ?? _config.Ephemeral.SelectedMainTab;
        _config.Ephemeral.LastSeenVersion    = _data["LastSeenVersion"]?.ToObject<int>() ?? _config.Ephemeral.LastSeenVersion;
        _config.Version                      = 5;
        _config.Ephemeral.Version            = 5;
        _config.Ephemeral.Save();
    }

    private void MigrateV1To2()
    {
        if (_config.Version > 1)
            return;

        backupService.CreateMigrationBackup("pre_v1_to_v2_migration", saveService.FileNames.MigrationDesignFile);
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
        foreach (var color in ColorId.Values)
            save |= config.Colors.TryAdd(color, color.Data().DefaultColor);

        if (save || forceSave)
            config.Save();
        Colors.SetColors(config);
    }
}
