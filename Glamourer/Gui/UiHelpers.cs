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
    public static void OpenCombo(ReadOnlySpan<byte> comboLabel)
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
            AddItemNameToTooltip(item, textureSize);
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
            AddItemNameToTooltip(item, textureSize);
        }
    }

    private static void AddItemNameToTooltip(EquipItem item, Vector2 textureSize)
    {
        if (!Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            return;

        using var tt = Im.Tooltip.Begin();
        Im.Line.Same();
        Im.Cursor.Y += Math.Max(0.0f, (textureSize.Y - Im.Style.TextHeight) * 0.5f);
        Im.Text(item.Name);
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

    public static bool DrawItemSlots(Utf8StringHandler<LabelStringHandlerBuffer> id, ref CombinedItemSlotFlag slots,
        CombinedItemSlotFlag allowedSlots = EquipFlagExtensions.AllCombined)
    {
        var first   = true;
        var changed = false;
        var control = Im.Io.KeyControl;

        allowedSlots &= ~slots;

        using var _     = Im.Id.Push(ref id);
        using var group = Im.Group();

        var remainingSlots = slots;
        while (remainingSlots is not 0)
        {
            // Extract the least significant bit.
            var slot = unchecked(remainingSlots & (~remainingSlots + 1));
            TrySameLine(Im.Font.CalculateButtonSize(slot.ToLabelU8()).X, ref first);
            Im.Button(slot.ToLabelU8());
            var delete = control && Im.Item.RightClicked();
            Im.Tooltip.OnHover("Hold control and right-click to delete."u8);
            if (delete)
            {
                slots   &= ~slot;
                changed =  true;
            }

            remainingSlots &= ~slot;
        }

        if (slots is not 0)
        {
            TrySameLine(Im.Style.FrameHeight, ref first);
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Hold control and click to delete all slots."u8, !control))
            {
                slots   = 0;
                changed = true;
            }
        }

        if (allowedSlots is 0)
            return changed;

        if (slots is not 0)
            TrySameLine(Im.Style.FrameHeight, ref first);
        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, "Add Slots"u8))
            Im.Popup.Open("Add Slots"u8);

        using var popup = Im.Popup.Begin("Add Slots"u8);
        if (!popup)
            return changed;

        using (Im.Disabled(!control))
        {
            if (Im.Selectable("All"u8))
            {
                slots   |= allowedSlots;
                changed =  true;
            }
        }

        Im.Tooltip.OnHover("Hold control and click to add all slots."u8);
        Im.Separator();

        remainingSlots = allowedSlots;
        while (remainingSlots is not 0)
        {
            // Extract the least significant bit.
            var slot = unchecked(remainingSlots & (~remainingSlots + 1));
            if (Im.Selectable(slot.ToLabelU8(), flags: control && slot != allowedSlots ? SelectableFlags.NoAutoClosePopups : 0))
            {
                slots   |= slot;
                changed =  true;
            }

            remainingSlots &= ~slot;
        }

        return changed;
    }

    private static void TrySameLine(float minWidth, ref bool first)
    {
        if (first)
        {
            first = false;
            return;
        }

        Im.Line.Same();
        if (Im.ContentRegion.Available.X < minWidth + Im.Style.ItemSpacing.X)
            Im.Line.New();
    }
}
