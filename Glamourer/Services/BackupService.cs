using Luna;

namespace Glamourer.Services;

public sealed class BackupService(Logger log, FilenameService provider) : BaseBackupService<FilenameService>(log, provider)
{
    /// <summary> Collect all relevant files for glamourer configuration. </summary>
    private static IReadOnlyList<FileInfo> GlamourerFiles(FilenameService fileNames)
    {
        var list = new List<FileInfo>(16)
        {
            new(fileNames.ConfigurationFile),
            new(fileNames.UiConfiguration),
            new(fileNames.MigrationDesignFileSystem),
            new(fileNames.MigrationDesignFile),
            new(fileNames.AutomationFile),
            new(fileNames.UnlockFileCustomize),
            new(fileNames.UnlockFileItems),
            new(fileNames.FavoriteFile),
            new(fileNames.DesignColorFile),
        };

        list.AddRange(fileNames.Designs());

        return list;
    }
}
