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

            ImGuiCustom.HoverTooltip(tooltip);
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
            ImGuiCustom.HoverTooltip(
                $"Reset to default: #{defaultValue & 0xFF:X2}{(defaultValue >> 8) & 0xFF:X2}{(defaultValue >> 16) & 0xFF:X2}{defaultValue >> 24:X2}");
            ImGui.SameLine();
            ImGui.Text(name);
            ImGuiCustom.HoverTooltip(tooltip);
        }

        private void DrawRestorePenumbraButton()
        {
            const string buttonLabel = "Re-Register Penumbra";
            if (!Glamourer.Config.AttachToPenumbra)
            {
                using var raii = new ImGuiRaii().PushStyle(ImGuiStyleVar.Alpha, 0.5f);
                ImGui.Button(buttonLabel);
                return;
            }

            if (ImGui.Button(buttonLabel))
                Glamourer.Penumbra.Reattach(true);

            ImGuiCustom.HoverTooltip(
                    "If Penumbra did not register the functions for some reason, pressing this button might help restore functionality.");
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
            DrawConfigCheckMark("Attach to Penumbra",
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

            DrawConfigCheckMark("Apply Fixed Designs",
                "Automatically apply fixed designs to characters and redraw them when anything changes.",
                cfg.ApplyFixedDesigns,
                v =>
                {
                    cfg.ApplyFixedDesigns = v;
                    if (v)
                        Glamourer.PlayerWatcher.Enable();
                    else
                        Glamourer.PlayerWatcher.Disable();
                });

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
