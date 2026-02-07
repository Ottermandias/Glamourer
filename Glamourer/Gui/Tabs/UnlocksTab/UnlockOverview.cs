using Dalamud.Game.Text.SeStringHandling;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
    FavoriteManager favorites,
    PenumbraService penumbra)
{
    private static readonly Vector4 UnavailableTint = new(0.3f, 0.3f, 0.3f, 1.0f);

    private FullEquipType _selected1 = FullEquipType.Unknown;
    private SubRace       _selected2 = SubRace.Unknown;
    private Gender        _selected3 = Gender.Unknown;
    private BonusItemFlag _selected4 = BonusItemFlag.Unknown;

    private Rgba32 _favoriteColor;
    private Rgba32 _moddedColor;

    private void DrawSelector()
    {
        using var child = Im.Child.Begin("Selector"u8, Im.ContentRegion.Available with { X = 200 * Im.Style.GlobalScale }, true);
        if (!child)
            return;

        foreach (var type in FullEquipType.Values)
        {
            if (type.IsOffhandType() || type.IsBonus() || !items.ItemData.ByType.TryGetValue(type, out var value) || value.Count == 0)
                continue;

            if (Im.Selectable(type.ToNameU8(), _selected1 == type))
            {
                _selected1 = type;
                _selected2 = SubRace.Unknown;
                _selected3 = Gender.Unknown;
                _selected4 = BonusItemFlag.Unknown;
            }
        }

        if (Im.Selectable("Bonus Items"u8, _selected4 is BonusItemFlag.Glasses))
        {
            _selected1 = FullEquipType.Unknown;
            _selected2 = SubRace.Unknown;
            _selected3 = Gender.Unknown;
            _selected4 = BonusItemFlag.Glasses;
        }

        foreach (var (clan, gender) in CustomizeManager.AllSets())
        {
            if (customizations.Manager.GetSet(clan, gender).HairStyles.Count is 0)
                continue;

            if (Im.Selectable($"{(gender is Gender.Male ? '♂' : '♀')} {clan.ToShortName()} Hair & Paint",
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
        using var color = ImGuiColor.Border.Push(Im.Style[ImGuiColor.TableBorderStrong]);
        DrawSelector();
        Im.Line.Same();
        DrawPanel();
    }

    private void DrawPanel()
    {
        using var child = Im.Child.Begin("Panel"u8, Im.ContentRegion.Available, true);
        if (!child)
            return;

        _moddedColor   = ColorId.ModdedItemMarker.Value();
        _favoriteColor = ColorId.FavoriteStarOn.Value();

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
        using var style       = ImStyleDouble.ItemSpacing.Push(spacing);
        var       iconSize    = ImEx.ScaledVector(128);
        var       iconsPerRow = IconsPerRow(iconSize.X, spacing.X);

        var counter = 0;
        foreach (var customize in set.HairStyles.Concat(set.FacePaints))
        {
            if (!customizeUnlocks.Unlockable.TryGetValue(customize, out var unlockData))
                continue;

            var unlocked = customizeUnlocks.IsUnlocked(customize, out var time);
            var icon     = customizations.Manager.GetIcon(customize.IconId);
            var hasIcon  = icon.TryGetWrap(out var wrap, out _);
            Im.Image.Draw(wrap?.Id ?? icon.GetWrapOrEmpty().Id, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);

            if (favorites.Contains(_selected3, _selected2, customize.Index, customize.Value))
                Im.Window.DrawList.Shape.Rectangle(Im.Item.UpperLeftCorner, Im.Item.LowerRightCorner, _favoriteColor, 12 * Im.Style.GlobalScale,
                    ImDrawFlagsRectangle.RoundCornersAll, 6 * Im.Style.GlobalScale);

            if (hasIcon && Im.Item.Hovered())
            {
                using var tt   = Im.Tooltip.Begin();
                var       size = new Vector2(wrap!.Width, wrap.Height);
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    Im.Image.Draw(wrap.Id, size);
                Im.Text(unlockData.Name);
                Im.Text($"{customize.Index.ToNameU8()} {customize.Value.Value}");
                Im.Text(unlocked ? $"Unlocked on {time:g}" : "Not unlocked."u8);
            }

            if (counter != iconsPerRow - 1)
            {
                Im.Line.Same();
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
        var       iconSize       = ImEx.ScaledVector(64);
        var       iconsPerRow    = IconsPerRow(iconSize.X, spacing.X);
        Im.ListClipper.DrawGrouped(items.DictBonusItems.Values, DrawItem, items.DictBonusItems.Count, iconsPerRow, iconSize.Y + spacing.Y, spacing.X);
        return;

        void DrawItem(EquipItem item)
        {
            // TODO check unlocks
            var unlocked = true;
            if (!textures.TryLoadIcon(item.IconId.Id, out var iconHandle))
                return;

            var (icon, size) = (iconHandle.Id, new Vector2(iconHandle.Width, iconHandle.Height));

            Im.Image.Draw(icon, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);
            if (favorites.Contains(item))
                Im.Window.DrawList.Shape.Rectangle(Im.Item.UpperLeftCorner, Im.Item.LowerRightCorner, _favoriteColor,
                    2 * Im.Style.GlobalScale, ImDrawFlagsRectangle.RoundCornersAll, 4 * Im.Style.GlobalScale);

            var mods = DrawModdedMarker(item, iconSize);

            // TODO handle clicking
            if (Im.Item.Hovered())
            {
                using var style = Im.Style.PushDefault();
                using var tt    = Im.Tooltip.Begin();
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    Im.Image.Draw(icon, size);
                Im.Text(item.Name);
                Im.Text(item.Type.ToNameU8());
                Im.Text($"{item.Id.Id}");
                Im.Text($"{item.PrimaryId.Id}-{item.Variant.Id}");
                // TODO
                Im.Text("Always Unlocked"u8); // : $"Unlocked on {time:g}" : "Not Unlocked.");
                // TODO
                //tooltip.CreateTooltip(item, string.Empty, false);
                DrawModTooltip(mods);
            }
        }
    }

    private void DrawItems()
    {
        if (!items.ItemData.ByType.TryGetValue(_selected1, out var value))
            return;

        var       spacing        = IconSpacing;
        var       iconSize       = ImEx.ScaledVector(64);
        var       iconsPerRow    = IconsPerRow(iconSize.X, spacing.X);
        Im.ListClipper.DrawGrouped(value, DrawItem, iconsPerRow, iconSize.Y + spacing.Y, spacing.X);
        return;

        void DrawItem(EquipItem item)
        {
            var unlocked = itemUnlocks.IsUnlocked(item.Id, out var time);
            if (!textures.TryLoadIcon(item.IconId.Id, out var iconHandle))
                return;

            var (icon, size) = (iconHandle.Id, new Vector2(iconHandle.Width, iconHandle.Height));

            Im.Image.Draw(icon, iconSize, Vector2.Zero, Vector2.One,
                unlocked || codes.Enabled(CodeService.CodeFlag.Shirts) ? Vector4.One : UnavailableTint);
            if (favorites.Contains(item))
                Im.Window.DrawList.Shape.Rectangle(Im.Item.UpperLeftCorner, Im.Item.LowerRightCorner, ColorId.FavoriteStarOn.Value(),
                    2 * Im.Style.GlobalScale, ImDrawFlagsRectangle.RoundCornersAll, 4 * Im.Style.GlobalScale);

            var mods = DrawModdedMarker(item, iconSize);

            if (Im.Item.Clicked())
                Glamourer.Messager.Chat.Print(new SeStringBuilder().AddItemLink(item.ItemId.Id, false).BuiltString);

            if (Im.Item.RightClicked() && tooltip.Player(out var state))
                tooltip.ApplyItem(state, item);

            if (Im.Item.Hovered())
            {
                using var style = Im.Style.PushDefault();
                using var tt    = Im.Tooltip.Begin();
                if (size.X >= iconSize.X && size.Y >= iconSize.Y)
                    Im.Image.Draw(icon, size);
                Im.Text(item.Name);
                var slot = item.Type.ToSlot();
                Im.Text($"{item.Type.ToNameU8()} ({slot.ToNameU8()})");
                if (item.Type.ValidOffhand().IsOffhandType())
                    Im.Text(
                        $"{item.Weapon()}{(items.ItemData.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand) ? $" | {offhand.Weapon()}" : StringU8.Empty)}");
                else
                    Im.Text(slot is EquipSlot.MainHand ? $"{item.Weapon()}" : $"{item.Armor()}");
                Im.Text(
                    unlocked ? time == DateTimeOffset.MinValue ? "Always Unlocked"u8 : $"Unlocked on {time:g}" : "Not Unlocked."u8);

                if (item.Level.Value <= 1)
                {
                    if (item.JobRestrictions.Id <= 1 || item.JobRestrictions.Id >= jobs.AllJobGroups.Count)
                        Im.Text("For Everyone"u8);
                    else
                        Im.Text($"For all {jobs.AllJobGroups[item.JobRestrictions.Id].Name}");
                }
                else
                {
                    if (item.JobRestrictions.Id <= 1 || item.JobRestrictions.Id >= jobs.AllJobGroups.Count)
                        Im.Text($"For Everyone of at least Level {item.Level}");
                    else
                        Im.Text($"For all {jobs.AllJobGroups[item.JobRestrictions.Id].Name} of at least Level {item.Level}");
                }

                if (item.Flags.HasFlag(ItemFlags.IsDyable1))
                    Im.Text(item.Flags.HasFlag(ItemFlags.IsDyable2) ? "Dyable (2 Slots)"u8 : "Dyable"u8);
                if (item.Flags.HasFlag(ItemFlags.IsTradable))
                    Im.Text("Tradable"u8);
                if (item.Flags.HasFlag(ItemFlags.IsCrestWorthy))
                    Im.Text("Can apply Crest"u8);
                DrawModTooltip(mods);
                tooltip.CreateTooltip(item, string.Empty, false);
            }
        }
    }

    private static Vector2 IconSpacing
        => ImEx.ScaledVector(2);

    private static int IconsPerRow(float iconWidth, float iconSpacing)
        => (int)(Im.ContentRegion.Available.X / (iconWidth + iconSpacing));

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private (string ModDirectory, string ModName)[] DrawModdedMarker(in EquipItem item, Vector2 iconSize)
    {
        var mods = penumbra.CheckCurrentChangedItem(item.Name);
        if (mods.Length is 0)
            return mods;

        var center = Im.Item.UpperLeftCorner + new Vector2(iconSize.X * 0.85f, iconSize.Y * 0.15f);
        Im.Window.DrawList.Shape.CircleFilled(center, iconSize.X * 0.1f, _moddedColor);
        Im.Window.DrawList.Shape.Circle(center, iconSize.X * 0.1f, Rgba32.Black);
        return mods;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private void DrawModTooltip((string ModDirectory, string ModName)[] mods)
    {
        switch (mods.Length)
        {
            case 0: return;
            case 1:
                Im.Text("Modded by: "u8, _moddedColor);
                Im.Line.NoSpacing();
                Im.Text(mods[0].ModName);
                return;
            default:
                Im.Text("Modded by:"u8, _moddedColor);
                foreach (var (_, mod) in mods)
                    Im.BulletText(mod);
                return;
        }
    }
}
