using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Dalamud.Interface;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private readonly CharacterSave _currentSave         = new();
        private          string        _newDesignName       = string.Empty;
        private          bool          _keyboardFocus       = false;
        private const    string        DesignNamePopupLabel = "Save Design As...";
        private const    uint          RedHeaderColor       = 0xFF1818C0;
        private const    uint          GreenHeaderColor     = 0xFF18C018;

        private void DrawActorHeader()
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
            ImGui.Button($"{_currentActorName}##actorHeader", -Vector2.UnitX * 0.0001f);
        }

        private static void DrawCopyClipboardButton(CharacterSave save)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Clipboard.ToIconString()))
                Clipboard.SetText(save.ToBase64());
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

            var text = Clipboard.GetText();
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
                GlamourerPlugin.PluginInterface.ClientState.Targets.SetCurrentTarget(_player);
        }

        private void DrawApplyToPlayerButton(CharacterSave save)
        {
            if (ImGui.Button("Apply to Self"))
            {
                var player = _inGPose
                    ? GlamourerPlugin.PluginInterface.ClientState.Actors[GPoseActorId]
                    : GlamourerPlugin.PluginInterface.ClientState.LocalPlayer;
                var fallback = _inGPose ? GlamourerPlugin.PluginInterface.ClientState.LocalPlayer : null;
                if (player != null)
                {
                    save.Apply(player);
                    if (_inGPose)
                        save.Apply(fallback!);
                    _plugin.UpdateActors(player, fallback);
                }
            }
        }

        private void DrawApplyToTargetButton(CharacterSave save)
        {
            if (ImGui.Button("Apply to Target"))
            {
                var player = GlamourerPlugin.PluginInterface.ClientState.Targets.CurrentTarget;
                if (player != null)
                {
                    var fallBackActor = _playerNames[player.Name];
                    save.Apply(player);
                    if (fallBackActor != null)
                        save.Apply(fallBackActor);
                    _plugin.UpdateActors(player, fallBackActor);
                }
            }
        }

        private void SaveNewDesign(CharacterSave save)
        {
            try
            {
                var (folder, name) = _designs.FileSystem.CreateAllFolders(_newDesignName);
                if (name.Any())
                {
                    var newDesign = new Design(folder, name) { Data = save };
                    folder.AddChild(newDesign);
                    _designs.Designs[newDesign.FullName()] = save;
                    _designs.SaveToFile();
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not save new design {_newDesignName}:\n{e}");
            }
        }

        private void DrawActorPanel()
        {
            ImGui.BeginGroup();
            DrawActorHeader();
            if (!ImGui.BeginChild("##actorData", -Vector2.One, true))
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
                _plugin.UpdateActors(_player);
            ImGui.EndChild();
            ImGui.EndGroup();
        }
    }
}
