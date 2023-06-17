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

    private readonly Configuration _config;

    public readonly IdentifierService                             IdentifierService;
    public readonly ExcelSheet<Lumina.Excel.GeneratedSheets.Item> ItemSheet;
    public readonly StainData                                     Stains;
    public readonly ItemService                                   ItemService;
    public readonly RestrictedGear                                RestrictedGear;

    public readonly EquipItem DefaultSword;

    public ItemManager(Configuration config, DalamudPluginInterface pi, DataManager gameData, IdentifierService identifierService,
        ItemService itemService)
    {
        _config           = config;
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
        => _config.UseRestrictedGearProtection ? RestrictedGear.ResolveRestricted(armor, slot, race, gender) : (false, armor);


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

        if (!ItemService.AwaitedService.TryGetValue(itemId, slot is not EquipSlot.OffHand, out var item))
            return new EquipItem(string.Intern($"Unknown #{itemId}"), itemId, 0, 0, 0, 0, 0);

        if (item.Type.ToSlot() != slot)
            return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.ModelId, item.WeaponType, item.Variant, 0);

        return item;
    }

    public EquipItem Resolve(FullEquipType type, uint itemId)
    {
        if (itemId == NothingId(type))
            return NothingItem(type);

        if (!ItemService.AwaitedService.TryGetValue(itemId, false, out var item))
            return new EquipItem(string.Intern($"Unknown #{itemId}"), itemId, 0, 0, 0, 0, 0);

        if (item.Type != type)
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

    /// <summary>
    /// Check whether an item id resolves to an existing item of the correct slot (which should not be weapons.)
    /// The returned item is either the resolved correct item, or the Nothing item for that slot.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public string ValidateItem(EquipSlot slot, uint itemId, out EquipItem item)
    {
        if (slot is EquipSlot.MainHand or EquipSlot.OffHand)
            throw new Exception("Internal Error: Used armor functionality for weapons.");

        item = Resolve(slot, itemId);
        if (item.Valid)
            return string.Empty;

        item = NothingItem(slot);
        return $"The {slot.ToName()} item {itemId} does not exist, reset to Nothing.";
    }

    /// <summary>
    /// Check whether a stain id is an existing stain. 
    /// The returned stain id is either the input or 0.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public  string ValidateStain(StainId stain, out StainId ret)
    {
        if (stain.Value == 0 || Stains.ContainsKey(stain))
        {
            ret = stain;
            return string.Empty;
        }

        ret = 0;
        return $"The Stain {stain} does not exist, reset to unstained.";
    }

    /// <summary>
    /// Check whether a combination of an item id for a mainhand and for an offhand is valid.
    /// The returned items are either the resolved correct items,
    /// the correct mainhand and an appropriate offhand (implicit offhand or nothing),
    /// or the default sword and a nothing offhand.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public string ValidateWeapons(uint mainId, uint offId, out EquipItem main, out EquipItem off)
    {
        var ret = string.Empty;
        main = Resolve(EquipSlot.MainHand, mainId);
        if (!main.Valid)
        {
            main = DefaultSword;
            ret = $"The mainhand weapon {mainId} does not exist, reset to default sword.";
        }

        var offhandType = main.Type.Offhand();
        off = Resolve(offhandType, offId);
        if (off.Valid)
            return ret;

        // Try implicit offhand.
        off = Resolve(offhandType, mainId);
        if (off.Valid)
        {
            // Can not be set to default sword before because then it could not be valid.
            ret = $"The offhand weapon {offId} does not exist, reset to implied offhand.";
        }
        else
        {
            if (FullEquipTypeExtensions.OffhandTypes.Contains(offhandType))
            {
                main = DefaultSword;
                off = NothingItem(FullEquipType.Shield);
                ret =
                    $"The offhand weapon {offId} does not exist, but no default could be restored, reset mainhand to default sword and offhand to nothing.";
            }
            else
            {
                off = NothingItem(offhandType);
                if (ret.Length == 0)
                    ret = $"The offhand weapon {offId} does not exist, reset to no offhand.";
            }
        }

        return ret;
    }
}
