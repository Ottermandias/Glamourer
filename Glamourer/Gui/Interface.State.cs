using System;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Interop;
using ImGuiNET;

namespace Glamourer.Gui;

internal partial class Interface
{
    private static readonly ImGuiScene.TextureWrap? LegacyTattoo = GetLegacyTattooIcon();
    private static readonly Vector4                 RedTint      = new(0.6f, 0.3f, 0.3f, 1f);


    private static Vector2 _iconSize       = Vector2.Zero;
    private static Vector2 _framedIconSize = Vector2.Zero;
    private static Vector2 _spacing        = Vector2.Zero;
    private static float   _actorSelectorWidth;
    private static float   _inputIntSize;
    private static float   _comboSelectorSize;
    private static float   _raceSelectorWidth;


    private static void UpdateState()
    {
        // General
        _spacing            = _spacing with { Y = ImGui.GetTextLineHeightWithSpacing() / 2 };
        _actorSelectorWidth = 200 * ImGuiHelpers.GlobalScale;

        // Customize
        _iconSize          = new Vector2(ImGui.GetTextLineHeightWithSpacing() * 2);
        _framedIconSize    = _iconSize + 2 * ImGui.GetStyle().FramePadding;
        _inputIntSize      = 2 * _framedIconSize.X + ImGui.GetStyle().ItemSpacing.X;
        _comboSelectorSize = 4 * _framedIconSize.X + 3 * ImGui.GetStyle().ItemSpacing.X;
        _raceSelectorWidth = _inputIntSize + _comboSelectorSize - _framedIconSize.X;

        //        _itemComboWidth    = 6 * _actualIconSize.X + 4 * ImGui.GetStyle().ItemSpacing.X - ColorButtonWidth + 1;
    }

    private static ImGuiScene.TextureWrap? GetLegacyTattooIcon()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
        if (resource != null)
        {
            var rawImage = new byte[resource.Length];
            var length   = resource.Read(rawImage, 0, (int)resource.Length);
            if (length != resource.Length)
                return null;

            return Dalamud.PluginInterface.UiBuilder.LoadImageRaw(rawImage, 192, 192, 4);
        }

        return null;
    }
}
