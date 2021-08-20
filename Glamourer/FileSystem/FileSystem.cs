using System;
using System.Collections.Generic;
using System.Linq;

namespace Glamourer.FileSystem
{
    public class FileSystem
    {
        public Folder Root { get; } = Folder.CreateRoot();

        public void Clear()
            => Root.Children.Clear();

        // Find a specific child by its path from Root.
        // Returns true if the folder was found, and false if not.
        // The out parameter will contain the furthest existing folder.
        public bool Find(string path, out IFileSystemBase child)
        {
            var split  = Split(path);
            var folder = Root;
            child = Root;
            foreach (var part in split)
            {
                if (!folder.FindChild(part, out var c))
                {
                    child = folder;
                    return false;
                }

                child = c;
                if (c is not Folder f)
                    return part == split.Last();

                folder = f;
            }

            return true;
        }

        public Folder CreateAllFolders(IEnumerable<string> names)
        {
            var last = Root;
            foreach (var name in names)
                last = last.FindOrCreateSubFolder(name).Item1;
            return last;
        }

        public (Folder, string) CreateAllFolders(string path)
        {
            if (!path.Any())
                return (Root, string.Empty);

            var split = Split(path);
            if (split.Length == 1)
                return (Root, path);

            return (CreateAllFolders(split.Take(split.Length - 1)), split.Last());
        }

        public bool Rename(IFileSystemBase child, string newName)
        {
            if (ReferenceEquals(child, Root))
                throw new InvalidOperationException("Can not rename root.");

            newName = FixName(newName);
            if (child.Name == newName)
                return false;

            if (child.Parent.FindChild(newName, out var preExisting))
            {
                if (MergeIfFolders(child, preExisting, false))
                    return true;

                throw new Exception($"Can not rename {child.Name} in {child.Parent.FullName()} to {newName} because {newName} already exists.");
            }

            var parent = child.Parent;
            parent.RemoveChildIgnoreEmpty(child);
            child.Name = newName;
            parent.FindOrAddChild(child);
            return true;
        }

        public bool Move(IFileSystemBase child, Folder newParent, bool deleteEmpty)
        {
            var oldParent = child.Parent;
            if (ReferenceEquals(newParent, oldParent))
                return false;

            // Moving into its own subfolder or itself is not allowed.
            if (child.IsFolder(out var f)
             && (ReferenceEquals(newParent, f)
                 || newParent.FullName().StartsWith(f.FullName(), StringComparison.InvariantCultureIgnoreCase)))
                return false;

            if (newParent.FindChild(child.Name, out var conflict))
            {
                if (MergeIfFolders(child, conflict, deleteEmpty))
                    return true;

                throw new Exception($"Can not move {child.Name} into {newParent.FullName()} because {conflict.FullName()} already exists.");
            }

            oldParent.RemoveChild(child, deleteEmpty);
            newParent.FindOrAddChild(child);
            return true;
        }

        public bool Merge(Folder source, Folder target, bool deleteEmpty)
        {
            if (ReferenceEquals(source, target))
                return false;

            if (!source.Children.Any())
            {
                if (deleteEmpty)
                {
                    source.Parent.RemoveChild(source, true);
                    return true;
                }

                return false;
            }

            while (source.Children.Count > 0)
                Move(source.Children.First(), target, deleteEmpty); // Can throw.

            source.Parent.RemoveChild(source, deleteEmpty);

            return true;
        }

        private bool MergeIfFolders(IFileSystemBase source, IFileSystemBase target, bool deleteEmpty)
        {
            if (source is Folder childF && target.IsFolder(out var preF))
            {
                Merge(childF, preF, deleteEmpty);
                return true;
            }

            return false;
        }

        private static string[] Split(string path)
            => path.Split(new[]
            {
                '/',
            }, StringSplitOptions.RemoveEmptyEntries);

        private static string FixName(string name)
            => name.Replace('/', '\\');
    }
}
