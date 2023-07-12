using System.Numerics;
using Dalamud.Interface;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

public static class UiHelpers
{
    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size)
    {
        var isEmpty = item.ModelId.Value == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item);
        if (empty)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size,
                ImGui.GetColorU32(isEmpty ? ImGuiCol.FrameBg : ImGuiCol.FrameBgActive), 5 * ImGuiHelpers.GlobalScale);
            if (ptr != nint.Zero)
                ImGui.Image(ptr, size, Vector2.Zero, Vector2.One,
                    isEmpty ? new Vector4(0.1f, 0.1f, 0.1f, 0.5f) : new Vector4(0.3f, 0.3f, 0.3f, 0.8f));
            else
                ImGui.Dummy(size);
        }
        else
        {
            ImGuiUtil.HoverIcon(ptr, textureSize, size);
        }
    }

    public static bool DrawCheckbox(string label, string tooltip, bool value, out bool on, bool locked)
    {
        using var disabled = ImRaii.Disabled(locked);
        var       ret      = ImGuiUtil.Checkbox(label, string.Empty, value, v => value = v);
        ImGuiUtil.HoverTooltip(tooltip);
        on = value;
        return ret;
    }

    public static bool DrawVisor(bool current, out bool on, bool locked)
        => DrawCheckbox("##visorToggled", string.Empty, current, out on, locked);

    public static bool DrawHat(bool current, out bool on, bool locked)
        => DrawCheckbox("##hatVisible", string.Empty, current, out on, locked);

    public static bool DrawWeapon(bool current, out bool on, bool locked)
        => DrawCheckbox("##weaponVisible", string.Empty, current, out on, locked);

    public static bool DrawWetness(bool current, out bool on, bool locked)
        => DrawCheckbox("##wetness", string.Empty, current, out on, locked);
}
