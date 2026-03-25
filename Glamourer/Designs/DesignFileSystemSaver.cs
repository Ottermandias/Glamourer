using Glamourer.Services;
using Luna;

namespace Glamourer.Designs;

public sealed class DesignFileSystemSaver(Logger log, BaseFileSystem fileSystem, SaveService saveService, DesignStorage designs)
    : FileSystemSaver<SaveService, FilenameService>(log, fileSystem, saveService)
{
    protected override void SaveDataValue(IFileSystemValue value)
    {
        if (value is Design design)
            SaveService.QueueSave(design);
    }

    protected override string LockedFile(FilenameService provider)
        => provider.FileSystemLockedNodes;

    protected override string ExpandedFile(FilenameService provider)
        => provider.FileSystemExpandedFolders;

    protected override string EmptyFoldersFile(FilenameService provider)
        => provider.FileSystemEmptyFolders;

    protected override string SelectionFile(FilenameService provider)
        => provider.FileSystemSelectedNodes;

    protected override string MigrationFile(FilenameService provider)
        => provider.MigrationDesignFileSystem;

    protected override bool GetValueFromIdentifier(ReadOnlySpan<char> identifier, [NotNullWhen(true)] out IFileSystemValue? value)
    {
        if (!Guid.TryParse(identifier, out var guid))
        {
            value = null;
            return false;
        }

        value = designs.FirstOrDefault(d => d.Identifier == guid);
        return value is not null;
    }

    protected override void CreateDataNodes()
    {
        foreach (var design in designs)
        {
            try
            {
                var folder = design.Path.Folder.Length is 0 ? FileSystem.Root : FileSystem.FindOrCreateAllFolders(design.Path.Folder);
                FileSystem.CreateDuplicateDataNode(folder, design.Path.SortName ?? design.Name, design);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not create folder structure for design {design.Name} at path {design.Path.Folder}: {ex}");
            }
        }
    }
}
