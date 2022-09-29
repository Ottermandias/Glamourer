using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Glamourer.Gui.Customization;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class DebugStateTab
    {
        private readonly CurrentManipulations _currentManipulations;

        private LowerString       _manipulationFilter = LowerString.Empty;
        private Actor.IIdentifier _selection          = Actor.IIdentifier.Invalid;
        private CurrentDesign?    _save               = null;
        private bool              _delete             = false;

        public DebugStateTab(CurrentManipulations currentManipulations)
            => _currentManipulations = currentManipulations;

        [Conditional("DEBUG")]
        public void Draw()
        {
            using var tab = ImRaii.TabItem("Current Manipulations");
            if (!tab)
                return;

            DrawManipulationSelector();
            if (_save == null)
                return;

            ImGui.SameLine();
            DrawActorPanel();
            if (_delete)
            {
                _delete = false;
                _currentManipulations.DeleteSave(_selection);
                _selection = Actor.IIdentifier.Invalid;
            }
        }

        private void DrawSelector(Vector2 oldSpacing)
        {
            using var child = ImRaii.Child("##actorSelector", new Vector2(_actorSelectorWidth, -1), true);
            if (!child)
                return;

            using var style     = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, oldSpacing);
            var       skips     = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeight());
            var       remainder = ImGuiClip.FilteredClippedDraw(_currentManipulations, skips, CheckFilter, DrawSelectable);
            ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
        }

        private void DrawManipulationSelector()
        {
            using var group      = ImRaii.Group();
            var       oldSpacing = ImGui.GetStyle().ItemSpacing;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(_actorSelectorWidth);
            LowerString.InputWithHint("##actorFilter", "Filter...", ref _manipulationFilter, 64);

            _save = null;
            DrawSelector(oldSpacing);
        }

        private bool CheckFilter(KeyValuePair<Actor.IIdentifier, CurrentDesign> data)
        {
            if (data.Key.Equals(_selection))
                _save = data.Value;
            return _manipulationFilter.Length == 0 || _manipulationFilter.IsContained(data.Key.ToString()!);
        }

        private void DrawSelectable(KeyValuePair<Actor.IIdentifier, CurrentDesign> data)
        {
            var equal = data.Key.Equals(_selection);
            if (ImGui.Selectable(data.Key.ToString(), equal))
            {
                _selection = data.Key;
                _save      = data.Value;
            }
        }

        private void DrawActorPanel()
        {
            using var group = ImRaii.Group();
            if (ImGui.Button("Delete"))
                _delete = true;
            CustomizationDrawer.Draw(_save!.Data.Customize, _save.Data.Equipment, Array.Empty<Actor>(), false);
        }
    }
}
