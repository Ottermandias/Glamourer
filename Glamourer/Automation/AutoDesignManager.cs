using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Designs.Special;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public class AutoDesignManager : ISavable, IReadOnlyList<AutoDesignSet>, IDisposable
{
    public const int CurrentVersion = 1;

    private readonly SaveService _saveService;

    private readonly JobService            _jobs;
    private readonly DesignManager         _designs;
    private readonly ActorManager          _actors;
    private readonly AutomationChanged     _event;
    private readonly DesignChanged         _designEvent;
    private readonly RandomDesignGenerator _randomDesigns;
    private readonly QuickSelectedDesign   _quickSelectedDesign;

    private readonly List<AutoDesignSet>                        _data    = [];
    private readonly Dictionary<ActorIdentifier, AutoDesignSet> _enabled = [];

    public IReadOnlyDictionary<ActorIdentifier, AutoDesignSet> EnabledSets
        => _enabled;

    public AutoDesignManager(JobService jobs, ActorManager actors, SaveService saveService, DesignManager designs, AutomationChanged @event,
        FixedDesignMigrator migrator, DesignFileSystem fileSystem, DesignChanged designEvent, RandomDesignGenerator randomDesigns,
        QuickSelectedDesign quickSelectedDesign)
    {
        _jobs                = jobs;
        _actors              = actors;
        _saveService         = saveService;
        _designs             = designs;
        _event               = @event;
        _designEvent         = designEvent;
        _randomDesigns       = randomDesigns;
        _quickSelectedDesign = quickSelectedDesign;
        _designEvent.Subscribe(OnDesignChange, DesignChanged.Priority.AutoDesignManager);
        Load();
        migrator.ConsumeMigratedData(_actors, fileSystem, this);
    }

    public void Dispose()
        => _designEvent.Unsubscribe(OnDesignChange);

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

    public void AddDesign(AutoDesignSet set, IDesignStandIn design)
    {
        var newDesign = new AutoDesign()
        {
            Design = design,
            Type   = ApplicationType.All,
            Jobs   = _jobs.JobGroups[1],
        };
        set.Designs.Add(newDesign);
        Save();
        Glamourer.Log.Debug(
            $"Added new associated design {design.ResolveName(true)} as design {set.Designs.Count} to design set.");
        _event.Invoke(AutomationChanged.Type.AddedDesign, set, set.Designs.Count - 1);
    }

    /// <remarks> Only used to move between sets. </remarks>
    public void MoveDesignToSet(AutoDesignSet from, int idx, AutoDesignSet to)
    {
        if (ReferenceEquals(from, to))
            return;

        var design = from.Designs[idx];
        to.Designs.Add(design);
        from.Designs.RemoveAt(idx);
        Save();
        Glamourer.Log.Debug($"Moved design {idx} from design set {from.Name} to design set {to.Name}.");
        _event.Invoke(AutomationChanged.Type.AddedDesign,   to,   to.Designs.Count - 1);
        _event.Invoke(AutomationChanged.Type.DeletedDesign, from, idx);
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

    public void ChangeDesign(AutoDesignSet set, int which, IDesignStandIn newDesign)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        var design = set.Designs[which];
        if (design.Design.Equals(newDesign))
            return;

        var old = design.Design;
        design.Design = newDesign;
        Save();
        Glamourer.Log.Debug(
            $"Changed linked design from {old.ResolveName(true)} to {newDesign.ResolveName(true)} for associated design {which + 1} in design set.");
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

    public void ChangeGearsetCondition(AutoDesignSet set, int which, short index)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        var design = set.Designs[which];
        if (design.GearsetIndex == index)
            return;

        var old = design.GearsetIndex;
        design.GearsetIndex = index;
        Save();
        Glamourer.Log.Debug($"Changed gearset condition from {old} to {index} for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedConditions, set, (which, old, index));
    }

    public void ChangeApplicationType(AutoDesignSet set, int which, ApplicationType applicationType)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        applicationType &= ApplicationType.All;
        var design = set.Designs[which];
        if (design.Type == applicationType)
            return;

        var old = design.Type;
        design.Type = applicationType;
        Save();
        Glamourer.Log.Debug($"Changed application type from {old} to {applicationType} for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedType, set, (which, old, applicationType));
    }

    public void ChangeData(AutoDesignSet set, int which, object data)
    {
        if (which >= set.Designs.Count || which < 0)
            return;

        var design = set.Designs[which];
        if (!design.Design.ChangeData(data))
            return;

        Save();
        Glamourer.Log.Debug($"Changed additional design data for associated design {which + 1} in design set.");
        _event.Invoke(AutomationChanged.Type.ChangedData, set, (which, data));
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.AutomationFile;

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
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
                    Glamourer.Messager.NotificationMessage("Failure to load automated designs: No valid version available.",
                        NotificationType.Error);
                    break;
                case 1:
                    LoadV1(obj["Data"]);
                    break;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Failure to load automated designs: Error during parsing.",
                "Failure to load automated designs", NotificationType.Error);
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
                Glamourer.Messager.NotificationMessage("Skipped loading Automation Set: No name provided.", NotificationType.Warning);
                continue;
            }

            var id = _actors.FromJson(obj["Identifier"] as JObject);
            if (!IdentifierValid(id, out var group))
            {
                Glamourer.Messager.NotificationMessage("Skipped loading Automation Set: Invalid Identifier.", NotificationType.Warning);
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
                    Glamourer.Messager.NotificationMessage(
                        $"Skipped loading design in Automation Set {name}: Unknown design.", NotificationType.Warning);
                    continue;
                }

                if (ToDesignObject(set.Name, j) is { } design)
                    set.Designs.Add(design);
            }
        }
    }

    private AutoDesign? ToDesignObject(string setName, JObject jObj)
    {
        var             designIdentifier = jObj["Design"]?.ToObject<string?>();
        IDesignStandIn? design;
        // designIdentifier == null means Revert-Design for backwards compatibility
        if (designIdentifier is null or RevertDesign.SerializedName)
        {
            design = new RevertDesign();
        }
        else if (designIdentifier is RandomDesign.SerializedName)
        {
            design = new RandomDesign(_randomDesigns);
        }
        else if (designIdentifier is QuickSelectedDesign.SerializedName)
        {
            design = _quickSelectedDesign;
        }
        else
        {
            if (designIdentifier.Length == 0)
            {
                Glamourer.Messager.NotificationMessage($"Error parsing automatically applied design for set {setName}: No design specified.",
                    NotificationType.Warning);
                return null;
            }

            if (!Guid.TryParse(designIdentifier, out var guid))
            {
                Glamourer.Messager.NotificationMessage(
                    $"Error parsing automatically applied design for set {setName}: {designIdentifier} is not a valid GUID.",
                    NotificationType.Warning);

                return null;
            }

            if (!_designs.Designs.TryGetValue(guid, out var d))
            {
                Glamourer.Messager.NotificationMessage(
                    $"Error parsing automatically applied design for set {setName}: The specified design {guid} does not exist.",
                    NotificationType.Warning);
                return null;
            }

            design = d;
        }

        design.ParseData(jObj);

        // ApplicationType is a migration from an older property name.
        var applicationType = (ApplicationType)(jObj["Type"]?.ToObject<uint>() ?? jObj["ApplicationType"]?.ToObject<uint>() ?? 0);

        var ret = new AutoDesign
        {
            Design = design,
            Type   = applicationType & ApplicationType.All,
        };
        return ParseConditions(setName, jObj, ret) ? ret : null;
    }

    private bool ParseConditions(string setName, JObject jObj, AutoDesign ret)
    {
        var conditions = jObj["Conditions"];
        if (conditions == null)
            return true;

        var jobs = conditions["JobGroup"]?.ToObject<int>() ?? -1;
        if (jobs >= 0)
        {
            if (!_jobs.JobGroups.TryGetValue((JobGroupId)jobs, out var jobGroup))
            {
                Glamourer.Messager.NotificationMessage(
                    $"Error parsing automatically applied design for set {setName}: The job condition {jobs} does not exist.",
                    NotificationType.Warning);
                return false;
            }

            ret.Jobs = jobGroup;
        }

        ret.GearsetIndex = conditions["Gearset"]?.ToObject<short>() ?? -1;
        return true;
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
            return [];

        return identifier.Type switch
        {
            IdentifierType.Player =>
            [
                identifier.CreatePermanent(),
            ],
            IdentifierType.Retainer =>
            [
                _actors.CreateRetainer(identifier.PlayerName,
                    identifier.Retainer == ActorIdentifier.RetainerType.Mannequin
                        ? ActorIdentifier.RetainerType.Mannequin
                        : ActorIdentifier.RetainerType.Bell).CreatePermanent(),
            ],
            IdentifierType.Npc => CreateNpcs(_actors, identifier),
            _                  => [],
        };

        static ActorIdentifier[] CreateNpcs(ActorManager manager, ActorIdentifier identifier)
        {
            var name = manager.Data.ToName(identifier.Kind, identifier.DataId);
            var table = identifier.Kind switch
            {
                ObjectKind.BattleNpc => (IReadOnlyDictionary<NpcId, string>)manager.Data.BNpcs,
                ObjectKind.EventNpc  => manager.Data.ENpcs,
                _                    => new Dictionary<NpcId, string>(),
            };
            return table.Where(kvp => kvp.Value == name)
                .Select(kvp => manager.CreateIndividualUnchecked(identifier.Type, identifier.PlayerName, identifier.HomeWorld.Id,
                    identifier.Kind,
                    kvp.Key)).ToArray();
        }
    }

    private void OnDesignChange(DesignChanged.Type type, Design design, ITransaction? _)
    {
        if (type is not DesignChanged.Type.Deleted)
            return;

        foreach (var (set, idx) in this.WithIndex())
        {
            var deleted = 0;
            for (var i = 0; i < set.Designs.Count; ++i)
            {
                if (set.Designs[i].Design != design)
                    continue;

                DeleteDesign(set, i--);
                ++deleted;
            }

            if (deleted > 0)
                Glamourer.Log.Information(
                    $"Removed {deleted} automated designs from automated design set {idx} due to deletion of {design.Incognito}.");
        }
    }
}
