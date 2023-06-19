using System;
using System.Numerics;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private const string ColorPickerPopupName = "ColorPicker";

    private void DrawColorPicker(CustomizeIndex index)
    {
        using var _ = SetId(index);
        var (current, custom) = GetCurrentCustomization(index);
        var color = ImGui.ColorConvertU32ToFloat4(custom.Color);

        // Print 1-based index instead of 0.
        if (ImGui.ColorButton($"{current + 1}##color", color, ImGuiColorEditFlags.None, _framedIconSize))
            ImGui.OpenPopup(ColorPickerPopupName);

        ImGui.SameLine();

        using (var group = ImRaii.Group())
        {
            DataInputInt(current);
            ImGui.TextUnformatted(_currentOption);
        }
        DrawColorPickerPopup();
    }

    private void DrawColorPickerPopup()
    {
        using var popup = ImRaii.Popup(ColorPickerPopupName, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize[CustomizeIndex.Face]);
            if (ImGui.ColorButton((i + 1).ToString(), ImGui.ColorConvertU32ToFloat4(custom.Color)))
            {
                UpdateValue(custom.Value);
                ImGui.CloseCurrentPopup();
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
            throw new Exception($"Read invalid customization value {_customize[index]} for {index}.");

        return (current, custom!.Value);
    }
}