using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private string?        _currentRevertableName;
        private CharacterSave? _currentRevertable;

        private void DrawRevertablesSelector()
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

            foreach (var (name, save) in Glamourer.RevertableDesigns.Saves)
            {
                if (name.ToLowerInvariant().Contains(_playerFilterLower) && ImGui.Selectable(name, name == _currentRevertableName))
                {
                    _currentRevertableName = name;
                    _currentRevertable     = save;
                }
            }

            using (var _ = new ImGuiRaii().PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            {
                ImGui.EndChild();
            }

            DrawSelectionButtons();
            ImGui.EndGroup();
        }

        private void DrawRevertablePanel()
        {
            using var group       = ImGuiRaii.NewGroup();
            {
                var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
                using var raii = new ImGuiRaii()
                    .PushColor(ImGuiCol.Text,          GreenHeaderColor)
                    .PushColor(ImGuiCol.Button,        buttonColor)
                    .PushColor(ImGuiCol.ButtonHovered, buttonColor)
                    .PushColor(ImGuiCol.ButtonActive,  buttonColor)
                    .PushStyle(ImGuiStyleVar.ItemSpacing,   Vector2.Zero)
                    .PushStyle(ImGuiStyleVar.FrameRounding, 0);
                ImGui.Button($"{_currentRevertableName}##playerHeader", -Vector2.UnitX * 0.0001f);
            }

            if (!ImGui.BeginChild("##revertableData", -Vector2.One, true))
            {
                ImGui.EndChild();
                return;
            }

            var save = _currentRevertable!.Copy();
            DrawCustomization(ref save.Customizations);
            DrawEquip(save.Equipment);
            DrawMiscellaneous(save, null);

            ImGui.EndChild();
        }

        [Conditional("DEBUG")]
        private void DrawRevertablesTab()
        {
            using var raii = new ImGuiRaii();
            if (!raii.Begin(() => ImGui.BeginTabItem("Revertables"), ImGui.EndTabItem))
                return;

            DrawRevertablesSelector();

            if (_currentRevertableName == null)
                return;

            ImGui.SameLine();
            DrawRevertablePanel();
        }
    }
}
