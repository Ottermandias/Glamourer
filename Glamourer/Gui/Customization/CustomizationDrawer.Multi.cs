using System.Linq;
using System.Numerics;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

internal partial class CustomizationDrawer
{
    // Only used for facial features, so fixed ID.
    private void DrawMultiIconSelector()
    {
        using var _       = SetId(CustomizationId.FacialFeaturesTattoos);
        using var bigGroup = ImRaii.Group();

        DrawMultiIcons();
        ImGui.SameLine();
        using var group = ImRaii.Group();
        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y / 2));

        _currentCount = 256;
        PercentageInputInt();

        ImGui.TextUnformatted(_set.Option(CustomizationId.FacialFeaturesTattoos));
    }

    private void DrawMultiIcons()
    {
        using var _    = ImRaii.Group();
        for (var i = 0; i < _currentCount; ++i)
        {
            var enabled = _customize.FacialFeatures[i];
            var feature = _set.FacialFeature(_customize.Face, i);
            var icon = i == _currentCount - 1
                ? LegacyTattoo ?? Glamourer.Customization.GetIcon(feature.IconId)
                : Glamourer.Customization.GetIcon(feature.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int)ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero, enabled ? Vector4.One : RedTint))
            {
                _customize.FacialFeatures.Set(i, !enabled);
                UpdateActors();
            }

            ImGuiUtil.HoverIconTooltip(icon, _iconSize);
            if (i % 4 != 3)
                ImGui.SameLine();
        }
    }
}
