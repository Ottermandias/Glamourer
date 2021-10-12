using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Glamourer.FileSystem;
using Penumbra.GameData.Enums;
using Penumbra.PlayerWatch;

namespace Glamourer.Designs
{
    public class FixedDesigns : IDisposable
    {
        public class FixedDesign
        {
            public string   Name;
            public JobGroup Jobs;
            public Design   Design;
            public bool     Enabled;

            public GlamourerConfig.FixedDesign ToSave()
                => new()
                {
                    Name      = Name,
                    Path      = Design.FullName(),
                    Enabled   = Enabled,
                    JobGroups = Jobs.Id,
                };

            public FixedDesign(string name, Design design, bool enabled, JobGroup jobs)
            {
                Name    = name;
                Design  = design;
                Enabled = enabled;
                Jobs    = jobs;
            }
        }

        public          List<FixedDesign>                     Data;
        public          Dictionary<string, List<FixedDesign>> EnabledDesigns;
        public readonly IReadOnlyDictionary<ushort, JobGroup> JobGroups;

        public bool EnableDesign(FixedDesign design)
        {
            var changes = !design.Enabled;

            if (!EnabledDesigns.TryGetValue(design.Name, out var designs))
            {
                EnabledDesigns[design.Name] = new List<FixedDesign> { design };
                Glamourer.PlayerWatcher.AddPlayerToWatch(design.Name);
                changes = true;
            }
            else if (!designs.Contains(design))
            {
                designs.Add(design);
                changes = true;
            }

            design.Enabled = true;
            if (Glamourer.Config.ApplyFixedDesigns)
            {
                var character =
                    CharacterFactory.Convert(Dalamud.Objects.FirstOrDefault(o
                        => o.ObjectKind == ObjectKind.Player && o.Name.ToString() == design.Name));
                if (character != null)
                    OnPlayerChange(character);
            }

            return changes;
        }

        public bool DisableDesign(FixedDesign design)
        {
            if (!design.Enabled)
                return false;

            design.Enabled = false;
            if (!EnabledDesigns.TryGetValue(design.Name, out var designs))
                return false;
            if (!designs.Remove(design))
                return false;

            if (designs.Count == 0)
            {
                EnabledDesigns.Remove(design.Name);
                Glamourer.PlayerWatcher.RemovePlayerFromWatch(design.Name);
            }

            return true;
        }

        public FixedDesigns(DesignManager designs)
        {
            JobGroups                             =  GameData.JobGroups(Dalamud.GameData);
            Data                                  =  new List<FixedDesign>(Glamourer.Config.FixedDesigns.Count);
            EnabledDesigns                        =  new Dictionary<string, List<FixedDesign>>(Glamourer.Config.FixedDesigns.Count);
            Glamourer.PlayerWatcher.PlayerChanged += OnPlayerChange;
            var changes = false;
            for (var i = 0; i < Glamourer.Config.FixedDesigns.Count; ++i)
            {
                var save = Glamourer.Config.FixedDesigns[i];
                if (designs.FileSystem.Find(save.Path, out var d) && d is Design design)
                {
                    if (!JobGroups.TryGetValue((ushort) save.JobGroups, out var jobGroup))
                        jobGroup = JobGroups[1];
                    Data.Add(new FixedDesign(save.Name, design, save.Enabled, jobGroup));
                    if (save.Enabled)
                        changes |= EnableDesign(Data.Last());
                }
                else
                {
                    PluginLog.Warning($"{save.Path} does not exist anymore, removing {save.Name} from fixed designs.");
                    Glamourer.Config.FixedDesigns.RemoveAt(i--);
                    changes = true;
                }
            }

            if (changes)
                Glamourer.Config.Save();
        }

        private void OnPlayerChange(Character character)
        {
            var name = character.Name.ToString();
            if (!EnabledDesigns.TryGetValue(name, out var designs))
                return;

            var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(character.ClassJob.Id));
            if (design == null)
                return;

            PluginLog.Debug("Redrawing {CharacterName} with {DesignName} for job {JobGroup}.", name, design.Design.FullName(),
                design.Jobs.Name);
            design.Design.Data.Apply(character);
            Glamourer.PlayerWatcher.UpdatePlayerWithoutEvent(character);
            Glamourer.Penumbra.RedrawObject(character, RedrawType.WithSettings, false);
        }

        public void Add(string name, Design design, JobGroup group, bool enabled = false)
        {
            Data.Add(new FixedDesign(name, design, enabled, group));
            Glamourer.Config.FixedDesigns.Add(Data.Last().ToSave());

            if (enabled)
                EnableDesign(Data.Last());

            Glamourer.Config.Save();
        }

        public void Remove(FixedDesign design)
        {
            var idx = Data.IndexOf(design);
            if (idx < 0)
                return;

            Data.RemoveAt(idx);
            Glamourer.Config.FixedDesigns.RemoveAt(idx);
            if (design.Enabled)
            {
                EnabledDesigns.Remove(design.Name);
                Glamourer.PlayerWatcher.RemovePlayerFromWatch(design.Name);
            }

            Glamourer.Config.Save();
        }

        public void Move(FixedDesign design, int newIdx)
        {
            if (newIdx < 0)
                newIdx = 0;
            if (newIdx >= Data.Count)
                newIdx = Data.Count - 1;

            var idx = Data.IndexOf(design);
            if (idx < 0 || idx == newIdx)
                return;

            Data.RemoveAt(idx);
            Data.Insert(newIdx, design);
            Glamourer.Config.FixedDesigns.RemoveAt(idx);
            Glamourer.Config.FixedDesigns.Insert(newIdx, design.ToSave());
            Glamourer.Config.Save();
        }

        public void Dispose()
        {
            Glamourer.Config.FixedDesigns = Data.Select(d => d.ToSave()).ToList();
            Glamourer.Config.Save();
        }
    }
}
