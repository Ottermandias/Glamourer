using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Services;

namespace Glamourer.Services;

public class BackupService : IAsyncService
{
    private readonly Logger                  _logger;
    private readonly DirectoryInfo           _configDirectory;
    private readonly IReadOnlyList<FileInfo> _fileNames;

    public BackupService(Logger logger, FilenameService fileNames)
    {
        _logger          = logger;
        _fileNames       = GlamourerFiles(fileNames);
        _configDirectory = new DirectoryInfo(fileNames.ConfigDirectory);
        Awaiter          = Task.Run(() => Backup.CreateAutomaticBackup(logger, new DirectoryInfo(fileNames.ConfigDirectory), _fileNames));
    }

    /// <summary> Create a permanent backup with a given name for migrations. </summary>
    public void CreateMigrationBackup(string name)
        => Backup.CreatePermanentBackup(_logger, _configDirectory, _fileNames, name);

    /// <summary> Collect all relevant files for glamourer configuration. </summary>
    private static IReadOnlyList<FileInfo> GlamourerFiles(FilenameService fileNames)
    {
        var list = new List<FileInfo>(16)
        {
            new(fileNames.ConfigFile),
            new(fileNames.DesignFileSystem),
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

    public Task Awaiter { get; }

    public bool Finished
        => Awaiter.IsCompletedSuccessfully;
}
