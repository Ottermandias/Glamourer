using System;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui;

public partial class Interface
{
    private void Checkmark(string label, string tooltip, bool value, Action<bool> setter)
    {
        if (ImGuiUtil.Checkbox(label, tooltip, value, setter))
            _config.Save();
    }

    private void ChangeAndSave<T>(T value, T currentValue, Action<T> setter) where T : IEquatable<T>
    {
        if (value.Equals(currentValue))
            return;

        setter(value);
        _config.Save();
    }

    private void DrawColorPicker(string name, string tooltip, uint value, uint defaultValue, Action<uint> setter)
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
        //const string buttonLabel = "Re-Register Penumbra";
        // TODO
        //if (ImGui.Button(buttonLabel))
        //    Glamourer.Penumbra.Reattach(true);

        //ImGuiUtil.HoverTooltip(
        //    "If Penumbra did not register the functions for some reason, pressing this button might help restore functionality.");
    }

    private void DrawSettingsTab()
    {
        using var tab = ImRaii.TabItem("Settings");
        if (!tab)
            return;

        ImGui.Dummy(_spacing);

        Checkmark("Folders First", "Sort Folders before all designs instead of lexicographically.", _config.FoldersFirst,
            v => _config.FoldersFirst = v);
        Checkmark("Color Designs", "Color the names of designs in the selector using the colors from below for the given cases.",
            _config.ColorDesigns,
            v => _config.ColorDesigns = v);
        Checkmark("Show Locks", "Write-protected Designs show a lock besides their name in the selector.", _config.ShowLocks,
            v => _config.ShowLocks = v);
        DrawRestorePenumbraButton();

        Checkmark("Apply Fixed Designs",
            "Automatically apply fixed designs to characters and redraw them when anything changes.",
            _config.ApplyFixedDesigns,
            v => { _config.ApplyFixedDesigns = v; });

        ImGui.Dummy(_spacing);

        DrawColorPicker("Customization Color", "The color for designs that only apply their character customization.",
            _config.CustomizationColor,            ConfigurationOld.DefaultCustomizationColor, c => _config.CustomizationColor = c);
        DrawColorPicker("Equipment Color", "The color for designs that only apply some or all of their equipment slots and stains.",
            _config.EquipmentColor,            ConfigurationOld.DefaultEquipmentColor, c => _config.EquipmentColor = c);
        DrawColorPicker("State Color", "The color for designs that only apply some state modification.",
            _config.StateColor,            ConfigurationOld.DefaultStateColor, c => _config.StateColor = c);
    }
}
