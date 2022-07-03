using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui;

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

        using (var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
        {
            ImGui.EndChild();
        }

        DrawSelectionButtons();
        ImGui.EndGroup();
    }

    private void DrawRevertablePanel()
    {
        using var group = ImRaii.Group();
        {
            var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
            using var color = ImRaii.PushColor(ImGuiCol.Text, GreenHeaderColor)
                .Push(ImGuiCol.Button,        buttonColor)
                .Push(ImGuiCol.ButtonHovered, buttonColor)
                .Push(ImGuiCol.ButtonActive,  buttonColor);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
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
        using var tabItem = ImRaii.TabItem("Revertables");
        if (!tabItem)
            return;

        DrawRevertablesSelector();

        if (_currentRevertableName == null)
            return;

        ImGui.SameLine();
        DrawRevertablePanel();
    }
}
