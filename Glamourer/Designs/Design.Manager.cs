using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Utility;
using Glamourer.Customization;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public partial class Design
{
    public partial class Manager
    {
        public const    string DesignFolderName = "designs";
        public readonly string DesignFolder;

        private readonly ItemManager  _items;
        private readonly SaveService  _saveService;
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
            Customize,
            Equip,
            Weapon,
            Stain,
            ApplyCustomize,
            ApplyEquip,
            Other,
        }

        public delegate void DesignChangeDelegate(DesignChangeType type, Design design, object? changeData = null);

        public event DesignChangeDelegate DesignChange;

        public IReadOnlyList<Design> Designs
            => _designs;

        public Manager(DalamudPluginInterface pi, SaveService saveService, ItemManager items)
        {
            _saveService =  saveService;
            _items       =  items;
            DesignFolder =  SetDesignFolder(pi);
            DesignChange += OnChange;
            LoadDesigns();
            MigrateOldDesigns(pi, Path.Combine(new DirectoryInfo(DesignFolder).Parent!.FullName, "Designs.json"));
        }

        private void OnChange(DesignChangeType type, Design design, object? _)
        {
            switch (type)
            {
                case DesignChangeType.Created:
                    SaveDesignInternal(design);
                    return;
                case DesignChangeType.Renamed:
                case DesignChangeType.ChangedDescription:
                case DesignChangeType.AddedTag:
                case DesignChangeType.RemovedTag:
                case DesignChangeType.ChangedTag:
                case DesignChangeType.Customize:
                case DesignChangeType.Equip:
                case DesignChangeType.Weapon:
                case DesignChangeType.Stain:
                case DesignChangeType.ApplyCustomize:
                case DesignChangeType.ApplyEquip:
                case DesignChangeType.Other:
                    SaveDesign(design);
                    return;
            }
        }

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
            => Path.Combine(DesignFolder, $"{design.Identifier}.json");

        public void SaveDesign(Design design)
            => _saveService.QueueSave(design);

        private void SaveDesignInternal(Design design)
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
            List<(Design, string)> invalidNames = new();
            var                    skipped      = 0;
            foreach (var file in new DirectoryInfo(DesignFolder).EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var text   = File.ReadAllText(file.FullName);
                    var data   = JObject.Parse(text);
                    var design = LoadDesign(_items, data, out var changes);
                    if (design.Identifier.ToString() != Path.GetFileNameWithoutExtension(file.Name))
                        invalidNames.Add((design, file.FullName));
                    if (_designs.Any(f => f.Identifier == design.Identifier))
                        throw new Exception($"Identifier {design.Identifier} was not unique.");

                    // TODO something when changed?
                    design.Index = _designs.Count;
                    _designs.Add(design);
                }
                catch (Exception ex)
                {
                    Glamourer.Log.Error($"Could not load design, skipped:\n{ex}");
                    ++skipped;
                }
            }

            var failed = 0;
            foreach (var (design, name) in invalidNames)
            {
                try
                {
                    var correctName = CreateFileName(design);
                    File.Move(name, correctName, false);
                    Glamourer.Log.Information($"Moved invalid design file from {Path.GetFileName(name)} to {Path.GetFileName(correctName)}.");
                }
                catch (Exception ex)
                {
                    ++failed;
                    Glamourer.Log.Error($"Failed to move invalid design file from {Path.GetFileName(name)}:\n{ex}");
                }
            }

            if (invalidNames.Count > 0)
                Glamourer.Log.Information(
                    $"Moved {invalidNames.Count - failed} designs to correct names.{(failed > 0 ? $" Failed to move {failed} designs to correct names." : string.Empty)}");

            Glamourer.Log.Information(
                $"Loaded {_designs.Count} designs.{(skipped > 0 ? $" Skipped loading {skipped} designs due to errors." : string.Empty)}");
            DesignChange.Invoke(DesignChangeType.ReloadedAll, null!);
        }

        public Design Create(string name)
        {
            var design = new Design(_items)
            {
                CreationDate = DateTimeOffset.UtcNow,
                Identifier   = CreateNewGuid(),
                Index        = _designs.Count,
                Name         = name,
            };
            _designs.Add(design);
            Glamourer.Log.Debug($"Added new design {design.Identifier}.");
            DesignChange.Invoke(DesignChangeType.Created, design);
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
                DesignChange.Invoke(DesignChangeType.Deleted, design);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not delete design file for {design.Identifier}:\n{ex}");
            }
        }

        public void Rename(Design design, string newName)
        {
            var oldName     = design.Name.Text;
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
            Glamourer.Log.Debug($"Renamed design {design.Identifier}.");
            DesignChange.Invoke(DesignChangeType.Renamed, design, oldName);
        }

        public void ChangeDescription(Design design, string description)
        {
            design.Description = description;
            Glamourer.Log.Debug($"Changed description of design {design.Identifier}.");
            DesignChange.Invoke(DesignChangeType.ChangedDescription, design);
        }

        public void AddTag(Design design, string tag)
        {
            if (design.Tags.Contains(tag))
                return;

            design.Tags = design.Tags.Append(tag).OrderBy(t => t).ToArray();
            var idx = design.Tags.IndexOf(tag);
            Glamourer.Log.Debug($"Added tag {tag} at {idx} to design {design.Identifier}.");
            DesignChange.Invoke(DesignChangeType.AddedTag, design);
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
            Glamourer.Log.Debug($"Removed tag {oldTag} at {tagIdx} from design {design.Identifier}.");
            DesignChange.Invoke(DesignChangeType.RemovedTag, design);
        }


        public void RenameTag(Design design, int tagIdx, string newTag)
        {
            var oldTag = design.Tags[tagIdx];
            if (oldTag == newTag)
                return;

            design.Tags[tagIdx] = newTag;
            Array.Sort(design.Tags);
            SaveDesign(design);
            Glamourer.Log.Debug($"Renamed tag {oldTag} at {tagIdx} to {newTag} in design {design.Identifier} and reordered tags.");
            DesignChange.Invoke(DesignChangeType.ChangedTag, design);
        }

        public void ChangeCustomize(Design design, CustomizeIndex idx, CustomizeValue value)
        {
            var old = design.GetCustomize(idx);
            if (design.SetCustomize(idx, value))
            {
                Glamourer.Log.Debug($"Changed customize {idx} in design {design.Identifier} from {old.Value} to {value.Value}");
                DesignChange.Invoke(DesignChangeType.Customize, design, idx);
            }
        }

        public void ChangeApplyCustomize(Design design, CustomizeIndex idx, bool value)
        {
            if (design.SetApplyCustomize(idx, value))
            {
                Glamourer.Log.Debug($"Set applying of customization {idx} to {value}.");
                DesignChange.Invoke(DesignChangeType.ApplyCustomize, design, idx);
            }
        }

        public void ChangeEquip(Design design, EquipSlot slot, uint itemId, Lumina.Excel.GeneratedSheets.Item? item = null)
        {
            var old = design.Armor(slot);
            if (design.SetArmor(_items, slot, itemId, item))
            {
                var n = design.Armor(slot);
                Glamourer.Log.Debug(
                    $"Set {slot} equipment piece in design {design.Identifier} from {old.Name} ({old.ItemId}) to {n.Name} ({n.ItemId}).");
                DesignChange.Invoke(DesignChangeType.Equip, design, slot);
            }
        }

        public void ChangeWeapon(Design design, uint itemId, EquipSlot offhand, Lumina.Excel.GeneratedSheets.Item? item = null)
        {
            var (old, change, n) = offhand == EquipSlot.OffHand
                ? (design.WeaponOff, design.SetOffhand(_items, itemId, item), design.WeaponOff)
                : (design.WeaponMain, design.SetMainhand(_items, itemId, item), design.WeaponMain);
            if (change)
            {
                Glamourer.Log.Debug(
                    $"Set {offhand} weapon in design {design.Identifier} from {old.Name} ({old.ItemId}) to {n.Name} ({n.ItemId}).");
                DesignChange.Invoke(DesignChangeType.Weapon, design, offhand);
            }
        }

        public void ChangeApplyEquip(Design design, EquipSlot slot, bool value)
        {
            if (design.SetApplyEquip(slot, value))
            {
                Glamourer.Log.Debug($"Set applying of {slot} equipment piece to {value}.");
                DesignChange.Invoke(DesignChangeType.ApplyEquip, design, slot);
            }
        }

        public void ChangeStain(Design design, EquipSlot slot, StainId stain)
        {
            if (design.SetStain(slot, stain))
            {
                Glamourer.Log.Debug($"Set stain of {slot} equipment piece to {stain.Value}.");
                DesignChange.Invoke(DesignChangeType.Stain, design, slot);
            }
        }

        public void ChangeApplyStain(Design design, EquipSlot slot, bool value)
        {
            if (design.SetApplyStain(slot, value))
            {
                Glamourer.Log.Debug($"Set applying of stain of {slot} equipment piece to {value}.");
                DesignChange.Invoke(DesignChangeType.Stain, design, slot);
            }
        }

        private Guid CreateNewGuid()
        {
            while (true)
            {
                var guid = Guid.NewGuid();
                if (_designs.All(d => d.Identifier != guid))
                    return guid;
            }
        }

        private bool Add(Design design, string? message)
        {
            if (_designs.Any(d => d == design || d.Identifier == design.Identifier))
                return false;

            design.Index = _designs.Count;
            _designs.Add(design);
            if (!message.IsNullOrEmpty())
                Glamourer.Log.Debug(message);
            DesignChange.Invoke(DesignChangeType.Created, design);
            return true;
        }

        private void MigrateOldDesigns(DalamudPluginInterface pi, string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var errors    = 0;
            var successes = 0;
            try
            {
                var text = File.ReadAllText(filePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(text) ?? new Dictionary<string, string>();
                var migratedFileSystemPaths = new Dictionary<string, string>(dict.Count);
                foreach (var (name, base64) in dict)
                {
                    try
                    {
                        var actualName = Path.GetFileName(name);
                        var design = new Design(_items)
                        {
                            CreationDate = DateTimeOffset.UtcNow,
                            Identifier   = CreateNewGuid(),
                            Name         = actualName,
                        };
                        design.MigrateBase64(_items, base64);
                        Add(design, $"Migrated old design to {design.Identifier}.");
                        migratedFileSystemPaths.Add(design.Identifier.ToString(), name);
                        ++successes;
                    }
                    catch (Exception ex)
                    {
                        Glamourer.Log.Error($"Could not migrate design {name}:\n{ex}");
                        ++errors;
                    }
                }

                DesignFileSystem.MigrateOldPaths(pi, migratedFileSystemPaths);
                Glamourer.Log.Information($"Successfully migrated {successes} old designs. Failed to migrate {errors} designs.");
            }
            catch (Exception e)
            {
                Glamourer.Log.Error($"Could not migrate old design file {filePath}:\n{e}");
            }

            try
            {
                File.Move(filePath, Path.ChangeExtension(filePath, ".json.bak"));
                Glamourer.Log.Information($"Moved migrated design file {filePath} to backup file.");
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not move migrated design file {filePath} to backup file:\n{ex}");
            }
        }
    }
}
