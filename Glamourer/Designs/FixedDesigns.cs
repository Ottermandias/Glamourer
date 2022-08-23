using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Dalamud.Logging;
using System.Runtime;
using System.Text;
using Dalamud.Utility;
using Glamourer.Interop;
using Glamourer.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public struct FixedCondition
{
    private const ulong _territoryFlag = 1ul << 32;
    private const ulong _jobFlag       = 1ul << 33;
    private       ulong _data;

    public static FixedCondition TerritoryCondition(ushort territoryType)
        => new() { _data = territoryType | _territoryFlag };

    public static FixedCondition JobCondition(JobGroup group)
        => new() { _data = group.Id | _jobFlag };

    public bool Check(Actor actor)
    {
        if ((_data & (_territoryFlag | _jobFlag)) == 0)
            return true;

        if ((_data & _territoryFlag) != 0)
            return Dalamud.ClientState.TerritoryType == (ushort)_data;

        if (actor && GameData.JobGroups(Dalamud.GameData).TryGetValue((ushort)_data, out var group) && group.Fits(actor.Job))
            return true;

        return true;
    }

    public override string ToString()
        => _data.ToString();
}

public class FixedDesign
{
    public const int CurrentVersion = 0;

    public string                         Name    { get; private set; }
    public bool                           Enabled;
    public List<Actor.IIdentifier>        Actors;
    public List<(FixedCondition, Design)> Customization;
    public List<(FixedCondition, Design)> Equipment;
    public List<(FixedCondition, Design)> Weapons;

    public FixedDesign(string name)
    {
        Name          = name;
        Actors        = new List<Actor.IIdentifier>();
        Customization = new List<(FixedCondition, Design)>();
        Equipment     = new List<(FixedCondition, Design)>();
        Weapons       = new List<(FixedCondition, Design)>();
    }

    public static FixedDesign? Load(JObject j)
    {
        try
        {
            var name = j[nameof(Name)]?.Value<string>();
            if (name.IsNullOrEmpty())
                return null;

            var version = j["Version"]?.Value<int>();
            if (version == null)
                return null;

            return version switch
            {
                CurrentVersion => LoadCurrentVersion(j, name),
                _              => null,
            };
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error loading fixed design:\n{e}");
            return null;
        }
    }

    private static FixedDesign? LoadCurrentVersion(JObject j, string name)
    {
        var enabled = j[nameof(Enabled)]?.Value<bool>() ?? false;
        var ret = new FixedDesign(name)
        {
            Enabled = enabled,
        };

        var actors  = j[nameof(Actors)];
        //foreach(var pair in actors?.Children().)
        return null;
    }


    public void Save(FileInfo file)
    {
        try
        {
            using var s = file.Exists ? file.Open(FileMode.Truncate) : file.Open(FileMode.CreateNew);
            using var w = new StreamWriter(s, Encoding.UTF8);
            using var j = new JsonTextWriter(w)
            {
                Formatting = Formatting.Indented,
            };
            j.WriteStartObject();
            j.WritePropertyName(nameof(Name));
            j.WriteValue(Name);
            j.WritePropertyName("Version");
            j.WriteValue(CurrentVersion);
            j.WritePropertyName(nameof(Enabled));
            j.WriteValue(Enabled);
            j.WritePropertyName(nameof(Actors));
            j.WriteStartArray();
            foreach (var actor in Actors)
                actor.ToJson(j);
            j.WriteEndArray();
            j.WritePropertyName(nameof(Customization));
            j.WriteStartArray();
            foreach (var (condition, design) in Customization)
            {
                j.WritePropertyName(condition.ToString());
                j.WriteValue(design.Name);
            }

            j.WriteEndArray();
            j.WritePropertyName(nameof(Equipment));
            j.WriteStartArray();
            foreach (var (condition, design) in Equipment)
            {
                j.WritePropertyName(condition.ToString());
                j.WriteValue(design.Name);
            }

            j.WriteEndArray();
            j.WritePropertyName(nameof(Weapons));
            j.WriteStartArray();
            foreach (var (condition, design) in Weapons)
            {
                j.WritePropertyName(condition.ToString());
                j.WriteValue(design.Name);
            }

            j.WriteEndArray();
        }
        catch (Exception e)
        {
            PluginLog.Error($"Could not save collection {Name}:\n{e}");
        }
    }

    public static bool Load(FileInfo path, [NotNullWhen(true)] out FixedDesign? result)
    {
        result = null;
        return true;
    }
}

