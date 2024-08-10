using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.UnlocksTab;

public class UnlockOverview(
    ItemManager items,
    CustomizeService customizations,
    ItemUnlockManager itemUnlocks,
    CustomizeUnlockManager customizeUnlocks,
    PenumbraChangedItemTooltip tooltip,
    TextureService textures,
    CodeService codes,
    JobService jobs,
    FavoriteManager favorites)
{
    private static readonly Vector4 UnavailableTint = new(0.3f, 0.3f, 0.3f, 1.0f);

    private FullEquipType _selected1 = FullEquipType.Unknown;
    private SubRace       _selected2 = SubRace.Unknown;
    private Gender        _selected3 = Gender.Unknown;
    private BonusItemFlag _selected4 = BonusItemFlag.Unknown;

    private void DrawSelector()
    {
        using var child = ImRaii.Child("Selector", new Vector2(200 * ImGuiHelpers.GlobalScale, -1), true);
        if (!child)
            return;

        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.IsOffhandType() || !items.ItemData.ByType.TryGetValue(type, out var value) || value.Count == 0)
                continue;

            if (ImGui.Selectable(type.ToName(), _selected1 == type))
            {
                _selected1 = type;
                _selected2 = SubRace.Unknown;
                _selected3 = Gender.Unknown;
                _selected4 = BonusItemFlag.Unknown;
            }
        }

        if (ImGui.Selectable("Bonus Items", _selected4 == BonusItemFlag.Glasses))
        {
            _selected1 = FullEquipType.Unknown;
            _selected2 = SubRace.Unknown;
            _selected3 = Gender.Unknown;
            _selected4 = BonusItemFlag.Glasses;
        }

        foreach (var (clan, gender) in CustomizeManager.AllSets())
        {
            if (customizations.Manager.GetSet(clan, gender).HairStyles.Count == 0)
                continue;

            if (ImGui.Selectable($"{(gender is Gender.Male ? '♂' : '♀')} {clan.ToShortName()} Hair & Paint",
                    _selected2 == clan && _selected3 == gender))
            {
                _selected1 = FullEquipType.Unknown;
                _selected2 = clan;
                _selected3 = gender;
                _selected4 = BonusItemFlag.Unknown;
            }
        }
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
        else if (_selected4 is not BonusItemFlag.Unknown)
            DrawBonusItems();
    }

    private void DrawCustomizations()
    {
        var set = customizations.Manager.GetSet(_selected2, _selected3);

        var       spacing     = IconSpacing;
        using var style       = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       iconSize    = ImGuiHelpers.ScaledVector2(128);
        var       iconsPerRow = IconsPerRow(iconSize.X, spacing.X);

        var counter = 0;
        foreach (var customize in set.HairStyles.Concat(set.FacePaints))
        {
            if (!customizeUnlocks.Unlockable.TryGetValue(customize, out var unlockData))
                continue;

            var unlocked = customizeUnlocks.IsUnlocked(customize, out var time);
            var icon     = customizations.Manager.GetIcon(customize.IconId);
            var hasIcon  = icon.TryGetWrap(out var wrap, out _);
            ImGui.Image(wrap?.ImGuiHandle ?? icon.GetWrapOrEmpty().ImGuiHandle, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);

            if (favorites.Contains(_selected3, _selected2, customize.Index, customize.Value))
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ColorId.FavoriteStarOn.Value(),
                    12 * ImGuiHelpers.GlobalScale, ImDrawFlags.RoundCornersAll, 6 * ImGuiHelpers.GlobalScale);

            if (hasIcon && ImGui.IsItemHovered())
            {
                using var tt   = ImRaii.Tooltip();
                var       size = new Vector2(wrap!.Width, wrap.Height);
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(wrap.ImGuiHandle, size);
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

    private void DrawBonusItems()
    {
        var       spacing        = IconSpacing;
        using var style          = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       iconSize       = ImGuiHelpers.ScaledVector2(64);
        var       iconsPerRow    = IconsPerRow(iconSize.X, spacing.X);
        var       numRows        = (items.DictBonusItems.Count + iconsPerRow - 1) / iconsPerRow;
        var       numVisibleRows = (int)(Math.Ceiling(ImGui.GetContentRegionAvail().Y / (iconSize.Y + spacing.Y)) + 0.5f) + 1;

        var skips   = ImGuiClip.GetNecessarySkips(iconSize.Y + spacing.Y);
        var start   = skips * iconsPerRow;
        var end     = Math.Min(numVisibleRows * iconsPerRow + skips * iconsPerRow, items.DictBonusItems.Count);
        var counter = 0;

        foreach (var item in items.DictBonusItems.Values.Skip(start).Take(end - start))
        {
            DrawItem(item);
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

        void DrawItem(BonusItem item)
        {
            // TODO check unlocks
            var unlocked = true;
            if (!textures.TryLoadIcon(item.Icon.Id, out var iconHandle))
                return;

            var (icon, size) = (iconHandle.ImGuiHandle, new Vector2(iconHandle.Width, iconHandle.Height));

            ImGui.Image(icon, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);
            if (favorites.Contains(item))
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ColorId.FavoriteStarOn.Value(),
                    2 * ImGuiHelpers.GlobalScale, ImDrawFlags.RoundCornersAll, 4 * ImGuiHelpers.GlobalScale);

            // TODO handle clicking
            if (ImGui.IsItemHovered())
            {
                using var tt = ImRaii.Tooltip();
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon, size);
                ImUtf8.Text(item.Name);
                ImUtf8.Text($"{item.Slot.ToName()}");
                ImUtf8.Text($"{item.Id.Id}");
                ImUtf8.Text($"{item.ModelId.Id}-{item.Variant.Id}");
                // TODO
                ImUtf8.Text("Always Unlocked"); // : $"Unlocked on {time:g}" : "Not Unlocked.");
                // TODO
                //tooltip.CreateTooltip(item, string.Empty, false);
            }
        }
    }

    private void DrawItems()
    {
        if (!items.ItemData.ByType.TryGetValue(_selected1, out var value))
            return;

        var       spacing        = IconSpacing;
        using var style          = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);
        var       iconSize       = ImGuiHelpers.ScaledVector2(64);
        var       iconsPerRow    = IconsPerRow(iconSize.X, spacing.X);
        var       numRows        = (value.Count + iconsPerRow - 1) / iconsPerRow;
        var       numVisibleRows = (int)(Math.Ceiling(ImGui.GetContentRegionAvail().Y / (iconSize.Y + spacing.Y)) + 0.5f) + 1;

        var skips   = ImGuiClip.GetNecessarySkips(iconSize.Y + spacing.Y);
        var end     = Math.Min(numVisibleRows * iconsPerRow + skips * iconsPerRow, value.Count);
        var counter = 0;
        for (var idx = skips * iconsPerRow; idx < end; ++idx)
        {
            DrawItem(value[idx]);
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
        return;

        void DrawItem(EquipItem item)
        {
            var unlocked = itemUnlocks.IsUnlocked(item.Id, out var time);
            if (!textures.TryLoadIcon(item.IconId.Id, out var iconHandle))
                return;

            var (icon, size) = (iconHandle.ImGuiHandle, new Vector2(iconHandle.Width, iconHandle.Height));

            ImGui.Image(icon, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);
            if (favorites.Contains(item))
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ColorId.FavoriteStarOn.Value(),
                    2 * ImGuiHelpers.GlobalScale, ImDrawFlags.RoundCornersAll, 4 * ImGuiHelpers.GlobalScale);

            if (ImGui.IsItemClicked())
                Glamourer.Messager.Chat.Print(new SeStringBuilder().AddItemLink(item.ItemId.Id, false).BuiltString);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && tooltip.Player(out var state))
                tooltip.ApplyItem(state, item);

            if (ImGui.IsItemHovered())
            {
                using var tt = ImRaii.Tooltip();
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    ImGui.Image(icon, size);
                ImGui.TextUnformatted(item.Name);
                var slot = item.Type.ToSlot();
                ImGui.TextUnformatted($"{item.Type.ToName()} ({slot.ToName()})");
                if (item.Type.ValidOffhand().IsOffhandType())
                    ImGui.TextUnformatted(
                        $"{item.Weapon()}{(items.ItemData.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand) ? $" | {offhand.Weapon()}" : string.Empty)}");
                else
                    ImGui.TextUnformatted(slot is EquipSlot.MainHand ? $"{item.Weapon()}" : $"{item.Armor()}");
                ImGui.TextUnformatted(
                    unlocked ? time == DateTimeOffset.MinValue ? "Always Unlocked" : $"Unlocked on {time:g}" : "Not Unlocked.");

                if (item.Level.Value <= 1)
                {
                    if (item.JobRestrictions.Id <= 1 || item.JobRestrictions.Id >= jobs.AllJobGroups.Count)
                        ImGui.TextUnformatted("For Everyone");
                    else
                        ImGui.TextUnformatted($"For all {jobs.AllJobGroups[item.JobRestrictions.Id].Name}");
                }
                else
                {
                    if (item.JobRestrictions.Id <= 1 || item.JobRestrictions.Id >= jobs.AllJobGroups.Count)
                        ImGui.TextUnformatted($"For Everyone of at least Level {item.Level}");
                    else
                        ImGui.TextUnformatted($"For all {jobs.AllJobGroups[item.JobRestrictions.Id].Name} of at least Level {item.Level}");
                }

                if (item.Flags.HasFlag(ItemFlags.IsDyable1))
                    ImGui.TextUnformatted(item.Flags.HasFlag(ItemFlags.IsDyable2) ? "Dyable (2 Slots)" : "Dyable");
                if (item.Flags.HasFlag(ItemFlags.IsTradable))
                    ImGui.TextUnformatted("Tradable");
                if (item.Flags.HasFlag(ItemFlags.IsCrestWorthy))
                    ImGui.TextUnformatted("Can apply Crest");
                tooltip.CreateTooltip(item, string.Empty, false);
            }
        }
    }

    private static Vector2 IconSpacing
        => ImGuiHelpers.ScaledVector2(2);

    private static int IconsPerRow(float iconWidth, float iconSpacing)
        => (int)(ImGui.GetContentRegionAvail().X / (iconWidth + iconSpacing));
}
