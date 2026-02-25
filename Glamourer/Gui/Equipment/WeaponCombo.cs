using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class WeaponCombo(FavoriteManager favorites, ItemManager items, FullEquipType slot)
    : BaseItemCombo(favorites, items)
{
    public override StringU8      Label { get; } = GetLabel(slot);
    public readonly FullEquipType Slot = slot;

    protected override bool Identify(out EquipItem item)
    {
        if (Slot is not FullEquipType.Unknown && ItemData.ConvertWeaponId(CustomSetId) != CurrentItem.Type)
        {
            item = default;
            return false;
        }
        item = Items.Identify(Slot.ToSlot(), CustomSetId, CustomWeaponId, CustomVariant);
        return true;
    }

    private static IReadOnlyList<EquipItem> GetItems(FavoriteManager favorites, ItemManager items, FullEquipType type)
    {
        if (type is FullEquipType.Unknown)
        {
            var enumerable = Array.Empty<EquipItem>().AsEnumerable();
            foreach (var t in FullEquipType.Values.Where(e => e.ToSlot() is EquipSlot.MainHand))
            {
                if (items.ItemData.ByType.TryGetValue(t, out var l))
                    enumerable = enumerable.Concat(l);
            }

            return [.. enumerable.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];
        }

        if (!items.ItemData.ByType.TryGetValue(type, out var list))
            return [];

        if (type.AllowsNothing())
            return [ItemManager.NothingItem(type), .. list.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];

        return [.. list.OrderByDescending(favorites.Contains).ThenBy(e => e.Name)];
    }

    protected override IEnumerable<CacheItem> GetItems()
    {
        if (Slot is FullEquipType.Unknown)
        {
            var enumerable = Array.Empty<EquipItem>().AsEnumerable();
            foreach (var t in FullEquipType.Values.Where(e => e.ToSlot() is EquipSlot.MainHand))
            {
                if (Items.ItemData.ByType.TryGetValue(t, out var l))
                    enumerable = enumerable.Concat(l);
            }

            return enumerable.OrderByDescending(Favorites.Contains).ThenBy(e => e.Name).Select(e => new CacheItem(e));
        }

        if (!Items.ItemData.ByType.TryGetValue(Slot, out var list))
            return [];

        IEnumerable<EquipItem> ret = list.OrderByDescending(Favorites.Contains).ThenBy(e => e.Name);
        if (Slot.AllowsNothing())
            ret = ret.Prepend(ItemManager.NothingItem(Slot));
        return ret.Select(e => new CacheItem(e));
    }

    private static StringU8 GetLabel(FullEquipType type)
        => type.IsUnknown() ? new StringU8("Mainhand"u8) : new StringU8(type.ToName());
}