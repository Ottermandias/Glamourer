using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Interop.PalettePlus;
using Glamourer.State;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

public class CustomizeParameterDrawer(Configuration config, PaletteImport import) : IService
{
    private readonly Dictionary<Design, CustomizeParameterData> _lastData    = [];
    private          string                                     _paletteName = string.Empty;
    private          CustomizeParameterData                     _data;
    private          CustomizeParameterFlag                     _flags;
    private          float                                      _width;
    private          CustomizeParameterValue?                   _copy;

    public void Draw(DesignManager designManager, Design design)
    {
        using var generalSize = EnsureSize();
        DrawPaletteImport(designManager, design);
        DrawConfig(true);

        using (_ = ImRaii.ItemWidth(_width - 2 * ImGui.GetFrameHeight() - 2 * ImGui.GetStyle().ItemInnerSpacing.X))
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
        using (_ = ImRaii.ItemWidth(_width - 2 * ImGui.GetFrameHeight() - 2 * ImGui.GetStyle().ItemInnerSpacing.X))
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
        using var id    = ImRaii.PushId("Palettes");
        using var combo = ImRaii.Combo("##import", _paletteName.Length > 0 ? _paletteName : "Select Palette...");
        if (!combo)
            return;

        foreach (var (name, (palette, flags)) in import.Data)
        {
            if (!ImGui.Selectable(name, _paletteName == name))
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

        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

        DrawPaletteCombo();

        ImGui.SameLine(0, spacing);
        var value = true;
        if (ImGui.Checkbox("Show Import", ref value))
        {
            config.ShowPalettePlusImport = false;
            config.Save();
        }

        ImGuiUtil.HoverTooltip("Hide the Palette+ Import bar from all designs. You can re-enable it in Glamourers interface settings.");

        var buttonWidth = new Vector2((_width - spacing) / 2, 0);
        var tt = _paletteName.Length > 0
            ? $"Apply the imported data from the Palette+ palette [{_paletteName}] to this design."
            : "Please select a palette first.";
        if (ImGuiUtil.DrawDisabledButton("Apply Import", buttonWidth, tt, _paletteName.Length == 0 || design.WriteProtected()))
        {
            _lastData[design] = design.DesignData.Parameters;
            foreach (var parameter in _flags.Iterate())
                manager.ChangeCustomizeParameter(design, parameter, _data[parameter]);
        }

        ImGui.SameLine(0, spacing);
        var enabled = _lastData.TryGetValue(design, out var oldData);
        tt = enabled
            ? $"Revert to the last set of advanced customization parameters of [{design.Name}] before importing."
            : $"You have not imported any data that could be reverted for [{design.Name}].";
        if (ImGuiUtil.DrawDisabledButton("Revert Import", buttonWidth, tt, !enabled || design.WriteProtected()))
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
        if (ImGui.Checkbox("Show Config", ref value))
        {
            config.ShowColorConfig = value;
            config.Save();
        }

        ImGuiUtil.HoverTooltip(
            "Hide the color configuration options from the Advanced Customization panel. You can re-enable it in Glamourers interface settings.");
    }

    private void DrawColorDisplayOptions()
    {
        using var group = ImRaii.Group();
        if (ImGui.RadioButton("RGB", config.UseRgbForColors) && !config.UseRgbForColors)
        {
            config.UseRgbForColors = true;
            config.Save();
        }

        Im.Line.Same();
        if (ImGui.RadioButton("HSV", !config.UseRgbForColors) && config.UseRgbForColors)
        {
            config.UseRgbForColors = false;
            config.Save();
        }
    }

    private void DrawColorFormatOptions(bool withApply)
    {
        var width = _width
          - (ImGui.CalcTextSize("Float").X
              + ImGui.CalcTextSize("Integer").X
              + 2 * (ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X)
              + ImGui.GetStyle().ItemInnerSpacing.X
              + ImGui.GetItemRectSize().X);
        if (!withApply)
            width -= ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X;

        ImGui.SameLine(0, width);
        if (ImGui.RadioButton("Float", config.UseFloatForColors) && !config.UseFloatForColors)
        {
            config.UseFloatForColors = true;
            config.Save();
        }

        Im.Line.Same();
        if (ImGui.RadioButton("Integer", !config.UseFloatForColors) && config.UseFloatForColors)
        {
            config.UseFloatForColors = false;
            config.Save();
        }
    }

    private void DrawColorInput3(in CustomizeParameterDrawData data, bool allowHighlights)
    {
        using var id           = ImRaii.PushId((int)data.Flag);
        var       value        = data.CurrentValue.InternalTriple;
        var       noHighlights = !allowHighlights && data.Flag is CustomizeParameterFlag.HairHighlight;
        DrawCopyPasteButtons(data, data.Locked || noHighlights);
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        using (_ = ImRaii.Disabled(data.Locked || noHighlights))
        {
            if (ImGui.ColorEdit3("##value", ref value, GetFlags()))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        if (noHighlights)
            ImGuiUtil.HoverTooltip("Highlights are disabled in your regular customizations.", ImGuiHoveredFlags.AllowWhenDisabled);

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawColorInput4(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue.InternalQuadruple;
        DrawCopyPasteButtons(data, data.Locked);
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.ColorEdit4("##value", ref value, GetFlags() | ImGuiColorEditFlags.AlphaPreviewHalf))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawValueInput(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue[0];

        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.InputFloat("##value", ref value, 0.1f, 0.5f))
                data.ChangeParameter(new CustomizeParameterValue(value));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawPercentageInput(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue[0] * 100f;

        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.SliderFloat("##value", ref value, -100f, 300, "%.2f"))
                data.ChangeParameter(new CustomizeParameterValue(value / 100f));
            ImGuiUtil.HoverTooltip("You can control-click this to enter arbitrary values by hand instead of dragging.");
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private static void DrawRevert(in CustomizeParameterDrawData data)
    {
        if (data.Locked || !data.AllowRevert)
            return;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
            data.ChangeParameter(data.GameValue);

        ImGuiUtil.HoverTooltip("Hold Control and Right-click to revert to game values.");
    }

    private static void DrawApply(in CustomizeParameterDrawData data)
    {
        if (UiHelpers.DrawCheckbox("##apply", "Apply this custom parameter when applying the Design.", data.CurrentApply, out var enabled,
                data.Locked))
            data.ChangeApplyParameter(enabled);
    }

    private void DrawApplyAndLabel(in CustomizeParameterDrawData data)
    {
        if (data.DisplayApplication && !config.HideApplyCheckmarks)
        {
            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            DrawApply(data);
        }

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.TextUnformatted(data.Flag.ToName());
    }

    private ImGuiColorEditFlags GetFlags()
        => Format | Display | ImGuiColorEditFlags.Hdr | ImGuiColorEditFlags.NoOptions;

    private ImGuiColorEditFlags Format
        => config.UseFloatForColors ? ImGuiColorEditFlags.Float : ImGuiColorEditFlags.Uint8;

    private ImGuiColorEditFlags Display
        => config.UseRgbForColors ? ImGuiColorEditFlags.DisplayRgb : ImGuiColorEditFlags.DisplayHsv;

    private ImRaii.IEndObject EnsureSize()
    {
        var iconSize = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + 4 * ImGui.GetStyle().FramePadding.Y;
        _width = 7 * iconSize + 4 * ImGui.GetStyle().ItemInnerSpacing.X;
        return ImRaii.ItemWidth(_width);
    }

    private void DrawCopyPasteButtons(in CustomizeParameterDrawData data, bool locked)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Copy this color for later use.", false, true))
            _copy = data.CurrentValue;
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Paste.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                _copy.HasValue ? "Paste the currently copied value." : "No value copied yet.", locked || !_copy.HasValue, true))
            data.ChangeParameter(_copy!.Value);
    }
}
