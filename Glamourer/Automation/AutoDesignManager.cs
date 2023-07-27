using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Filesystem;
using Penumbra.GameData.Actors;

namespace Glamourer.Automation;

public class AutoDesignManager : ISavable, IReadOnlyList<AutoDesignSet>
{
    public const int CurrentVersion = 1;

    private readonly SaveService _saveService;

    private readonly JobService        _jobs;
    private readonly DesignManager     _designs;
    private readonly ActorService      _actors;
    private readonly AutomationChanged _event;

    private readonly List<AutoDesignSet>                        _data    = new();
    private readonly Dictionary<ActorIdentifier, AutoDesignSet> _enabled = new();

    public IReadOnlyDictionary<ActorIdentifier, AutoDesignSet> EnabledSets
        => _enabled;

    public AutoDesignManager(JobService jobs, ActorService actors, SaveService saveService, DesignManager designs, AutomationChanged @event,
        FixedDesignMigrator migrator, DesignFileSystem fileSystem)
    {
        _jobs        = jobs;
        _actors      = actors;
        _saveService = saveService;
        _designs     = designs;
        _event       = @event;
        Load();
        migrator.ConsumeMigratedData(_actors, fileSystem, this);
    }

    public IEnumerator<AutoDesignSet> GetEnumerator()
        => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _data.Count;

    public AutoDesignSet this[int index]
        => _data[index];

    public void AddDesignSet(string name, ActorIdentifier identifier)
    {
        if (!IdentifierValid(identifier, out var group) || name.Length == 0)
            return;

        var newSet = new AutoDesignSet(name, group) { Enabled = false };
        _data.Add(newSet);
        Save();
        Glamourer.Log.Debug($"Created new design set for {newSet.Identifiers[0].Incognito(null)}.");
        _event.Invoke(AutomationChanged.Type.AddedSet, newSet, (_data.Count - 1, name));
    }

    public void DuplicateDesignSet(AutoDesignSet set)
    {
        var name = set.Name;
        var match = Regex.Match(name, @"\(Duplicate( (?<Number>\d+))?\)$",
            RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture);
        if (match.Success)
        {
            var number      = match.Groups["Number"];
            var replacement = number.Success ? $"(Duplicate {int.Parse(number.Value) + 1})" : "(Duplicate 2)";
            name = name.Replace(match.Value, replacement);
        }
        else
        {
            name += " (Duplicate)";
        }

        var newSet = new AutoDesignSet(name, set.Identifiers) { Enabled = false };
        newSet.Designs.AddRange(set.Designs.Select(d => d.Clone()));
        _data.Add(newSet);
        Save();
        Glamourer.Log.Debug(
            $"Duplicated new design set for {newSet.Identifiers[0].Incognito(null)} with {newSet.Designs.Count} auto designs from existing set.");
        _event.Invoke(AutomationChanged.Type.AddedSet, newSet, (_data.Count - 1, name));
    }

    public void DeleteDesignSet(int whichSet)
    {
        if (whichSet >= _data.Count || whichSet < 0)
            return;

        var set = _data[whichSet];
        if (set.Enabled)
        {
            set.Enabled = false;
            foreach (var id in set.Identifiers)
                _enabled.Remove(id);
        }

        _data.RemoveAt(whichSet);
        Save();
        Glamourer.Log.Debug($"Deleted design set {whichSet + 1}.");
        _event.Invoke(AutomationChanged.Type.DeletedSet, set, whichSet);
    }

    public void Rename(int whichSet, string newName)
    {
        if (whichSet >= _data.Count || whichSet < 0 || newName.Length == 0)
            return;

        var set = _data[whichSet];
        if (set.Name == newName)
            return;

        var old = set.Name;
        set.Name = newName;
        Save();
        Glamourer.Log.Debug($"Renamed design set {whichSet + 1} from {old} to {newName}.");
        _event.Invoke(AutomationChanged.Type.RenamedSet, set, (old, newName));
    }


