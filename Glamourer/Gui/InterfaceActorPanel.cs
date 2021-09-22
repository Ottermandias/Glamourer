using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Logging;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.FileSystem;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private readonly CharacterSave   _currentSave   = new();
        private          string          _newDesignName = string.Empty;
        private          bool            _keyboardFocus;
        private const    string          DesignNamePopupLabel = "Save Design As...";
        private const    uint            RedHeaderColor       = 0xFF1818C0;
        private const    uint            GreenHeaderColor     = 0xFF18C018;
        private readonly ConstructorInfo _characterConstructor;

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
            ImGui.Button($"{_currentLabel}##playerHeader", -Vector2.UnitX * 0.0001f);
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

        private const int ModelTypeOffset = 0x01B4;

        private static unsafe int ModelType(GameObject actor)
            => *(int*) (actor.Address + ModelTypeOffset);

        private static unsafe void SetModelType(GameObject actor, int value)
            => *(int*) (actor.Address + ModelTypeOffset) = value;

        private Character Character(IntPtr address)
            => (Character) _characterConstructor.Invoke(new object[]
            {
                address,
            });

        private Character? CreateCharacter(GameObject? actor)
        {
            if (actor == null)
                return null;

            return actor switch
            {
                PlayerCharacter p => p,
                BattleChara b     => b,
                _ => actor.ObjectKind switch
                {
                    ObjectKind.BattleNpc => Character(actor.Address),
                    ObjectKind.Companion => Character(actor.Address),
                    ObjectKind.EventNpc  => Character(actor.Address),
                    _                    => null,
                },
            };
        }


        private static Character? TransformToCustomizable(Character? actor)
        {
            if (actor == null)
                return null;

            if (ModelType(actor) == 0)
                return actor;

            SetModelType(actor, 0);
            CharacterCustomization.Default.Write(actor.Address);
            return actor;
        }

        private void DrawApplyToTargetButton(CharacterSave save)
        {
            if (!ImGui.Button("Apply to Target"))
                return;

            var player = TransformToCustomizable(CreateCharacter(Dalamud.Targets.Target));
            if (player == null)
                return;

            var fallBackCharacter = _gPoseActors.TryGetValue(player.Name.ToString(), out var f) ? f : null;
            save.Apply(player);
            if (fallBackCharacter != null)
                save.Apply(fallBackCharacter);
            Glamourer.Penumbra.UpdateCharacters(player, fallBackCharacter);
        }

        private void DrawRevertButton()
        {
            if (!DrawDisableButton("Revert", _player == null))
                return;

            Glamourer.RevertableDesigns.Revert(_player!);
            var fallBackCharacter = _gPoseActors.TryGetValue(_player!.Name.ToString(), out var f) ? f : null;
            if (fallBackCharacter != null)
                Glamourer.RevertableDesigns.Revert(fallBackCharacter);
            Glamourer.Penumbra.UpdateCharacters(_player, fallBackCharacter);
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

        private void DrawMonsterPanel()
        {
            if (DrawApplyClipboardButton())
                Glamourer.Penumbra.UpdateCharacters(_player!);

            ImGui.SameLine();
            if (ImGui.Button("Convert to Character"))
            {
                TransformToCustomizable(_player);
                _currentLabel = _currentLabel.Replace("(Monster)", "(NPC)");
                Glamourer.Penumbra.UpdateCharacters(_player!);
            }

            if (!_inGPose)
            {
                ImGui.SameLine();
                DrawTargetPlayerButton();
            }

            var       currentModel = ModelType(_player!);
            using var raii         = new ImGuiRaii();
            if (!raii.Begin(() => ImGui.BeginCombo("Model Id", currentModel.ToString()), ImGui.EndCombo))
                return;

            foreach (var (id, _) in _models.Skip(1))
            {
                if (!ImGui.Selectable($"{id:D6}##models", id == currentModel) || id == currentModel)
                    continue;

                SetModelType(_player!, (int) id);
                Glamourer.Penumbra.UpdateCharacters(_player!);
            }
        }

        private void DrawPlayerPanel()
        {
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

            ImGui.SameLine();
            DrawRevertButton();

            if (DrawCustomization(ref _currentSave.Customizations) && _player != null)
            {
                Glamourer.RevertableDesigns.Add(_player);
                _currentSave.Customizations.Write(_player.Address);
                changes = true;
            }

            changes |= DrawEquip(_currentSave.Equipment);
            changes |= DrawMiscellaneous(_currentSave, _player);

            if (_player != null && changes)
                Glamourer.Penumbra.UpdateCharacters(_player);
        }

        private void DrawActorPanel()
        {
            using var raii = ImGuiRaii.NewGroup();
            DrawPlayerHeader();
            if (!ImGui.BeginChild("##playerData", -Vector2.One, true))
            {
                ImGui.EndChild();
                return;
            }

            if (_player == null || ModelType(_player) == 0)
                DrawPlayerPanel();
            else
                DrawMonsterPanel();

            ImGui.EndChild();
        }
    }
}
