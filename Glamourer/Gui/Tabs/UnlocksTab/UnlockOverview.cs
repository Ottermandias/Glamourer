using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlockOverview
{
    private readonly ItemManager                _items;
    private readonly ItemUnlockManager          _itemUnlocks;
    private readonly CustomizationService       _customizations;
    private readonly CustomizeUnlockManager     _customizeUnlocks;
    private readonly PenumbraChangedItemTooltip _tooltip;

    private static readonly Vector4 UnavailableTint = new(0.3f, 0.3f, 0.3f, 1.0f);

    public UnlockOverview(ItemManager items, CustomizationService customizations, ItemUnlockManager itemUnlocks,
        CustomizeUnlockManager customizeUnlocks, PenumbraChangedItemTooltip tooltip)
    {
        _items            = items;
        _customizations   = customizations;
        _itemUnlocks      = itemUnlocks;
        _customizeUnlocks = customizeUnlocks;
        _tooltip          = tooltip;
    }

    public void Draw()
    {
        using var color = ImRaii.PushColor(ImGuiCol.Border, ImGui.GetColorU32(ImGuiCol.TableBorderStrong));
        using var child = ImRaii.Child("Panel", -Vector2.One, true);
        if (!child)
            return;

        var iconSize = ImGuiHelpers.ScaledVector2(32);
        foreach (var type in Enum.GetValues<FullEquipType>())
            DrawEquipTypeHeader(iconSize, type);

        iconSize = ImGuiHelpers.ScaledVector2(64);
        foreach (var gender in _customizations.AwaitedService.Genders)
        {
            foreach (var clan in _customizations.AwaitedService.Clans)
                DrawCustomizationHeader(iconSize, clan, gender);
        }
    }

    private void DrawCustomizationHeader(Vector2 iconSize, SubRace subRace, Gender gender)
    {
        var set = _customizations.AwaitedService.GetList(subRace, gender);
        if (set.HairStyles.Count == 0 && set.FacePaints.Count == 0)
            return;

        if (!ImGui.CollapsingHeader($"Unlockable {subRace.ToName()} {gender.ToName()} Customizations"))
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        foreach (var customization in set.HairStyles.Concat(set.FacePaints))
        {
            if (!_customizeUnlocks.Unlockable.TryGetValue(customization, out var unlockData))
                continue;

            var unlocked = _customizeUnlocks.IsUnlocked(customization, out var time);
            var icon     = _customizations.AwaitedService.GetIcon(customization.IconId);

            ImGui.Image(icon.ImGuiHandle, iconSize, Vector2.Zero, Vector2.One, unlocked ? Vector4.One : UnavailableTint);
            if (ImGui.IsItemHovered())
            {
                using var tt   = ImRaii.Tooltip();
                var       size = new Vector2(icon.Width, icon.Height);
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon.ImGuiHandle, size);
                ImGui.TextUnformatted(unlockData.Name);
                ImGui.TextUnformatted($"{customization.Index.ToDefaultName()} {customization.Value.Value}");
                ImGui.TextUnformatted(unlocked ? $"Unlocked on {time:g}" : "Not unlocked.");
            }

            ImGui.SameLine();
            if (ImGui.GetContentRegionAvail().X < iconSize.X)
                ImGui.NewLine();
        }

        if (ImGui.GetCursorPosX() != 0)
            ImGui.NewLine();
    }

    private void DrawEquipTypeHeader(Vector2 iconSize, FullEquipType type)
    {
        if (type.IsOffhandType() || !_items.ItemService.AwaitedService.TryGetValue(type, out var items) || items.Count == 0)
            return;

        if (!ImGui.CollapsingHeader($"{type.ToName()}s"))
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * ImGuiHelpers.GlobalScale));
        foreach (var item in items)
        {
            if (!ImGui.IsItemVisible())
            { }

            var unlocked = _itemUnlocks.IsUnlocked(item.Id, out var time);
            var icon     = _customizations.AwaitedService.GetIcon(item.IconId);

            ImGui.Image(icon.ImGuiHandle, iconSize, Vector2.Zero, Vector2.One, unlocked ? Vector4.One : UnavailableTint);
            if (ImGui.IsItemClicked())
            {
                // TODO link
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _tooltip.Player(out var state))
                _tooltip.ApplyItem(state, item);

            if (ImGui.IsItemHovered())
            {
                using var tt   = ImRaii.Tooltip();
                var       size = new Vector2(icon.Width, icon.Height);
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon.ImGuiHandle, size);
                ImGui.TextUnformatted(item.Name);
                var slot = item.Type.ToSlot();
                ImGui.TextUnformatted($"{item.Type.ToName()} ({slot.ToName()})");
                if (item.Type.Offhand().IsOffhandType())
                    ImGui.TextUnformatted(
                        $"{item.Weapon()}{(_items.ItemService.AwaitedService.TryGetValue(item.Id, false, out var offhand) ? $" | {offhand.Weapon()}" : string.Empty)}");
                else
                    ImGui.TextUnformatted(slot is EquipSlot.MainHand ? $"{item.Weapon()}" : $"{item.Armor()}");
                ImGui.TextUnformatted(
                    unlocked ? time == DateTimeOffset.MinValue ? "Always Unlocked" : $"Unlocked on {time:g}" : "Not Unlocked.");
                _tooltip.CreateTooltip(item, string.Empty, false);
            }

            ImGui.SameLine();
            if (ImGui.GetContentRegionAvail().X < iconSize.X)
                ImGui.NewLine();
        }

        if (ImGui.GetCursorPosX() != 0)
            ImGui.NewLine();
    }
}