public class FixedDesigns : IDisposable
{
    //public class FixedDesign
    //{
    //    public string   Name;
    //    public JobGroup Jobs;
    //    public Design   Design;
    //    public bool     Enabled;
    //
    //    public GlamourerConfig.FixedDesign ToSave()
    //        => new()
    //        {
    //            Name      = Name,
    //            Path      = Design.FullName(),
    //            Enabled   = Enabled,
    //            JobGroups = Jobs.Id,
    //        };
    //
    //    public FixedDesign(string name, Design design, bool enabled, JobGroup jobs)
    //    {
    //        Name    = name;
    //        Design  = design;
    //        Enabled = enabled;
    //        Jobs    = jobs;
    //    }
    //}
    //
    //public          List<FixedDesign>                     Data;
    //public          Dictionary<string, List<FixedDesign>> EnabledDesigns;
    //public readonly IReadOnlyDictionary<ushort, JobGroup> JobGroups;
    //
    //public bool EnableDesign(FixedDesign design)
    //{
    //    var changes = !design.Enabled;
    //
    //    if (!EnabledDesigns.TryGetValue(design.Name, out var designs))
    //    {
    //        EnabledDesigns[design.Name] = new List<FixedDesign> { design };
    //        // TODO 
    //        changes = true;
    //    }
    //    else if (!designs.Contains(design))
    //    {
    //        designs.Add(design);
    //        changes = true;
    //    }
    //
    //    design.Enabled = true;
    //    // TODO
    //    //if (Glamourer.Config.ApplyFixedDesigns)
    //    //{
    //    //    var character =
    //    //        CharacterFactory.Convert(Dalamud.Objects.FirstOrDefault(o
    //    //            => o.ObjectKind == ObjectKind.Player && o.Name.ToString() == design.Name));
    //    //    if (character != null)
    //    //        OnPlayerChange(character);
    //    //}
    //
    //    return changes;
    //}
    //
    //public bool DisableDesign(FixedDesign design)
    //{
    //    if (!design.Enabled)
    //        return false;
    //
    //    design.Enabled = false;
    //    if (!EnabledDesigns.TryGetValue(design.Name, out var designs))
    //        return false;
    //    if (!designs.Remove(design))
    //        return false;
    //
    //    if (designs.Count == 0)
    //    {
    //        EnabledDesigns.Remove(design.Name);
    //        // TODO
    //    }
    //
    //    return true;
    //}
    //
    //public FixedDesigns(DesignManager designs)
    //{
    //    JobGroups                             =  GameData.JobGroups(Dalamud.GameData);
    //    Data                                  =  new List<FixedDesign>(Glamourer.Config.FixedDesigns.Count);
    //    EnabledDesigns                        =  new Dictionary<string, List<FixedDesign>>(Glamourer.Config.FixedDesigns.Count);
    //    var changes = false;
    //    for (var i = 0; i < Glamourer.Config.FixedDesigns.Count; ++i)
    //    {
    //        var save = Glamourer.Config.FixedDesigns[i];
    //        if (designs.FileSystem.Find(save.Path, out var d) && d is Design design)
    //        {
    //            if (!JobGroups.TryGetValue((ushort)save.JobGroups, out var jobGroup))
    //                jobGroup = JobGroups[1];
    //            Data.Add(new FixedDesign(save.Name, design, save.Enabled, jobGroup));
    //            if (save.Enabled)
    //                changes |= EnableDesign(Data.Last());
    //        }
    //        else
    //        {
    //            PluginLog.Warning($"{save.Path} does not exist anymore, removing {save.Name} from fixed designs.");
    //            Glamourer.Config.FixedDesigns.RemoveAt(i--);
    //            changes = true;
    //        }
    //    }
    //
    //    if (changes)
    //        Glamourer.Config.Save();
    //}
    //
    //private void OnPlayerChange(Character character)
    //{
    //    //var name = character.Name.ToString();
    //    //if (!EnabledDesigns.TryGetValue(name, out var designs))
    //    //    return;
    //    //
    //    //var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(character.ClassJob.Id));
    //    //if (design == null)
    //    //    return;
    //    //
    //    //PluginLog.Debug("Redrawing {CharacterName} with {DesignName} for job {JobGroup}.", name, design.Design.FullName(),
    //    //    design.Jobs.Name);
    //    //design.Design.Data.Apply(character);
    //    //Glamourer.PlayerWatcher.UpdatePlayerWithoutEvent(character);
    //    //Glamourer.Penumbra.RedrawObject(character, RedrawType.Redraw, false);
    //}
    //
    //public void Add(string name, Design design, JobGroup group, bool enabled = false)
    //{
    //    Data.Add(new FixedDesign(name, design, enabled, group));
    //    Glamourer.Config.FixedDesigns.Add(Data.Last().ToSave());
    //
    //    if (enabled)
    //        EnableDesign(Data.Last());
    //
    //    Glamourer.Config.Save();
    //}
    //
    //public void Remove(FixedDesign design)
    //{
    //    var idx = Data.IndexOf(design);
    //    if (idx < 0)
    //        return;
    //
    //    Data.RemoveAt(idx);
    //    Glamourer.Config.FixedDesigns.RemoveAt(idx);
    //    if (design.Enabled)
    //    {
    //        EnabledDesigns.Remove(design.Name);
    //        // TODO
    //    }
    //
    //    Glamourer.Config.Save();
    //}
    //
    //public void Move(FixedDesign design, int newIdx)
    //{
    //    if (newIdx < 0)
    //        newIdx = 0;
    //    if (newIdx >= Data.Count)
    //        newIdx = Data.Count - 1;
    //
    //    var idx = Data.IndexOf(design);
    //    if (idx < 0 || idx == newIdx)
    //        return;
    //
    //    Data.RemoveAt(idx);
    //    Data.Insert(newIdx, design);
    //    Glamourer.Config.FixedDesigns.RemoveAt(idx);
    //    Glamourer.Config.FixedDesigns.Insert(newIdx, design.ToSave());
    //    Glamourer.Config.Save();
    //}
    //
    public void Dispose()
    {
        //Glamourer.Config.FixedDesigns = Data.Select(d => d.ToSave()).ToList();
        //Glamourer.Config.Save();
    }
}
