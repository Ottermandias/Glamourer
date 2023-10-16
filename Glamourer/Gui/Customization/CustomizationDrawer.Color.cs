using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private const string ColorPickerPopupName = "ColorPicker";

    private void DrawColorPicker(CustomizeIndex index)
    {
        using var _ = SetId(index);
        var (current, custom) = GetCurrentCustomization(index);

        var color = ImGui.ColorConvertU32ToFloat4(current < 0 ? ImGui.GetColorU32(ImGuiCol.FrameBg) : custom.Color);

        using (var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, current < 0))
        {
            if (ImGui.ColorButton($"{_customize[index].Value}##color", color, ImGuiColorEditFlags.None, _framedIconSize))
                ImGui.OpenPopup(ColorPickerPopupName);
        }

        var npc = false;
        if (current < 0)
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            var       size = ImGui.CalcTextSize(FontAwesomeIcon.Question.ToIconString());
            var       pos  = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() - size) / 2;
            ImGui.GetWindowDrawList().AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Question.ToIconString());
            current = 0;
            npc     = true;
        }

        ImGui.SameLine();

        using (var group = ImRaii.Group())
        {
            DataInputInt(current, npc);
            if (_withApply)
            {
                ApplyCheckbox();
                ImGui.SameLine();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(custom.Color == 0 ? $"{_currentOption} (NPC)" : _currentOption);
        }

        DrawColorPickerPopup(current);
    }

    private void DrawColorPickerPopup(int current)
    {
        using var popup = ImRaii.Popup(ColorPickerPopupName, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize[CustomizeIndex.Face]);
            if (ImGui.ColorButton(custom.Value.ToString(), ImGui.ColorConvertU32ToFloat4(custom.Color)))
            {
                UpdateValue(custom.Value);
                ImGui.CloseCurrentPopup();
            }

            if (i == current)
            {
                var size = ImGui.GetItemRectSize();
                ImGui.GetWindowDrawList()
                    .AddCircleFilled(ImGui.GetItemRectMin() + size / 2, size.X / 4, ImGuiUtil.ContrastColorBW(custom.Color));
            }

            if (i % 8 != 7)
                ImGui.SameLine();
        }
    }

    // Obtain the current customization and print a warning if it is not known.
    private (int, CustomizeData) GetCurrentCustomization(CustomizeIndex index)
    {
        var current = _set.DataByValue(index, _customize[index], out var custom, _customize.Face);
        if (_set.IsAvailable(index) && current < 0)
            return (current, new CustomizeData(index, _customize[index], 0, 0));

        return (current, custom!.Value);
    }
}
