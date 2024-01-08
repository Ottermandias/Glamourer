using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
        {
            var newValue = (CustomizeValue)(ImGui.GetIO().KeyCtrl
                ? Math.Clamp(tmp, 0, byte.MaxValue)
                : Math.Clamp(tmp, 0, _currentCount - 1));
            UpdateValue(newValue);
        }

        ImGuiUtil.HoverTooltip($"Input Range: [0, {_currentCount - 1}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    // Integral input for an icon- or color based item.
    private void DataInputInt(int currentIndex, bool npc)
    {
        int value = _currentByte.Value;
        // Hrothgar face hack.
        if (_currentIndex is CustomizeIndex.Face && _set.Race is Race.Hrothgar && value is > 4 and < 9)
            value -= 4;

        using var group    = ImRaii.Group();
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

    private void DrawListSelector(CustomizeIndex index, bool indexedBy1)
    {
        using var id        = SetId(index);
        using var bigGroup = ImRaii.Group();

        using (_ = ImRaii.Disabled(_locked))
        {
            if (indexedBy1)
            {
                ListCombo1();
                ImGui.SameLine();
                ListInputInt1();
            }
            else
            {
                ListCombo0();
                ImGui.SameLine();
                ListInputInt0();
            }

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

    private void ListCombo0()
    {
        ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
        var       current = _currentByte.Value;
        using var combo   = ImRaii.Combo("##combo", $"{_currentOption} #{current + 1}");

        if (!combo)
            return;

        for (var i = 0; i < _currentCount; ++i)
        {
            if (ImGui.Selectable($"{_currentOption} #{i + 1}##combo", i == current))
                UpdateValue((CustomizeValue)i);
        }
    }

    private void ListInputInt0()
    {
        var tmp = _currentByte.Value + 1;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1))
        {
            var newValue = (CustomizeValue)(ImGui.GetIO().KeyCtrl
                ? Math.Clamp(tmp, 1, byte.MaxValue + 1)
                : Math.Clamp(tmp, 1, _currentCount));
            UpdateValue(newValue - 1);
        }

        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    private void ListCombo1()
    {
        ImGui.SetNextItemWidth(_comboSelectorSize * ImGui.GetIO().FontGlobalScale);
        var       current = _currentByte.Value;
        using var combo   = ImRaii.Combo("##combo", $"{_currentOption} #{current}");

        if (!combo)
            return;

        for (var i = 1; i <= _currentCount; ++i)
        {
            if (ImGui.Selectable($"{_currentOption} #{i}##combo", i == current))
                UpdateValue((CustomizeValue)i);
        }
    }

    private void ListInputInt1()
    {
        var tmp = (int)_currentByte.Value;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1))
        {
            var newValue = (CustomizeValue)(ImGui.GetIO().KeyCtrl
                ? Math.Clamp(tmp, 0, byte.MaxValue)
                : Math.Clamp(tmp, 1, _currentCount));
            UpdateValue(newValue);
        }

        ImGuiUtil.HoverTooltip($"Input Range: [1, {_currentCount}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    // Draw a customize checkbox.
    private void DrawCheckbox(CustomizeIndex idx)
    {
        using var id  = SetId(idx);
        var       tmp = _currentByte != CustomizeValue.Zero;
        if (_withApply)
        {
            switch (UiHelpers.DrawMetaToggle(_currentIndex.ToDefaultName(), tmp, _currentApply, out var newValue, out var newApply, _locked))
            {
                case (true, false):
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
                case (false, true):
                    ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
                    break;
                case (true, true):
                    ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
            }
        }
        else
        {
            using (_ = ImRaii.Disabled(_locked))
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
