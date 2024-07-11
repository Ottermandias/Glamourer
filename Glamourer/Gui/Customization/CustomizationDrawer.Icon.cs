using Glamourer.GameData;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private const string IconSelectorPopup = "Style Picker";

    private void DrawIconSelector(CustomizeIndex index)
    {
        using var id       = SetId(index);
        using var bigGroup = ImRaii.Group();
        var       label    = _currentOption;

        var current         = _set.DataByValue(index, _currentByte, out var custom, _customize.Face);
        var originalCurrent = current;
        var npc             = false;
        if (current < 0)
        {
            label   = $"{_currentOption} (NPC)";
            current = 0;
            custom  = _set.Data(index, 0);
            npc     = true;
        }

        var icon    = _service.Manager.GetIcon(custom!.Value.IconId);
        var hasIcon = icon.TryGetWrap(out var wrap, out _);
        using (_ = ImRaii.Disabled(_locked || _currentIndex is CustomizeIndex.Face && _lockedRedraw))
        {
            if (ImGui.ImageButton(wrap?.ImGuiHandle ?? icon.GetWrapOrEmpty().ImGuiHandle, _iconSize))
            {
                ImGui.OpenPopup(IconSelectorPopup);
            }
            else if (originalCurrent >= 0 && CaptureMouseWheel(ref current, 0, _currentCount))
            {
                var data = _set.Data(_currentIndex, current, _customize.Face);
                UpdateValue(data.Value);
            }
        }

        if (hasIcon)
            ImGuiUtil.HoverIconTooltip(wrap!, _iconSize);

        ImGui.SameLine();
        using (_ = ImRaii.Group())
        {
            DataInputInt(current, npc);
            if (_lockedRedraw && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(
                    "The face can not be changed as this requires a redraw of the character, which is not supported for this actor.");

            if (_withApply)
            {
                ApplyCheckbox();
                ImGui.SameLine();
            }

            ImGui.TextUnformatted(label);
        }

        DrawIconPickerPopup(current);
    }

    private void DrawIconPickerPopup(int current)
    {
        using var popup = ImRaii.Popup(IconSelectorPopup, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize.Face);
            var icon   = _service.Manager.GetIcon(custom.IconId);
            using (var _ = ImRaii.Group())
            {
                var isFavorite = _favorites.Contains(_set.Gender, _set.Clan, _currentIndex, custom.Value);
                using var frameColor = current == i
                    ? ImRaii.PushColor(ImGuiCol.Button, Colors.SelectedRed)
                    : ImRaii.PushColor(ImGuiCol.Button, ColorId.FavoriteStarOn.Value(), isFavorite);
                var hasIcon = icon.TryGetWrap(out var wrap, out var _);

                if (ImGui.ImageButton(wrap?.ImGuiHandle ?? icon.GetWrapOrEmpty().ImGuiHandle, _iconSize))
                {
                    UpdateValue(custom.Value);
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    if (isFavorite)
                        _favorites.Remove(_set.Gender, _set.Clan, _currentIndex, custom.Value);
                    else
                        _favorites.TryAdd(_set.Gender, _set.Clan, _currentIndex, custom.Value);

                if (hasIcon)
                    ImGuiUtil.HoverIconTooltip(wrap!, _iconSize,
                        FavoriteManager.TypeAllowed(_currentIndex) ? "Right-Click to toggle favorite." : string.Empty);

                var text      = custom.Value.ToString();
                var textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_iconSize.X - textWidth + 2 * ImGui.GetStyle().FramePadding.X) / 2);
                ImGui.TextUnformatted(text);
            }

            if (i % 8 != 7)
                ImGui.SameLine();
        }
    }


    // Only used for facial features, so fixed ID.
    private void DrawMultiIconSelector()
    {
        using var bigGroup = ImRaii.Group();
        using var disabled = ImRaii.Disabled(_locked);
        DrawMultiIcons();
        ImGui.SameLine();
        using var group = ImRaii.Group();

        _currentCount = 256;
        if (_withApply)
        {
            ApplyCheckbox(CustomizeIndex.FacialFeature1);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + _spacing.X);
            ApplyCheckbox(CustomizeIndex.FacialFeature2);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature3);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature4);
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
        }

        var oldValue = _customize.AtIndex(_currentIndex.ToByteAndMask().ByteIdx);
        var tmp      = (int)oldValue.Value;
        ImGui.SetNextItemWidth(_inputIntSize);
        if (ImGui.InputInt("##text", ref tmp, 1, 1))
        {
            tmp = Math.Clamp(tmp, 0, byte.MaxValue);
            if (tmp != oldValue.Value)
            {
                _customize.SetByIndex(_currentIndex.ToByteAndMask().ByteIdx, (CustomizeValue)tmp);
                var changes = (byte)tmp ^ oldValue.Value;
                Changed |= ((changes & 0x01) == 0x01 ? CustomizeFlag.FacialFeature1 : 0)
                  | ((changes & 0x02) == 0x02 ? CustomizeFlag.FacialFeature2 : 0)
                  | ((changes & 0x04) == 0x04 ? CustomizeFlag.FacialFeature3 : 0)
                  | ((changes & 0x08) == 0x08 ? CustomizeFlag.FacialFeature4 : 0)
                  | ((changes & 0x10) == 0x10 ? CustomizeFlag.FacialFeature5 : 0)
                  | ((changes & 0x20) == 0x20 ? CustomizeFlag.FacialFeature6 : 0)
                  | ((changes & 0x40) == 0x40 ? CustomizeFlag.FacialFeature7 : 0)
                  | ((changes & 0x80) == 0x80 ? CustomizeFlag.LegacyTattoo : 0);
            }
        }

        if (_set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0)
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            using var _ = ImRaii.Enabled();
            ImGui.TextUnformatted("(Using Face 1)");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + _spacing.Y);
        ImGui.AlignTextToFramePadding();
        using (var _ = ImRaii.Enabled())
        {
            ImGui.TextUnformatted("Facial Features & Tattoos");
        }

        if (_withApply)
        {
            ApplyCheckbox(CustomizeIndex.FacialFeature5);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + _spacing.X);
            ApplyCheckbox(CustomizeIndex.FacialFeature6);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature7);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.LegacyTattoo);
        }
    }

    private void DrawMultiIcons()
    {
        var options = _set.Order[CharaMakeParams.MenuType.IconCheckmark];
        using var group = ImRaii.Group();
        var face = _set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0 ? _set.Faces[0].Value : _customize.Face;
        foreach (var (featureIdx, idx) in options.WithIndex())
        {
            using var id      = SetId(featureIdx);
            var       enabled = _customize.Get(featureIdx) != CustomizeValue.Zero;
            var       feature = _set.Data(featureIdx, 0, face);
            var icon = featureIdx == CustomizeIndex.LegacyTattoo
                ? _legacyTattoo ?? _service.Manager.GetIcon(feature.IconId)
                : _service.Manager.GetIcon(feature.IconId);
            var hasIcon = icon.TryGetWrap(out var wrap, out _);
            if (ImGui.ImageButton(wrap?.ImGuiHandle ?? icon.GetWrapOrEmpty().ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One,
                    (int)ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero, enabled ? Vector4.One : _redTint))
            {
                _customize.Set(featureIdx, enabled ? CustomizeValue.Zero : CustomizeValue.Max);
                Changed |= _currentFlag;
            }

            if (hasIcon)
                ImGuiUtil.HoverIconTooltip(wrap!, _iconSize);
            if (idx % 4 != 3)
                ImGui.SameLine();
        }
    }
}
