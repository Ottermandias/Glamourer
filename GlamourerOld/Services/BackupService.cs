using System.Collections.Generic;
using System.IO;
using OtterGui.Classes;
using OtterGui.Log;

namespace Glamourer.Services;

public class BackupService
{
    public BackupService(Logger logger, FilenameService fileNames)
    {
        var files = GlamourerFiles(fileNames);
        Backup.CreateBackup(logger, new DirectoryInfo(fileNames.ConfigDirectory), files);
    }

    /// <summary> Collect all relevant files for glamourer configuration. </summary>
    private static IReadOnlyList<FileInfo> GlamourerFiles(FilenameService fileNames)
    {
        var list = new List<FileInfo>(16)
        {
            new(fileNames.ConfigFile),
            new(fileNames.DesignFileSystem),
            new(fileNames.MigrationDesignFile),
        };

        list.AddRange(fileNames.Designs());

        return list;
    }
}
