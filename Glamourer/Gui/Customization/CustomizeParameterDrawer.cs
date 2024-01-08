using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.State;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Services;

namespace Glamourer.Gui.Customization;

public class CustomizeParameterDrawer(Configuration config) : IService
{
    public void Draw(DesignManager designManager, Design design)
    {
        foreach (var flag in CustomizeParameterExtensions.TripleFlags)
            DrawColorInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromDesign(designManager, design, flag));
    }

    public void Draw(StateManager stateManager, ActorState state)
    {
        foreach (var flag in CustomizeParameterExtensions.TripleFlags)
            DrawColorInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
            DrawPercentageInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
            DrawValueInput(CustomizeParameterDrawData.FromState(stateManager, state, flag));
    }

    private void DrawColorInput(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue;
        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.ColorEdit3("##value", ref value, ImGuiColorEditFlags.Float))
                data.ValueSetter(value);
        }

        DrawApplyAndLabel(data);
    }

    private void DrawValueInput(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue[0];

        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.InputFloat("##value", ref value, 0.1f, 0.5f))
                data.ValueSetter(new Vector3(value));
        }

        DrawApplyAndLabel(data);
    }

    private void DrawPercentageInput(in CustomizeParameterDrawData data)
    {
        using var id    = ImRaii.PushId((int)data.Flag);
        var       value = data.CurrentValue[0] * 100f;

        using (_ = ImRaii.Disabled(data.Locked))
        {
            if (ImGui.SliderFloat("##value", ref value, 0, 100, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                data.ValueSetter(new Vector3(value / 100f));
        }

        DrawApplyAndLabel(data);
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
        ImGui.TextUnformatted(data.Flag.ToString());
    }
}
