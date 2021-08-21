using System;
using System.Numerics;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private static void DrawConfigCheckMark(string label, string tooltip, bool value, Action<bool> setter)
        {
            if (DrawCheckMark(label, value, setter))
                Glamourer.Config.Save();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
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
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(
                    $"Reset to default: #{defaultValue & 0xFF:X2}{(defaultValue >> 8) & 0xFF:X2}{(defaultValue >> 16) & 0xFF:X2}{defaultValue >> 24:X2}");
            ImGui.SameLine();
            ImGui.Text(name);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void DrawConfigTab()
        {
            using var raii = new ImGuiRaii();
            if (!raii.Begin(() => ImGui.BeginTabItem("Config"), ImGui.EndTabItem))
                return;

            var cfg = Glamourer.Config;
            ImGui.Dummy(Vector2.UnitY * ImGui.GetTextLineHeightWithSpacing() / 2);

            DrawConfigCheckMark("Folders First", "Sort Folders before all designs instead of lexicographically.", cfg.FoldersFirst,
                v => cfg.FoldersFirst = v);
            DrawConfigCheckMark("Color Designs", "Color the names of designs in the selector using the colors from below for the given cases.",
                cfg.ColorDesigns,
                v => cfg.ColorDesigns = v);
            DrawConfigCheckMark("Show Locks", "Write-protected Designs show a lock besides their name in the selector.", cfg.ShowLocks,
                v => cfg.ShowLocks = v);

            ImGui.Dummy(Vector2.UnitY * ImGui.GetTextLineHeightWithSpacing() / 2);

            DrawColorPicker("Customization Color", "The color for designs that only apply their character customization.",
                cfg.CustomizationColor,            GlamourerConfig.DefaultCustomizationColor, c => cfg.CustomizationColor = c);
            DrawColorPicker("Equipment Color", "The color for designs that only apply some or all of their equipment slots and stains.",
                cfg.EquipmentColor,            GlamourerConfig.DefaultEquipmentColor, c => cfg.EquipmentColor = c);
            DrawColorPicker("State Color", "The color for designs that only apply some state modification.",
                cfg.StateColor,            GlamourerConfig.DefaultStateColor, c => cfg.StateColor = c);
        }
    }
}
