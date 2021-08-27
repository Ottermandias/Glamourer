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
        private          Character?                 _player;
        private          string                     _currentPlayerName = string.Empty;
        private          string                     _playerFilter      = string.Empty;
        private          string                     _playerFilterLower = string.Empty;
        private readonly Dictionary<string, Character?> _playerNames      = new(400);

        private void DrawPlayerFilter()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(SelectorWidth * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputTextWithHint("##playerFilter", "Filter Players...", ref _playerFilter, 32))
                _playerFilterLower = _playerFilter.ToLowerInvariant();
        }

        private void DrawPlayerSelectable(Character player, bool gPose)
        {
            var playerName = player.Name.ToString();
            if (!playerName.Any())
                return;

            if (_playerNames.ContainsKey(playerName))
            {
                _playerNames[playerName] = player;
                return;
            }

            _playerNames.Add(playerName, null);

            var label = gPose ? $"{playerName} (GPose)" : playerName;
            if (!_playerFilterLower.Any() || playerName.ToLowerInvariant().Contains(_playerFilterLower))
                if (ImGui.Selectable(label, _currentPlayerName == playerName))
                {
                    _currentPlayerName = playerName;
                    _currentSave.LoadCharacter(player);
                    _player = player;
                    return;
                }

            if (_currentPlayerName == playerName)
            {
                _currentSave.LoadCharacter(player);
                _player = player;
            }
        }

        private void DrawSelectionButtons()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0)
                .PushFont(UiBuilder.IconFont);
            Character? select      = null;
            var    buttonWidth = Vector2.UnitX * SelectorWidth / 2;
            if (ImGui.Button(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth))
                select = Dalamud.ClientState.LocalPlayer;
            raii.PopFonts();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the local player character.");
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
                    select = Dalamud.Targets.Target as Character;
            }

            raii.PopFonts();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the current target, if it is a player object.");

            if (select == null || select.ObjectKind != ObjectKind.Player)
                return;

            _player           = select;
            _currentPlayerName = _player.Name.ToString();
            _currentSave.LoadCharacter(_player);
        }

        private void DrawPlayerSelector()
        {
            ImGui.BeginGroup();
            DrawPlayerFilter();
            if (!ImGui.BeginChild("##playerSelector",
                new Vector2(SelectorWidth * ImGui.GetIO().FontGlobalScale, -ImGui.GetFrameHeight() - 1), true))
                return;

            _playerNames.Clear();
            for (var i = GPoseObjectId; i < GPoseObjectId + 48; ++i)
            {
                var player = Dalamud.Objects[i] as Character;
                if (player == null)
                    break;

                if (player.ObjectKind == ObjectKind.Player)
                    DrawPlayerSelectable(player, true);
            }

            for (var i = 0; i < GPoseObjectId; i += 2)
            {
                var player = Dalamud.Objects[i] as Character;
                if (player != null && player.ObjectKind == ObjectKind.Player)
                    DrawPlayerSelectable(player, false);
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
            if (!raii.Begin(() => ImGui.BeginTabItem("Current Players"), ImGui.EndTabItem))
                return;

            _player = null;
            DrawPlayerSelector();

            if (!_currentPlayerName.Any())
                return;

            ImGui.SameLine();
            DrawPlayerPanel();
        }
    }
}
