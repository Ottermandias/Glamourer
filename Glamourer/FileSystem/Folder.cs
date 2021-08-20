using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Glamourer.FileSystem
{
    public enum SortMode
    {
        FoldersFirst    = 0x00,
        Lexicographical = 0x01,
    }

    public class Folder : IFileSystemBase
    {
        public          Folder                Parent { get; set; }
        public          string                Name   { get; set; }
        public readonly List<IFileSystemBase> Children = new();

        public Folder(Folder parent, string name)
        {
            Parent = parent;
            Name   = name;
        }

        public override string ToString()
            => this.FullName();

        // Return the number of all leaves with this folder in their path.
        public int TotalDescendantLeaves()
        {
            var sum = 0;
            foreach (var child in Children)
            {
                switch (child)
                {
                    case Folder f:
                        sum += f.TotalDescendantLeaves();
                        break;
                    case Link l:
                        sum += l.Data is Folder fl ? fl.TotalDescendantLeaves() : 1;
                        break;
                    default:
                        ++sum;
                        break;
                }
            }

            return sum;
        }

        // Return all descendant mods in the specified order.
        public IEnumerable<IFileSystemBase> AllLeaves(SortMode mode)
        {
            return GetSortedEnumerator(mode).SelectMany(f =>
            {
                if (f.IsFolder(out var folder))
                    return folder.AllLeaves(mode);

                return new[]
                {
                    f,
                };
            });
        }

        public IEnumerable<IFileSystemBase> AllChildren(SortMode mode)
            => GetSortedEnumerator(mode);

        // Get an enumerator for actually sorted objects instead of folder-first objects.
        private IEnumerable<IFileSystemBase> GetSortedEnumerator(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.FoldersFirst:
                    foreach (var child in Children.Where(c => c.IsFolder()))
                        yield return child;

                    foreach (var child in Children.Where(c => c.IsLeaf()))
                        yield return child;

                    break;
                case SortMode.Lexicographical:
                    foreach (var child in Children)
                        yield return child;

                    break;
                default: throw new InvalidEnumArgumentException();
            }
        }

        internal static Folder CreateRoot()
            => new(null!, "");

        // Find a subfolder by name. Returns true and sets folder to it if it exists.
        public bool FindChild(string name, out IFileSystemBase ret)
        {
            var idx = Search(name);
            ret = idx >= 0 ? Children[idx] : this;
            return idx >= 0;
        }

        // Checks if an equivalent child to child already exists and returns its index.
        // If it does not exist, inserts child as a child and returns the new index.
        // Also sets this as childs parent.
        public int FindOrAddChild(IFileSystemBase child)
        {
            var idx = Search(child);
            if (idx >= 0)
                return idx;

            idx = ~idx;
            Children.Insert(idx, child);
            child.Parent = this;
            return idx;
        }

        // Checks if an equivalent child to child already exists and throws if it does.
        // If it does not exist, inserts child as a child and returns the new index.
        // Also sets this as childs parent.
        public int AddChild(IFileSystemBase child)
        {
            var idx = Search(child);
            if (idx >= 0)
                throw new Exception("Could not add child: Child of that name already exists.");

            idx = ~idx;
            Children.Insert(idx, child);
            child.Parent = this;
            return idx;
        }

        // Checks if a subfolder with the given name already exists and returns it and its index.
        // If it does not exists, creates and inserts it and returns the new subfolder and its index.
        public (Folder, int) FindOrCreateSubFolder(string name)
        {
            var subFolder = new Folder(this, name);
            var idx       = FindOrAddChild(subFolder);
            var child     = Children[idx];
            if (!child.IsFolder(out var folder))
                throw new Exception($"The child {name} already exists in {this.FullName()} but is not a folder.");

            return (folder, idx);
        }

        // Remove child if it exists.
        // If this folder is empty afterwards and deleteEmpty is true, remove it from its parent.
        public void RemoveChild(IFileSystemBase child, bool deleteEmpty)
        {
            RemoveChildIgnoreEmpty(child);
            if (deleteEmpty)
                CheckEmpty();
        }

        private void CheckEmpty()
        {
            if (Children.Count == 0)
                Parent?.RemoveChild(this, true);
        }

        // Remove a child but do not remove this folder from its parent if it is empty afterwards.
        internal void RemoveChildIgnoreEmpty(IFileSystemBase folder)
        {
            var idx = Search(folder);
            if (idx < 0)
                return;

            Children[idx].Parent = null!;
            Children.RemoveAt(idx);
        }

        private int Search(string name)
            => Children.BinarySearch(new FileSystemObject(name), FolderStructureComparer.Default);

        private int Search(IFileSystemBase child)
            => Children.BinarySearch(child, FolderStructureComparer.Default);
    }
}
