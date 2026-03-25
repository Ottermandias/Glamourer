using Dalamud.Plugin.Services;
using Glamourer.Config;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class EquipCombo(FavoriteManager favorites, ItemManager items, Configuration config, IDataManager gameData, EquipSlot slot)
    : BaseItemCombo(favorites, items, config)
{
    public override StringU8  Label { get; } = GetLabel(gameData, slot);
    public readonly EquipSlot Slot = slot;

    protected override bool Identify(out EquipItem item)
    {
        item = Items.Identify(Slot, CustomSetId, CustomVariant);
        return true;
    }

    protected override IEnumerable<CacheItem> GetItems()
    {
        var nothing = ItemManager.NothingItem(Slot);
        if (!Items.ItemData.ByType.TryGetValue(Slot.ToEquipType(), out var list))
            return [new CacheItem(nothing)];

        var enumerable = list.AsEnumerable();
        if (Slot.IsEquipment())
            enumerable = enumerable.Append(ItemManager.SmallClothesItem(Slot));
        return enumerable.OrderByDescending(Favorites.Contains).ThenBy(i => i.Name).Prepend(nothing).Select(e => new CacheItem(e));
    }

    private static StringU8 GetLabel(IDataManager gameData, EquipSlot slot)
    {
        var sheet = gameData.GetExcelSheet<Addon>();

        return slot switch
        {
            EquipSlot.Head    => sheet.TryGetRow(740, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Head"u8),
            EquipSlot.Body    => sheet.TryGetRow(741, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Body"u8),
            EquipSlot.Hands   => sheet.TryGetRow(742, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Hands"u8),
            EquipSlot.Legs    => sheet.TryGetRow(744, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Legs"u8),
            EquipSlot.Feet    => sheet.TryGetRow(745, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Feet"u8),
            EquipSlot.Ears    => sheet.TryGetRow(746, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Ears"u8),
            EquipSlot.Neck    => sheet.TryGetRow(747, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Neck"u8),
            EquipSlot.Wrists  => sheet.TryGetRow(748, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Wrists"u8),
            EquipSlot.RFinger => sheet.TryGetRow(749, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Right Ring"u8),
            EquipSlot.LFinger => sheet.TryGetRow(750, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Left Ring"u8),
            _                 => StringU8.Empty,
        };
    }
}
