using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private readonly CharacterSave _currentSave   = new();
        private          string        _newDesignName = string.Empty;
        private          bool          _keyboardFocus;
        private const    string        DesignNamePopupLabel = "Save Design As...";
        private const    uint          RedHeaderColor       = 0xFF1818C0;
        private const    uint          GreenHeaderColor     = 0xFF18C018;

        private void DrawPlayerHeader()
        {
            var color       = _player == null ? RedHeaderColor : GreenHeaderColor;
            var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
            using var raii = new ImGuiRaii()
                .PushColor(ImGuiCol.Text,          color)
                .PushColor(ImGuiCol.Button,        buttonColor)
                .PushColor(ImGuiCol.ButtonHovered, buttonColor)
                .PushColor(ImGuiCol.ButtonActive,  buttonColor)
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"{_currentPlayerName}##playerHeader", -Vector2.UnitX * 0.0001f);
        }

        private static void DrawCopyClipboardButton(CharacterSave save)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Clipboard.ToIconString()))
                ImGui.SetClipboardText(save.ToBase64());
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy customization code to clipboard.");
        }

        private bool DrawApplyClipboardButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var applyButton = ImGui.Button(FontAwesomeIcon.Paste.ToIconString()) && _player != null;
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Apply customization code from clipboard.");

            if (!applyButton)
                return false;

            var text = ImGui.GetClipboardText();
            if (!text.Any())
                return false;

            try
            {
                var save = CharacterSave.FromString(text);
                save.Apply(_player!);
            }
            catch (Exception e)
            {
                PluginLog.Information($"{e}");
                return false;
            }

            return true;
        }

        private void DrawSaveDesignButton()
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Save.ToIconString()))
                OpenDesignNamePopup(DesignNameUse.SaveCurrent);

            ImGui.PopFont();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Save the current design.");

            DrawDesignNamePopup(DesignNameUse.SaveCurrent);
        }

        private void DrawTargetPlayerButton()
        {
            if (ImGui.Button("Target Player"))
                Dalamud.Targets.SetTarget(_player);
        }

        private void DrawApplyToPlayerButton(CharacterSave save)
        {
            if (!ImGui.Button("Apply to Self"))
                return;

            var player = _inGPose
                ? (Character?) Dalamud.Objects[GPoseObjectId]
                : Dalamud.ClientState.LocalPlayer;
            var fallback = _inGPose ? Dalamud.ClientState.LocalPlayer : null;
            if (player == null)
                return;

            save.Apply(player);
            if (_inGPose)
                save.Apply(fallback!);
            Glamourer.Penumbra.UpdateCharacters(player, fallback);
        }

        private void DrawApplyToTargetButton(CharacterSave save)
        {
            if (!ImGui.Button("Apply to Target"))
                return;

            var player = Dalamud.Targets.Target as Character;
            if (player == null)
                return;

            var fallBackCharacter = _playerNames[player.Name.ToString()];
            save.Apply(player);
            if (fallBackCharacter != null)
                save.Apply(fallBackCharacter);
            Glamourer.Penumbra.UpdateCharacters(player, fallBackCharacter);
        }

        private void SaveNewDesign(CharacterSave save)
        {
            try
            {
                var (folder, name) = _designs.FileSystem.CreateAllFolders(_newDesignName);
                if (!name.Any())
                    return;

                var newDesign = new Design(folder, name) { Data = save };
                folder.AddChild(newDesign);
                _designs.Designs[newDesign.FullName()] = save;
                _designs.SaveToFile();
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not save new design {_newDesignName}:\n{e}");
            }
        }

        private void DrawPlayerPanel()
        {
            ImGui.BeginGroup();
            DrawPlayerHeader();
            if (!ImGui.BeginChild("##playerData", -Vector2.One, true))
                return;

            DrawCopyClipboardButton(_currentSave);
            ImGui.SameLine();
            var changes = DrawApplyClipboardButton();
            ImGui.SameLine();
            DrawSaveDesignButton();
            ImGui.SameLine();
            DrawApplyToPlayerButton(_currentSave);
            if (!_inGPose)
            {
                ImGui.SameLine();
                DrawApplyToTargetButton(_currentSave);
                if (_player != null)
                {
                    ImGui.SameLine();
                    DrawTargetPlayerButton();
                }
            }


            if (DrawCustomization(ref _currentSave.Customizations) && _player != null)
            {
                _currentSave.Customizations.Write(_player.Address);
                changes = true;
            }

            changes |= DrawEquip(_currentSave.Equipment);
            changes |= DrawMiscellaneous(_currentSave, _player);

            if (_player != null && changes)
                Glamourer.Penumbra.UpdateCharacters(_player);
            ImGui.EndChild();
            ImGui.EndGroup();
        }
    }
}
