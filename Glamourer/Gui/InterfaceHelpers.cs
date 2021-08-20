using System;
using System.Linq;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Glamourer.Customization;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        // Push the stain color to type and if it is too bright, turn the text color black.
        // Return number of pushed styles.
        private static int PushColor(Stain stain, ImGuiCol type = ImGuiCol.Button)
        {
            ImGui.PushStyleColor(type, stain.RgbaColor);
            if (stain.Intensity > 127)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF101010);
                return 2;
            }

            return 1;
        }

        // Go through a whole customization struct and fix up all settings that need fixing.
        private static void FixUpAttributes(ref ActorCustomization customization)
        {
            var set = GlamourerPlugin.Customization.GetList(customization.Clan, customization.Gender);
            foreach (CustomizationId id in Enum.GetValues(typeof(CustomizationId)))
            {
                switch (id)
                {
                    case CustomizationId.Race:                  break;
                    case CustomizationId.Clan:                  break;
                    case CustomizationId.BodyType:              break;
                    case CustomizationId.Gender:                break;
                    case CustomizationId.FacialFeaturesTattoos: break;
                    case CustomizationId.HighlightsOnFlag:      break;
                    case CustomizationId.Face:
                        if (customization.Race != Race.Hrothgar)
                            goto default;
                        break;
                    default:
                        var count = set.Count(id);
                        if (set.DataByValue(id, customization[id], out var value) < 0)
                            if (count == 0)
                                customization[id] = 0;
                            else
                                customization[id] = set.Data(id, 0).Value;
                        break;
                }
            }
        }

        // Change a race and fix up all required customizations afterwards.
        private static bool ChangeRace(ref ActorCustomization customization, SubRace clan)
        {
            if (clan == customization.Clan)
                return false;

            var race = clan.ToRace();
            customization.Race = race;
            customization.Clan = clan;

            customization.Gender = race switch
            {
                Race.Hrothgar => Gender.Male,
                Race.Viera    => Gender.Female,
                _             => customization.Gender,
            };

            FixUpAttributes(ref customization);

            return true;
        }

        // Change a gender and fix up all required customizations afterwards.
        private static bool ChangeGender(ref ActorCustomization customization, Gender gender)
        {
            if (gender == customization.Gender)
                return false;

            customization.Gender = gender;
            FixUpAttributes(ref customization);

            return true;
        }

        private static string ClanName(SubRace race, Gender gender)
        {
            if (gender == Gender.Female)
                return race switch
                {
                    SubRace.Midlander       => GlamourerPlugin.Customization.GetName(CustomName.MidlanderM),
                    SubRace.Highlander      => GlamourerPlugin.Customization.GetName(CustomName.HighlanderM),
                    SubRace.Wildwood        => GlamourerPlugin.Customization.GetName(CustomName.WildwoodM),
                    SubRace.Duskwight       => GlamourerPlugin.Customization.GetName(CustomName.DuskwightM),
                    SubRace.Plainsfolk      => GlamourerPlugin.Customization.GetName(CustomName.PlainsfolkM),
                    SubRace.Dunesfolk       => GlamourerPlugin.Customization.GetName(CustomName.DunesfolkM),
                    SubRace.SeekerOfTheSun  => GlamourerPlugin.Customization.GetName(CustomName.SeekerOfTheSunM),
                    SubRace.KeeperOfTheMoon => GlamourerPlugin.Customization.GetName(CustomName.KeeperOfTheMoonM),
                    SubRace.Seawolf         => GlamourerPlugin.Customization.GetName(CustomName.SeawolfM),
                    SubRace.Hellsguard      => GlamourerPlugin.Customization.GetName(CustomName.HellsguardM),
                    SubRace.Raen            => GlamourerPlugin.Customization.GetName(CustomName.RaenM),
                    SubRace.Xaela           => GlamourerPlugin.Customization.GetName(CustomName.XaelaM),
                    SubRace.Helion          => GlamourerPlugin.Customization.GetName(CustomName.HelionM),
                    SubRace.Lost            => GlamourerPlugin.Customization.GetName(CustomName.LostM),
                    SubRace.Rava            => GlamourerPlugin.Customization.GetName(CustomName.RavaF),
                    SubRace.Veena           => GlamourerPlugin.Customization.GetName(CustomName.VeenaF),
                    _                       => throw new ArgumentOutOfRangeException(nameof(race), race, null),
                };

            return race switch
            {
                SubRace.Midlander       => GlamourerPlugin.Customization.GetName(CustomName.MidlanderF),
                SubRace.Highlander      => GlamourerPlugin.Customization.GetName(CustomName.HighlanderF),
                SubRace.Wildwood        => GlamourerPlugin.Customization.GetName(CustomName.WildwoodF),
                SubRace.Duskwight       => GlamourerPlugin.Customization.GetName(CustomName.DuskwightF),
                SubRace.Plainsfolk      => GlamourerPlugin.Customization.GetName(CustomName.PlainsfolkF),
                SubRace.Dunesfolk       => GlamourerPlugin.Customization.GetName(CustomName.DunesfolkF),
                SubRace.SeekerOfTheSun  => GlamourerPlugin.Customization.GetName(CustomName.SeekerOfTheSunF),
                SubRace.KeeperOfTheMoon => GlamourerPlugin.Customization.GetName(CustomName.KeeperOfTheMoonF),
                SubRace.Seawolf         => GlamourerPlugin.Customization.GetName(CustomName.SeawolfF),
                SubRace.Hellsguard      => GlamourerPlugin.Customization.GetName(CustomName.HellsguardF),
                SubRace.Raen            => GlamourerPlugin.Customization.GetName(CustomName.RaenF),
                SubRace.Xaela           => GlamourerPlugin.Customization.GetName(CustomName.XaelaF),
                SubRace.Helion          => GlamourerPlugin.Customization.GetName(CustomName.HelionM),
                SubRace.Lost            => GlamourerPlugin.Customization.GetName(CustomName.LostM),
                SubRace.Rava            => GlamourerPlugin.Customization.GetName(CustomName.RavaF),
                SubRace.Veena           => GlamourerPlugin.Customization.GetName(CustomName.VeenaF),
                _                       => throw new ArgumentOutOfRangeException(nameof(race), race, null),
            };
        }

        private enum DesignNameUse
        {
            SaveCurrent,
            NewDesign,
            DuplicateDesign,
            NewFolder,
            FromClipboard,
        }

        private void DrawDesignNamePopup(DesignNameUse use)
        {
            if (ImGui.BeginPopup($"{DesignNamePopupLabel}{use}"))
            {
                if (ImGui.InputText("##designName", ref _newDesignName, 64, ImGuiInputTextFlags.EnterReturnsTrue)
                 && _newDesignName.Any())
                {
                    switch (use)
                    {
                        case DesignNameUse.SaveCurrent:
                            SaveNewDesign(_currentSave);
                            break;
                        case DesignNameUse.NewDesign:
                            var empty = new CharacterSave();
                            empty.Load(ActorCustomization.Default);
                            empty.WriteCustomizations = false;
                            SaveNewDesign(empty);
                            break;
                        case DesignNameUse.DuplicateDesign:
                            SaveNewDesign(_selection!.Data.Copy());
                            break;
                        case DesignNameUse.NewFolder:
                            _designs.FileSystem.CreateAllFolders($"{_newDesignName}/a"); // Filename is just ignored, but all folders are created.
                            break;
                        case DesignNameUse.FromClipboard:
                            try
                            {
                                var text = Clipboard.GetText();
                                var save = CharacterSave.FromString(text);
                                SaveNewDesign(save);
                            }
                            catch (Exception e)
                            {
                                PluginLog.Information($"Could not save new Design from Clipboard:\n{e}");
                            }

                            break;
                    }

                    _newDesignName = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                if (_keyboardFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _keyboardFocus = false;
                }

                ImGui.EndPopup();
            }
        }

        private void OpenDesignNamePopup(DesignNameUse use)
        {
            _newDesignName = string.Empty;
            _keyboardFocus = true;
            ImGui.OpenPopup($"{DesignNamePopupLabel}{use}");
        }
    }
}
