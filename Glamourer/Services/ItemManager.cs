using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public static ItemId NothingId(EquipSlot slot)
        => uint.MaxValue - 128 - (uint)slot.ToSlot();

    public static ItemId SmallclothesId(EquipSlot slot)
        => uint.MaxValue - 256 - (uint)slot.ToSlot();

    public static ItemId NothingId(FullEquipType type)
        => uint.MaxValue - 384 - (uint)type;

    public static EquipItem NothingItem(EquipSlot slot)
        => new(Nothing, NothingId(slot), 0, 0, 0, 0, slot.ToEquipType());

    public static EquipItem NothingItem(FullEquipType type)
        => new(Nothing, NothingId(type), 0, 0, 0, 0, type);

    public static EquipItem SmallClothesItem(EquipSlot slot)
        => new(SmallClothesNpc, SmallclothesId(slot), 0, SmallClothesNpcModel, 0, 1, slot.ToEquipType());

    public EquipItem Resolve(EquipSlot slot, ItemId itemId)
    {
        slot = slot.ToSlot();
        if (itemId == NothingId(slot))
            return NothingItem(slot);
        if (itemId == SmallclothesId(slot))
            return SmallClothesItem(slot);

        if (!ItemService.AwaitedService.TryGetValue(itemId, slot, out var item))
            return EquipItem.FromId(itemId);

        if (item.Type.ToSlot() != slot)
            return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.ModelId, item.WeaponType, item.Variant, 0);

        return item;
    }

    public EquipItem Resolve(FullEquipType type, ItemId itemId)
    {
        if (itemId == NothingId(type))
            return NothingItem(type);

        if (!ItemService.AwaitedService.TryGetValue(itemId, type is FullEquipType.Shield ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
            return EquipItem.FromId(itemId);

        if (item.Type != type)
            return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.ModelId, item.WeaponType, item.Variant, 0);

        return item;
    }

    public EquipItem Identify(EquipSlot slot, SetId id, Variant variant)
    {
        slot = slot.ToSlot();
        if (slot.ToIndex() == uint.MaxValue)
            return new EquipItem($"Invalid ({id.Id}-{variant})", 0, 0, id, 0, variant, 0);

        switch (id.Id)
        {
            case 0:                    return NothingItem(slot);
            case SmallClothesNpcModel: return SmallClothesItem(slot);
            default:
                var item = IdentifierService.AwaitedService.Identify(id, variant, slot).FirstOrDefault();
                return item.Valid
                    ? item
                    : EquipItem.FromIds(0, 0, id, 0, variant, slot.ToEquipType());
        }
    }

    /// <summary> Return the default offhand for a given mainhand, that is for both handed weapons, return the correct offhand part, and for everything else Nothing. </summary>
    public EquipItem GetDefaultOffhand(EquipItem mainhand)
    {
        var offhandType = mainhand.Type.ValidOffhand();
        if (offhandType.IsOffhandType())
            return Resolve(offhandType, mainhand.ItemId);

        return NothingItem(offhandType);
    }

    public EquipItem Identify(EquipSlot slot, SetId id, WeaponType type, Variant variant, FullEquipType mainhandType = FullEquipType.Unknown)
    {
        if (slot is EquipSlot.OffHand)
        {
            var weaponType = mainhandType.ValidOffhand();
            if (id.Id == 0)
                return NothingItem(weaponType);
        }

        if (slot is not EquipSlot.MainHand and not EquipSlot.OffHand)
            return new EquipItem($"Invalid ({id.Id}-{type.Id}-{variant})", 0, 0, id, type, variant, 0);

        var item = IdentifierService.AwaitedService.Identify(id, type, variant, slot).FirstOrDefault();
        return item.Valid
            ? item
            : EquipItem.FromIds(0, 0, id, type, variant, slot.ToEquipType(), null);
    }

    /// <summary> Returns whether an item id represents a valid item for a slot and gives the item. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsItemValid(EquipSlot slot, ItemId itemId, out EquipItem item)
    {
        item = Resolve(slot, itemId);
        return item.Valid;
    }

    /// <summary>
    /// Check whether an item id resolves to an existing item of the correct slot (which should not be weapons.)
    /// The returned item is either the resolved correct item, or the Nothing item for that slot.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public string ValidateItem(EquipSlot slot, CustomItemId itemId, out EquipItem item, bool allowUnknown)
    {
        if (slot is EquipSlot.MainHand or EquipSlot.OffHand)
            throw new Exception("Internal Error: Used armor functionality for weapons.");

        if (!itemId.IsItem)
        {
            var (id, _, variant, _) = itemId.Split;
            item = Identify(slot, id, variant);
            return allowUnknown ? string.Empty : $"The item {itemId} yields an unknown item.";
        }

        if (IsItemValid(slot, itemId.Item, out item))
            return string.Empty;

        item = NothingItem(slot);
        return $"The {slot.ToName()} item {itemId} does not exist, reset to Nothing.";
    }

    /// <summary> Returns whether a stain id is a valid stain. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsStainValid(StainId stain)
        => stain.Id == 0 || Stains.ContainsKey(stain);

    /// <summary>
    /// Check whether a stain id is an existing stain. 
    /// The returned stain id is either the input or 0.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public string ValidateStain(StainId stain, out StainId ret, bool allowUnknown)
    {
        if (allowUnknown || IsStainValid(stain))
        {
            ret = stain;
            return string.Empty;
        }

        ret = 0;
        return $"The Stain {stain} does not exist, reset to unstained.";
    }

    /// <summary> Returns whether an offhand is valid given the required offhand type. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsOffhandValid(FullEquipType offType, ItemId offId, out EquipItem off)
    {
        off = Resolve(offType, offId);
        return offType == FullEquipType.Unknown || off.Valid;
    }

    /// <summary> Returns whether an offhand is valid given mainhand. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsOffhandValid(in EquipItem main, ItemId offId, out EquipItem off)
        => IsOffhandValid(main.Type.ValidOffhand(), offId, out off);

    /// <summary>
    /// Check whether a combination of an item id for a mainhand and for an offhand is valid.
    /// The returned items are either the resolved correct items,
    /// the correct mainhand and an appropriate offhand (implicit offhand or nothing),
    /// or the default sword and a nothing offhand.
    /// The return value is an empty string if there was no problem and a warning otherwise.
    /// </summary>
    public string ValidateWeapons(ItemId mainId, ItemId offId, out EquipItem main, out EquipItem off)
    {
        var ret = string.Empty;
        if (!IsItemValid(EquipSlot.MainHand, mainId, out main))
        {
            main = DefaultSword;
            ret  = $"The mainhand weapon {mainId} does not exist, reset to default sword.";
        }

        var offType = main.Type.ValidOffhand();
        if (IsOffhandValid(offType, offId, out off))
            return ret;

        // Try implicit offhand.
        // Can not be set to default sword before because then it could not be valid.
        if (IsOffhandValid(offType, mainId, out off))
            return $"The offhand weapon {offId} does not exist, reset to implied offhand.";

        if (FullEquipTypeExtensions.OffhandTypes.Contains(offType))
        {
            main = DefaultSword;
            off  = NothingItem(FullEquipType.Shield);
            return
                $"The offhand weapon {offId} does not exist, but no default could be restored, reset mainhand to default sword and offhand to nothing.";
        }

        off = NothingItem(offType);
        return ret.Length == 0 ? $"The offhand weapon {offId} does not exist, reset to no offhand." : ret;
    }
}
