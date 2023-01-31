using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using OtterGui.Filesystem;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : FileSystem<Design>, IDisposable
{
    public static string GetDesignFileSystemFile(DalamudPluginInterface pi)
        => Path.Combine(pi.GetPluginConfigDirectory(), "sort_order.json");

    public readonly  string           DesignFileSystemFile;
    private readonly FrameworkManager _framework;
    private readonly Design.Manager   _designManager;

    public DesignFileSystem(Design.Manager designManager, DalamudPluginInterface pi, FrameworkManager framework)
    {
        DesignFileSystemFile        =  GetDesignFileSystemFile(pi);
        _designManager              =  designManager;
        _framework                  =  framework;
        _designManager.DesignChange += OnDataChange;
        Changed                     += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(DesignFileSystemFile), _designManager.Designs, DesignToIdentifier, DesignToName))
            SaveFilesystem();

        Glamourer.Log.Debug("Reloaded design filesystem.");
    }

    public void Dispose()
    {
        _designManager.DesignChange -= OnDataChange;
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
            SaveFilesystem();
    }

    private void SaveFilesystem()
    {
        SaveToFile(new FileInfo(DesignFileSystemFile), SaveDesign, true);
        Glamourer.Log.Verbose("Saved design filesystem.");
    }

    public void Save()
        => _framework.RegisterDelayed(nameof(SaveFilesystem), SaveFilesystem);

    private void OnDataChange(Design.Manager.DesignChangeType type, Design design, object? data)
    {
        switch (type)
        {
            case Design.Manager.DesignChangeType.Created:
                var originalName = design.Name.Text.FixName();
                var name         = originalName;
                var counter      = 1;
                while (Find(name, out _))
                    name = $"{originalName} ({++counter})";

                CreateLeaf(Root, name, design);
                break;
            case Design.Manager.DesignChangeType.Deleted:
                if (FindLeaf(design, out var leaf))
                    Delete(leaf);
                break;
            case Design.Manager.DesignChangeType.ReloadedAll:
                Reload();
                break;
            case Design.Manager.DesignChangeType.Renamed when data is string oldName:
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

    internal static void MigrateOldPaths(DalamudPluginInterface pi, Dictionary<string, string> oldPaths)
    {
        if (oldPaths.Count == 0)
            return;

        var file = GetDesignFileSystemFile(pi);
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
}
