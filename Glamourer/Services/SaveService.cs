using Luna;

namespace Glamourer.Services;

/// <summary>
/// Any file type that we want to save via SaveService.
/// </summary>
public interface ISavable : ISavable<FilenameService>;

public sealed class SaveService : BaseSaveService<FilenameService>, IService
{
    public SaveService(LunaLogger log, FrameworkManager framework, FilenameService fileNames)
        : base(log, framework, fileNames)
    {
        BackupMode = BackupMode.SingleBackup;
    }
}
