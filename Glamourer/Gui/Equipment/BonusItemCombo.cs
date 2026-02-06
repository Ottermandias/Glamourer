using Dalamud.Plugin.Services;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Addon = Lumina.Excel.Sheets.Addon;

namespace Glamourer.Gui.Equipment;

public sealed class BonusItemCombo(FavoriteManager favorites, ItemManager items, IDataManager gameData, BonusItemFlag slot)
    : BaseItemCombo(favorites, items)
{
    public override StringU8      Label { get; } = GetLabel(gameData, slot);
    public readonly BonusItemFlag Slot = slot;

    protected override bool Identify(out EquipItem item)
    {
        item = Items.Identify(Slot, CustomSetId, CustomVariant);
        return true;
    }

    protected override IEnumerable<CacheItem> GetItems()
    {
        var nothing = EquipItem.BonusItemNothing(Slot);
        return Items.ItemData.ByType[Slot.ToEquipType()].OrderByDescending(Favorites.Contains).ThenBy(i => i.Id.Id).Prepend(nothing)
            .Select(i => new CacheItem(i));
    }

    private static StringU8 GetLabel(IDataManager gameData, BonusItemFlag slot)
    {
        var sheet = gameData.GetExcelSheet<Addon>()!;

        return slot switch
        {
            BonusItemFlag.Glasses => sheet.TryGetRow(16050, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Facewear"u8),
            BonusItemFlag.UnkSlot => sheet.TryGetRow(16051, out var text) ? new StringU8(text.Text.Data, false) : new StringU8("Facewear"u8),

            _ => StringU8.Empty,
        };
    }
}
