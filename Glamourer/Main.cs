using System;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Logging;
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
        public const int RequiredPenumbraShareVersion = 3;

        private const string HelpString = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";

        public string Name
            => "Glamourer";

        public static GlamourerConfig       Config        = null!;
        private       Interface             _interface    = null!;
        public static ICustomizationManager Customization = null!;
        public        DesignManager         Designs       = null!;
        public        IPlayerWatcher        PlayerWatcher = null!;

        public static string Version = string.Empty;

        public static IPenumbraApi? Penumbra;

        private static void PenumbraTooltip(object? it)
        {
            if (it is Lumina.Excel.GeneratedSheets.Item)
                ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
        }

        private void PenumbraRightClick(MouseButton button, object? it)
        {
            if (button != MouseButton.Right || it is not Lumina.Excel.GeneratedSheets.Item item)
                return;

            var gPose     = Dalamud.Objects[Interface.GPoseActorId] as Character;
            var player    = Dalamud.Objects[0] as Character;
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

        public void RegisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip += PenumbraTooltip;
            Penumbra!.ChangedItemClicked += PenumbraRightClick;
        }

        public void UnregisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip -= PenumbraTooltip;
            Penumbra!.ChangedItemClicked -= PenumbraRightClick;
        }

        internal static bool GetPenumbra()
        {
            try
            {
                var subscriber      = Dalamud.PluginInterface.GetIpcSubscriber<IPenumbraApiBase>("Penumbra.Api");
                var penumbraApiBase = subscriber.InvokeFunc();
                if (penumbraApiBase.ApiVersion != RequiredPenumbraShareVersion)
                {
                    PluginLog.Debug("Could not get Penumbra because API version {penumbraApiBase.ApiVersion} does not equal the required version {RequiredPenumbraShareVersion}.");
                    Penumbra = null;
                    return false;
                }

                Penumbra = penumbraApiBase as IPenumbraApi;
            }
            catch (IpcNotReadyError ipc)
            {
                Penumbra = null;
                PluginLog.Debug($"Could not get Penumbra because IPC not registered:\n{ipc}");
            }
            catch (Exception e)
            {
                Penumbra = null;
                PluginLog.Debug($"Could not get Penumbra for unknown reason:\n{e}");
            }

            return Penumbra != null;
        }

        public Glamourer(DalamudPluginInterface pluginInterface)
        {
            Dalamud.Initialize(pluginInterface);
            Version       = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Config        = GlamourerConfig.Load();
            Customization = CustomizationManager.Create(Dalamud.PluginInterface, Dalamud.GameData, Dalamud.ClientState.ClientLanguage);
            Designs       = new DesignManager();
            if (GetPenumbra() && Config.AttachToPenumbra)
                RegisterFunctions();
            PlayerWatcher = PlayerWatchFactory.Create(Dalamud.Framework, Dalamud.ClientState, Dalamud.Objects);

            Dalamud.Commands.AddHandler("/glamourer", new CommandInfo(OnGlamourer)
            {
                HelpMessage = "Open or close the Glamourer window.",
            });
            Dalamud.Commands.AddHandler("/glamour", new CommandInfo(OnGlamour)
            {
                HelpMessage = $"Use Glamourer Functions: {HelpString}",
            });

            _interface = new Interface(this);
        }

        public void OnGlamourer(string command, string arguments)
            => _interface.ToggleVisibility();

        private static GameObject? GetActor(string name)
        {
            var lowerName = name.ToLowerInvariant();
            return lowerName switch
            {
                ""          => null,
                "<me>"      => Dalamud.Objects[Interface.GPoseActorId] ?? Dalamud.ClientState.LocalPlayer,
                "self"      => Dalamud.Objects[Interface.GPoseActorId] ?? Dalamud.ClientState.LocalPlayer,
                "<t>"       => Dalamud.Targets.Target,
                "target"    => Dalamud.Targets.Target,
                "<f>"       => Dalamud.Targets.FocusTarget,
                "focus"     => Dalamud.Targets.FocusTarget,
                "<mo>"      => Dalamud.Targets.MouseOverTarget,
                "mouseover" => Dalamud.Targets.MouseOverTarget,
                _ => Dalamud.Objects.LastOrDefault(
                    a => string.Equals(a.Name.ToString(), lowerName, StringComparison.InvariantCultureIgnoreCase)),
            };
        }

        public void CopyToClipboard(Character actor)
        {
            var save = new CharacterSave();
            save.LoadActor(actor);
            ImGui.SetClipboardText(save.ToBase64());
        }

        public void ApplyCommand(Character actor, string target)
        {
            CharacterSave? save = null;
            if (target.ToLowerInvariant() == "clipboard")
                try
                {
                    save = CharacterSave.FromString(ImGui.GetClipboardText());
                }
                catch (Exception)
                {
                    Dalamud.Chat.PrintError("Clipboard does not contain a valid customization string.");
                }
            else if (!Designs.FileSystem.Find(target, out var child) || child is not Design d)
                Dalamud.Chat.PrintError("The given path to a saved design does not exist or does not point to a design.");
            else
                save = d.Data;

            save?.Apply(actor);
            UpdateActors(actor);
        }

        public void SaveCommand(Character actor, string path)
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
                Dalamud.Chat.PrintError("Could not save file:");
                Dalamud.Chat.PrintError($"    {e.Message}");
            }
        }

        public void OnGlamour(string command, string arguments)
        {
            static void PrintHelp()
            {
                Dalamud.Chat.Print("Usage:");
                Dalamud.Chat.Print($"    {HelpString}");
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

            var actor = GetActor(split[1]) as Character;
            if (actor == null)
            {
                Dalamud.Chat.Print($"Could not find actor for {split[1]} or it was not a Character.");
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
                        Dalamud.Chat.Print("Applying requires a name for the save to be applied or 'clipboard'.");
                        return;
                    }

                    ApplyCommand(actor, split[2]);

                    return;
                }
                case "save":
                {
                    if (split.Length < 3)
                    {
                        Dalamud.Chat.Print("Saving requires a name for the save.");
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
            Dalamud.Commands.RemoveHandler("/glamour");
            Dalamud.Commands.RemoveHandler("/glamourer");
        }

        // Update actors without triggering PlayerWatcher Events,
        // then manually redraw using Penumbra.
        public void UpdateActors(Character actor, Character? gPoseOriginalActor = null)
        {
            var newEquip = PlayerWatcher.UpdateActorWithoutEvent(actor);
            Penumbra?.RedrawObject(actor, RedrawType.WithSettings);

            // Special case for carrying over changes to the gPose actor to the regular player actor, too.
            if (gPoseOriginalActor == null)
                return;

            newEquip.Write(gPoseOriginalActor.Address);
            PlayerWatcher.UpdateActorWithoutEvent(gPoseOriginalActor);
            Penumbra?.RedrawObject(gPoseOriginalActor, RedrawType.AfterGPoseWithSettings);
        }
    }
}
