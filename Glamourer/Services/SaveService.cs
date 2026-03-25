using Luna;

namespace Glamourer.Services;

/// <summary>
/// Any file type that we want to save via SaveService.
/// </summary>
public interface ISavable : ISavable<FilenameService>;

public sealed class SaveService(Logger log, FrameworkManager framework, FilenameService fileNames)
    : BaseSaveService<FilenameService>(log, framework, fileNames), IService;
