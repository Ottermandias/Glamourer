using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Plugin;
using ImGuizmoNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Filesystem;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : FileSystem<Design>, IDisposable
{
    public readonly string         DesignFileSystemFile;
    private readonly Design.Manager _designManager;

    public DesignFileSystem(Design.Manager designManager, DalamudPluginInterface pi)
    {
        DesignFileSystemFile = Path.Combine(pi.GetPluginConfigDirectory(), "sort_order.json");
        _designManager       = designManager;
    }

    public struct CreationDate : ISortMode<Design>
    {
        public string Name
            => "Creation Date (Older First)";

        public string Description
            => "In each folder, sort all subfolders lexicographically, then sort all leaves using their creation date.";

        public IEnumerable<IPath> GetChildren(Folder f)
            => f.GetSubFolders().Cast<IPath>().Concat(f.GetLeaves().OrderBy(l => l.Value.CreationDate));
    }

    public struct InverseCreationDate : ISortMode<Design>
    {
        public string Name
            => "Creation Date (Newer First)";

        public string Description
            => "In each folder, sort all subfolders lexicographically, then sort all leaves using their inverse creation date.";

        public IEnumerable<IPath> GetChildren(Folder f)
            => f.GetSubFolders().Cast<IPath>().Concat(f.GetLeaves().OrderByDescending(l => l.Value.CreationDate));
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
        {
            SaveFilesystem();
        }
    }

    private void SaveFilesystem()
    {
        SaveToFile(new FileInfo(DesignFileSystemFile), SaveDesign, true);
        Glamourer.Log.Verbose($"Saved design filesystem.");
    }

    private void Save()
        => Glamourer.Framework.RegisterDelayed(nameof(SaveFilesystem), SaveFilesystem);

    private void OnDataChange(Design.Manager.DesignChangeType type, Design design, string? oldName, string? _2, int _3)
    {
        switch (type)
        {

        }
        if (type == Design.Manager.DesignChangeType.Renamed && oldName != null)
        {
            var old = oldName.FixName();
            if (Find(old, out var child) && child is not Folder)
            {
                Rename(child, design.Name);
            }
        }


    }

    // Used for saving and loading.
    private static string DesignToIdentifier(Design design)
        => design.Identifier.ToString();

    private static string DesignToName(Design design)
        => design.Name.FixName();

    private static bool DesignHasDefaultPath(Design design, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(DesignToName(design))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveDesign(Design design, string fullPath)
        // Only save pairs with non-default paths.
        => DesignHasDefaultPath(design, fullPath)
            ? (string.Empty, false)
            : (DesignToName(design), true);
}

public partial class Design
{
    public partial class Manager
    {
        public const    string DesignFolderName = "designs";
        public readonly string DesignFolder;

        private readonly List<Design> _designs = new();

        public enum DesignChangeType
        {
            Created,
            Deleted,
            ReloadedAll,
            Renamed,
            ChangedDescription,
            AddedTag,
            RemovedTag,
            ChangedTag,
        }

        public delegate void DesignChangeDelegate(DesignChangeType type, int designIdx, string? oldData = null, string? newData = null,
            int tagIdx = -1);

        public event DesignChangeDelegate? DesignChange;

        public IReadOnlyList<Design> Designs
            => _designs;

        public Manager(DalamudPluginInterface pi)
            => DesignFolder = SetDesignFolder(pi);

        private static string SetDesignFolder(DalamudPluginInterface pi)
        {
            var ret = Path.Combine(pi.GetPluginConfigDirectory(), DesignFolderName);
            if (Directory.Exists(ret))
                return ret;

            try
            {
                Directory.CreateDirectory(ret);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not create design folder directory at {ret}:\n{ex}");
            }

            return ret;
        }

        private string CreateFileName(Design design)
            => Path.Combine(DesignFolder, $"{design.Name.RemoveInvalidPathSymbols()}_{design.Identifier}.json");

        public void SaveDesign(Design design)
        {
            var fileName = CreateFileName(design);
            try
            {
                var data = design.JsonSerialize().ToString(Formatting.Indented);
                File.WriteAllText(fileName, data);
                Glamourer.Log.Debug($"Saved design {design.Identifier}.");
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not save design {design.Identifier} to file:\n{ex}");
            }
        }

        public void LoadDesigns()
        {
            _designs.Clear();
            foreach (var file in new DirectoryInfo(DesignFolder).EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var text   = File.ReadAllText(file.FullName);
                    var data   = JObject.Parse(text);
                    var design = LoadDesign(data);
                    design.Index = _designs.Count;
                    _designs.Add(design);
                }
                catch (Exception ex)
                {
                    Glamourer.Log.Error($"Could not load design, skipped:\n{ex}");
                }
            }

            Glamourer.Log.Information($"Loaded {_designs.Count} designs.");
            DesignChange?.Invoke(DesignChangeType.ReloadedAll, -1);
        }

        public Design Create(string name)
        {
            var design = new Design()
            {
                CreationDate = DateTimeOffset.UtcNow,
                Identifier   = Guid.NewGuid(),
                Index        = _designs.Count,
                Name         = name,
            };
            _designs.Add(design);
            Glamourer.Log.Debug($"Added new design {design.Identifier}.");
            DesignChange?.Invoke(DesignChangeType.Created, design.Index);
            return design;
        }

        public void Delete(Design design)
        {
            _designs.RemoveAt(design.Index);
            foreach (var d in _designs.Skip(design.Index + 1))
                --d.Index;
            var fileName = CreateFileName(design);
            try
            {
                File.Delete(fileName);
                Glamourer.Log.Debug($"Deleted design {design.Identifier}.");
                DesignChange?.Invoke(DesignChangeType.Deleted, design.Index);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not delete design file for {design.Identifier}:\n{ex}");
            }
        }

        public void Rename(Design design, string newName)
        {
            var oldName     = design.Name;
            var oldFileName = CreateFileName(design);
            if (File.Exists(oldFileName))
                try
                {
                    File.Delete(oldFileName);
                }
                catch (Exception ex)
                {
                    Glamourer.Log.Error($"Could not delete old design file for rename from {design.Identifier}:\n{ex}");
                    return;
                }

            design.Name = newName;
            SaveDesign(design);
            Glamourer.Log.Debug($"Renamed design {design.Identifier}.");
            DesignChange?.Invoke(DesignChangeType.Renamed, design.Index, oldName, newName);
        }

        public void ChangeDescription(Design design, string description)
        {
            var oldDescription = design.Description;
            design.Description = description;
            SaveDesign(design);
            Glamourer.Log.Debug($"Renamed design {design.Identifier}.");
            DesignChange?.Invoke(DesignChangeType.ChangedDescription, design.Index, oldDescription, description);
        }

        public void AddTag(Design design, string tag)
        {
            if (design.Tags.Contains(tag))
                return;

            design.Tags = design.Tags.Append(tag).OrderBy(t => t).ToArray();
            var idx = design.Tags.IndexOf(tag);
            SaveDesign(design);
            Glamourer.Log.Debug($"Added tag at {idx} to design {design.Identifier}.");
            DesignChange?.Invoke(DesignChangeType.AddedTag, design.Index, null, tag, idx);
        }

        public void RemoveTag(Design design, string tag)
        {
            var idx = design.Tags.IndexOf(tag);
            if (idx >= 0)
                RemoveTag(design, idx);
        }

        public void RemoveTag(Design design, int tagIdx)
        {
            var oldTag = design.Tags[tagIdx];
            design.Tags = design.Tags.Take(tagIdx).Concat(design.Tags.Skip(tagIdx + 1)).ToArray();
            SaveDesign(design);
            Glamourer.Log.Debug($"Removed tag at {tagIdx} from design {design.Identifier}.");
            DesignChange?.Invoke(DesignChangeType.RemovedTag, design.Index, oldTag, null, tagIdx);
        }


        public void RenameTag(Design design, int tagIdx, string newTag)
        {
            var oldTag = design.Tags[tagIdx];
            if (oldTag == newTag)
                return;

            design.Tags[tagIdx] = newTag;
            Array.Sort(design.Tags);
            SaveDesign(design);
            Glamourer.Log.Debug($"Renamed tag at {tagIdx} in design {design.Identifier} and resorted tags.");
            DesignChange?.Invoke(DesignChangeType.ChangedTag, design.Index, oldTag, newTag, tagIdx);
        }
    }
}
