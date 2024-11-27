using Dalamud.Utility;
using Glamourer.Designs.History;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public sealed class DesignManager : DesignEditor
{
    public readonly  DesignStorage  Designs;
    private readonly HumanModelList _humans;

    public DesignManager(SaveService saveService, ItemManager items, CustomizeService customizations,
        DesignChanged @event, HumanModelList humans, DesignStorage storage, DesignLinkLoader designLinkLoader, Configuration config)
        : base(saveService, @event, customizations, items, config)
    {
        Designs = storage;
        _humans = humans;

        LoadDesigns(designLinkLoader);
        CreateDesignFolder(saveService);
        MigrateOldDesigns();
        designLinkLoader.SetAllObjects();
    }

    #region Design Management

    /// <summary>
    /// Clear currently loaded designs and load all designs anew from file.
    /// Invalid data is fixed, but changes are not saved until manual changes.
    /// </summary>
    private void LoadDesigns(DesignLinkLoader linkLoader)
    {
        _humans.Awaiter.Wait();
        Customizations.Awaiter.Wait();
        Items.ItemData.Awaiter.Wait();

        var stopwatch = Stopwatch.StartNew();
        Designs.Clear();
        var                                 skipped = 0;
        ThreadLocal<List<(Design, string)>> designs = new(() => [], true);
        Parallel.ForEach(SaveService.FileNames.Designs(), (f, _) =>
        {
            try
            {
                var text   = File.ReadAllText(f.FullName);
                var data   = JObject.Parse(text);
                var design = Design.LoadDesign(SaveService, Customizations, Items, linkLoader, data);
                designs.Value!.Add((design, f.FullName));
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Could not load design, skipped:\n{ex}");
                Interlocked.Increment(ref skipped);
            }
        });

        List<(Design, string)> invalidNames = [];
        foreach (var (design, path) in designs.Values.SelectMany(v => v))
        {
            if (design.Identifier.ToString() != Path.GetFileNameWithoutExtension(path))
                invalidNames.Add((design, path));
            if (Designs.Contains(design.Identifier))
            {
                Glamourer.Log.Error($"Could not load design, skipped: Identifier {design.Identifier} was not unique.");
                ++skipped;
                continue;
            }

            design.Index = Designs.Count;
            Designs.Add(design);
        }

        var failed = MoveInvalidNames(invalidNames);
        if (invalidNames.Count > 0)
            Glamourer.Log.Information(
                $"Moved {invalidNames.Count - failed} designs to correct names.{(failed > 0 ? $" Failed to move {failed} designs to correct names." : string.Empty)}");

        Glamourer.Log.Information(
            $"Loaded {Designs.Count} designs in {stopwatch.ElapsedMilliseconds} ms.{(skipped > 0 ? $" Skipped loading {skipped} designs due to errors." : string.Empty)}");
        DesignChanged.Invoke(DesignChanged.Type.ReloadedAll, null!, null);
    }

    /// <summary> Create a new temporary design without adding it to the manager. </summary>
    public DesignBase CreateTemporary()
        => new(Customizations, Items);

    /// <summary> Create a new design of a given name. </summary>
    public Design CreateEmpty(string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(Customizations, Items)
        {
            CreationDate      = DateTimeOffset.UtcNow,
            LastEdit          = DateTimeOffset.UtcNow,
            Identifier        = CreateNewGuid(),
            Name              = actualName,
            Index             = Designs.Count,
            ForcedRedraw      = Config.DefaultDesignSettings.AlwaysForceRedrawing,
            ResetAdvancedDyes = Config.DefaultDesignSettings.ResetAdvancedDyes,
            QuickDesign       = Config.DefaultDesignSettings.ShowQuickDesignBar,
        };
        Designs.Add(design);
        Glamourer.Log.Debug($"Added new design {design.Identifier}.");
        SaveService.ImmediateSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Created, design, new CreationTransaction(actualName, path));
        return design;
    }

    /// <summary> Create a new design cloning a given temporary design. </summary>
    public Design CreateClone(DesignBase clone, string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(clone)
        {
            CreationDate      = DateTimeOffset.UtcNow,
            LastEdit          = DateTimeOffset.UtcNow,
            Identifier        = CreateNewGuid(),
            Name              = actualName,
            Index             = Designs.Count,
            ForcedRedraw      = Config.DefaultDesignSettings.AlwaysForceRedrawing,
            ResetAdvancedDyes = Config.DefaultDesignSettings.ResetAdvancedDyes,
            QuickDesign       = Config.DefaultDesignSettings.ShowQuickDesignBar,
        };

        Designs.Add(design);
        Glamourer.Log.Debug($"Added new design {design.Identifier} by cloning Temporary Design.");
        SaveService.ImmediateSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Created, design, new CreationTransaction(actualName, path));
        return design;
    }

    /// <summary> Create a new design cloning a given design. </summary>
    public Design CreateClone(Design clone, string name, bool handlePath)
    {
        var (actualName, path) = ParseName(name, handlePath);
        var design = new Design(clone)
        {
            CreationDate      = DateTimeOffset.UtcNow,
            LastEdit          = DateTimeOffset.UtcNow,
            Identifier        = CreateNewGuid(),
            Name              = actualName,
            Index             = Designs.Count,
        };
        Designs.Add(design);
        Glamourer.Log.Debug(
            $"Added new design {design.Identifier} by cloning {clone.Identifier.ToString()}.");
        SaveService.ImmediateSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Created, design, new CreationTransaction(actualName, path));
        return design;
    }

    /// <summary> Delete a design. </summary>
    public void Delete(Design design)
    {
        foreach (var d in Designs.Skip(design.Index + 1))
            --d.Index;
        Designs.RemoveAt(design.Index);
        SaveService.ImmediateDelete(design);
        DesignChanged.Invoke(DesignChanged.Type.Deleted, design, null);
    }

    #endregion

    #region Edit Information

    /// <summary> Rename a design. </summary>
    public void Rename(Design design, string newName)
    {
        var oldName = design.Name.Text;
        if (oldName == newName)
            return;

        design.Name     = newName;
        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Renamed design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.Renamed, design, new RenameTransaction(oldName, newName));
    }

    /// <summary> Change the description of a design. </summary>
    public void ChangeDescription(Design design, string description)
    {
        var oldDescription = design.Description;
        if (oldDescription == description)
            return;

        design.Description = description;
        design.LastEdit    = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Changed description of design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.ChangedDescription, design, new DescriptionTransaction(oldDescription, description));
    }

    /// <summary> Change the associated color of a design. </summary>
    public void ChangeColor(Design design, string newColor)
    {
        var oldColor = design.Color;
        if (oldColor == newColor)
            return;

        design.Color    = newColor;
        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Changed color of design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.ChangedColor, design, new DesignColorTransaction(oldColor, newColor));
    }

    /// <summary> Add a new tag to a design. The tags remain sorted. </summary>
    public void AddTag(Design design, string tag)
    {
        if (design.Tags.Contains(tag))
            return;

        design.Tags     = design.Tags.Append(tag).OrderBy(t => t).ToArray();
        design.LastEdit = DateTimeOffset.UtcNow;
        var idx = design.Tags.IndexOf(tag);
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Added tag {tag} at {idx} to design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.AddedTag, design, new TagAddedTransaction(tag, idx));
    }

    /// <summary> Remove a tag from a design by its index. </summary>
    public void RemoveTag(Design design, int tagIdx)
    {
        if (tagIdx < 0 || tagIdx >= design.Tags.Length)
            return;

        var oldTag = design.Tags[tagIdx];
        design.Tags     = design.Tags.Take(tagIdx).Concat(design.Tags.Skip(tagIdx + 1)).ToArray();
        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Removed tag {oldTag} at {tagIdx} from design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.RemovedTag, design, new TagRemovedTransaction(oldTag, tagIdx));
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
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Renamed tag {oldTag} at {tagIdx} to {newTag} in design {design.Identifier} and reordered tags.");
        DesignChanged.Invoke(DesignChanged.Type.ChangedTag, design,
            new TagChangedTransaction(oldTag, newTag, tagIdx, design.Tags.IndexOf(newTag)));
    }

    /// <summary> Add an associated mod to a design. </summary>
    public void AddMod(Design design, Mod mod, ModSettings settings)
    {
        if (!design.AssociatedMods.TryAdd(mod, settings))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Added associated mod {mod.DirectoryName} to design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.AddedMod, design, new ModAddedTransaction(mod, settings));
    }

    /// <summary> Remove an associated mod from a design. </summary>
    public void RemoveMod(Design design, Mod mod)
    {
        if (!design.AssociatedMods.Remove(mod, out var settings))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Removed associated mod {mod.DirectoryName} from design {design.Identifier}.");
        DesignChanged.Invoke(DesignChanged.Type.RemovedMod, design, new ModRemovedTransaction(mod, settings));
    }

    /// <summary> Add or update an associated mod to a design. </summary>
    public void UpdateMod(Design design, Mod mod, ModSettings settings)
    {
        var hasOldSettings = design.AssociatedMods.TryGetValue(mod, out var oldSettings);
        design.AssociatedMods[mod] = settings;
        design.LastEdit            = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        if (hasOldSettings)
        {
            Glamourer.Log.Debug($"Updated associated mod {mod.DirectoryName} from design {design.Identifier}.");
            DesignChanged.Invoke(DesignChanged.Type.UpdatedMod, design, new ModUpdatedTransaction(mod, oldSettings, settings));
        }
        else
        {
            Glamourer.Log.Debug($"Added associated mod {mod.DirectoryName} from design {design.Identifier}.");
            DesignChanged.Invoke(DesignChanged.Type.AddedMod, design, new ModAddedTransaction(mod, settings));
        }
    }

    /// <summary> Set the write protection status of a design. </summary>
    public void SetWriteProtection(Design design, bool value)
    {
        if (!design.SetWriteProtected(value))
            return;

        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set design {design.Identifier} to {(value ? "no longer be " : string.Empty)} write-protected.");
        DesignChanged.Invoke(DesignChanged.Type.WriteProtection, design, null);
    }

    /// <summary> Set the quick design bar display status of a design. </summary>
    public void SetQuickDesign(Design design, bool value)
    {
        if (value == design.QuickDesign)
            return;

        design.QuickDesign = value;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug(
            $"Set design {design.Identifier} to {(!value ? "no longer be " : string.Empty)} displayed in the quick design bar.");
        DesignChanged.Invoke(DesignChanged.Type.QuickDesignBar, design, null);
    }

    #endregion

    #region Edit Application Rules

    public void ChangeForcedRedraw(Design design, bool forcedRedraw)
    {
        if (design.ForcedRedraw == forcedRedraw)
            return;

        design.ForcedRedraw = forcedRedraw;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set {design.Identifier} to {(forcedRedraw ? string.Empty : "not")} force redraws.");
        DesignChanged.Invoke(DesignChanged.Type.ForceRedraw, design, null);
    }

    public void ChangeResetAdvancedDyes(Design design, bool resetAdvancedDyes)
    {
        if (design.ResetAdvancedDyes == resetAdvancedDyes)
            return;

        design.ResetAdvancedDyes = resetAdvancedDyes;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set {design.Identifier} to {(resetAdvancedDyes ? string.Empty : "not")} reset advanced dyes.");
        DesignChanged.Invoke(DesignChanged.Type.ResetAdvancedDyes, design, null);
    }

    /// <summary> Change whether to apply a specific customize value. </summary>
    public void ChangeApplyCustomize(Design design, CustomizeIndex idx, bool value)
    {
        if (!design.SetApplyCustomize(idx, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of customization {idx.ToDefaultName()} to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyCustomize, design, new ApplicationTransaction(idx, !value, value));
    }

    /// <summary> Change whether to apply a specific equipment piece. </summary>
    public void ChangeApplyItem(Design design, EquipSlot slot, bool value)
    {
        if (!design.SetApplyEquip(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of {slot} equipment piece to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyEquip, design, new ApplicationTransaction((slot, false), !value, value));
    }

    /// <summary> Change whether to apply a specific equipment piece. </summary>
    public void ChangeApplyBonusItem(Design design, BonusItemFlag slot, bool value)
    {
        if (!design.SetApplyBonusItem(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of {slot} bonus item to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyBonusItem, design, new ApplicationTransaction(slot, !value, value));
    }

    /// <summary> Change whether to apply a specific stain. </summary>
    public void ChangeApplyStains(Design design, EquipSlot slot, bool value)
    {
        if (!design.SetApplyStain(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of stain of {slot} equipment piece to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyStain, design, new ApplicationTransaction((slot, true), !value, value));
    }

    /// <summary> Change whether to apply a specific crest visibility. </summary>
    public void ChangeApplyCrest(Design design, CrestFlag slot, bool value)
    {
        if (!design.SetApplyCrest(slot, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of crest visibility of {slot} equipment piece to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyCrest, design, new ApplicationTransaction(slot, !value, value));
    }

    /// <summary> Change the application value of one of the meta flags. </summary>
    public void ChangeApplyMeta(Design design, MetaIndex metaIndex, bool value)
    {
        if (!design.SetApplyMeta(metaIndex, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of {metaIndex} to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.Other, design, new ApplicationTransaction(metaIndex, !value, value));
    }

    /// <summary> Change the application value of a customize parameter. </summary>
    public void ChangeApplyParameter(Design design, CustomizeParameterFlag flag, bool value)
    {
        if (!design.SetApplyParameter(flag, value))
            return;

        design.LastEdit = DateTimeOffset.UtcNow;
        SaveService.QueueSave(design);
        Glamourer.Log.Debug($"Set applying of parameter {flag} to {value}.");
        DesignChanged.Invoke(DesignChanged.Type.ApplyParameter, design, new ApplicationTransaction(flag, !value, value));
    }

    #endregion

    public void UndoDesignChange(Design design)
    {
        if (!UndoStore.Remove(design.Identifier, out var otherData))
            return;

        var other = CreateTemporary();
        other.SetDesignData(Customizations, otherData);
        ApplyDesign(design, other);
    }

    private void MigrateOldDesigns()
    {
        if (!File.Exists(SaveService.FileNames.MigrationDesignFile))
            return;

        var errors     = 0;
        var skips      = 0;
        var successes  = 0;
        var oldDesigns = Designs.ToList();
        try
        {
            var text                    = File.ReadAllText(SaveService.FileNames.MigrationDesignFile);
            var dict                    = JsonConvert.DeserializeObject<Dictionary<string, string>>(text) ?? new Dictionary<string, string>();
            var migratedFileSystemPaths = new Dictionary<string, string>(dict.Count);
            foreach (var (name, base64) in dict)
            {
                try
                {
                    var actualName = Path.GetFileName(name);
                    var design = new Design(Customizations, Items)
                    {
                        CreationDate = File.GetCreationTimeUtc(SaveService.FileNames.MigrationDesignFile),
                        LastEdit     = File.GetLastWriteTimeUtc(SaveService.FileNames.MigrationDesignFile),
                        Identifier   = CreateNewGuid(),
                        Name         = actualName,
                    };
                    design.MigrateBase64(Customizations, Items, _humans, base64);
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

            DesignFileSystem.MigrateOldPaths(SaveService, migratedFileSystemPaths);
            Glamourer.Log.Information(
                $"Successfully migrated {successes} old designs. Skipped {skips} already migrated designs. Failed to migrate {errors} designs.");
        }
        catch (Exception e)
        {
            Glamourer.Log.Error($"Could not migrate old design file {SaveService.FileNames.MigrationDesignFile}:\n{e}");
        }

        try
        {
            File.Move(SaveService.FileNames.MigrationDesignFile,
                Path.ChangeExtension(SaveService.FileNames.MigrationDesignFile, ".json.bak"));
            Glamourer.Log.Information($"Moved migrated design file {SaveService.FileNames.MigrationDesignFile} to backup file.");
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"Could not move migrated design file {SaveService.FileNames.MigrationDesignFile} to backup file:\n{ex}");
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
                var correctName = SaveService.FileNames.DesignFile(design);
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
            if (!Designs.Contains(guid))
                return guid;
        }
    }

    /// <summary>
    /// Try to add an external design to the list.
    /// Returns false if the design is already contained or if the identifier is already in use.
    /// The design is treated as newly created and invokes an event.
    /// </summary>
    private void Add(Design design, string? message)
    {
        if (Designs.Any(d => d == design || d.Identifier == design.Identifier))
            return;

        design.Index = Designs.Count;
        Designs.Add(design);
        if (!message.IsNullOrEmpty())
            Glamourer.Log.Debug(message);
        SaveService.ImmediateSave(design);
        DesignChanged.Invoke(DesignChanged.Type.Created, design, null);
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