    public void MoveSet(int whichSet, int toWhichSet)
    {
        if (!_data.Move(whichSet, toWhichSet))
            return;

        Save();
        Glamourer.Log.Debug($"Moved design set {whichSet + 1} to position {toWhichSet + 1}.");
        _event.Invoke(AutomationChanged.Type.MovedSet, _data[toWhichSet], (whichSet, toWhichSet));
    }

    public void ChangeIdentifier(int whichSet, ActorIdentifier to)
    {
        if (whichSet >= _data.Count || whichSet < 0 || !IdentifierValid(to, out var group))
            return;

        var set = _data[whichSet];
        if (set.Identifiers.Any(id => id == to))
            return;

        var old = set.Identifiers;
        set.Identifiers = group;
        AutoDesignSet? oldEnabled = null;
        if (set.Enabled)
        {
            foreach (var id in old)
                _enabled.Remove(id);
            if (_enabled.Remove(to, out oldEnabled))
            {
                foreach (var id in oldEnabled.Identifiers)
                    _enabled.Remove(id);
                oldEnabled.Enabled = false;
            }

            foreach (var id in group)
                _enabled.Add(id, set);
        }

        Save();
        Glamourer.Log.Debug($"Changed Identifier of design set {whichSet + 1} from {old[0].Incognito(null)} to {to.Incognito(null)}.");
        _event.Invoke(AutomationChanged.Type.ChangeIdentifier, set, (old, to, oldEnabled));
    }

    public void SetState(int whichSet, bool value)
    {
        if (whichSet >= _data.Count || whichSet < 0)
            return;

        var set = _data[whichSet];
        if (set.Enabled == value)
            return;

        set.Enabled = value;
        AutoDesignSet? oldEnabled;
        if (value)
        {
            if (_enabled.Remove(set.Identifiers[0], out oldEnabled))
            {
                foreach (var id in oldEnabled.Identifiers)
                    _enabled.Remove(id);
                oldEnabled.Enabled = false;
            }

            foreach (var id in set.Identifiers)
                _enabled.Add(id, set);
        }
        else if (_enabled.Remove(set.Identifiers[0], out oldEnabled))
        {
            foreach (var id in oldEnabled.Identifiers)
                _enabled.Remove(id);
        }

        Save();
        Glamourer.Log.Debug($"Changed enabled state of design set {whichSet + 1} to {value}.");
        _event.Invoke(AutomationChanged.Type.ToggleSet, set, oldEnabled);
    }

    public void ChangeBaseState(int whichSet, AutoDesignSet.Base newBase)
    {
        if (whichSet >= _data.Count || whichSet < 0)
            return;

        var set = _data[whichSet];
        if (newBase == set.BaseState)
            return;

        var old = set.BaseState;
        set.BaseState = newBase;
        Save();
        Glamourer.Log.Debug($"Changed base state of set {whichSet + 1} from {old} to {newBase}.");
        _event.Invoke(AutomationChanged.Type.ChangedBase, set, (old, newBase));
    }

    public void AddDesign(AutoDesignSet set, Design? design)
    {
        var newDesign = new AutoDesign()
        {
            Design          = design,
            ApplicationType = AutoDesign.Type.All,
            Jobs            = _jobs.JobGroups[1],
        };
        set.Designs.Add(newDesign);
        Save();
        Glamourer.Log.Debug(
            $"Added new associated design {design?.Identifier.ToString() ?? "Reverter"} as design {set.Designs.Count} to design set.");
        _event.Invoke(AutomationChanged.Type.AddedDesign, set, set.Designs.Count - 1);
    }

