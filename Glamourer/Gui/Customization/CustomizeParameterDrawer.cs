using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Interop.PalettePlus;
using Glamourer.State;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Customization;

public class CustomizeParameterDrawer(Configuration.Configuration config, PaletteImport import) : IService
{
    private readonly Dictionary<Design, CustomizeParameterData> _lastData    = [];
    private          StringU8                                   _paletteName = StringU8.Empty;
    private          CustomizeParameterData                     _data;
    private          CustomizeParameterFlag                     _flags;
    private          float                                      _width;
    private          CustomizeParameterValue?                   _copy;

    public void Draw(DesignManager designManager, Design design)
    {
        using var generalSize = EnsureSize();
        DrawPaletteImport(designManager, design);
        DrawConfig(true);

        using (Im.Item.PushWidth(_width - 2 * Im.Style.FrameHeight - 2 * Im.Style.ItemInnerSpacing.X))
        {
            foreach (var flag in CustomizeParameterExtensions.RgbFlags)
                DrawColorInput3(CustomizeParameterDrawData.FromDesign(designManager, design, flag), true);

            foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
                DrawColorInput4(CustomizeParameterDrawData.FromDesign(designManager, design, flag));
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));
    }

    public void Draw(StateManager stateManager, ActorState state)
    {
        using var generalSize = EnsureSize();
        DrawConfig(false);
        using (Im.Item.PushWidth(_width - 2 * Im.Style.FrameHeight - 2 * Im.Style.ItemInnerSpacing.X))
        {
            foreach (var flag in CustomizeParameterExtensions.RgbFlags)
                DrawColorInput3(CustomizeParameterDrawData.FromState(stateManager, state, flag), state.ModelData.Customize.Highlights);

            foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
                DrawColorInput4(CustomizeParameterDrawData.FromState(stateManager, state, flag));
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));
    }

    private void DrawPaletteCombo()
    {
        using var id    = Im.Id.Push("Palettes"u8);
        using var combo = Im.Combo.Begin("##import"u8, _paletteName.Length > 0 ? _paletteName : "Select Palette..."u8);
        if (!combo)
            return;

        foreach (var (name, (palette, flags)) in import.Data)
        {
            if (!Im.Selectable(name, _paletteName == name))
                continue;

            _paletteName = name;
            _data        = palette;
            _flags       = flags;
        }
    }

    private void DrawPaletteImport(DesignManager manager, Design design)
    {
        if (!config.ShowPalettePlusImport)
            return;

        DrawPaletteCombo();

        Im.Line.SameInner();
        var value = true;
        if (Im.Checkbox("Show Import"u8, ref value))
        {
            config.ShowPalettePlusImport = false;
            config.Save();
        }

        Im.Tooltip.OnHover("Hide the Palette+ Import bar from all designs. You can re-enable it in Glamourers interface settings."u8);

        var buttonWidth = new Vector2((_width - Im.Style.ItemInnerSpacing.X) / 2, 0);
        if (ImEx.Button("Apply Import"u8, buttonWidth, _paletteName.Length > 0
                ? $"Apply the imported data from the Palette+ palette [{_paletteName}] to this design."
                : "Please select a palette first.", _paletteName.Length is 0 || design.WriteProtected()))
        {
            _lastData[design] = design.DesignData.Parameters;
            foreach (var parameter in _flags.Iterate())
                manager.ChangeCustomizeParameter(design, parameter, _data[parameter]);
        }

        Im.Line.SameInner();
        var enabled = _lastData.TryGetValue(design, out var oldData);
        if (ImEx.Button("Revert Import"u8, buttonWidth, enabled
                ? $"Revert to the last set of advanced customization parameters of [{design.Name}] before importing."
                : $"You have not imported any data that could be reverted for [{design.Name}].", !enabled || design.WriteProtected()))
        {
            _lastData.Remove(design);
            foreach (var parameter in CustomizeParameterExtensions.AllFlags)
                manager.ChangeCustomizeParameter(design, parameter, oldData[parameter]);
        }
    }


    private void DrawConfig(bool withApply)
    {
        if (!config.ShowColorConfig)
            return;

        DrawColorDisplayOptions();
        DrawColorFormatOptions(withApply);
        var value = config.ShowColorConfig;
        Im.Line.Same();
        if (Im.Checkbox("Show Config"u8, ref value))
        {
            config.ShowColorConfig = value;
            config.Save();
        }

        Im.Tooltip.OnHover(
            "Hide the color configuration options from the Advanced Customization panel. You can re-enable it in Glamourers interface settings."u8);
    }

    private void DrawColorDisplayOptions()
    {
        using var group = Im.Group();
        if (Im.RadioButton("RGB"u8, config.UseRgbForColors) && !config.UseRgbForColors)
        {
            config.UseRgbForColors = true;
            config.Save();
        }

        Im.Line.Same();
        if (Im.RadioButton("HSV"u8, !config.UseRgbForColors) && config.UseRgbForColors)
        {
            config.UseRgbForColors = false;
            config.Save();
        }
    }

    private void DrawColorFormatOptions(bool withApply)
    {
        var width = _width
          - (Im.Font.CalculateSize("Float"u8).X
              + Im.Font.CalculateButtonSize("Integer"u8).X
              + 2 * Im.Style.ItemSpacing.X)
          + Im.Style.ItemInnerSpacing.X
          + Im.Item.Size.X;
        if (!withApply)
            width -= Im.Style.FrameHeight + Im.Style.ItemInnerSpacing.X;

        Im.Line.Same(0, width);
        if (Im.RadioButton("Float"u8, config.UseFloatForColors) && !config.UseFloatForColors)
        {
            config.UseFloatForColors = true;
            config.Save();
        }

        Im.Line.Same();
        if (Im.RadioButton("Integer"u8, !config.UseFloatForColors) && config.UseFloatForColors)
        {
            config.UseFloatForColors = false;
            config.Save();
        }
    }

    private void DrawColorInput3(in CustomizeParameterDrawData data, bool allowHighlights)
    {
        using var id           = Im.Id.Push((int)data.Flag);
        var       value        = data.CurrentValue.InternalTriple;
        var       noHighlights = !allowHighlights && data.Flag is CustomizeParameterFlag.HairHighlight;
        DrawCopyPasteButtons(data, data.Locked || noHighlights);
        Im.Line.SameInner();
        using (Im.Disabled(data.Locked || noHighlights))
        {
            if (Im.Color.Editor("##value"u8, ref value, GetFlags()))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        if (noHighlights)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, "Highlights are disabled in your regular customizations."u8);

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawColorInput4(in CustomizeParameterDrawData data)
    {
        using var id    = Im.Id.Push((int)data.Flag);
        var       value = data.CurrentValue.InternalQuadruple;
        DrawCopyPasteButtons(data, data.Locked);
        Im.Line.SameInner();
        using (Im.Disabled(data.Locked))
        {
            if (Im.Color.Editor("##value"u8, ref value, GetFlags() | ColorEditorFlags.AlphaPreviewHalf))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawValueInput(in CustomizeParameterDrawData data)
    {
        using var id    = Im.Id.Push((int)data.Flag);
        var       value = data.CurrentValue[0];

        using (Im.Disabled(data.Locked))
        {
            if (Im.Input.Scalar("##value"u8, ref value, 0.1f, 0.5f))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawPercentageInput(in CustomizeParameterDrawData data)
    {
        using var id    = Im.Id.Push((int)data.Flag);
        var       value = data.CurrentValue[0] * 100f;

        using (Im.Disabled(data.Locked))
        {
            if (Im.Slider("##value"u8, ref value, "%.2f"u8, -100f, 300))
                data.ChangeParameter(new CustomizeParameterValue(value / 100f));
            Im.Tooltip.OnHover("You can control-click this to enter arbitrary values by hand instead of dragging."u8);
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private static void DrawRevert(in CustomizeParameterDrawData data)
    {
        if (data.Locked || !data.AllowRevert)
            return;

        if (Im.Item.RightClicked() && Im.Io.KeyControl)
            data.ChangeParameter(data.GameValue);

        Im.Tooltip.OnHover("Hold Control and Right-click to revert to game values."u8);
    }

    private static void DrawApply(in CustomizeParameterDrawData data)
    {
        if (UiHelpers.DrawCheckbox("##apply"u8, "Apply this custom parameter when applying the Design."u8, data.CurrentApply, out var enabled,
                data.Locked))
            data.ChangeApplyParameter(enabled);
    }

    private void DrawApplyAndLabel(in CustomizeParameterDrawData data)
    {
        if (data.DisplayApplication && !config.HideApplyCheckmarks)
        {
            Im.Line.SameInner();
            DrawApply(data);
        }

        Im.Line.SameInner();
        Im.Text(data.Flag.ToNameU8());
    }

    private ColorEditorFlags GetFlags()
        => Format | Display | ColorEditorFlags.Hdr | ColorEditorFlags.NoOptions;

    private ColorEditorFlags Format
        => config.UseFloatForColors ? ColorEditorFlags.Float : ColorEditorFlags.Uint8;

    private ColorEditorFlags Display
        => config.UseRgbForColors ? ColorEditorFlags.DisplayRgb : ColorEditorFlags.DisplayHsv;

    private Im.ItemWidthDisposable EnsureSize()
    {
        var iconSize = Im.Style.TextHeight * 2 + Im.Style.ItemSpacing.Y + 4 * Im.Style.FramePadding.Y;
        _width = 7 * iconSize + 4 * Im.Style.ItemInnerSpacing.X;
        return Im.Item.PushWidth(_width);
    }

    private void DrawCopyPasteButtons(in CustomizeParameterDrawData data, bool locked)
    {
        if (ImEx.Icon.Button(LunaStyle.ToClipboardIcon, "Copy this color for later use."u8))
            _copy = data.CurrentValue;
        Im.Line.SameInner();
        if (ImEx.Icon.Button(LunaStyle.FromClipboardIcon, _copy.HasValue ? "Paste the currently copied value."u8 : "No value copied yet."u8,
                locked || !_copy.HasValue))
            data.ChangeParameter(_copy!.Value);
    }
}
