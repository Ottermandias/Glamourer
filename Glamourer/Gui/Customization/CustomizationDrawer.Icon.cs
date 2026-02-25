using Dalamud.Interface.Textures.TextureWraps;
using Glamourer.GameData;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private static ReadOnlySpan<byte> IconSelectorPopup
        => "Style Picker"u8;

    private void DrawIconSelector(CustomizeIndex index)
    {
        using var id       = SetId(index);
        using var bigGroup = Im.Group();
        var       label    = _currentOption;

        var current         = _set.DataByValue(index, _currentByte, out var custom, _customize.Face);
        var originalCurrent = current;
        var npc             = false;
        if (current < 0)
        {
            label   = new StringU8($"{_currentOption} (NPC)");
            current = 0;
            custom  = _set.Data(index, 0);
            npc     = true;
        }

        var icon    = service.Manager.GetIcon(custom!.Value.IconId);
        var hasIcon = icon.TryGetWrap(out var wrap, out _);
        using (Im.Disabled(_locked || _currentIndex is CustomizeIndex.Face && _lockedRedraw))
        {
            if (Im.Image.Button(wrap?.Id ?? icon.GetWrapOrEmpty().Id, _iconSize))
            {
                Im.Popup.Open(IconSelectorPopup);
            }
            else if (originalCurrent >= 0 && CaptureMouseWheel(ref current, 0, _currentCount))
            {
                var data = _set.Data(_currentIndex, current, _customize.Face);
                UpdateValue(data.Value);
            }
        }

        if (hasIcon)
            Im.Tooltip.ImageOnHover(wrap!.Id, wrap!.Size);

        Im.Line.Same();
        using (Im.Group())
        {
            DataInputInt(current, npc);
            if (_lockedRedraw)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
                    "The face can not be changed as this requires a redraw of the character, which is not supported for this actor."u8);

            if (_withApply)
            {
                ApplyCheckbox();
                Im.Line.Same();
            }

            Im.Text(label);
        }

        DrawIconPickerPopup(current);
    }

    private void DrawIconPickerPopup(int current)
    {
        using var popup = Im.Popup.Begin(IconSelectorPopup, WindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImStyleDouble.ItemSpacing.Push(Vector2.Zero)
            .Push(ImStyleSingle.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize.Face);
            var icon   = service.Manager.GetIcon(custom.IconId);
            using (Im.Group())
            {
                var isFavorite = favorites.Contains(_set.Gender, _set.Clan, _currentIndex, custom.Value);
                using var frameColor = current == i
                    ? ImGuiColor.Button.Push(Colors.SelectedRed)
                    : ImGuiColor.Button.Push(ColorId.FavoriteStarOn.Value(), isFavorite);
                var hasIcon = icon.TryGetWrap(out var wrap, out _);

                if (Im.Image.Button(wrap?.Id ?? icon.GetWrapOrEmpty().Id, _iconSize))
                {
                    UpdateValue(custom.Value);
                    Im.Popup.CloseCurrent();
                }

                if (Im.Item.RightClicked())
                    if (isFavorite)
                        favorites.Remove(_set.Gender, _set.Clan, _currentIndex, custom.Value);
                    else
                        favorites.TryAdd(_set.Gender, _set.Clan, _currentIndex, custom.Value);

                if (hasIcon)
                    Im.Tooltip.ImageOnHover(wrap!.Id, _iconSize,
                        FavoriteManager.TypeAllowed(_currentIndex) ? "Right-Click to toggle favorite."u8 : StringU8.Empty);

                var text      = new StringU8($"{custom.Value.Value}");
                var textWidth = Im.Font.CalculateButtonSize(text).X;
                Im.Cursor.X += +(_iconSize.X - textWidth) / 2;
                Im.Text(text);
            }

            if (i % 8 is not 7)
                Im.Line.Same();
        }
    }


    // Only used for facial features, so fixed ID.
    private void DrawMultiIconSelector()
    {
        using var bigGroup = Im.Group();
        using var disabled = Im.Disabled(_locked);
        DrawMultiIcons();
        Im.Line.Same();
        using var group = Im.Group();

        _currentCount = 256;
        if (_withApply)
        {
            ApplyCheckbox(CustomizeIndex.FacialFeature1);
            Im.Line.Same();
            Im.Cursor.X += _spacing.X;
            ApplyCheckbox(CustomizeIndex.FacialFeature2);
            Im.Line.Same();
            ApplyCheckbox(CustomizeIndex.FacialFeature3);
            Im.Line.Same();
            ApplyCheckbox(CustomizeIndex.FacialFeature4);
        }
        else
        {
            Im.FrameDummy();
        }

        var oldValue = _customize.AtIndex(_currentIndex.ToByteAndMask().ByteIdx);
        var tmp      = (int)oldValue.Value;
        Im.Item.SetNextWidth(_inputIntSize);
        if (Im.Input.Scalar("##text"u8, ref tmp, 1, 1))
        {
            tmp = Math.Clamp(tmp, 0, byte.MaxValue);
            if (tmp != oldValue.Value)
            {
                _customize.SetByIndex(_currentIndex.ToByteAndMask().ByteIdx, (CustomizeValue)tmp);
                var changes = (byte)tmp ^ oldValue.Value;
                Changed |= ((changes & 0x01) is 0x01 ? CustomizeFlag.FacialFeature1 : 0)
                  | ((changes & 0x02) is 0x02 ? CustomizeFlag.FacialFeature2 : 0)
                  | ((changes & 0x04) is 0x04 ? CustomizeFlag.FacialFeature3 : 0)
                  | ((changes & 0x08) is 0x08 ? CustomizeFlag.FacialFeature4 : 0)
                  | ((changes & 0x10) is 0x10 ? CustomizeFlag.FacialFeature5 : 0)
                  | ((changes & 0x20) is 0x20 ? CustomizeFlag.FacialFeature6 : 0)
                  | ((changes & 0x40) is 0x40 ? CustomizeFlag.FacialFeature7 : 0)
                  | ((changes & 0x80) is 0x80 ? CustomizeFlag.LegacyTattoo : 0);
            }
        }

        if (_set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0)
        {
            Im.Line.Same();
            using var _ = Im.Enabled();
            ImEx.TextFrameAligned("(Using Face 1)"u8);
        }

        Im.Cursor.Y += _spacing.Y;
        using (Im.Enabled())
        {
            ImEx.TextFrameAligned("Facial Features & Tattoos"u8);
        }

        if (!_withApply)
            return;

        ApplyCheckbox(CustomizeIndex.FacialFeature5);
        Im.Line.Same();
        Im.Cursor.X += _spacing.X;
        ApplyCheckbox(CustomizeIndex.FacialFeature6);
        Im.Line.Same();
        ApplyCheckbox(CustomizeIndex.FacialFeature7);
        Im.Line.Same();
        ApplyCheckbox(CustomizeIndex.LegacyTattoo);
    }

    private void DrawMultiIcons()
    {
        var options = _set.Order[MenuType.IconCheckmark];
        using var group = Im.Group();
        var face = _set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0 ? _set.Faces[0].Value : _customize.Face;
        foreach (var (idx, featureIdx) in options.Index())
        {
            using var            id      = SetId(featureIdx);
            var                  enabled = _customize.Get(featureIdx) != CustomizeValue.Zero;
            var                  feature = _set.Data(featureIdx, 0, face);
            bool                 hasIcon;
            IDalamudTextureWrap? wrap;
            var                  icon = service.Manager.GetIcon(feature.IconId);
            if (featureIdx is CustomizeIndex.LegacyTattoo)
            {
                wrap    = _legacyTattoo;
                hasIcon = wrap is not null;
            }
            else
            {
                hasIcon = icon.TryGetWrap(out wrap, out _);
            }

            if (Im.Image.Button(wrap?.Id ?? icon.GetWrapOrEmpty().Id, _iconSize, Vector2.Zero, Vector2.One,
                    enabled ? Vector4.One : _redTint, Vector4.Zero,
                    (int)Im.Style.FramePadding.X))
            {
                _customize.Set(featureIdx, enabled ? CustomizeValue.Zero : CustomizeValue.Max);
                Changed |= _currentFlag;
            }

            if (hasIcon)
                Im.Tooltip.ImageOnHover(wrap!.Id, wrap!.Size);
            if (idx % 4 is not 3)
                Im.Line.Same();
        }
    }
}
