using System;
using System.Linq;
using Dalamud.Data;
using Dalamud.Plugin;
using Lumina.Excel;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Services;

public class ItemManager : IDisposable
{
    public const string Nothing              = "Nothing";
    public const string SmallClothesNpc      = "Smallclothes (NPC)";
    public const ushort SmallClothesNpcModel = 9903;

    public readonly IdentifierService                             IdentifierService;
    public readonly ExcelSheet<Lumina.Excel.GeneratedSheets.Item> ItemSheet;
    public readonly StainData                                     Stains;
    public readonly ItemService                                   ItemService;
    public readonly RestrictedGear                                RestrictedGear;

    public readonly EquipItem DefaultSword;

    public ItemManager(DalamudPluginInterface pi, DataManager gameData, IdentifierService identifierService, ItemService itemService)
    {
        ItemSheet         = gameData.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!;
        IdentifierService = identifierService;
        Stains            = new StainData(pi, gameData, gameData.Language);
        ItemService       = itemService;
        RestrictedGear    = new RestrictedGear(pi, gameData.Language, gameData);
        DefaultSword      = EquipItem.FromMainhand(ItemSheet.GetRow(1601)!); // Weathered Shortsword
    }

    public void Dispose()
    {
        Stains.Dispose();
        RestrictedGear.Dispose();
    }


    public (bool, CharacterArmor) ResolveRestrictedGear(CharacterArmor armor, EquipSlot slot, Race race, Gender gender)
        // TODO
        //if (_config.UseRestrictedGearProtection)
        => RestrictedGear.ResolveRestricted(armor, slot, race, gender);
    //return (false, armor);

    public static uint NothingId(EquipSlot slot)
        => uint.MaxValue - 128 - (uint)slot.ToSlot();

    public static uint SmallclothesId(EquipSlot slot)
        => uint.MaxValue - 256 - (uint)slot.ToSlot();

    public static uint NothingId(FullEquipType type)
        => uint.MaxValue - 384 - (uint)type;

    public static EquipItem NothingItem(EquipSlot slot)
        => new(Nothing, NothingId(slot), 0, 0, 0, 0, slot.ToEquipType());

    public static EquipItem NothingItem(FullEquipType type)
        => new(Nothing, NothingId(type), 0, 0, 0, 0, type);

    public static EquipItem SmallClothesItem(EquipSlot slot)
        => new(SmallClothesNpc, SmallclothesId(slot), 0, SmallClothesNpcModel, 0, 1, slot.ToEquipType());

    public EquipItem Resolve(EquipSlot slot, uint itemId)
    {
        slot = slot.ToSlot();
        if (itemId == NothingId(slot))
            return NothingItem(slot);
        if (itemId == SmallclothesId(slot))
            return SmallClothesItem(slot);

        if (!ItemService.AwaitedService.TryGetValue(itemId, slot is EquipSlot.MainHand, out var item))
            return new EquipItem(string.Intern($"Unknown #{itemId}"), itemId, 0, 0, 0, 0, 0);

        if (item.Type.ToSlot() != slot)
            return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.ModelId, item.WeaponType, item.Variant, 0);

        return item;
    }

    public EquipItem Identify(EquipSlot slot, SetId id, byte variant)
    {
        slot = slot.ToSlot();
        if (!slot.IsEquipmentPiece())
            return new EquipItem($"Invalid ({id.Value}-{variant})", 0, 0, id, 0, variant, 0);

        switch (id.Value)
        {
            case 0:                    return NothingItem(slot);
            case SmallClothesNpcModel: return SmallClothesItem(slot);
            default:
                var item = IdentifierService.AwaitedService.Identify(id, variant, slot).FirstOrDefault();
                return item.Valid
                    ? item
                    : new EquipItem($"Unknown ({id.Value}-{variant})", 0, 0, id, 0, variant, 0);
        }
    }


    public EquipItem Identify(EquipSlot slot, SetId id, WeaponType type, byte variant, FullEquipType mainhandType = FullEquipType.Unknown)
    {
        if (slot is EquipSlot.OffHand)
        {
            var weaponType = mainhandType.Offhand();
            if (id.Value == 0)
                return NothingItem(weaponType);
        }

        if (slot is not EquipSlot.MainHand and not EquipSlot.OffHand)
            return new EquipItem($"Invalid ({id.Value}-{type.Value}-{variant})", 0, 0, id, type, variant, 0);

        var item = IdentifierService.AwaitedService.Identify(id, type, variant, slot).FirstOrDefault();
        return item.Valid
            ? item
            : new EquipItem($"Unknown ({id.Value}-{type.Value}-{variant})", 0, 0, id, type, variant, 0);
    }
}
