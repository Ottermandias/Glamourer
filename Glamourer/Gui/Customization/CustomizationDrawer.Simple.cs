using System.Text.Unicode;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private void PercentageSelector(CustomizeIndex index)
    {
        using var _        = SetId(index);
        using var bigGroup = Im.Group();

        using (Im.Disabled(_locked))
        {
            DrawPercentageSlider();
            Im.Line.Same();
            PercentageInputInt();
            if (_withApply)
            {
                Im.Line.Same();
                ApplyCheckbox();
            }
        }

        Im.Line.Same();
        ImEx.TextFrameAligned(_currentOption);
        if (_currentIndex is CustomizeIndex.Height)
            DrawHeight();
    }

    private void DrawHeight()
    {
        if (config.HeightDisplayType is HeightDisplayType.None)
            return;

        var height = heightService.Height(_customize);
        Im.Line.Same();

        Span<byte> t  = stackalloc byte[64];
        var        ic = CultureInfo.InvariantCulture;
        if (config.HeightDisplayType switch
            {
                HeightDisplayType.Centimetre  => Utf8.TryWrite(t, ic, $"({height * 100:F1} cm)",                                      out _),
                HeightDisplayType.Metre       => Utf8.TryWrite(t, ic, $"({height:F2} m)",                                             out _),
                HeightDisplayType.Wrong       => Utf8.TryWrite(t, ic, $"({height * 100 / 2.539:F1} in)",                              out _),
                HeightDisplayType.WrongFoot   => Utf8.TryWrite(t, ic, $"({(int)(height * 3.2821)}'{(int)(height * 39.3856) % 12}'')", out _),
                HeightDisplayType.Corgi       => Utf8.TryWrite(t, ic, $"({height * 100 / 40.0:F1} Corgis)",                           out _),
                HeightDisplayType.OlympicPool => Utf8.TryWrite(t, ic, $"({height / 3.0:F3} Pools)",                                   out _),
                _                             => Utf8.TryWrite(t, ic, $"({height})",                                                  out _),
            })
            Im.Text(t);
    }

    private void DrawPercentageSlider()
    {
        var tmp = (int)_currentByte.Value;
        Im.Item.SetNextWidth(_comboSelectorSize);
        if (Im.Slider("##slider"u8, ref tmp, "%i"u8, 0, _currentCount - 1, SliderFlags.AlwaysClamp)
         || CaptureMouseWheel(ref tmp, 0, _currentCount))
            UpdateValue((CustomizeValue)tmp);
    }

    private void PercentageInputInt()
    {
        var tmp = (int)_currentByte.Value;
        Im.Item.SetNextWidth(_inputIntSize);
        var cap = Im.Io.KeyControl ? byte.MaxValue : _currentCount - 1;
        if (Im.Input.Scalar("##text"u8, ref tmp, 1, 1))
        {
            var newValue = (CustomizeValue)Math.Clamp(tmp, 0, cap);
            UpdateValue(newValue);
        }

        Im.Tooltip.OnHover($"Input Range: [0, {_currentCount - 1}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    // Integral input for an icon- or color based item.
    private void DataInputInt(int currentIndex, bool npc)
    {
        int value = _currentByte.Value;
        // Hrothgar face hack.
        if (_currentIndex is CustomizeIndex.Face && _set.Race is Race.Hrothgar && value is > 4 and < 9)
            value -= 4;

        using var group    = Im.Group();
        using var disabled = Im.Disabled(_locked || _currentIndex is CustomizeIndex.Face && _lockedRedraw);
        Im.Item.SetNextWidth(_inputIntSizeNoButtons);
        if (Im.Input.Scalar("##text"u8, ref value))
        {
            var index = _set.DataByValue(_currentIndex, (CustomizeValue)value, out var data, _customize.Face);
            if (index >= 0)
                UpdateValue(data!.Value.Value);
            else if (Im.Io.KeyControl)
                UpdateValue((CustomizeValue)value);
        }
        else
        {
            CheckWheel();
        }

        if (!_withApply)
            Im.Tooltip.OnHover("Hold Control to force updates with invalid/unknown options at your own risk.");

        var size = new Vector2(Im.Style.FrameHeight);
        Im.Line.Same();
        if (ImEx.Button("-"u8, size, "Select the previous available option in order."u8, currentIndex <= 0))
            UpdateValue(_set.Data(_currentIndex, currentIndex - 1, _customize.Face).Value);
        else
            CheckWheel();
        Im.Line.Same();
        if (ImEx.Button("+"u8, size, "Select the next available option in order."u8, currentIndex >= _currentCount - 1 || npc))
            UpdateValue(_set.Data(_currentIndex, currentIndex + 1, _customize.Face).Value);
        else
            CheckWheel();
        return;

        void CheckWheel()
        {
            if (currentIndex < 0 || !CaptureMouseWheel(ref currentIndex, 0, _currentCount))
                return;

            var data = _set.Data(_currentIndex, currentIndex, _customize.Face);
            UpdateValue(data.Value);
        }
    }

    private void DrawListSelector(CustomizeIndex index, bool indexedBy1)
    {
        using var id       = SetId(index);
        using var bigGroup = Im.Group();

        using (Im.Disabled(_locked))
        {
            if (indexedBy1)
            {
                ListCombo1();
                Im.Line.Same();
                ListInputInt1();
            }
            else
            {
                ListCombo0();
                Im.Line.Same();
                ListInputInt0();
            }

            if (_withApply)
            {
                Im.Line.Same();
                ApplyCheckbox();
            }
        }

        Im.Line.Same();
        ImEx.TextFrameAligned(_currentOption);
    }

    private void ListCombo0()
    {
        Im.Item.SetNextWidth(_comboSelectorSize * Im.Io.GlobalScale);
        var current = (int)_currentByte.Value;
        using (var combo = Im.Combo.Begin("##combo"u8, $"{_currentOption} #{current + 1}"))
        {
            if (combo)

                for (var i = 0; i < _currentCount; ++i)
                {
                    if (Im.Selectable($"{_currentOption} #{i + 1}##combo", i == current))
                        UpdateValue((CustomizeValue)i);
                }
        }

        if (CaptureMouseWheel(ref current, 0, _currentCount))
            UpdateValue((CustomizeValue)current);
    }

    private void ListInputInt0()
    {
        var tmp = _currentByte.Value + 1;
        Im.Item.SetNextWidth(_inputIntSize);
        var cap = Im.Io.KeyControl ? byte.MaxValue + 1 : _currentCount;
        if (Im.Input.Scalar("##text"u8, ref tmp, 1, 1))
        {
            var newValue = Math.Clamp(tmp, 1, cap);
            UpdateValue((CustomizeValue)(newValue - 1));
        }

        Im.Tooltip.OnHover($"Input Range: [1, {_currentCount}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    private void ListCombo1()
    {
        Im.Item.SetNextWidth(_comboSelectorSize * Im.Io.GlobalScale);
        var current = (int)_currentByte.Value;
        using (var combo = Im.Combo.Begin("##combo"u8, $"{_currentOption} #{current}"))
        {
            if (combo)
                for (var i = 1; i <= _currentCount; ++i)
                {
                    if (Im.Selectable($"{_currentOption} #{i}##combo", i == current))
                        UpdateValue((CustomizeValue)i);
                }
        }

        if (CaptureMouseWheel(ref current, 1, _currentCount))
            UpdateValue((CustomizeValue)current);
    }

    private void ListInputInt1()
    {
        var tmp = (int)_currentByte.Value;
        Im.Item.SetNextWidth(_inputIntSize);
        var (offset, cap) = Im.Io.KeyControl ? (0, byte.MaxValue) : (1, _currentCount);
        if (Im.Input.Scalar("##text"u8, ref tmp, 1, 1))
        {
            var newValue = (CustomizeValue)Math.Clamp(tmp, offset, cap);
            UpdateValue(newValue);
        }

        Im.Tooltip.OnHover($"Input Range: [1, {_currentCount}]\n"
          + "Hold Control to force updates with invalid/unknown options at your own risk.");
    }

    private static bool CaptureMouseWheel(ref int value, int offset, int cap)
    {
        if (!Im.Item.Hovered() || !Im.Io.KeyControl)
            return false;

        Im.Item.SetUsingMouseWheel();

        var mw = (int)Im.Io.MouseWheel;
        if (mw is 0)
            return false;

        value -= offset;
        value = mw switch
        {
            < 0 => offset + (value + cap + mw) % cap,
            _   => offset + (value + mw) % cap,
        };
        return true;
    }

    // Draw a customize checkbox.
    private void DrawCheckbox(CustomizeIndex idx)
    {
        using var id  = SetId(idx);
        var       tmp = _currentByte != CustomizeValue.Zero;
        if (_withApply)
        {
            switch (UiHelpers.DrawMetaToggle(_currentIndex.ToNameU8(), tmp, _currentApply, out var newValue, out var newApply, _locked))
            {
                case (true, false):
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
                case (false, true): ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag; break;
                case (true, true):
                    ChangeApply = newApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
                    _customize.Set(idx, newValue ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                    break;
            }
        }
        else
        {
            using (Im.Disabled(_locked))
            {
                if (Im.Checkbox("##toggle"u8, ref tmp))
                {
                    _customize.Set(idx, tmp ? CustomizeValue.Max : CustomizeValue.Zero);
                    Changed |= _currentFlag;
                }
            }

            Im.Line.Same();
            Im.Text(_currentIndex.ToNameU8());
        }
    }

    private void ApplyCheckbox()
    {
        if (UiHelpers.DrawCheckbox("##apply"u8, $"Apply the {_currentOption} customization in this design.", _currentApply, out _, _locked))
            ToggleApply();
    }

    private void ApplyCheckbox(CustomizeIndex index)
    {
        using var id = SetId(index);
        if (UiHelpers.DrawCheckbox("##apply"u8, $"Apply the {_currentOption} customization in this design.", _currentApply, out _, _locked))
            ToggleApply();
    }

    // Update the current Apply value.
    private void ToggleApply()
    {
        _currentApply = !_currentApply;
        ChangeApply   = _currentApply ? ChangeApply | _currentFlag : ChangeApply & ~_currentFlag;
    }
}
