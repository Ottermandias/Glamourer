using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private          Character?                     _player;
        private          string                         _currentLabel      = string.Empty;
        private          string                         _playerFilter      = string.Empty;
        private          string                         _playerFilterLower = string.Empty;
        private readonly Dictionary<string, int>        _playerNames       = new(100);
        private readonly Dictionary<string, Character?> _gPoseActors       = new(48);

        private void DrawPlayerFilter()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(SelectorWidth * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputTextWithHint("##playerFilter", "Filter Players...", ref _playerFilter, 32))
                _playerFilterLower = _playerFilter.ToLowerInvariant();
        }

        private void DrawGPoseSelectable(Character player)
        {
            var playerName = player.Name.ToString();
            if (!playerName.Any())
                return;

            _gPoseActors[playerName] = null;

            DrawSelectable(player, $"{playerName} (GPose)");
        }

        private static string GetLabel(Character player, string playerName, int num)
        {
            if (player.ObjectKind == ObjectKind.Player)
                return num == 1 ? playerName : $"{playerName} #{num}";

            if (ModelType(player) == 0)
                return num == 1 ? $"{playerName} (NPC)" : $"{playerName} #{num} (NPC)";

            return num == 1 ? $"{playerName} (Monster)" : $"{playerName} #{num} (Monster)";
        }

        private void DrawPlayerSelectable(Character player)
        {
            var playerName = player.Name.ToString();
            if (!playerName.Any())
                return;

            if (_playerNames.TryGetValue(playerName, out var num))
                _playerNames[playerName] = ++num;
            else
                _playerNames[playerName] = num = 1;

            if (_gPoseActors.ContainsKey(playerName))
            {
                _gPoseActors[playerName] = player;
                return;
            }

            var label = GetLabel(player, playerName, num);
            DrawSelectable(player, label);
        }


        private void DrawSelectable(Character player, string label)
        {
            if (!_playerFilterLower.Any() || label.ToLowerInvariant().Contains(_playerFilterLower))
                if (ImGui.Selectable(label, _currentLabel == label))
                {
                    _currentLabel = label;
                    _currentSave.LoadCharacter(player);
                    _player = player;
                    return;
                }

            if (_currentLabel != label)
                return;

            _currentSave.LoadCharacter(player);
            _player = player;
        }

        private void DrawSelectionButtons()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0)
                .PushFont(UiBuilder.IconFont);
            Character? select      = null;
            var        buttonWidth = Vector2.UnitX * SelectorWidth / 2;
            if (ImGui.Button(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth))
                select = Dalamud.ClientState.LocalPlayer;
            raii.PopFonts();
            ImGuiCustom.HoverTooltip("Select the local player character.");
            ImGui.SameLine();
            raii.PushFont(UiBuilder.IconFont);
            if (_inGPose)
            {
                raii.PushStyle(ImGuiStyleVar.Alpha, 0.5f);
                ImGui.Button(FontAwesomeIcon.HandPointer.ToIconString(), buttonWidth);
                raii.PopStyles();
            }
            else
            {
                if (ImGui.Button(FontAwesomeIcon.HandPointer.ToIconString(), buttonWidth))
                    select = CreateCharacter(Dalamud.Targets.Target);
            }

            raii.PopFonts();
            ImGuiCustom.HoverTooltip("Select the current target, if it is in the list.");

            if (select == null)
                return;

            _player       = select;
            _currentLabel = _player.Name.ToString();
            _currentSave.LoadCharacter(_player);
        }

        private void DrawPlayerSelector()
        {
            ImGui.BeginGroup();
            DrawPlayerFilter();
            if (!ImGui.BeginChild("##playerSelector",
                new Vector2(SelectorWidth * ImGui.GetIO().FontGlobalScale, -ImGui.GetFrameHeight() - 1), true))
            {
                ImGui.EndChild();
                ImGui.EndGroup();
                return;
            }

            _playerNames.Clear();
            _gPoseActors.Clear();
            for (var i = GPoseObjectId; i < GPoseObjectId + 48; ++i)
            {
                var player = CreateCharacter(Dalamud.Objects[i]);
                if (player == null)
                    break;

                DrawGPoseSelectable(player);
            }

            for (var i = 0; i < GPoseObjectId; ++i)
            {
                var player = CreateCharacter(Dalamud.Objects[i])!;
                if (player != null)
                    DrawPlayerSelectable(player);
            }

            for (var i = GPoseObjectId + 48; i < Dalamud.Objects.Length; ++i)
            {
                var player = CreateCharacter(Dalamud.Objects[i])!;
                if (player != null)
                    DrawPlayerSelectable(player);
            }


            using (var _ = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            {
                ImGui.EndChild();
            }

            DrawSelectionButtons();
            ImGui.EndGroup();
        }

        private void DrawPlayerTab()
        {
            using var raii = new ImGuiRaii();
            _player = null;
            if (!raii.Begin(() => ImGui.BeginTabItem("Current Players"), ImGui.EndTabItem))
                return;

            DrawPlayerSelector();

            if (!_currentLabel.Any())
                return;

            ImGui.SameLine();
            DrawActorPanel();
        }
    }
}
