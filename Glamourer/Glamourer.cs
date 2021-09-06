using System;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.FileSystem;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.PlayerWatch;

namespace Glamourer
{
    public class Glamourer : IDalamudPlugin
    {
        private const string HelpString = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";

        public string Name
            => "Glamourer";

        public static    GlamourerConfig       Config        = null!;
        public static    IPlayerWatcher        PlayerWatcher = null!;
        public static    ICustomizationManager Customization = null!;
        private readonly Interface             _interface;
        public readonly  DesignManager         Designs;
        public readonly  FixedDesigns          FixedDesigns;


        public static string         Version  = string.Empty;
        public static PenumbraAttach Penumbra = null!;

        public Glamourer(DalamudPluginInterface pluginInterface)
        {
            Dalamud.Initialize(pluginInterface);
            Version       = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Config        = GlamourerConfig.Load();
            Customization = CustomizationManager.Create(Dalamud.PluginInterface, Dalamud.GameData, Dalamud.ClientState.ClientLanguage);
            Designs       = new DesignManager();
            Penumbra      = new PenumbraAttach(Config.AttachToPenumbra);
            PlayerWatcher = PlayerWatchFactory.Create(Dalamud.Framework, Dalamud.ClientState, Dalamud.Objects);
            FixedDesigns  = new FixedDesigns(Designs);
            
            if (Config.ApplyFixedDesigns)
                PlayerWatcher.Enable();

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

        private static GameObject? GetPlayer(string name)
        {
            var lowerName = name.ToLowerInvariant();
            return lowerName switch
            {
                ""          => null,
                "<me>"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
                "self"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
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

        public void CopyToClipboard(Character player)
        {
            var save = new CharacterSave();
            save.LoadCharacter(player);
            ImGui.SetClipboardText(save.ToBase64());
        }

        public void ApplyCommand(Character player, string target)
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

            save?.Apply(player);
            Penumbra.UpdateCharacters(player);
        }

        public void SaveCommand(Character player, string path)
        {
            var save = new CharacterSave();
            save.LoadCharacter(player);
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

            var player = GetPlayer(split[1]) as Character;
            if (player == null)
            {
                Dalamud.Chat.Print($"Could not find object for {split[1]} or it was not a Character.");
                return;
            }

            switch (split[0].ToLowerInvariant())
            {
                case "copy":
                    CopyToClipboard(player);
                    return;
                case "apply":
                {
                    if (split.Length < 3)
                    {
                        Dalamud.Chat.Print("Applying requires a name for the save to be applied or 'clipboard'.");
                        return;
                    }

                    ApplyCommand(player, split[2]);

                    return;
                }
                case "save":
                {
                    if (split.Length < 3)
                    {
                        Dalamud.Chat.Print("Saving requires a name for the save.");
                        return;
                    }

                    SaveCommand(player, split[2]);
                    return;
                }
                default:
                    PrintHelp();
                    return;
            }
        }

        public void Dispose()
        {
            FixedDesigns.Dispose();
            Penumbra.Dispose();
            PlayerWatcher.Dispose();
            _interface.Dispose();
            Dalamud.Commands.RemoveHandler("/glamour");
            Dalamud.Commands.RemoveHandler("/glamourer");
        }
    }
}
