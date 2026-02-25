using System.Collections.Frozen;
using Luna;

namespace Glamourer.Designs;

public readonly struct CreationDate : ISortMode
{
    public static readonly CreationDate Instance = new();

    public ReadOnlySpan<byte> Name
        => "Creation Date (Older First)"u8;

    public ReadOnlySpan<byte> Description
        => "In each folder, sort all subfolders lexicographically, then sort all leaves using their creation date."u8;

    public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder f)
        => f.GetSubFolders().Cast<IFileSystemNode>().Concat(f.GetLeaves().OfType<IFileSystemData<Design>>().OrderBy(l => l.Value.CreationDate));
}

public readonly struct UpdateDate : ISortMode
{
    public static readonly UpdateDate Instance = new();

    public ReadOnlySpan<byte> Name
        => "Update Date (Older First)"u8;

    public ReadOnlySpan<byte> Description
        => "In each folder, sort all subfolders lexicographically, then sort all leaves using their last update date."u8;

    public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder f)
        => f.GetSubFolders().Cast<IFileSystemNode>().Concat(f.GetLeaves().OfType<IFileSystemData<Design>>().OrderBy(l => l.Value.LastEdit));
}

public readonly struct InverseCreationDate : ISortMode
{
    public static readonly InverseCreationDate Instance = new();

    public ReadOnlySpan<byte> Name
        => "Creation Date (Newer First)"u8;

    public ReadOnlySpan<byte> Description
        => "In each folder, sort all subfolders lexicographically, then sort all leaves using their inverse creation date."u8;

    public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder f)
        => f.GetSubFolders().Cast<IFileSystemNode>()
            .Concat(f.GetLeaves().OfType<IFileSystemData<Design>>().OrderByDescending(l => l.Value.CreationDate));
}

public readonly struct InverseUpdateDate : ISortMode
{
    public static readonly InverseUpdateDate Instance = new();

    public ReadOnlySpan<byte> Name
        => "Update Date (Newer First)"u8;

    public ReadOnlySpan<byte> Description
        => "In each folder, sort all subfolders lexicographically, then sort all leaves using their inverse last update date."u8;

    public IEnumerable<IFileSystemNode> GetChildren(IFileSystemFolder f)
        => f.GetSubFolders().Cast<IFileSystemNode>()
            .Concat(f.GetLeaves().OfType<IFileSystemData<Design>>().OrderByDescending(l => l.Value.LastEdit));
}

public static class SortModeExtensions
{
    private static readonly FrozenDictionary<string, ISortMode> ValidSortModes = new Dictionary<string, ISortMode>
    {
        [nameof(ISortMode.FoldersFirst)]           = ISortMode.FoldersFirst,
        [nameof(ISortMode.Lexicographical)]        = ISortMode.Lexicographical,
        [nameof(CreationDate)]                     = ISortMode.CreationDate,
        [nameof(InverseCreationDate)]              = ISortMode.InverseCreationDate,
        [nameof(UpdateDate)]                       = ISortMode.UpdateDate,
        [nameof(InverseUpdateDate)]                = ISortMode.InverseUpdateDate,
        [nameof(ISortMode.InverseFoldersFirst)]    = ISortMode.InverseFoldersFirst,
        [nameof(ISortMode.InverseLexicographical)] = ISortMode.InverseLexicographical,
        [nameof(ISortMode.FoldersLast)]            = ISortMode.FoldersLast,
        [nameof(ISortMode.InverseFoldersLast)]     = ISortMode.InverseFoldersLast,
        [nameof(ISortMode.InternalOrder)]          = ISortMode.InternalOrder,
        [nameof(ISortMode.InverseInternalOrder)]   = ISortMode.InverseInternalOrder,
    }.ToFrozenDictionary();

    extension(ISortMode)
    {
        public static ISortMode CreationDate
            => CreationDate.Instance;

        public static ISortMode InverseCreationDate
            => InverseCreationDate.Instance;

        public static ISortMode UpdateDate
            => UpdateDate.Instance;

        public static ISortMode InverseUpdateDate
            => InverseUpdateDate.Instance;

        public static IReadOnlyDictionary<string, ISortMode> Valid
            => ValidSortModes;
    }
}
