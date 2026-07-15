using Dalamud.Plugin;
using Glamourer.Designs;
using Luna;

namespace Glamourer.Services;

public sealed class FilenameService(IDalamudPluginInterface pi) : BaseFilePathProvider(pi)
{
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
    public readonly string UiConfigurationFile       = Path.Combine(pi.ConfigDirectory.FullName, "ui_config.json");
    public readonly string PredefinedTagFile         = Path.Combine(pi.ConfigDirectory.FullName, "predefined_tags.json");
    public readonly string FilterFile                = Path.Combine(pi.ConfigDirectory.FullName, "filters.json");
    public readonly string FileSystemFolder          = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem");
    public readonly string FileSystemOrganization    = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "organization.json");
    public readonly string FileSystemExpandedFolders = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "expanded_folders.json");
    public readonly string FileSystemLockedNodes     = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "locked_nodes.json");
    public readonly string FileSystemSelectedNodes   = Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "selected_nodes.json");

    public readonly string MigrationFileSystemEmptyFolders =
        Path.Combine(pi.ConfigDirectory.FullName, "design_filesystem", "empty_folders.json");

    public readonly string MigrationDesignFileSystem = Path.Combine(pi.ConfigDirectory.FullName, "sort_order.json");
    public readonly string MigrationDesignFile       = Path.Combine(pi.ConfigDirectory.FullName, "Designs.json");

    public IEnumerable<string> Designs()
        => !Directory.Exists(DesignDirectory) ? [] : Directory.EnumerateFiles(DesignDirectory, "*.json", SearchOption.TopDirectoryOnly);

    public string DesignFile(string identifier)
        => Path.Combine(DesignDirectory, $"{identifier}.json");

    public string DesignFile(Design design)
        => DesignFile(design.Identifier.ToString());

    public override List<IBackupFile> GetBackupFiles()
    {
        var list = new List<IBackupFile>(16)
        {
            new DefaultBackupFile(ConfigurationFile),
            new DefaultBackupFile(AutomationFile),
            new DefaultBackupFile(PredefinedTagFile),
            new DefaultBackupFile(IgnoredModsFile),
            new DefaultBackupFile(UnlockFileCustomize),
            new DefaultBackupFile(UnlockFileItems),
            new DefaultBackupFile(FavoriteFile),
            new DefaultBackupFile(DesignColorFile),
            new DefaultBackupFile(MigrationFileSystemEmptyFolders),
            new DefaultBackupFile(FileSystemLockedNodes),
        };
        // Do not back up expanded folders, selected nodes, ui configuration or ephemeral config.
        list.AddRange(Designs().Select(f => new DefaultBackupFile(f)));
        return list;
    }
}
