using System;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private static bool DrawCheckMark(string label, bool value, Action<bool> setter)
        {
            var startValue = value;
            if (ImGui.Checkbox(label, ref startValue) && startValue != value)
            {
                setter(startValue);
                return true;
            }

            return false;
        }

        private static bool DrawMiscellaneous(CharacterSave save, Character? player)
        {
            var ret = false;
            if (!ImGui.CollapsingHeader("Miscellaneous"))
                return ret;

            ret |= DrawCheckMark("Hat Visible", save.HatState, v =>
            {
                save.HatState = v;
                player?.SetHatHidden(!v);
            });

            ret |= DrawCheckMark("Weapon Visible", save.WeaponState, v =>
            {
                save.WeaponState = v;
                player?.SetWeaponHidden(!v);
            });

            ret |= DrawCheckMark("Visor Toggled", save.VisorState, v =>
            {
                save.VisorState = v;
                player?.SetVisorToggled(v);
            });

            ret |= DrawCheckMark("Is Wet", save.IsWet, v =>
            {
                save.IsWet = v;
                player?.SetWetness(v);
            });

            var alpha = save.Alpha;
            if (ImGui.DragFloat("Alpha", ref alpha, 0.01f, 0f, 1f, "%.2f") && alpha != save.Alpha)
            {
                alpha      = (float) Math.Round(alpha > 1 ? 1 : alpha < 0 ? 0 : alpha, 2);
                save.Alpha = alpha;
                ret        = true;
                if (player != null)
                    player.Alpha() = alpha;
            }

            return ret;
        }
    }
}