    public void DeleteDesign(AutoDesignSet set, int which)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        set.Designs.RemoveAt(which);
        Save();
        Glamourer.Log.Debug($"Removed associated design {which + 1} from design set.");
        _event.Invoke(AutomationChanged.Type.DeletedDesign, set, which);
    }

    public void MoveDesign(AutoDesignSet set, int from, int to)
    {
        if (!set.Designs.Move(from, to))
            return;

        Save();
        Glamourer.Log.Debug($"Moved design {from + 1} to {to + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.MovedDesign, set, (from, to));
    }

    public void ChangeDesign(AutoDesignSet set, int which, Design? newDesign)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        var design = set.Designs[which];
        if (design.Design?.Identifier == newDesign?.Identifier)
            return;

        var old = design.Design;
        design.Design = newDesign;
        Save();
        Glamourer.Log.Debug(
            $"Changed linked design from {old?.Identifier.ToString() ?? "Reverter"} to {newDesign?.Identifier.ToString() ?? "Reverter"} for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedDesign, set, (which, old, newDesign));
    }

    public void ChangeJobCondition(AutoDesignSet set, int which, JobGroup jobs)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        var design = set.Designs[which];
        if (design.Jobs.Id == jobs.Id)
            return;

        var old = design.Jobs;
        design.Jobs = jobs;
        Save();
        Glamourer.Log.Debug($"Changed job condition from {old.Id} to {jobs.Id} for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedConditions, set, (which, old, jobs));
    }

    public void ChangeApplicationType(AutoDesignSet set, int which, AutoDesign.Type type)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        type &= AutoDesign.Type.All;
        var design = set.Designs[which];
        if (design.ApplicationType == type)
            return;

        var old = design.ApplicationType;
        design.ApplicationType = type;
        Save();
        Glamourer.Log.Debug($"Changed application type from {old} to {type} for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedType, set, (which, old, type));
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.AutomationFile;

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
        };
        Serialize().WriteTo(j);
    }

    private JObject Serialize()
    {
        var array = new JArray();
        foreach (var set in _data)
            array.Add(set.Serialize());

        return new JObject()
        {
            ["Version"] = CurrentVersion,
            ["Data"]    = array,
        };
    }

    private void Load()
    {
        var file = _saveService.FileNames.AutomationFile;
        _data.Clear();
        if (!File.Exists(file))
            return;

        try
        {
            var text    = File.ReadAllText(file);
            var obj     = JObject.Parse(text);
            var version = obj["Version"]?.ToObject<int>() ?? 0;

            switch (version)
            {
                case < 1:
                case > CurrentVersion:
                    Glamourer.Chat.NotificationMessage("Failure to load automated designs: No valid version available.", "Error",
                        NotificationType.Error);
                    break;
                case 1:
                    LoadV1(obj["Data"]);
                    break;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Chat.NotificationMessage(ex, "Failure to load automated designs: Error during parsing.",
                "Failure to load automated designs", "Error", NotificationType.Error);
        }
    }

    private void LoadV1(JToken? data)
    {
        if (data is not JArray array)
            return;

        foreach (var obj in array)
        {
            var name = obj["Name"]?.ToObject<string>() ?? string.Empty;
            if (name.Length == 0)
            {
                Glamourer.Chat.NotificationMessage("Skipped loading Automation Set: No name provided.", "Warning", NotificationType.Warning);
                continue;
            }

            var id = _actors.AwaitedService.FromJson(obj["Identifier"] as JObject);
            if (!IdentifierValid(id, out var group))
            {
                Glamourer.Chat.NotificationMessage("Skipped loading Automation Set: Invalid Identifier.", "Warning", NotificationType.Warning);
                continue;
            }

            var set = new AutoDesignSet(name, group)
            {
                Enabled   = obj["Enabled"]?.ToObject<bool>() ?? false,
                BaseState = obj["BaseState"]?.ToObject<AutoDesignSet.Base>() ?? AutoDesignSet.Base.Current,
            };

            if (set.Enabled)
            {
                if (_enabled.TryAdd(group[0], set))
                    foreach (var id2 in group.Skip(1))
                        _enabled[id2] = set;
                else
                    set.Enabled = false;
            }

            _data.Add(set);

            if (obj["Designs"] is not JArray designArray)
                continue;

            foreach (var designObj in designArray)
            {
                if (designObj is not JObject j)
                {
                    Glamourer.Chat.NotificationMessage($"Skipped loading design in Automation Set for {id.Incognito(null)}: Unknown design.");
                    continue;
                }

                var design = ToDesignObject(j);
                if (design != null)
                    set.Designs.Add(design);
            }
        }
    }

    private AutoDesign? ToDesignObject(JObject jObj)
    {
        var     designIdentifier = jObj["Design"]?.ToObject<string?>();
        Design? design           = null;
        // designIdentifier == null means Revert-Design.
        if (designIdentifier != null)
        {
            if (designIdentifier.Length == 0)
            {
                Glamourer.Chat.NotificationMessage("Error parsing automatically applied design: No design specified.");
                return null;
            }

            if (!Guid.TryParse(designIdentifier, out var guid))
            {
                Glamourer.Chat.NotificationMessage($"Error parsing automatically applied design: {designIdentifier} is not a valid GUID.");

                return null;
            }

            design = _designs.Designs.FirstOrDefault(d => d.Identifier == guid);
            if (design == null)
            {
                Glamourer.Chat.NotificationMessage($"Error parsing automatically applied design: The specified design {guid} does not exist.");
                return null;
            }
        }

        var applicationType = (AutoDesign.Type)(jObj["ApplicationType"]?.ToObject<uint>() ?? 0);

        var ret = new AutoDesign()
        {
            Design          = design,
            ApplicationType = applicationType & AutoDesign.Type.All,
        };

        var conditions = jObj["Conditions"];
        if (conditions == null)
            return ret;

        var jobs = conditions["JobGroup"]?.ToObject<int>() ?? -1;
        if (jobs >= 0)
        {
            if (!_jobs.JobGroups.TryGetValue((ushort)jobs, out var jobGroup))
            {
                Glamourer.Chat.NotificationMessage($"Error parsing automatically applied design: The job condition {jobs} does not exist.");
                return null;
            }

            ret.Jobs = jobGroup;
        }

        return ret;
    }

    private void Save()
        => _saveService.DelaySave(this);

    private bool IdentifierValid(ActorIdentifier identifier, out ActorIdentifier[] group)
    {
        var validType = identifier.Type switch
        {
            IdentifierType.Player   => true,
            IdentifierType.Retainer => true,
            IdentifierType.Npc      => true,
            _                       => false,
        };

        if (!validType)
        {
            group = Array.Empty<ActorIdentifier>();
            return false;
        }

        group = GetGroup(identifier);
        return group.Length > 0;
    }

    private ActorIdentifier[] GetGroup(ActorIdentifier identifier)
    {
        if (!identifier.IsValid)
            return Array.Empty<ActorIdentifier>();

        static ActorIdentifier[] CreateNpcs(ActorManager manager, ActorIdentifier identifier)
        {
            var name = manager.Data.ToName(identifier.Kind, identifier.DataId);
            var table = identifier.Kind switch
            {
                ObjectKind.BattleNpc => manager.Data.BNpcs,
                ObjectKind.EventNpc  => manager.Data.ENpcs,
                _                    => throw new NotImplementedException(),
            };
            return table.Where(kvp => kvp.Value == name)
                .Select(kvp => manager.CreateIndividualUnchecked(identifier.Type, identifier.PlayerName, identifier.HomeWorld, identifier.Kind,
                    kvp.Key)).ToArray();
        }

        return identifier.Type switch
        {
            IdentifierType.Player => new[]
            {
                identifier.CreatePermanent(),
            },
            IdentifierType.Retainer => new[]
            {
                _actors.AwaitedService.CreateRetainer(identifier.PlayerName,
                    identifier.Retainer == ActorIdentifier.RetainerType.Mannequin
                        ? ActorIdentifier.RetainerType.Mannequin
                        : ActorIdentifier.RetainerType.Bell).CreatePermanent(),
            },
            IdentifierType.Npc => CreateNpcs(_actors.AwaitedService, identifier),
            _                  => Array.Empty<ActorIdentifier>(),
        };
    }
}
