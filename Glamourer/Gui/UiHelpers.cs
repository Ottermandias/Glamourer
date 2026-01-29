using Dalamud.Interface;
using Glamourer.Services;
using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Lumina.Misc;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

public static class UiHelpers
{
    /// <summary> Open a combo popup with another method than the combo itself. </summary>
    public static void OpenCombo(string comboLabel)
    {
        var windowId = ImGui.GetID(comboLabel);
        var popupId  = ~Crc32.Get("##ComboPopup", windowId);
        ImGui.OpenPopup(popupId);
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, EquipSlot slot)
    {
        var isEmpty = item.PrimaryId.Id == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * Im.Style.GlobalScale);
            if (!ptr.IsNull)
                Im.Image.Draw(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                ImGui.Dummy(size);
        }
        else
        {
            Im.Image.DrawScaled(ptr, size, textureSize);
        }
    }

    public static void DrawIcon(this EquipItem item, TextureService textures, Vector2 size, BonusItemFlag slot)
    {
        var isEmpty = item.PrimaryId.Id == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGui.GetColorU32(ImGuiCol.FrameBg), Vector4.One)
                : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 1f));
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * Im.Style.GlobalScale);
            if (!ptr.IsNull)
                Im.Image.Draw(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                ImGui.Dummy(size);
        }
        else
        {
            Im.Image.DrawScaled(ptr, size, textureSize);
        }
    }

    public static bool DrawCheckbox(string label, string tooltip, bool value, out bool on, bool locked)
    {
        var  startsWithHash = label.StartsWith("##");
        bool ret;
        using (_ = ImRaii.Disabled(locked))
        {
            ret = ImGuiUtil.Checkbox(startsWithHash ? label : "##" + label, string.Empty, value, v => value = v);
        }

        if (!startsWithHash)
        {
            ImGui.SameLine(0, Im.Style.ItemInnerSpacing.X);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
        }

        ImGuiUtil.HoverTooltip(tooltip, ImGuiHoveredFlags.AllowWhenDisabled);
        on = value;
        return ret;
    }

    public static (bool, bool) DrawMetaToggle(string label, bool currentValue, bool currentApply, out bool newValue,
        out bool newApply, bool locked)
    {
        var flags = (sbyte)(currentApply ? currentValue ? 1 : -1 : 0);
        using (_ = ImRaii.Disabled(locked))
        {
            if (ImEx.TriStateCheckbox(ColorId.TriStateCross.Value(), ColorId.TriStateCheck.Value(), ColorId.TriStateNeutral.Value()).Draw(
                    "##" + label, flags, out flags))
            {
                (newValue, newApply) = flags switch
                {
                    -1 => (false, true),
                    0  => (true, false),
                    _  => (true, true),
                };
            }
            else
            {
                newValue = currentValue;
                newApply = currentApply;
            }
        }

        ImGuiUtil.HoverTooltip($"This attribute will be {(currentApply ? currentValue ? "enabled." : "disabled." : "kept as is.")}");

        ImGui.SameLine(0, Im.Style.ItemInnerSpacing.X);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);

        return (currentValue != newValue, currentApply != newApply);
    }

    public static (bool, bool) ConvertKeysToBool()
        => (ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift) switch
        {
            (false, false) => (true, true),
            (true, true)   => (true, true),
            (true, false)  => (true, false),
            (false, true)  => (false, true),
        };

    public static bool DrawFavoriteStar(FavoriteManager favorites, EquipItem item)
    {
        var favorite = favorites.Contains(item);
        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(),
            ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text,
            hovering ? ColorId.FavoriteStarHovered.Value() : favorite ? ColorId.FavoriteStarOn.Value() : ColorId.FavoriteStarOff.Value());
        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (!ImGui.IsItemClicked())
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
        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(),
            ImGui.GetCursorScreenPos() + new Vector2(Im.Style.FrameHeight));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text,
            hovering ? ColorId.FavoriteStarHovered.Value() : favorite ? ColorId.FavoriteStarOn.Value() : ColorId.FavoriteStarOff.Value());
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (!ImGui.IsItemClicked())
            return false;

        if (favorite)
            favorites.Remove(stain);
        else
            favorites.TryAdd(stain);
        return true;

    }
}