using System;
using System.Linq;
using System.Numerics;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
    private const string IconSelectorPopup = "Style Picker";

    private void DrawIconSelector(CustomizationId id)
    {
        using var _        = SetId(id);
        using var bigGroup = ImRaii.Group();
        var       label    = _currentOption;

        var current = _set.DataByValue(id, _currentByte, out var custom);
        if (current < 0)
        {
            label   = $"{_currentOption} (Custom #{_customize[id]})";
            current = 0;
            custom  = _set.Data(id, 0);
        }

        var icon = Glamourer.Customization.GetIcon(custom!.Value.IconId);
        if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
            ImGui.OpenPopup(IconSelectorPopup);
        ImGuiUtil.HoverIconTooltip(icon, _iconSize);

        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            if (_currentId == CustomizationId.Face)
                FaceInputInt(current);
            else
                DataInputInt(current);
            ImGui.TextUnformatted($"{label} ({custom.Value.Value})");
        }

        DrawIconPickerPopup();
    }

    private void UpdateFace(CustomizationData data)
    {
        // Hrothgar Hack
        var value = _set.Race == Race.Hrothgar ? data.Value + 4 : data.Value;
        if (_customize.Face == value)
            return;

        _customize.Face = value;
        foreach (var actor in _actors)
            Glamourer.Penumbra.RedrawObject(actor.Character, RedrawType.Redraw, false);
    }

    private void FaceInputInt(int currentIndex)
    {
        ++currentIndex;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref currentIndex, 1, 1))
        {
            currentIndex = Math.Clamp(currentIndex - 1, 0, _currentCount - 1);
            var data = _set.Data(_currentId, currentIndex, _customize.Face);
            UpdateFace(data);
        }

        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]");
    }

    private void DrawIconPickerPopup()
    {
        using var popup = ImRaii.Popup(IconSelectorPopup, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentId, i, _customize.Face);
            var icon   = Glamourer.Customization.GetIcon(custom.IconId);
            using (var _ = ImRaii.Group())
            {
                if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                {
                    if (_currentId == CustomizationId.Face)
                        UpdateFace(custom);
                    else
                        UpdateValue(custom.Value);
                    ImGui.CloseCurrentPopup();
                }

                ImGuiUtil.HoverIconTooltip(icon, _iconSize);

                var text      = custom.Value.ToString();
                var textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_iconSize.X - textWidth + 2 * ImGui.GetStyle().FramePadding.X) / 2);
                ImGui.TextUnformatted(text);
            }

            if (i % 8 != 7)
                ImGui.SameLine();
        }
    }
}
