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
        using var bigGroup = ImRaii.Group();
        DrawMultiIcons();
        ImGui.SameLine();
        using var group = ImRaii.Group();
        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y / 2));

        _currentCount = 256;
        PercentageInputInt();

        ImGui.TextUnformatted(_set.Option(CustomizeIndex.LegacyTattoo));
    }

    private void DrawMultiIcons()
    {
        var       options = _set.Order[CharaMakeParams.MenuType.IconCheckmark];
        using var _       = ImRaii.Group();
        foreach (var (featureIdx, idx) in options.WithIndex())
        {
            using var id      = SetId(featureIdx);
            var       enabled = _customize.Get(featureIdx) != CustomizeValue.Zero;
            var       feature = _set.Data(featureIdx, 0, _customize.Face);
            var icon = featureIdx == CustomizeIndex.LegacyTattoo
                ? LegacyTattoo ?? Glamourer.Customization.GetIcon(feature.IconId)
                : Glamourer.Customization.GetIcon(feature.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int)ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero, enabled ? Vector4.One : RedTint))
            {
                _customize.Set(featureIdx, enabled ? CustomizeValue.Zero : CustomizeValue.Max);
                UpdateActors();
            }

            ImGuiUtil.HoverIconTooltip(icon, _iconSize);
            if (idx % 4 != 3)
                ImGui.SameLine();
        }
    }
}
