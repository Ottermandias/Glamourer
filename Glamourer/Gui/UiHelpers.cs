using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using Glamourer.Unlocks;
using ImGuiNET;
using Lumina.Misc;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

[Flags]
public enum DataChange : byte
{
    None        = 0x00,
    Item        = 0x01,
    Stain       = 0x02,
    ApplyItem   = 0x04,
    ApplyStain  = 0x08,
    Item2       = 0x10,
    Stain2      = 0x20,
    ApplyItem2  = 0x40,
    ApplyStain2 = 0x80,
}

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
        var isEmpty = item.ModelId.Id == 0;
        var (ptr, textureSize, empty) = textures.GetIcon(item, slot);
        if (empty)
        {
            var (bgColor, tint) = isEmpty
                ? (ImGui.GetColorU32(ImGuiCol.FrameBg), new Vector4(0.1f,       0.1f, 0.1f, 0.5f))
                : (ImGui.GetColorU32(ImGuiCol.FrameBgActive), new Vector4(0.3f, 0.3f, 0.3f, 0.8f));
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, bgColor, 5 * ImGuiHelpers.GlobalScale);
            if (ptr != nint.Zero)
                ImGui.Image(ptr, size, Vector2.Zero, Vector2.One, tint);
            else
                ImGui.Dummy(size);
        }
        else
        {
            ImGuiUtil.HoverIcon(ptr, textureSize, size);
        }
    }

    public static bool DrawCheckbox(string label, string tooltip, bool value, out bool on, bool locked)
    {
        using var disabled = ImRaii.Disabled(locked);
        var       ret      = ImGuiUtil.Checkbox(label, string.Empty, value, v => value = v);
        ImGuiUtil.HoverTooltip(tooltip);
        on = value;
        return ret;
    }

    public static (bool, bool) DrawMetaToggle(string label, bool currentValue, bool currentApply, out bool newValue,
        out bool newApply, bool locked)
    {
        var       flags = (sbyte)(currentApply ? currentValue ? 1 : -1 : 0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        using (var disabled = ImRaii.Disabled(locked))
        {
            if (new TristateCheckbox(ColorId.TriStateCross.Value(), ColorId.TriStateCheck.Value(), ColorId.TriStateNeutral.Value()).Draw(
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

        ImGui.SameLine();
        ImGui.TextUnformatted(label);

        return (currentValue != newValue, currentApply != newApply);
    }

    public static (EquipFlag, CustomizeFlag) ConvertKeysToFlags()
        => (ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift) switch
        {
            (false, false) => (EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant),
            (true, true)   => (EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant),
            (true, false)  => (EquipFlagExtensions.All, (CustomizeFlag)0),
            (false, true)  => ((EquipFlag)0, CustomizeFlagExtensions.AllRelevant),
        };

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
        if (ImGui.IsItemClicked())
        {
            if (favorite)
                favorites.Remove(item);
            else
                favorites.TryAdd(item);
            return true;
        }

        return false;
    }

    public static bool DrawFavoriteStar(FavoriteManager favorites, StainId stain)
    {
        var favorite = favorites.Contains(stain);
        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(),
            ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetFrameHeight()));

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        using var c = ImRaii.PushColor(ImGuiCol.Text,
            hovering ? ColorId.FavoriteStarHovered.Value() : favorite ? ColorId.FavoriteStarOn.Value() : ColorId.FavoriteStarOff.Value());
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString());
        if (ImGui.IsItemClicked())
        {
            if (favorite)
                favorites.Remove(stain);
            else
                favorites.TryAdd(stain);
            return true;
        }

        return false;
    }
}
