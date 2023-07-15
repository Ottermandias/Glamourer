using System;
using System.Numerics;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private void PercentageSelector(CustomizeIndex index)
    {
        using var _        = SetId(index);
        using var bigGroup = ImRaii.Group();

        using (var disabled = ImRaii.Disabled(_locked))
        {
            DrawPercentageSlider();
            ImGui.SameLine();
            PercentageInputInt();
            if (_withApply)
            {
                ImGui.SameLine();
                ApplyCheckbox();
            }
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(_currentOption);
    }

    private void DrawPercentageSlider()
    {
        var tmp = (int)_currentByte.Value;
        ImGui.SetNextItemWidth(_comboSelectorSize);
        if (ImGui.SliderInt("##slider", ref tmp, 0, _currentCount - 1, "%i", ImGuiSliderFlags.AlwaysClamp))
            UpdateValue((CustomizeValue)tmp);
    }

    private void PercentageInputInt()
    {
        var tmp = (int)_currentByte.Value;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1))
            UpdateValue((CustomizeValue)Math.Clamp(tmp, 0, _currentCount - 1));
        ImGuiUtil.HoverTooltip($"Input Range: [0, {_currentCount - 1}]");
    }

    // Integral input for an icon- or color based item.
    private void DataInputInt(int currentIndex, bool npc)
    {
        int value = _currentByte.Value;
        // Hrothgar face hack.
        if (_currentIndex is CustomizeIndex.Face && _set.Race is Race.Hrothgar && value is > 4 and < 9)
            value -= 4;

        using var disabled = ImRaii.Disabled(_locked || _currentIndex is CustomizeIndex.Face && _lockedRedraw);
        ImGui.SetNextItemWidth(_inputIntSizeNoButtons);
        if (ImGui.InputInt("##text", ref value, 0, 0))
        {
            var index = _set.DataByValue(_currentIndex, (CustomizeValue)value, out var data, _customize.Face);
            if (index >= 0)
                UpdateValue(data!.Value.Value);
            else if (ImGui.GetIO().KeyCtrl)
                UpdateValue((CustomizeValue)value);
        }

        if (!_withApply)
            ImGuiUtil.HoverTooltip("Hold Control to force updates with invalid/unknown options at your own risk.");

        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("-", new Vector2(ImGui.GetFrameHeight()), "Select the previous available option in order.",
                currentIndex <= 0))
            UpdateValue(_set.Data(_currentIndex, currentIndex - 1, _customize.Face).Value);
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("+", new Vector2(ImGui.GetFrameHeight()), "Select the next available option in order.",
                currentIndex >= _currentCount - 1 || npc))
            UpdateValue(_set.Data(_currentIndex, currentIndex + 1, _customize.Face).Value);
    }

    private void DrawListSelector(CustomizeIndex index)
    {
        using var _        = SetId(index);
        using var bigGroup = ImRaii.Group();

        using var disabled = ImRaii.Disabled(_locked);
        ListCombo();
        ImGui.SameLine();
        ListInputInt();
        if (_withApply)
        {
            ImGui.SameLine();
            ApplyCheckbox();
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
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
                UpdateValue((CustomizeValue)i);
        }
    }

    private void ListInputInt()
    {
        var tmp = _currentByte.Value + 1;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1) && tmp > 0 && tmp <= _currentCount)
            UpdateValue((CustomizeValue)Math.Clamp(tmp - 1, 0, _currentCount - 1));
        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]");
    }

    // Draw a customize checkbox.
    private void DrawCheckbox(CustomizeIndex idx)
    {
        using var id  = SetId(idx);
        var       tmp = _currentByte != CustomizeValue.Zero;
        if (_withApply)
        {
            switch (UiHelpers.DrawMetaToggle(_currentIndex.ToDefaultName(), string.Empty, tmp, _currentApply, out var newValue,
                        out var newApply, _locked))
            {
                case DataChange.Item:
                    ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
                case DataChange.ApplyItem:
                    ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
                    break;
                case DataChange.Item | DataChange.ApplyItem:
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
            }
        }
        else
        {
            using (var disabled = ImRaii.Disabled(_locked))
            {
                if (ImGui.Checkbox("##toggle", ref tmp))
                {
                    _customize.Set(idx, tmp ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                }
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(_currentIndex.ToDefaultName());
        }
    }

    private void ApplyCheckbox()
    {
        if (UiHelpers.DrawCheckbox("##apply", $"Apply the {_currentOption} customization in this design.", _currentApply, out _, _locked))
            ToggleApply();
    }

    private void ApplyCheckbox(CustomizeIndex index)
    {
        SetId(index);
        if (UiHelpers.DrawCheckbox("##apply", $"Apply the {_currentOption} customization in this design.", _currentApply, out _, _locked))
            ToggleApply();
    }

    // Update the current Apply value.
    private void ToggleApply()
    {
        _currentApply = !_currentApply;
        ChangeApply   = _currentApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
    }
}
