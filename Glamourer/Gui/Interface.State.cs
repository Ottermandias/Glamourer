using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace Glamourer.Gui;

internal partial class Interface
{
    private static Vector2 _spacing        = Vector2.Zero;
    private static float   _actorSelectorWidth;

    private static void UpdateState()
    {
        _spacing            = _spacing with { Y = ImGui.GetTextLineHeightWithSpacing() / 2 };
        _actorSelectorWidth = 200 * ImGuiHelpers.GlobalScale;
    }
}
