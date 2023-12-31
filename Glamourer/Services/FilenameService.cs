using System.Collections.Generic;
using System.IO;
using Dalamud.Plugin;
using Glamourer.Designs;

namespace Glamourer.Services;

public class FilenameService
{
    public readonly string ConfigDirectory;
    public readonly string ConfigFile;
    public readonly string DesignFileSystem;
    public readonly string MigrationDesignFile;
    public readonly string DesignDirectory;
    public readonly string AutomationFile;
    public readonly string UnlockFileCustomize;
    public readonly string UnlockFileItems;
    public readonly string FavoriteFile;
    public readonly string DesignColorFile;
    public readonly string EphemeralConfigFile;
    public readonly string NpcAppearanceFile;

    public FilenameService(DalamudPluginInterface pi)
    {
        ConfigDirectory     = pi.ConfigDirectory.FullName;
        ConfigFile          = pi.ConfigFile.FullName;
        AutomationFile      = Path.Combine(ConfigDirectory, "automation.json");
        DesignFileSystem    = Path.Combine(ConfigDirectory, "sort_order.json");
        MigrationDesignFile = Path.Combine(ConfigDirectory, "Designs.json");
        UnlockFileCustomize = Path.Combine(ConfigDirectory, "unlocks_customize.json");
        UnlockFileItems     = Path.Combine(ConfigDirectory, "unlocks_items.json");
        DesignDirectory     = Path.Combine(ConfigDirectory, "designs");
        FavoriteFile        = Path.Combine(ConfigDirectory, "favorites.json");
        DesignColorFile     = Path.Combine(ConfigDirectory, "design_colors.json");
        EphemeralConfigFile = Path.Combine(ConfigDirectory, "ephemeral_config.json");
        NpcAppearanceFile   = Path.Combine(ConfigDirectory, "npc_appearance_data.json");
    }

    public IEnumerable<FileInfo> Designs()
    {
        if (!Directory.Exists(DesignDirectory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(DesignDirectory, "*.json", SearchOption.TopDirectoryOnly))
            yield return new FileInfo(file);
    }

    public string DesignFile(string identifier)
        => Path.Combine(DesignDirectory, $"{identifier}.json");

    public string DesignFile(Design design)
        => DesignFile(design.Identifier.ToString());
}
