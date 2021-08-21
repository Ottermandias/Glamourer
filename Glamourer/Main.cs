using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.FileSystem;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.PlayerWatch;

namespace Glamourer
{
    public class Glamourer : IDalamudPlugin
    {
        public const int RequiredPenumbraShareVersion = 1;

        private const string HelpString = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";

        public string Name
            => "Glamourer";

        public static DalamudPluginInterface PluginInterface = null!;
        public static GlamourerConfig        Config          = null!;
        private       Interface              _interface      = null!;
        public static ICustomizationManager  Customization   = null!;
        public        DesignManager          Designs         = null!;
        public        IPlayerWatcher         PlayerWatcher   = null!;

        public static string Version = string.Empty;

        public static IPenumbraApi? Penumbra;

        private Dalamud.Dalamud                                                                                                _dalamud = null!;
        private List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)> _plugins = null!;

        private void SetDalamud(DalamudPluginInterface pi)
        {
            var dalamud = (Dalamud.Dalamud?) pi.GetType()
                ?.GetField("dalamud", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(pi);

            _dalamud = dalamud ?? throw new Exception("Could not obtain Dalamud.");
        }

        private static void PenumbraTooltip(object? it)
        {
            if (it is Lumina.Excel.GeneratedSheets.Item)
                ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
        }

        private void PenumbraRightClick(MouseButton button, object? it)
        {
            if (button == MouseButton.Right && it is Lumina.Excel.GeneratedSheets.Item item)
            {
                var actors    = PluginInterface.ClientState.Actors;
                var gPose     = actors[Interface.GPoseActorId];
                var player    = actors[0];
                var writeItem = new Item(item, string.Empty);
                if (gPose != null)
                {
                    writeItem.Write(gPose.Address);
                    UpdateActors(gPose, player);
                }
                else if (player != null)
                {
                    writeItem.Write(player.Address);
                    UpdateActors(player);
                }
            }
        }

        private void RegisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip += PenumbraTooltip;
            Penumbra!.ChangedItemClicked += PenumbraRightClick;
        }

