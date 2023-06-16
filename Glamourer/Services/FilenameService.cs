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

    public FilenameService(DalamudPluginInterface pi)
    {
        ConfigDirectory     = pi.ConfigDirectory.FullName;
        ConfigFile          = pi.ConfigFile.FullName;
        DesignFileSystem    = Path.Combine(ConfigDirectory, "sort_order.json");
        MigrationDesignFile = Path.Combine(ConfigDirectory, "Designs.json");
        DesignDirectory     = Path.Combine(ConfigDirectory, "designs");
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
