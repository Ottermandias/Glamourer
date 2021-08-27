using System;
using System.Collections.Generic;
using System.Linq;

namespace Glamourer.FileSystem
{
    internal class FolderStructureComparer : IComparer<IFileSystemBase>
    {
        // Compare only the direct folder names since this is only used inside an enumeration of children of one folder.
        public static int Cmp(IFileSystemBase? x, IFileSystemBase? y)
            => ReferenceEquals(x, y) ? 0 : string.Compare(x?.Name, y?.Name, StringComparison.InvariantCultureIgnoreCase);

        public int Compare(IFileSystemBase? x, IFileSystemBase? y)
            => Cmp(x, y);

        internal static readonly FolderStructureComparer Default = new();
    }

    public interface IFileSystemBase
    {
        public Folder Parent { get; set; }
        public string Name   { get; set; }
    }

    public static class FileSystemExtensions
    {
        public static string FullName(this IFileSystemBase data)
            => data.Parent?.Name.Any() ?? false ? $"{data.Parent.FullName()}/{data.Name}" : data.Name;

        public static bool IsLeaf(this IFileSystemBase data)
            => data is not Folder && data is not Link { Data: Folder };

        public static bool IsFolder(this IFileSystemBase data)
            => data.IsFolder(out _);

        public static bool IsFolder(this IFileSystemBase data, out Folder folder)
        {
            switch (data)
            {
                case Folder f:
                    folder = f;
                    return true;
                case Link { Data: Folder fl }:
                    folder = fl;
                    return true;
                default:
                    folder = null!;
                    return false;
            }
        }
    }


    public class FileSystemObject : IFileSystemBase
    {
        public FileSystemObject(string name)
            => Name = name;

        public Folder Parent { get; set; } = null!;
        public string Name   { get; set; }

        public string FullName()
            => Name;
    }
}
