using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Utility;
using Glamourer.Customization;
using Glamourer.Events;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignManager
{
    private readonly CustomizationService _customizations;
    private readonly ItemManager          _items;
    private readonly HumanModelList       _humans;
    private readonly SaveService          _saveService;
    private readonly DesignChanged        _event;
    private readonly List<Design>         _designs = new();

    public IReadOnlyList<Design> Designs
        => _designs;

    public DesignManager(SaveService saveService, ItemManager items, CustomizationService customizations,
        DesignChanged @event, HumanModelList humans)
    {
        _saveService    = saveService;
        _items          = items;
        _customizations = customizations;
        _event          = @event;
        _humans         = humans;
        CreateDesignFolder(saveService);
        LoadDesigns();
        MigrateOldDesigns();
    }

    /// <summary>
    /// Clear currently loaded designs and load all designs anew from file.
    /// Invalid data is fixed, but changes are not saved until manual changes.
    /// </summary>
    public void LoadDesigns()
    {
        _designs.Clear();
        List<(Design, string)> invalidNames = new();
        var                    skipped      = 0;
        foreach (var file in _saveService.FileNames.Designs())
        {
            try
            {
                var text   = File.ReadAllText(file.FullName);
                var data   = JObject.Parse(text);
                var design = Design.LoadDesign(_customizations, _items, data);
                if (design.Identifier.ToString() != Path.GetFileNameWithoutExtension(file.Name))
                    invalidNames.Add((design, file.FullName));
                if (_designs.Any(f => f.Identifier == design.Identifier))
                    throw new Exception($"Identifier {design.Identifier} was not unique.");

                design.Index = _designs.Count;
                _designs.Add(design);
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not load design, skipped:\n{ex}");
                ++skipped;
            }
        }

        var failed = MoveInvalidNames(invalidNames);
        if (invalidNames.Count > 0)
            Glamourer.Log.Information(
                $"Moved {invalidNames.Count - failed} designs to correct names.{(failed > 0 ? $" Failed to move {failed} designs to correct names." : string.Empty)}");

        Glamourer.Log.Information(
            $"Loaded {_designs.Count} designs.{(skipped > 0 ? $" Skipped loading {skipped} designs due to errors." : string.Empty)}");
        _event.Invoke(DesignChanged.Type.ReloadedAll, null!);
    }

    /// <summary> Create a new temporary design without adding it to the manager. </summary>
    public DesignBase CreateTemporary()
        => new(_items);

    /// <summary> Create a new design of a given name. </summary>
    public Design CreateEmpty(string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(_items)
        {
            CreationDate = DateTimeOffset.UtcNow,
            LastEdit     = DateTimeOffset.UtcNow,
            Identifier   = CreateNewGuid(),
            Name         = actualName,
            Index        = _designs.Count,
        };
        _designs.Add(design);
        Glamourer.Log.Debug($"Added new design {design.Identifier}.");
        _saveService.ImmediateSave(design);
        _event.Invoke(DesignChanged.Type.Created, design, path);
        return design;
    }

    /// <summary> Create a new design cloning a given temporary design. </summary>
    public Design CreateClone(DesignBase clone, string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(clone)
        {
            CreationDate = DateTimeOffset.UtcNow,
            LastEdit     = DateTimeOffset.UtcNow,
            Identifier   = CreateNewGuid(),
            Name         = actualName,
            Index        = _designs.Count,
        };

        _designs.Add(design);
        Glamourer.Log.Debug($"Added new design {design.Identifier} by cloning Temporary Design.");
        _saveService.ImmediateSave(design);
        _event.Invoke(DesignChanged.Type.Created, design, path);
        return design;
    }

    /// <summary> Create a new design cloning a given design. </summary>
    public Design CreateClone(Design clone, string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(clone)
        {
            CreationDate = DateTimeOffset.UtcNow,
            LastEdit     = DateTimeOffset.UtcNow,
            Identifier   = CreateNewGuid(),
            Name         = actualName,
            Index        = _designs.Count,
        };
        _designs.Add(design);
        Glamourer.Log.Debug(
            $"Added new design {design.Identifier} by cloning {clone.Identifier.ToString()}.");
        _saveService.ImmediateSave(design);
        _event.Invoke(DesignChanged.Type.Created, design, path);
        return design;
    }

    /// <summary> Delete a design. </summary>
    public void Delete(Design design)
    {
        foreach (var d in _designs.Skip(design.Index + 1))
            --d.Index;
        _designs.RemoveAt(design.Index);
        _saveService.ImmediateDelete(design);
        _event.Invoke(DesignChanged.Type.Deleted, design);
    }

    /// <summary> Rename a design. </summary>
    public void Rename(Design design, string newName)
    {
        var oldName = design.Name.Text;
        if (oldName == newName)
            return;

        design.Name     = newName;
        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Renamed design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.Renamed, design, oldName);
    }

    /// <summary> Change the description of a design. </summary>
    public void ChangeDescription(Design design, string description)
    {
        var oldDescription = design.Description;
        if (oldDescription == description)
            return;

        design.Description = description;
        design.LastEdit    = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Changed description of design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.ChangedDescription, design, oldDescription);
    }

    /// <summary> Add a new tag to a design. The tags remain sorted. </summary>
    public void AddTag(Design design, string tag)
    {
        if (design.Tags.Contains(tag))
            return;

        design.Tags     = design.Tags.Append(tag).OrderBy(t => t).ToArray();
        design.LastEdit = DateTimeOffset.UtcNow;
        var idx = design.Tags.IndexOf(tag);
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Added tag {tag} at {idx} to design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.AddedTag, design, (tag, idx));
    }

    /// <summary> Remove a tag from a design if it exists. </summary>
    public void RemoveTag(Design design, string tag)
        => RemoveTag(design, design.Tags.IndexOf(tag));

    /// <summary> Remove a tag from a design by its index. </summary>
    public void RemoveTag(Design design, int tagIdx)
    {
        if (tagIdx < 0 || tagIdx >= design.Tags.Length)
            return;

        var oldTag = design.Tags[tagIdx];
        design.Tags     = design.Tags.Take(tagIdx).Concat(design.Tags.Skip(tagIdx + 1)).ToArray();
        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Removed tag {oldTag} at {tagIdx} from design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.RemovedTag, design, (oldTag, tagIdx));
    }

    /// <summary> Rename a tag from a design by its index. The tags stay sorted.</summary>
    public void RenameTag(Design design, int tagIdx, string newTag)
    {
        var oldTag = design.Tags[tagIdx];
        if (oldTag == newTag)
            return;

        design.Tags[tagIdx] = newTag;
        Array.Sort(design.Tags);
        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Renamed tag {oldTag} at {tagIdx} to {newTag} in design {design.Identifier} and reordered tags.");
        _event.Invoke(DesignChanged.Type.ChangedTag, design, (oldTag, newTag, tagIdx));
    }

    /// <summary> Add an associated mod to a design. </summary>
    public void AddMod(Design design, Mod mod, ModSettings settings)
    {
        if (!design.AssociatedMods.TryAdd(mod, settings))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Added associated mod {mod.DirectoryName} to design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.AddedMod, design, (mod, settings));
    }

    /// <summary> Remove an associated mod from a design. </summary>
    public void RemoveMod(Design design, Mod mod)
    {
        if (!design.AssociatedMods.Remove(mod, out var settings))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Removed associated mod {mod.DirectoryName} from design {design.Identifier}.");
        _event.Invoke(DesignChanged.Type.RemovedMod, design, (mod, settings));
    }

    /// <summary> Set the write protection status of a design. </summary>
    public void SetWriteProtection(Design design, bool value)
    {
        if (!design.SetWriteProtected(value))
            return;

        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set design {design.Identifier} to {(value ? "no longer be " : string.Empty)} write-protected.");
        _event.Invoke(DesignChanged.Type.WriteProtection, design, value);
    }

    /// <summary> Change a customization value. </summary>
    public void ChangeCustomize(Design design, CustomizeIndex idx, CustomizeValue value)
    {
        var oldValue = design.DesignData.Customize[idx];
        switch (idx)
        {
            case CustomizeIndex.Race:
            case CustomizeIndex.BodyType:
                Glamourer.Log.Error("Somehow race or body type was changed in a design. This should not happen.");
                return;
            case CustomizeIndex.Clan:
                if (_customizations.ChangeClan(ref design.DesignData.Customize, (SubRace)value.Value) == 0)
                    return;

                design.RemoveInvalidCustomize(_customizations);
                break;
            case CustomizeIndex.Gender:
                if (_customizations.ChangeGender(ref design.DesignData.Customize, (Gender)(value.Value + 1)) == 0)
                    return;

                design.RemoveInvalidCustomize(_customizations);
                break;
            default:
                if (!_customizations.IsCustomizationValid(design.DesignData.Customize.Clan, design.DesignData.Customize.Gender,
                        design.DesignData.Customize.Face, idx, value)
                 || !design.DesignData.Customize.Set(idx, value))
                    return;

                break;
        }

        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug($"Changed customize {idx.ToDefaultName()} in design {design.Identifier} from {oldValue.Value} to {value.Value}.");
        _saveService.QueueSave(design);
        _event.Invoke(DesignChanged.Type.Customize, design, (oldValue, value, idx));
    }

    /// <summary> Change whether to apply a specific customize value. </summary>
    public void ChangeApplyCustomize(Design design, CustomizeIndex idx, bool value)
    {
        if (!design.SetApplyCustomize(idx, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of customization {idx.ToDefaultName()} to {value}.");
        _event.Invoke(DesignChanged.Type.ApplyCustomize, design, idx);
    }

    /// <summary> Change a non-weapon equipment piece. </summary>
    public void ChangeEquip(Design design, EquipSlot slot, EquipItem item)
    {
        if (!_items.IsItemValid(slot, item.ItemId, out item))
            return;

        var old = design.DesignData.Item(slot);
        if (!design.DesignData.SetItem(slot, item))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        Glamourer.Log.Debug(
            $"Set {slot.ToName()} equipment piece in design {design.Identifier} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}).");
        _saveService.QueueSave(design);
        _event.Invoke(DesignChanged.Type.Equip, design, (old, item, slot));
    }

    /// <summary> Change a weapon. </summary>
    public void ChangeWeapon(Design design, EquipSlot slot, EquipItem item)
    {
        var currentMain = design.DesignData.Item(EquipSlot.MainHand);
        var currentOff  = design.DesignData.Item(EquipSlot.OffHand);
        switch (slot)
        {
            case EquipSlot.MainHand:
                var newOff = currentOff;
                if (!_items.IsItemValid(EquipSlot.MainHand, item.ItemId, out item))
                    return;

                if (item.Type != currentMain.Type)
                {
                    var defaultOffhand = _items.GetDefaultOffhand(item);
                    if (!_items.IsOffhandValid(item, defaultOffhand.ItemId, out newOff))
                        return;
                }

                if (!(design.DesignData.SetItem(EquipSlot.MainHand, item) | design.DesignData.SetItem(EquipSlot.OffHand, newOff)))
                    return;

                design.LastEdit = DateTimeOffset.UtcNow;
                _saveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.MainHand.ToName()} weapon in design {design.Identifier} from {currentMain.Name} ({currentMain.ItemId}) to {item.Name} ({item.ItemId}).");
                _event.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, item, newOff));

                return;
            case EquipSlot.OffHand:
                if (!_items.IsOffhandValid(currentOff.Type, item.ItemId, out item))
                    return;

                if (!design.DesignData.SetItem(EquipSlot.OffHand, item))
                    return;

                design.LastEdit = DateTimeOffset.UtcNow;
                _saveService.QueueSave(design);
                Glamourer.Log.Debug(
                    $"Set {EquipSlot.OffHand.ToName()} weapon in design {design.Identifier} from {currentOff.Name} ({currentOff.ItemId}) to {item.Name} ({item.ItemId}).");
                _event.Invoke(DesignChanged.Type.Weapon, design, (currentMain, currentOff, currentMain, item));
                return;
            default: return;
        }
    }

    /// <summary> Change whether to apply a specific equipment piece. </summary>
    public void ChangeApplyEquip(Design design, EquipSlot slot, bool value)
    {
        if (!design.SetApplyEquip(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of {slot} equipment piece to {value}.");
        _event.Invoke(DesignChanged.Type.ApplyEquip, design, slot);
    }

    /// <summary> Change the stain for any equipment piece. </summary>
    public void ChangeStain(Design design, EquipSlot slot, StainId stain)
    {
        if (_items.ValidateStain(stain, out _, false).Length > 0)
            return;

        var oldStain = design.DesignData.Stain(slot);
        if (!design.DesignData.SetStain(slot, stain))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set stain of {slot} equipment piece to {stain.Id}.");
        _event.Invoke(DesignChanged.Type.Stain, design, (oldStain, stain, slot));
    }

    /// <summary> Change whether to apply a specific stain. </summary>
    public void ChangeApplyStain(Design design, EquipSlot slot, bool value)
    {
        if (!design.SetApplyStain(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of stain of {slot} equipment piece to {value}.");
        _event.Invoke(DesignChanged.Type.ApplyStain, design, slot);
    }

    /// <summary> Change the bool value of one of the meta flags. </summary>
    public void ChangeMeta(Design design, ActorState.MetaIndex metaIndex, bool value)
    {
        var change = metaIndex switch
        {
            ActorState.MetaIndex.Wetness     => design.DesignData.SetIsWet(value),
            ActorState.MetaIndex.HatState    => design.DesignData.SetHatVisible(value),
            ActorState.MetaIndex.VisorState  => design.DesignData.SetVisor(value),
            ActorState.MetaIndex.WeaponState => design.DesignData.SetWeaponVisible(value),
            _                                => throw new ArgumentOutOfRangeException(nameof(metaIndex), metaIndex, null),
        };
        if (!change)
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set value of {metaIndex} to {value}.");
        _event.Invoke(DesignChanged.Type.Other, design, (metaIndex, false, value));
    }

    /// <summary> Change the application value of one of the meta flags. </summary>
    public void ChangeApplyMeta(Design design, ActorState.MetaIndex metaIndex, bool value)
    {
        var change = metaIndex switch
        {
            ActorState.MetaIndex.Wetness     => design.SetApplyWetness(value),
            ActorState.MetaIndex.HatState    => design.SetApplyHatVisible(value),
            ActorState.MetaIndex.VisorState  => design.SetApplyVisorToggle(value),
            ActorState.MetaIndex.WeaponState => design.SetApplyWeaponVisible(value),
            _                                => throw new ArgumentOutOfRangeException(nameof(metaIndex), metaIndex, null),
        };
        if (!change)
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of {metaIndex} to {value}.");
        _event.Invoke(DesignChanged.Type.Other, design, (metaIndex, true, value));
    }

    /// <summary> Apply an entire design based on its appliance rules piece by piece. </summary>
    public void ApplyDesign(Design design, DesignBase other)
    {
        if (other.DoApplyWetness())
            design.DesignData.SetIsWet(other.DesignData.IsWet());
        if (other.DoApplyHatVisible())
            design.DesignData.SetHatVisible(other.DesignData.IsHatVisible());
        if (other.DoApplyVisorToggle())
            design.DesignData.SetVisor(other.DesignData.IsVisorToggled());
        if (other.DoApplyWeaponVisible())
            design.DesignData.SetWeaponVisible(other.DesignData.IsWeaponVisible());

        if (design.DesignData.IsHuman)
        {
            foreach (var index in Enum.GetValues<CustomizeIndex>())
            {
                if (other.DoApplyCustomize(index))
                    ChangeCustomize(design, index, other.DesignData.Customize[index]);
            }

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                if (other.DoApplyEquip(slot))
                    ChangeEquip(design, slot, other.DesignData.Item(slot));

                if (other.DoApplyStain(slot))
                    ChangeStain(design, slot, other.DesignData.Stain(slot));
            }
        }

        if (other.DoApplyEquip(EquipSlot.MainHand))
            ChangeWeapon(design, EquipSlot.MainHand, other.DesignData.Item(EquipSlot.MainHand));

        if (other.DoApplyEquip(EquipSlot.OffHand))
            ChangeWeapon(design, EquipSlot.OffHand, other.DesignData.Item(EquipSlot.OffHand));

        if (other.DoApplyStain(EquipSlot.MainHand))
            ChangeStain(design, EquipSlot.MainHand, other.DesignData.Stain(EquipSlot.MainHand));

        if (other.DoApplyStain(EquipSlot.OffHand))
            ChangeStain(design, EquipSlot.OffHand, other.DesignData.Stain(EquipSlot.OffHand));
    }

    private void MigrateOldDesigns()
    {
        if (!File.Exists(_saveService.FileNames.MigrationDesignFile))
            return;

        var errors     = 0;
        var skips      = 0;
        var successes  = 0;
        var oldDesigns = _designs.ToList();
        try
        {
            var text                    = File.ReadAllText(_saveService.FileNames.MigrationDesignFile);
            var dict                    = JsonConvert.DeserializeObject<Dictionary<string, string>>(text) ?? new Dictionary<string, string>();
            var migratedFileSystemPaths = new Dictionary<string, string>(dict.Count);
            foreach (var (name, base64) in dict)
            {
                try
                {
                    var actualName = Path.GetFileName(name);
                    var design = new Design(_items)
                    {
                        CreationDate = File.GetCreationTimeUtc(_saveService.FileNames.MigrationDesignFile),
                        LastEdit     = File.GetLastWriteTimeUtc(_saveService.FileNames.MigrationDesignFile),
                        Identifier   = CreateNewGuid(),
                        Name         = actualName,
                    };
                    design.MigrateBase64(_items, _humans, base64);
                    if (!oldDesigns.Any(d => d.Name == design.Name && d.CreationDate == design.CreationDate))
                    {
                        Add(design, $"Migrated old design to {design.Identifier}.");
                        migratedFileSystemPaths.Add(design.Identifier.ToString(), name);
                        ++successes;
                    }
                    else
                    {
                        Glamourer.Log.Debug(
                            "Skipped migrating old design because a design of the same name and creation date already existed.");
                        ++skips;
                    }
                }
                catch (Exception ex)
                {
                    Glamourer.Log.Error($"Could not migrate design {name}:\n{ex}");
                    ++errors;
                }
            }

            DesignFileSystem.MigrateOldPaths(_saveService, migratedFileSystemPaths);
            Glamourer.Log.Information(
                $"Successfully migrated {successes} old designs. Skipped {skips} already migrated designs. Failed to migrate {errors} designs.");
        }
        catch (Exception e)
        {
            Glamourer.Log.Error($"Could not migrate old design file {_saveService.FileNames.MigrationDesignFile}:\n{e}");
        }

        try
        {
            File.Move(_saveService.FileNames.MigrationDesignFile,
                Path.ChangeExtension(_saveService.FileNames.MigrationDesignFile, ".json.bak"));
            Glamourer.Log.Information($"Moved migrated design file {_saveService.FileNames.MigrationDesignFile} to backup file.");
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not move migrated design file {_saveService.FileNames.MigrationDesignFile} to backup file:\n{ex}");
        }
    }

    /// <summary> Try to ensure existence of the design folder. </summary>
    private static void CreateDesignFolder(SaveService service)
    {
        var ret = service.FileNames.DesignDirectory;
        if (Directory.Exists(ret))
            return;

        try
        {
            Directory.CreateDirectory(ret);
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not create design folder directory at {ret}:\n{ex}");
        }
    }

    /// <summary> Move all files that were discovered to have names not corresponding to their identifier to correct names, if possible. </summary>
    /// <returns>The number of files that could not be moved.</returns>
    private int MoveInvalidNames(IEnumerable<(Design, string)> invalidNames)
    {
        var failed = 0;
        foreach (var (design, name) in invalidNames)
        {
            try
            {
                var correctName = _saveService.FileNames.DesignFile(design);
                File.Move(name, correctName, false);
                Glamourer.Log.Information($"Moved invalid design file from {Path.GetFileName(name)} to {Path.GetFileName(correctName)}.");
            }
            catch (Exception ex)
            {
                ++failed;
                Glamourer.Log.Error($"Failed to move invalid design file from {Path.GetFileName(name)}:\n{ex}");
            }
        }

        return failed;
    }

    /// <summary> Create new GUIDs until we have one that is not in use. </summary>
    private Guid CreateNewGuid()
    {
        while (true)
        {
            var guid = Guid.NewGuid();
            if (_designs.All(d => d.Identifier != guid))
                return guid;
        }
    }

    /// <summary>
    /// Try to add an external design to the list.
    /// Returns false if the design is already contained or if the identifier is already in use.
    /// The design is treated as newly created and invokes an event.
    /// </summary>
    private bool Add(Design design, string? message)
    {
        if (_designs.Any(d => d == design || d.Identifier == design.Identifier))
            return false;

        design.Index = _designs.Count;
        _designs.Add(design);
        if (!message.IsNullOrEmpty())
            Glamourer.Log.Debug(message);
        _saveService.ImmediateSave(design);
        _event.Invoke(DesignChanged.Type.Created, design);
        return true;
    }

    /// <summary> Split a given string into its folder path and its name, if <paramref name="handlePath"/> is true. </summary>
    private static (string Name, string? Path) ParseName(string name, bool handlePath)
    {
        var     actualName = name;
        string? path       = null;
        if (handlePath)
        {
            var slashPos = name.LastIndexOf('/');
            if (slashPos >= 0)
            {
                path       = name[..slashPos];
                actualName = slashPos >= name.Length - 1 ? "<Unnamed>" : name[(slashPos + 1)..];
            }
        }

        return (actualName, path);
    }
}
