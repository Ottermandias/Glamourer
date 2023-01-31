using System;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface
{
    private static void Checkmark(string label, string tooltip, bool value, Action<bool> setter)
    {
        if (ImGuiUtil.Checkbox(label, tooltip, value, setter))
            Glamourer.Config.Save();
    }

    private static void ChangeAndSave<T>(T value, T currentValue, Action<T> setter) where T : IEquatable<T>
    {
        if (value.Equals(currentValue))
            return;

        setter(value);
        Glamourer.Config.Save();
    }

    private static void DrawColorPicker(string name, string tooltip, uint value, uint defaultValue, Action<uint> setter)
    {
        const ImGuiColorEditFlags flags = ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs;

        var tmp = ImGui.ColorConvertU32ToFloat4(value);
        if (ImGui.ColorEdit4($"##{name}", ref tmp, flags))
            ChangeAndSave(ImGui.ColorConvertFloat4ToU32(tmp), value, setter);
        ImGui.SameLine();
        if (ImGui.Button($"Default##{name}"))
            ChangeAndSave(defaultValue, value, setter);
        ImGuiUtil.HoverTooltip(
            $"Reset to default: #{defaultValue & 0xFF:X2}{(defaultValue >> 8) & 0xFF:X2}{(defaultValue >> 16) & 0xFF:X2}{defaultValue >> 24:X2}");
        ImGui.SameLine();
        ImGui.Text(name);
        ImGuiUtil.HoverTooltip(tooltip);
    }

    private static void DrawRestorePenumbraButton()
    {
        const string buttonLabel = "Re-Register Penumbra";
        if (!Glamourer.Config.AttachToPenumbra)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.Button(buttonLabel);
            return;
        }

        if (ImGui.Button(buttonLabel))
            Glamourer.Penumbra.Reattach(true);

        ImGuiUtil.HoverTooltip(
            "If Penumbra did not register the functions for some reason, pressing this button might help restore functionality.");
    }

    private static void DrawSettingsTab()
    {
        using var tab = ImRaii.TabItem("Settings");
        if (!tab)
            return;

        var cfg = Glamourer.Config;
        ImGui.Dummy(_spacing);

        Checkmark("Folders First", "Sort Folders before all designs instead of lexicographically.", cfg.FoldersFirst,
            v => cfg.FoldersFirst = v);
        Checkmark("Color Designs", "Color the names of designs in the selector using the colors from below for the given cases.",
            cfg.ColorDesigns,
            v => cfg.ColorDesigns = v);
        Checkmark("Show Locks", "Write-protected Designs show a lock besides their name in the selector.", cfg.ShowLocks,
            v => cfg.ShowLocks = v);
        Checkmark("Attach to Penumbra",
            "Allows you to right-click items in the Changed Items tab of a mod in Penumbra to apply them to your player character.",
            cfg.AttachToPenumbra,
            v =>
            {
                cfg.AttachToPenumbra = v;
                if (v)
                    Glamourer.Penumbra.Reattach(true);
                else
                    Glamourer.Penumbra.Unattach();
            });
        ImGui.SameLine();
        DrawRestorePenumbraButton();

        Checkmark("Apply Fixed Designs",
            "Automatically apply fixed designs to characters and redraw them when anything changes.",
            cfg.ApplyFixedDesigns,
            v => { cfg.ApplyFixedDesigns = v; });

        ImGui.Dummy(_spacing);

        DrawColorPicker("Customization Color", "The color for designs that only apply their character customization.",
            cfg.CustomizationColor,            GlamourerConfig.DefaultCustomizationColor, c => cfg.CustomizationColor = c);
        DrawColorPicker("Equipment Color", "The color for designs that only apply some or all of their equipment slots and stains.",
            cfg.EquipmentColor,            GlamourerConfig.DefaultEquipmentColor, c => cfg.EquipmentColor = c);
        DrawColorPicker("State Color", "The color for designs that only apply some state modification.",
            cfg.StateColor,            GlamourerConfig.DefaultStateColor, c => cfg.StateColor = c);
    }
}
