using System;
using System.Linq;
using Glamourer.Customization;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
    private void DrawListSelector(CustomizationId id)
    {
        using var _        = SetId(id);
        using var bigGroup = ImRaii.Group();

        ListCombo();
        ImGui.SameLine();
        ListInputInt();
        ImGui.SameLine();
        ImGui.TextUnformatted(_currentOption);
    }

    private void ListCombo()
    {
        ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
        using var combo = ImRaii.Combo("##combo", $"{_currentOption} #{_currentByte.Value + 1}");

        if (!combo)
            return;

        for (var i = 0; i < _currentCount; ++i)
        {
            if (ImGui.Selectable($"{_currentOption} #{i + 1}##combo", i == _currentByte.Value))
                UpdateValue((CustomizationByteValue)i);
        }
    }

    private void ListInputInt()
    {
        var tmp = _currentByte.Value + 1;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1) && tmp > 0 && tmp <= _currentCount)
            UpdateValue((CustomizationByteValue)Math.Clamp(tmp - 1, 0, _currentCount - 1));
        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]");
    }

    private void PercentageSelector(CustomizationId id)
    {
        using var _        = SetId(id);
        using var bigGroup = ImRaii.Group();

        DrawPercentageSlider();
        ImGui.SameLine();
        PercentageInputInt();
        ImGui.SameLine();
        ImGui.TextUnformatted(_currentOption);
    }

    private void DrawPercentageSlider()
    {
        var tmp = (int)_currentByte.Value;
        ImGui.SetNextItemWidth(_comboSelectorSize);
        if (ImGui.SliderInt("##slider", ref tmp, 0, _currentCount - 1, "%i", ImGuiSliderFlags.AlwaysClamp))
            UpdateValue((CustomizationByteValue)tmp);
    }

    private void PercentageInputInt()
    {
        var tmp = (int)_currentByte.Value;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1))
            UpdateValue((CustomizationByteValue)Math.Clamp(tmp, 0, _currentCount - 1));
        ImGuiUtil.HoverTooltip($"Input Range: [0, {_currentCount - 1}]");
    }


    // Draw one of the four checkboxes for single bool customization options.
    private void Checkbox(string label, bool current, Action<bool> setter)
    {
        var tmp = current;
        if (ImGui.Checkbox(label, ref tmp) && tmp != current)
        {
            setter(tmp);
            UpdateActors();
        }
    }

    // Integral input for an icon- or color based item.
    private void DataInputInt(int currentIndex)
    {
        ++currentIndex;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref currentIndex, 1, 1))
        {
            currentIndex = Math.Clamp(currentIndex - 1, 0, _currentCount - 1);
            var data = _set.Data(_currentId, currentIndex, _customize.Face);
            UpdateValue(data.Value);
        }

        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]");
    }
}
