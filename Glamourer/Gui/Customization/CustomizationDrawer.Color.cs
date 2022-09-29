using System.Numerics;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
    private const string ColorPickerPopupName = "ColorPicker";

    private void DrawColorPicker(CustomizationId id)
    {
        using var _ = SetId(id);
        var (current, custom) = GetCurrentCustomization(id);
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
            var custom = _set.Data(_currentId, i, _customize[CustomizationId.Face]);
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
    private (int, CustomizationData) GetCurrentCustomization(CustomizationId id)
    {
        var current = _set.DataByValue(id, _customize[id], out var custom);
        if (!_set.IsAvailable(id) || current >= 0)
            return (current, custom!.Value);

        Glamourer.Log.Warning($"Read invalid customization value {_customize[id]} for {id}.");
        return (0, _set.Data(id, 0));
    }
}
