using Luna;

namespace Glamourer.Services;

public sealed class BackupService(MainLogger log, FilenameService provider) : BaseBackupService<FilenameService>(log, provider);
