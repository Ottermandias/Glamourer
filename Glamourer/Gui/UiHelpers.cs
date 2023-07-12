
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
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
}