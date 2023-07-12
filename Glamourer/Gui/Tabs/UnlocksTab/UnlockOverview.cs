using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlockOverview
{
    private readonly ItemManager                _items;
    private readonly ItemUnlockManager          _itemUnlocks;
    private readonly CustomizationService       _customizations;
    private readonly CustomizeUnlockManager     _customizeUnlocks;
    private readonly PenumbraChangedItemTooltip _tooltip;
    private readonly TextureService             _textures;

    private static readonly Vector4 UnavailableTint = new(0.3f, 0.3f, 0.3f, 1.0f);

    private FullEquipType _selected1 = FullEquipType.Unknown;
    private SubRace       _selected2 = SubRace.Unknown;
    private Gender        _selected3 = Gender.Unknown;

    private void DrawSelector()
    {
        using var child = ImRaii.Child("Selector", new Vector2(200 * ImGuiHelpers.GlobalScale, -1), true);
        if (!child)
            return;

        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.IsOffhandType() || !_items.ItemService.AwaitedService.TryGetValue(type, out var items) || items.Count == 0)
                continue;

            if (ImGui.Selectable(type.ToName(), _selected1 == type))
            {
                _selected1 = type;
                _selected2 = SubRace.Unknown;
                _selected3 = Gender.Unknown;
            }
        }

        foreach (var clan in _customizations.AwaitedService.Clans)
        {
            foreach (var gender in _customizations.AwaitedService.Genders)
            {
                if (_customizations.AwaitedService.GetList(clan, gender).HairStyles.Count == 0)
                    continue;

                if (ImGui.Selectable($"{(gender is Gender.Male ? '♂' : '♀')} {clan.ToShortName()} Hair & Paint",
                        _selected2 == clan && _selected3 == gender))
                {
                    _selected1 = FullEquipType.Unknown;
                    _selected2 = clan;
                    _selected3 = gender;
                }
            }
        }
    }

    public UnlockOverview(ItemManager items, CustomizationService customizations, ItemUnlockManager itemUnlocks,
        CustomizeUnlockManager customizeUnlocks, PenumbraChangedItemTooltip tooltip, TextureService textures)
    {
        _items            = items;
        _customizations   = customizations;
        _itemUnlocks      = itemUnlocks;
        _customizeUnlocks = customizeUnlocks;
        _tooltip          = tooltip;
        _textures         = textures;
    }

    public void Draw()
    {
        using var color = ImRaii.PushColor(ImGuiCol.Border, ImGui.GetColorU32(ImGuiCol.TableBorderStrong));
        DrawSelector();
        ImGui.SameLine();
        DrawPanel();
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("Panel", -Vector2.One, true);
        if (!child)
            return;

        if (_selected1 is not FullEquipType.Unknown)
            DrawItems();
        else if (_selected2 is not SubRace.Unknown && _selected3 is not Gender.Unknown)
            DrawCustomizations();
    }

    private void DrawCustomizations()
    {
        var set = _customizations.AwaitedService.GetList(_selected2, _selected3);

        var       spacing     = IconSpacing;
        using var style       = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       iconSize    = ImGuiHelpers.ScaledVector2(128);
        var       iconsPerRow = IconsPerRow(iconSize.X, spacing.X);

        var counter = 0;
        foreach (var customize in set.HairStyles.Concat(set.FacePaints))
        {
            if (!_customizeUnlocks.Unlockable.TryGetValue(customize, out var unlockData))
                continue;

            var unlocked = _customizeUnlocks.IsUnlocked(customize, out var time);
            var icon     = _customizations.AwaitedService.GetIcon(customize.IconId);

            ImGui.Image(icon.ImGuiHandle, iconSize, Vector2.Zero, Vector2.One, unlocked ? Vector4.One : UnavailableTint);
            if (ImGui.IsItemHovered())
            {
                using var tt   = ImRaii.Tooltip();
                var       size = new Vector2(icon.Width, icon.Height);
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon.ImGuiHandle, size);
                ImGui.TextUnformatted(unlockData.Name);
                ImGui.TextUnformatted($"{customize.Index.ToDefaultName()} {customize.Value.Value}");
                ImGui.TextUnformatted(unlocked ? $"Unlocked on {time:g}" : "Not unlocked.");
            }

            if (counter != iconsPerRow - 1)
            {
                ImGui.SameLine();
                ++counter;
            }
            else
            {
                counter = 0;
            }
        }
    }

    private void DrawItems()
    {
        if (!_items.ItemService.AwaitedService.TryGetValue(_selected1, out var items))
            return;

        var       spacing        = IconSpacing;
        using var style          = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       iconSize       = ImGuiHelpers.ScaledVector2(64);
        var       iconsPerRow    = IconsPerRow(iconSize.X, spacing.X);
        var       numRows        = (items.Count + iconsPerRow - 1) / iconsPerRow;
        var       numVisibleRows = (int)(Math.Ceiling(ImGui.GetContentRegionAvail().Y / (iconSize.Y + spacing.Y)) + 0.5f);

        void DrawItem(EquipItem item)
        {
            var unlocked   = _itemUnlocks.IsUnlocked(item.ItemId, out var time);
            var iconHandle = _textures.LoadIcon(item.IconId);
            if (!iconHandle.HasValue)
                return;

            var (icon, size) = iconHandle.Value.Value;

            ImGui.Image(icon, iconSize, Vector2.Zero, Vector2.One, unlocked ? Vector4.One : UnavailableTint);
            if (ImGui.IsItemClicked())
            {
                // TODO link
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _tooltip.Player(out var state))
                _tooltip.ApplyItem(state, item);

            if (ImGui.IsItemHovered())
            {
                using var tt = ImRaii.Tooltip();
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon, size);
                ImGui.TextUnformatted(item.Name);
                var slot = item.Type.ToSlot();
                ImGui.TextUnformatted($"{item.Type.ToName()} ({slot.ToName()})");
                if (item.Type.Offhand().IsOffhandType())
                    ImGui.TextUnformatted(
                        $"{item.Weapon()}{(_items.ItemService.AwaitedService.TryGetValue(item.ItemId, false, out var offhand) ? $" | {offhand.Weapon()}" : string.Empty)}");
                else
                    ImGui.TextUnformatted(slot is EquipSlot.MainHand ? $"{item.Weapon()}" : $"{item.Armor()}");
                ImGui.TextUnformatted(
                    unlocked ? time == DateTimeOffset.MinValue ? "Always Unlocked" : $"Unlocked on {time:g}" : "Not Unlocked.");
                _tooltip.CreateTooltip(item, string.Empty, false);
            }
        }

        var skips   = ImGuiClip.GetNecessarySkips(iconSize.Y + spacing.Y);
        var end     = Math.Min(numVisibleRows * iconsPerRow + skips * iconsPerRow, items.Count);
        var counter = 0;
        for (var idx = skips * iconsPerRow; idx < end; ++idx)
        {
            DrawItem(items[idx]);
            if (counter != iconsPerRow - 1)
            {
                ImGui.SameLine();
                ++counter;
            }
            else
            {
                counter = 0;
            }
        }

        if (ImGui.GetCursorPosX() != 0)
            ImGui.NewLine();
        var remainder = numRows - numVisibleRows - skips;
        if (remainder > 0)
            ImGuiClip.DrawEndDummy(remainder, iconSize.Y + spacing.Y);
    }

    private static Vector2 IconSpacing
        => ImGuiHelpers.ScaledVector2(2);

    private static int IconsPerRow(float iconWidth, float iconSpacing)
        => (int)(ImGui.GetContentRegionAvail().X / (iconWidth + iconSpacing));
}
