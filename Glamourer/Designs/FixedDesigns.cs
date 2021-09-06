using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Glamourer.FileSystem;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs
{
    public class FixedDesigns : IDisposable
    {
        public class FixedDesign
        {
            public string Name;
            public Design Design;
            public bool   Enabled;

            public GlamourerConfig.FixedDesign ToSave()
                => new()
                {
                    Name    = Name,
                    Path    = Design.FullName(),
                    Enabled = Enabled,
                };

            public FixedDesign(string name, Design design, bool enabled)
            {
                Name    = name;
                Design  = design;
                Enabled = enabled;
            }
        }

        public List<FixedDesign>               Data;
        public Dictionary<string, FixedDesign> EnabledDesigns;

        public bool EnableDesign(FixedDesign design)
        {
            var changes = !design.Enabled;
            if (EnabledDesigns.TryGetValue(design.Name, out var oldDesign))
            {
                oldDesign.Enabled = false;
                changes           = true;
            }
            else
            {
                Glamourer.PlayerWatcher.AddPlayerToWatch(design.Name);
            }

            EnabledDesigns[design.Name] = design;
            design.Enabled              = true;
            if (Dalamud.Objects.FirstOrDefault(o => o.ObjectKind == ObjectKind.Player && o.Name.ToString() == design.Name)
                is Character character)
                OnPlayerChange(character);
            return changes;
        }

        public bool DisableDesign(FixedDesign design)
        {
            if (!design.Enabled)
                return false;

            design.Enabled = false;
            EnabledDesigns.Remove(design.Name);
            Glamourer.PlayerWatcher.RemovePlayerFromWatch(design.Name);
            return true;
        }

        public FixedDesigns(DesignManager designs)
        {
            Data                                  =  new List<FixedDesign>(Glamourer.Config.FixedDesigns.Count);
            EnabledDesigns                        =  new Dictionary<string, FixedDesign>(Glamourer.Config.FixedDesigns.Count);
            Glamourer.PlayerWatcher.PlayerChanged += OnPlayerChange;
            var changes = false;
            for (var i = 0; i < Glamourer.Config.FixedDesigns.Count; ++i)
            {
                var save = Glamourer.Config.FixedDesigns[i];
                if (designs.FileSystem.Find(save.Path, out var d) && d is Design design)
                {
                    Data.Add(new FixedDesign(save.Name, design, save.Enabled));
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
            if (EnabledDesigns.TryGetValue(name, out var design))
            {
                PluginLog.Debug("Redrawing {CharacterName} with {DesignName}.", name, design.Design.FullName());
                design.Design.Data.Apply(character);
                Glamourer.PlayerWatcher.UpdatePlayerWithoutEvent(character);
                Glamourer.Penumbra.RedrawObject(character, RedrawType.WithSettings, false);
            }
        }

        public void Add(string name, Design design, bool enabled = false)
        {
            Data.Add(new FixedDesign(name, design, enabled));
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
