using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using Glamourer.FileSystem;
using Newtonsoft.Json;

namespace Glamourer.Designs
{
    public class DesignManager
    {
        public const     string   FileName = "Designs.json";
        private readonly FileInfo _saveFile;

        public SortedList<string, CharacterSave> Designs = null!;
        public FileSystem.FileSystem             FileSystem { get; } = new();

        public DesignManager()
        {
            var saveFolder = new DirectoryInfo(Dalamud.PluginInterface.GetPluginConfigDirectory());
            if (!saveFolder.Exists)
                Directory.CreateDirectory(saveFolder.FullName);

            _saveFile = new FileInfo(Path.Combine(saveFolder.FullName, FileName));

            LoadFromFile();
        }

        private void BuildStructure()
        {
            FileSystem.Clear();
            var anyChanges = false;
            foreach (var (path, save) in Designs.ToArray())
            {
                try
                {
                    var (folder, name) = FileSystem.CreateAllFolders(path);
                    var design = new Design(folder, name) { Data = save };
                    folder.FindOrAddChild(design);
                    var fixedPath = design.FullName();
                    if (string.Equals(fixedPath, path, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    Designs.Remove(path);
                    Designs[fixedPath] = save;
                    anyChanges         = true;
                    PluginLog.Debug($"Problem loading saved designs, {path} was renamed to {fixedPath}.");
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Problem loading saved designs, {path} was removed because:\n{e}");
                    Designs.Remove(path);
                }
            }

            if (anyChanges)
                SaveToFile();
        }

        private bool UpdateRoot(string oldPath, Design child)
        {
            var newPath = child.FullName();
            if (string.Equals(newPath, oldPath, StringComparison.InvariantCultureIgnoreCase))
                return false;

            Designs.Remove(oldPath);
            Designs[child.FullName()] = child.Data;
            return true;
        }

        private void UpdateChild(string oldRootPath, string newRootPath, Design child)
        {
            var newPath = child.FullName();
            var oldPath = $"{oldRootPath}{newPath.Remove(0, newRootPath.Length)}";
            Designs.Remove(oldPath);
            Designs[newPath] = child.Data;
        }

        public void DeleteAllChildren(IFileSystemBase root, bool deleteEmpty)
        {
            if (root is Folder f)
                foreach (var child in f.AllLeaves(SortMode.Lexicographical))
                    Designs.Remove(child.FullName());
            var fullPath = root.FullName();
            root.Parent.RemoveChild(root, deleteEmpty);
            Designs.Remove(fullPath);

            SaveToFile();
        }

        public void UpdateAllChildren(string oldPath, IFileSystemBase root)
        {
            var changes = false;
            switch (root)
            {
                case Design d:
                    changes |= UpdateRoot(oldPath, d);
                    break;
                case Folder f:
                {
                    var newRootPath = root.FullName();
                    if (!string.Equals(oldPath, newRootPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        changes = true;
                        foreach (var descendant in f.AllLeaves(SortMode.Lexicographical).Where(l => l is Design).Cast<Design>())
                            UpdateChild(oldPath, newRootPath, descendant);
                    }

                    break;
                }
            }

            if (changes)
                SaveToFile();
        }

        public void SaveToFile()
        {
            try
            {
                var data = JsonConvert.SerializeObject(Designs, Formatting.Indented);
                File.WriteAllText(_saveFile.FullName, data);
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not write to save file {_saveFile.FullName}:\n{e}");
            }
        }

        public void LoadFromFile()
        {
            _saveFile.Refresh();
            SortedList<string, CharacterSave>? designs = null;
            if (_saveFile.Exists)
                try
                {
                    var data = File.ReadAllText(_saveFile.FullName);
                    designs = JsonConvert.DeserializeObject<SortedList<string, CharacterSave>>(data);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not load save file {_saveFile.FullName}:\n{e}");
                }

            if (designs == null)
            {
                Designs = new SortedList<string, CharacterSave>();
                SaveToFile();
            }
            else
            {
                Designs = designs;
            }

            BuildStructure();
        }
    }
}
