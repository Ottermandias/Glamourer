using Dalamud.Plugin;
using Glamourer.Designs;
using Luna;

namespace Glamourer.Services;

public sealed class FilenameService(IDalamudPluginInterface pi) : BaseFilePathProvider(pi)
{
    public readonly string MigrationDesignFileSystem = Path.Combine(pi.ConfigDirectory.FullName, "sort_order.json");
    public readonly string MigrationDesignFile       = Path.Combine(pi.ConfigDirectory.FullName, "Designs.json");
    public readonly string DesignDirectory           = Path.Combine(pi.ConfigDirectory.FullName, "designs");
    public readonly string AutomationFile            = Path.Combine(pi.ConfigDirectory.FullName, "automation.json");
    public readonly string IgnoredModsFile           = Path.Combine(pi.ConfigDirectory.FullName, "ignored_mods.json");
    public readonly string UnlockFileCustomize       = Path.Combine(pi.ConfigDirectory.FullName, "unlocks_customize.json");
    public readonly string UnlockFileItems           = Path.Combine(pi.ConfigDirectory.FullName, "unlocks_items.json");
    public readonly string FavoriteFile              = Path.Combine(pi.ConfigDirectory.FullName, "favorites.json");
    public readonly string DesignColorFile           = Path.Combine(pi.ConfigDirectory.FullName, "design_colors.json");
    public readonly string EphemeralConfigFile       = Path.Combine(pi.ConfigDirectory.FullName, "ephemeral_config.json");
    public readonly string NpcAppearanceFile         = Path.Combine(pi.ConfigDirectory.FullName, "npc_appearance_data.json");
    public readonly string CollectionOverrideFile    = Path.Combine(pi.ConfigDirectory.FullName, "collection_overrides.json");
    public readonly string UiConfiguration           = Path.Combine(pi.ConfigDirectory.FullName, "ui_config.json");
    public readonly string FileSystemFolder          = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem");
    public readonly string FileSystemEmptyFolders    = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "empty_folders.json");
    public readonly string FileSystemExpandedFolders = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "expanded_folders.json");
    public readonly string FileSystemLockedNodes     = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "locked_nodes.json");
    public readonly string FileSystemSelectedNodes   = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "selected_nodes.json");

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

    public override List<FileInfo> GetBackupFiles()
    {
        var list = new List<FileInfo>(16)
        {
            new(ConfigurationFile),
            new(AutomationFile),
            new(IgnoredModsFile),
            new(UnlockFileCustomize),
            new(UnlockFileItems),
            new(FavoriteFile),
            new(DesignColorFile),
            new(FileSystemEmptyFolders),
            new(FileSystemLockedNodes),
        };
        // Do not back up expanded folders, selected nodes, ui configuration or ephemeral config.
        list.AddRange(Designs());
        return list;
    }
}
