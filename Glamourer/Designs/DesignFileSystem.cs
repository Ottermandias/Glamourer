using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Glamourer.Events;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Filesystem;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : FileSystem<Design>, IDisposable, ISavable
{
    private readonly DesignChanged _designChanged;

    private readonly SaveService   _saveService;
    private readonly DesignManager _designManager;

    public DesignFileSystem(DesignManager designManager, SaveService saveService, DesignChanged designChanged)
    {
        _designManager = designManager;
        _saveService   = saveService;
        _designChanged = designChanged;
        _designChanged.Subscribe(OnDesignChange, DesignChanged.Priority.DesignFileSystem);
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_saveService.FileNames.DesignFileSystem), _designManager.Designs, DesignToIdentifier, DesignToName))
            _saveService.ImmediateSave(this);

        Glamourer.Log.Debug("Reloaded design filesystem.");
    }

    public void Dispose()
    {
        _designChanged.Unsubscribe(OnDesignChange);
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

    public struct UpdateDate : ISortMode<Design>
    {
        public string Name
            => "Update Date (Older First)";

        public string Description
            => "In each folder, sort all subfolders lexicographically, then sort all leaves using their last update date.";

        public IEnumerable<IPath> GetChildren(Folder f)
            => f.GetSubFolders().Cast<IPath>().Concat(f.GetLeaves().OrderBy(l => l.Value.LastEdit));
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

    public struct InverseUpdateDate : ISortMode<Design>
    {
        public string Name
            => "Update Date (Newer First)";

        public string Description
            => "In each folder, sort all subfolders lexicographically, then sort all leaves using their inverse last update date.";

        public IEnumerable<IPath> GetChildren(Folder f)
            => f.GetSubFolders().Cast<IPath>().Concat(f.GetLeaves().OrderByDescending(l => l.Value.LastEdit));
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _saveService.QueueSave(this);
    }

    private void OnDesignChange(DesignChanged.Type type, Design design, object? data)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
                var originalName = design.Name.Text.FixName();
                var name         = originalName;
                var counter      = 1;
                while (Find(name, out _))
                    name = $"{originalName} ({++counter})";

                CreateLeaf(Root, name, design);
                break;
            case DesignChanged.Type.Deleted:
                if (FindLeaf(design, out var leaf))
                    Delete(leaf);
                break;
            case DesignChanged.Type.ReloadedAll:
                Reload();
                break;
            case DesignChanged.Type.Renamed when data is string oldName:
                var old = oldName.FixName();
                if (Find(old, out var child) && child is not Folder)
                    Rename(child, design.Name);
                break;
        }
    }

    // Used for saving and loading.
    private static string DesignToIdentifier(Design design)
        => design.Identifier.ToString();

    private static string DesignToName(Design design)
        => design.Name.Text.FixName();

    private static bool DesignHasDefaultPath(Design design, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(DesignToName(design))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveDesign(Design design, string fullPath)
        // Only save pairs with non-default paths.
        => DesignHasDefaultPath(design, fullPath)
            ? (string.Empty, false)
            : (DesignToIdentifier(design), true);

    // Search the entire filesystem for the leaf corresponding to a design.
    public bool FindLeaf(Design design, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Design>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == design);
        return leaf != null;
    }

    internal static void MigrateOldPaths(SaveService saveService, Dictionary<string, string> oldPaths)
    {
        if (oldPaths.Count == 0)
            return;

        var file = saveService.FileNames.DesignFileSystem;
        try
        {
            JObject jObject;
            if (File.Exists(file))
            {
                var text = File.ReadAllText(file);
                jObject = JObject.Parse(text);
                var dict = jObject["Data"]?.ToObject<Dictionary<string, string>>();
                if (dict != null)
                    foreach (var (key, value) in dict)
                        oldPaths.TryAdd(key, value);

                jObject["Data"] = JToken.FromObject(oldPaths);
            }
            else
            {
                jObject = new JObject
                {
                    ["Data"]         = JToken.FromObject(oldPaths),
                    ["EmptyFolders"] = JToken.FromObject(Array.Empty<string>()),
                };
            }

            var data = jObject.ToString(Formatting.Indented);
            File.WriteAllText(file, data);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not migrate old folder paths to new version:\n{ex}");
        }
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.DesignFileSystem;

    public void Save(StreamWriter writer)
    {
        SaveToFile(writer, SaveDesign, true);
    }
}
