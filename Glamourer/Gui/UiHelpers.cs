using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Lumina.Misc;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

public static class UiHelpers
{
    /// <summary> Open a combo popup with another method than the combo itself. </summary>
    public static void OpenCombo(string comboLabel)
    {
        var windowId = Im.Id.Get(comboLabel);
        var popupId  = ~Crc32.Get("##ComboPopup", windowId.Id);
        Im.Popup.Open(popupId);
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, EquipSlot slot)
    {
        var isEmpty = item.PrimaryId.Id is 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGuiColor.FrameBackground.Get(), Vector4.One)
                : (ImGuiColor.FrameBackgroundActive.Get(), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = Im.Cursor.ScreenPosition;
            Im.Window.DrawList.Shape.RectangleFilled(pos, pos + size, bgColor, 5 * Im.Style.GlobalScale);
            if (!ptr.IsNull)
                Im.Image.Draw(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                Im.Dummy(size);
        }
        else
        {
            Im.Image.DrawScaled(ptr, size, textureSize);
        }
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot)
    {
        var isEmpty = item.PrimaryId.Id is 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGuiColor.FrameBackground.Get(), Vector4.One)
                : (ImGuiColor.FrameBackgroundActive.Get(), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = Im.Cursor.ScreenPosition;
            Im.Window.DrawList.Shape.RectangleFilled(pos, pos + size, bgColor, 5 * Im.Style.GlobalScale);
            if (!ptr.IsNull)
                Im.Image.Draw(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                Im.Dummy(size);
        }
        else
        {
            Im.Image.DrawScaled(ptr, size, textureSize);
        }
    }

    public static bool DrawCheckbox(ReadOnlySpan<byte> label, Utf8StringHandler<TextStringHandlerBuffer> tooltip, bool value, out bool on, bool locked)
    {
        bool ret;
        using (Im.Disabled(locked))
        {
            using var id = Im.Id.Push(label);
            ret = Im.Checkbox(StringU8.Empty, ref value);
        }

        if (!label.StartsWith("##"u8))
        {
            Im.Line.SameInner();
            ImEx.TextFrameAligned(label);
        }

        Im.Tooltip.OnHover(tooltip, HoveredFlags.AllowWhenDisabled);
        on = value;
        return ret;
    }

    public static (bool, bool) DrawMetaToggle(ReadOnlySpan<byte> label, bool currentValue, bool currentApply, out bool newValue,
        out bool newApply, bool locked)
    {
        bool? apply = currentApply ? currentValue : null;
        using (Im.Disabled(locked))
        {
            using var id = Im.Id.Push(label);
            if (ImEx.TriStateCheckbox(StringU8.Empty, ref apply, ColorId.TriStateNeutral.Value(), ColorId.TriStateCheck.Value(), ColorId.TriStateCross.Value()))
            {
                (newValue, newApply) = apply switch
                {
                    true  => (true, true),
                    false => (false, true),
                    _     => (true, false),
                };
            }
            else
            {
                newValue = currentValue;
                newApply = currentApply;
            }
        }

        Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
            $"This attribute will be {(currentApply ? currentValue ? "enabled." : "disabled." : "kept as is.")}");

        Im.Line.SameInner();
        ImEx.TextFrameAligned(label);

        return (currentValue != newValue, currentApply != newApply);
    }

    public static (bool, bool) ConvertKeysToBool()
        => (Im.Io.KeyControl, Im.Io.KeyShift) switch
        {
            (false, false) => (true, true),
            (true, true)   => (true, true),
            (true, false)  => (true, false),
            (false, true)  => (false, true),
        };

    public static bool DrawFavoriteStar(FavoriteManager favorites, EquipItem item)
    {
        var favorite = favorites.Contains(item);
        var hovering = Im.Mouse.IsHoveringRectangle(Rectangle.FromSize(Im.Cursor.ScreenPosition, new Vector2(Im.Style.TextHeight)));

        ImEx.Icon.DrawAligned(LunaStyle.FavoriteIcon,
            hovering ? ColorId.FavoriteStarHovered.Value() : favorite ? ColorId.FavoriteStarOn.Value() : ColorId.FavoriteStarOff.Value());
        if (!Im.Item.Clicked())
            return false;

        if (favorite)
            favorites.Remove(item);
        else
            favorites.TryAdd(item);
        return true;
    }

    public static bool DrawFavoriteStar(FavoriteManager favorites, StainId stain)
    {
        var favorite = favorites.Contains(stain);
        var hovering = Im.Mouse.IsHoveringRectangle(Rectangle.FromSize(Im.Cursor.ScreenPosition, new Vector2(Im.Style.TextHeight)));

        ImEx.Icon.DrawAligned(LunaStyle.FavoriteIcon,
            hovering ? ColorId.FavoriteStarHovered.Value() : favorite ? ColorId.FavoriteStarOn.Value() : ColorId.FavoriteStarOff.Value());
        if (!Im.Item.Clicked())
            return false;

        if (favorite)
            favorites.Remove(stain);
        else
            favorites.TryAdd(stain);
        return true;
    }
}