        private void UnregisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip -= PenumbraTooltip;
            Penumbra!.ChangedItemClicked -= PenumbraRightClick;
        }

        private void SetPlugins(DalamudPluginInterface pi)
        {
            var pluginManager = _dalamud?.GetType()
                ?.GetProperty("PluginManager", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_dalamud);

            if (pluginManager == null)
                throw new Exception("Could not obtain plugin manager.");

            var pluginsList =
                (List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)>?) pluginManager
                    ?.GetType()
                    ?.GetProperty("Plugins", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(pluginManager);

            _plugins = pluginsList ?? throw new Exception("Could not obtain Dalamud.");
        }

        private bool GetPenumbra()
        {
            if (Penumbra?.Valid ?? false)
                return true;

            var plugin = _plugins.Find(p
                => p.Definition.InternalName == "Penumbra"
             && string.Compare(p.Definition.AssemblyVersion, "0.4.0.3", StringComparison.Ordinal) >= 0).Plugin;

            var penumbra = (IPenumbraApiBase?) plugin?.GetType().GetProperty("Api", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(plugin);
            if (penumbra != null && penumbra.Valid && penumbra.ApiVersion >= RequiredPenumbraShareVersion)
            {
                Penumbra = (IPenumbraApi) penumbra!;
                RegisterFunctions();
            }
            else
            {
                Penumbra = null;
            }

            return Penumbra != null;
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Version         = Assembly.GetExecutingAssembly()?.GetName().Version.ToString() ?? "";
            PluginInterface = pluginInterface;
            Config          = GlamourerConfig.Create();
            Customization   = CustomizationManager.Create(PluginInterface);
            SetDalamud(PluginInterface);
            SetPlugins(PluginInterface);
            Designs = new DesignManager(PluginInterface);
            GetPenumbra();
            PlayerWatcher = PlayerWatchFactory.Create(PluginInterface);

            PluginInterface.CommandManager.AddHandler("/glamourer", new CommandInfo(OnGlamourer)
            {
                HelpMessage = "Open or close the Glamourer window.",
            });
            PluginInterface.CommandManager.AddHandler("/glamour", new CommandInfo(OnGlamour)
            {
                HelpMessage = $"Use Glamourer Functions: {HelpString}",
            });

            _interface = new Interface(this);
        }

        public void OnGlamourer(string command, string arguments)
            => _interface?.ToggleVisibility(null!, null!);

        private Actor? GetActor(string name)
        {
            var lowerName = name.ToLowerInvariant();
            return lowerName switch
            {
                ""          => null,
                "<me>"      => PluginInterface.ClientState.Actors[Interface.GPoseActorId] ?? PluginInterface.ClientState.LocalPlayer,
                "self"      => PluginInterface.ClientState.Actors[Interface.GPoseActorId] ?? PluginInterface.ClientState.LocalPlayer,
                "<t>"       => PluginInterface.ClientState.Targets.CurrentTarget,
                "target"    => PluginInterface.ClientState.Targets.CurrentTarget,
                "<f>"       => PluginInterface.ClientState.Targets.FocusTarget,
                "focus"     => PluginInterface.ClientState.Targets.FocusTarget,
                "<mo>"      => PluginInterface.ClientState.Targets.MouseOverTarget,
                "mouseover" => PluginInterface.ClientState.Targets.MouseOverTarget,
                _ => PluginInterface.ClientState.Actors.LastOrDefault(
                    a => string.Equals(a.Name, lowerName, StringComparison.InvariantCultureIgnoreCase)),
            };
        }


        public void CopyToClipboard(Actor actor)
        {
            var save = new CharacterSave();
            save.LoadActor(actor);
            Clipboard.SetText(save.ToBase64());
        }

        public void ApplyCommand(Actor actor, string target)
        {
            CharacterSave? save = null;
            if (target.ToLowerInvariant() == "clipboard")
            {
                try
                {
                    save = CharacterSave.FromString(Clipboard.GetText());
                }
                catch (Exception)
                {
                    PluginInterface.Framework.Gui.Chat.PrintError("Clipboard does not contain a valid customization string.");
                }
            }
            else if (!Designs.FileSystem.Find(target, out var child) || child is not Design d)
            {
                PluginInterface.Framework.Gui.Chat.PrintError("The given path to a saved design does not exist or does not point to a design.");
            }
            else
            {
                save = d.Data;
            }

            save?.Apply(actor);
            UpdateActors(actor);
        }

        public void SaveCommand(Actor actor, string path)
        {
            var save = new CharacterSave();
            save.LoadActor(actor);
            try
            {
                var (folder, name) = Designs.FileSystem.CreateAllFolders(path);
                var design = new Design(folder, name) { Data = save };
                folder.FindOrAddChild(design);
                Designs.Designs.Add(design.FullName(), design.Data);
                Designs.SaveToFile();
            }
            catch (Exception e)
            {
                PluginInterface.Framework.Gui.Chat.PrintError("Could not save file:");
                PluginInterface.Framework.Gui.Chat.PrintError($"    {e.Message}");
            }
        }

        public void OnGlamour(string command, string arguments)
        {
            static void PrintHelp()
            {
                PluginInterface.Framework.Gui.Chat.Print("Usage:");
                PluginInterface.Framework.Gui.Chat.Print($"    {HelpString}");
            }

            arguments = arguments.Trim();
            if (!arguments.Any())
            {
                PrintHelp();
                return;
            }

            var split = arguments.Split(new[]
            {
                ',',
            }, 3, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length < 2)
            {
                PrintHelp();
                return;
            }

            var actor = GetActor(split[1]);
            if (actor == null)
            {
                PluginInterface.Framework.Gui.Chat.Print($"Could not find actor for {split[1]}.");
                return;
            }

            switch (split[0].ToLowerInvariant())
            {
                case "copy":
                    CopyToClipboard(actor);
                    return;
                case "apply":
                {
                    if (split.Length < 3)
                    {
                        PluginInterface.Framework.Gui.Chat.Print("Applying requires a name for the save to be applied or 'clipboard'.");
                        return;
                    }
                    ApplyCommand(actor, split[2]);
                    
                    return;
                }
                case "save":
                {
                    if (split.Length < 3)
                    {
                        PluginInterface.Framework.Gui.Chat.Print("Saving requires a name for the save.");
                        return;
                    }
                    SaveCommand(actor, split[2]);
                    return;
                }
                default:
                    PrintHelp();
                    return;
            }
        }

        public void Dispose()
        {
            PlayerWatcher?.Dispose();
            UnregisterFunctions();
            _interface?.Dispose();
            PluginInterface.CommandManager.RemoveHandler("/glamour");
            PluginInterface.CommandManager.RemoveHandler("/glamourer");
            PluginInterface.Dispose();
        }

        // Update actors without triggering PlayerWatcher Events,
        // then manually redraw using Penumbra.
        public void UpdateActors(Actor actor, Actor? gPoseOriginalActor = null)
        {
            var newEquip = PlayerWatcher.UpdateActorWithoutEvent(actor);
            Penumbra?.RedrawActor(actor, RedrawType.WithSettings);

            // Special case for carrying over changes to the gPose actor to the regular player actor, too.
            if (gPoseOriginalActor != null)
            {
                newEquip.Write(gPoseOriginalActor.Address);
                PlayerWatcher.UpdateActorWithoutEvent(gPoseOriginalActor);
                Penumbra?.RedrawActor(gPoseOriginalActor, RedrawType.AfterGPoseWithSettings);
            }
        }
    }
}
