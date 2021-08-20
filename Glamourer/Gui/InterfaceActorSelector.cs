using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Interface;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private          Actor?                     _player;
        private          string                     _currentActorName = string.Empty;
        private          string                     _actorFilter      = string.Empty;
        private          string                     _actorFilterLower = string.Empty;
        private readonly Dictionary<string, Actor?> _playerNames      = new(400);

        private void DrawActorFilter()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(SelectorWidth * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputTextWithHint("##actorFilter", "Filter Players...", ref _actorFilter, 32))
                _actorFilterLower = _actorFilter.ToLowerInvariant();
        }

        private void DrawActorSelectable(Actor actor, bool gPose)
        {
            var actorName = actor.Name;
            if (!actorName.Any())
                return;

            if (_playerNames.ContainsKey(actorName))
            {
                _playerNames[actorName] = actor;
                return;
            }

            _playerNames.Add(actorName, null);

            var label = gPose ? $"{actorName} (GPose)" : actorName;
            if (!_actorFilterLower.Any() || actorName.ToLowerInvariant().Contains(_actorFilterLower))
                if (ImGui.Selectable(label, _currentActorName == actorName))
                {
                    _currentActorName = actorName;
                    _currentSave.LoadActor(actor);
                    _player = actor;
                    return;
                }

            if (_currentActorName == actor.Name)
            {
                _currentSave.LoadActor(actor);
                _player = actor;
            }
        }

        private void DrawSelectionButtons()
        {
            using var raii = new ImGuiRaii()
                .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                .PushStyle(ImGuiStyleVar.FrameRounding, 0)
                .PushFont(UiBuilder.IconFont);
            Actor? select      = null;
            var    buttonWidth = Vector2.UnitX * SelectorWidth / 2;
            if (ImGui.Button(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth))
                select = GlamourerPlugin.PluginInterface.ClientState.LocalPlayer;
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
                    select = GlamourerPlugin.PluginInterface.ClientState.Targets.CurrentTarget;
            }

            raii.PopFonts();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the current target, if it is a player actor.");

            if (select == null || select.ObjectKind != ObjectKind.Player)
                return;

            _player           = select;
            _currentActorName = _player.Name;
            _currentSave.LoadActor(_player);
        }

        private void DrawActorSelector()
        {
            ImGui.BeginGroup();
            DrawActorFilter();
            if (!ImGui.BeginChild("##actorSelector",
                new Vector2(SelectorWidth * ImGui.GetIO().FontGlobalScale, -ImGui.GetFrameHeight() - 1), true))
                return;

            _playerNames.Clear();
            for (var i = GPoseActorId; i < GPoseActorId + 48; ++i)
            {
                var actor = _actors[i];
                if (actor == null)
                    break;

                if (actor.ObjectKind == ObjectKind.Player)
                    DrawActorSelectable(actor, true);
            }

            for (var i = 0; i < GPoseActorId; i += 2)
            {
                var actor = _actors[i];
                if (actor != null && actor.ObjectKind == ObjectKind.Player)
                    DrawActorSelectable(actor, false);
            }


            using (var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            {
                ImGui.EndChild();
            }

            DrawSelectionButtons();
            ImGui.EndGroup();
        }

        private void DrawActorTab()
        {
            using var raii = new ImGuiRaii();
            if (!raii.Begin(() => ImGui.BeginTabItem("Current Players"), ImGui.EndTabItem))
                return;

            _player = null;
            DrawActorSelector();

            if (!_currentActorName.Any())
                return;

            ImGui.SameLine();
            DrawActorPanel();
        }
    }
}
