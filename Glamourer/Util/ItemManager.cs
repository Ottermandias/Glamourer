using System;
using System.Diagnostics;
using System.Linq;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Penumbra.GameData;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Util;

public class ItemManager : IDisposable
{
    public const string Nothing              = "Nothing";
    public const string SmallClothesNpc      = "Smallclothes (NPC)";
    public const ushort SmallClothesNpcModel = 9903;

    public readonly IObjectIdentifier Identifier;
    public readonly ExcelSheet<Item>  ItemSheet;
    public readonly StainData         Stains;
    public readonly ItemData          Items;
    public readonly RestrictedGear    RestrictedGear;

    public ItemManager(DalamudPluginInterface pi, DataManager gameData)
    {
        ItemSheet      = gameData.GetExcelSheet<Item>()!;
        Identifier     = Penumbra.GameData.GameData.GetIdentifier(pi, gameData, gameData.Language);
        Stains         = new StainData(pi, gameData, gameData.Language);
        Items          = new ItemData(pi, gameData, gameData.Language);
        RestrictedGear = new RestrictedGear(pi, gameData.Language, gameData);
        DefaultSword   = ItemSheet.GetRow(1601)!; // Weathered Shortsword
    }

    public void Dispose()
    {
        Stains.Dispose();
        Items.Dispose();
        Identifier.Dispose();
        RestrictedGear.Dispose();
    }

    public readonly Item DefaultSword;

    public static uint NothingId(EquipSlot slot)
        => uint.MaxValue - 128 - (uint)slot.ToSlot();

    public static uint SmallclothesId(EquipSlot slot)
        => uint.MaxValue - 256 - (uint)slot.ToSlot();

    public static uint NothingId(FullEquipType type)
        => uint.MaxValue - 384 - (uint)type;

    public static Designs.Item NothingItem(EquipSlot slot)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory(), $"Called {nameof(NothingItem)} on {slot}.");
        return new Designs.Item(Nothing, NothingId(slot), CharacterArmor.Empty);
    }

    public static Designs.Weapon NothingItem(FullEquipType type)
    {
        Debug.Assert(type.ToSlot() == EquipSlot.OffHand, $"Called {nameof(NothingItem)} on {type}.");
        return new Designs.Weapon(Nothing, NothingId(type), CharacterWeapon.Empty, type);
    }

    public static Designs.Item SmallClothesItem(EquipSlot slot)
    {
        Debug.Assert(slot.IsEquipment(), $"Called {nameof(SmallClothesItem)} on {slot}.");
        return new Designs.Item(SmallClothesNpc, SmallclothesId(slot), new CharacterArmor(SmallClothesNpcModel, 1, 0));
    }

    public (bool Valid, SetId Id, byte Variant, string ItemName) Resolve(EquipSlot slot, uint itemId, Item? item = null)
    {
        slot = slot.ToSlot();
        if (itemId == NothingId(slot))
            return (true, 0, 0, Nothing);
        if (itemId == SmallclothesId(slot))
            return (true, SmallClothesNpcModel, 1, SmallClothesNpc);

        if (item == null || item.RowId != itemId)
            item = ItemSheet.GetRow(itemId);

        if (item == null)
            return (false, 0, 0, string.Intern($"Unknown #{itemId}"));
        if (item.ToEquipType().ToSlot() != slot)
            return (false, 0, 0, string.Intern($"Invalid ({item.Name.ToDalamudString()})"));

        return (true, (SetId)item.ModelMain, (byte)(item.ModelMain >> 16), string.Intern(item.Name.ToDalamudString().TextValue));
    }

    public (bool Valid, SetId Id, WeaponType Weapon, byte Variant, string ItemName, FullEquipType Type) Resolve(uint itemId, Item? item = null)
    {
        if (item == null || item.RowId != itemId)
            item = ItemSheet.GetRow(itemId);

        if (item == null)
            return (false, 0, 0, 0, string.Intern($"Unknown #{itemId}"), FullEquipType.Unknown);

        var type = item.ToEquipType();
        if (type.ToSlot() != EquipSlot.MainHand)
            return (false, 0, 0, 0, string.Intern($"Invalid ({item.Name.ToDalamudString()})"), type);

        return (true, (SetId)item.ModelMain, (WeaponType)(item.ModelMain >> 16), (byte)(item.ModelMain >> 32),
            string.Intern(item.Name.ToDalamudString().TextValue), type);
    }

    public (bool Valid, SetId Id, WeaponType Weapon, byte Variant, string ItemName, FullEquipType Type) Resolve(uint itemId,
        FullEquipType mainType, Item? item = null)
    {
        var offType = mainType.Offhand();
        if (itemId == NothingId(offType))
            return (true, 0, 0, 0, Nothing, offType);

        if (item == null || item.RowId != itemId)
            item = ItemSheet.GetRow(itemId);

        if (item == null)
            return (false, 0, 0, 0, string.Intern($"Unknown #{itemId}"), FullEquipType.Unknown);


        var type = item.ToEquipType();
        if (offType != type)
            return (false, 0, 0, 0, string.Intern($"Invalid ({item.Name.ToDalamudString()})"), type);

        var (m, w, v) = offType.ToSlot() == EquipSlot.MainHand
            ? ((SetId)item.ModelSub, (WeaponType)(item.ModelSub >> 16), (byte)(item.ModelSub >> 32))
            : ((SetId)item.ModelMain, (WeaponType)(item.ModelMain >> 16), (byte)(item.ModelMain >> 32));

        return (true, m, w, v, string.Intern(item.Name.ToDalamudString().TextValue), type);
    }

    public (bool Valid, uint ItemId, string ItemName) Identify(EquipSlot slot, SetId id, byte variant)
    {
        slot = slot.ToSlot();
        if (!slot.IsEquipmentPiece())
            return (false, 0, string.Intern($"Unknown ({id.Value}-{variant})"));

        switch (id.Value)
        {
            case 0:                    return (true, NothingId(slot), Nothing);
            case SmallClothesNpcModel: return (true, SmallclothesId(slot), SmallClothesNpc);
            default:
                var item = Identifier.Identify(id, variant, slot).FirstOrDefault();
                return item == null
                    ? (false, 0, string.Intern($"Unknown ({id.Value}-{variant})"))
                    : (true, item.RowId, string.Intern(item.Name.ToDalamudString().TextValue));
        }
    }

    public (bool Valid, uint ItemId, string ItemName, FullEquipType Type) Identify(EquipSlot slot, SetId id, WeaponType type, byte variant,
        FullEquipType mainhandType = FullEquipType.Unknown)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
            {
                var item = Identifier.Identify(id, type, variant, slot).FirstOrDefault();
                return item != null
                    ? (true, item.RowId, string.Intern(item.Name.ToDalamudString().TextValue), item.ToEquipType())
                    : (false, 0, string.Intern($"Unknown ({id.Value}-{type.Value}-{variant})"), mainhandType);
            }
            case EquipSlot.OffHand:
            {
                var weaponType = mainhandType.Offhand();
                if (id.Value == 0)
                    return (true, NothingId(weaponType), Nothing, weaponType);

                var item = Identifier.Identify(id, type, variant, slot).FirstOrDefault();
                return item != null
                    ? (true, item.RowId, string.Intern(item.Name.ToDalamudString().TextValue), item.ToEquipType())
                    : (false, 0, string.Intern($"Unknown ({id.Value}-{type.Value}-{variant})"),
                        weaponType);
            }
            default: return (false, 0, string.Intern($"Unknown ({id.Value}-{type.Value}-{variant})"), FullEquipType.Unknown);
        }
    }
}
