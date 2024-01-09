using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;

namespace Glamourer.Gui.Customization;

public class CustomizeParameterDrawer(Configuration config) : IService
{
    public void Draw(DesignManager designManager, Design design)
    {
        using var _ = EnsureSize();
        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
            DrawColorInput3(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
            DrawColorInput4(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));
    }

    private ImRaii.IEndObject EnsureSize()
    {
        var iconSize = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + 4 * ImGui.GetStyle().FramePadding.Y;
        var width    = 6 * iconSize + 4 * ImGui.GetStyle().ItemInnerSpacing.X;
        return ImRaii.ItemWidth(width);
    }

    public void Draw(StateManager stateManager, ActorState state)
    {
        using var _ = EnsureSize();
        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
            DrawColorInput3(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
            DrawColorInput4(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));
    }

    private void DrawColorInput3(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue.InternalTriple;
        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.ColorEdit3("##value", ref value, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoOptions))
                data.ValueSetter(new CustomizeParameterValue(value));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private void DrawColorInput4(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue.InternalQuadruple;
        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.ColorEdit4("##value", ref value, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.NoOptions))
                data.ValueSetter(new CustomizeParameterValue(value));
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
                data.ValueSetter(new CustomizeParameterValue(value));
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
            if (ImGui.SliderFloat("##value", ref value, -1000f, 1000f, "%.2f"))
                data.ValueSetter(new CustomizeParameterValue(value / 100f));
        }

        DrawRevert(data);

        DrawApplyAndLabel(data);
    }

    private static void DrawRevert(in CustomizeParameterDrawData data)
    {
        if (data.Locked || !data.AllowRevert)
            return;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
            data.ValueSetter(data.GameValue);

        ImGuiUtil.HoverTooltip("Hold Control and Right-click to revert to game values.");
    }

    private static void DrawApply(in CustomizeParameterDrawData data)
    {
        if (UiHelpers.DrawCheckbox("##apply", "Apply this custom parameter when applying the Design.", data.CurrentApply, out var enabled,
                data.Locked))
            data.ApplySetter(enabled);
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
}
