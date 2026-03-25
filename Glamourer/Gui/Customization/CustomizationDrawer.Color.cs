using Dalamud.Interface;
using Glamourer.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private static ReadOnlySpan<byte> ColorPickerPopupName
        => "ColorPicker"u8;

    private static readonly AwesomeIcon UnknownCustomization = FontAwesomeIcon.Question;

    private CustomizeValue _draggedColorValue;
    private CustomizeIndex _draggedColorType;


    private void DrawDragDropSource(CustomizeIndex index, CustomizeData custom)
    {
        using var dragDropSource = Im.DragDrop.Source();
        if (!dragDropSource)
            return;

        if (!dragDropSource.SetPayload("##colorDragDrop"u8))
            _draggedColorValue = _customize[index];
        Im.Text($"Dragging {(custom.Color == 0 ? $"{_currentOption} (NPC)" : _currentOption)} #{_draggedColorValue.Value}...");
        _draggedColorType = index;
    }

    private void DrawDragDropTarget(CustomizeIndex index)
    {
        using var dragDropTarget = Im.DragDrop.Target();
        if (!dragDropTarget.IsDropping("##colorDragDrop"u8))
            return;

        var idx       = _set.DataByValue(_draggedColorType, _draggedColorValue, out var draggedData, _customize.Face);
        var bestMatch = _draggedColorValue;
        if (draggedData.HasValue)
        {
            var draggedColor = draggedData.Value.Color;
            var targetData   = _set.Data(index, idx);
            if (targetData.Color != draggedColor)
            {
                var bestDiff = Diff(targetData.Color, draggedColor);
                var count    = _set.Count(index);
                for (var i = 0; i < count; ++i)
                {
                    targetData = _set.Data(index, i);
                    if (targetData.Color == draggedColor)
                    {
                        UpdateValue(_draggedColorValue);
                        return;
                    }

                    var diff = Diff(targetData.Color, draggedColor);
                    if (diff >= bestDiff)
                        continue;

                    bestDiff  = diff;
                    bestMatch = (CustomizeValue)i;
                }
            }
        }

        UpdateValue(bestMatch);
        return;

        static uint Diff(Rgba32 color1, Rgba32 color2)
        {
            var r = color1.R - color2.R;
            var g = color1.G - color2.G;
            var b = color1.B - color2.B;
            return (uint) (30 * r * r + 59 * g * g + 11 * b * b);
        }
    }

    private void DrawColorPicker(CustomizeIndex index)
    {
        using var id = SetId(index);
        var (current, custom) = GetCurrentCustomization(index);

        var color = (current < 0 ? Im.Color.Get(ImGuiColor.FrameBackground) : custom.Color).ToVector();

        using (ImStyleSingle.FrameBorderThickness.Push(2 * Im.Style.GlobalScale, current < 0))
        {
            if (Im.Color.Button($"{_customize[index].Value}##color", color, ColorButtonFlags.NoDragDrop, _framedIconSize))
            {
                Im.Popup.Open(ColorPickerPopupName);
            }
            else if (current >= 0 && !_locked && CaptureMouseWheel(ref current, 0, _currentCount))
            {
                var data = _set.Data(_currentIndex, current, _customize.Face);
                UpdateValue(data.Value);
            }

            DrawDragDropSource(index, custom);
            DrawDragDropTarget(index);
        }

        var npc = false;
        if (current < 0)
        {
            using var font = Im.Font.Push(AwesomeIcon.Font);
            var       size = Im.Font.CalculateSize(UnknownCustomization.Span);
            var       pos  = Im.Item.UpperLeftCorner + (Im.Item.Size - size) / 2;
            Im.Window.DrawList.Text(pos, Im.Color.Get(ImGuiColor.Text), UnknownCustomization.Span);
            current = 0;
            npc     = true;
        }

        Im.Line.Same();

        using (Im.Group())
        {
            DataInputInt(current, npc);
            if (_withApply)
            {
                ApplyCheckbox();
                Im.Line.Same();
            }

            ImEx.TextFrameAligned(custom.Color.IsTransparent ? $"{_currentOption} (NPC)" : _currentOption);
        }

        DrawColorPickerPopup(current);
    }

    private void DrawColorPickerPopup(int current)
    {
        using var popup = Im.Popup.Begin(ColorPickerPopupName, WindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImStyleDouble.ItemSpacing.Push(Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize[CustomizeIndex.Face]);
            if (Im.Color.Button($"{custom.Value}", custom.Color) && !_locked)
            {
                UpdateValue(custom.Value);
                Im.Popup.CloseCurrent();
            }

            if (i == current)
            {
                var size = Im.Item.Size;
                Im.Window.DrawList.Shape.CircleFilled(Im.Item.UpperLeftCorner + size / 2, size.X / 4, custom.Color.ContrastColor());
            }

            if (i % 8 is not 7)
                Im.Line.Same();
        }
    }

    // Obtain the current customization and print a warning if it is not known.
    private (int, CustomizeData) GetCurrentCustomization(CustomizeIndex index)
    {
        var current = _set.DataByValue(index, _customize[index], out var custom, _customize.Face);
        if (_set.IsAvailable(index) && current < 0)
            return (current, new CustomizeData(index, _customize[index]));

        return (current, custom!.Value);
    }
}
