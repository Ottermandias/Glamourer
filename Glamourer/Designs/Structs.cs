using Dalamud.Utility;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public readonly struct Item
{
    public readonly string         Name;
    public readonly uint           ItemId;
    public readonly CharacterArmor Model;

    public SetId ModelBase
        => Model.Set;

    public byte Variant
        => Model.Variant;

    public StainId Stain
        => Model.Stain;

    public Item WithStain(StainId id)
        => new(Name, ItemId, Model with { Stain = id });

    public Item(string name, uint itemId, CharacterArmor armor)
    {
        Name          = name;
        ItemId        = itemId;
        Model.Set     = armor.Set;
        Model.Variant = armor.Variant;
        Model.Stain   = armor.Stain;
    }

    public Item(Lumina.Excel.GeneratedSheets.Item item)
    {
        Name          = string.Intern(item.Name.ToDalamudString().TextValue);
        ItemId        = item.RowId;
        Model.Set     = (SetId)item.ModelMain;
        Model.Variant = (byte)(item.ModelMain >> 16);
    }
}

public readonly struct Weapon
{
    public readonly string          Name = string.Empty;
    public readonly uint            ItemId;
    public readonly FullEquipType   Type;
    public readonly bool            Valid;
    public readonly CharacterWeapon Model;

    public SetId ModelBase
        => Model.Set;

    public WeaponType WeaponBase
        => Model.Type;

    public byte Variant
        => (byte)Model.Variant;

    public StainId Stain
        => Model.Stain;


    public Weapon WithStain(StainId id)
        => new(Name, ItemId, Model with { Stain = id }, Type);

    public Weapon(string name, uint itemId, CharacterWeapon weapon, FullEquipType type)
    {
        Name          = name;
        ItemId        = itemId;
        Type          = type;
        Valid         = true;
        Model.Set     = weapon.Set;
        Model.Type    = weapon.Type;
        Model.Variant = (byte)weapon.Variant;
        Model.Stain   = weapon.Stain;
    }

    public static Weapon Offhand(string name, uint itemId, CharacterWeapon weapon, FullEquipType type)
    {
        var offType = type.Offhand();
        return offType is FullEquipType.Unknown
            ? new Weapon()
            : new Weapon(name, itemId, weapon, offType);
    }

    public Weapon(Lumina.Excel.GeneratedSheets.Item item, bool offhand)
    {
        Name   = string.Intern(item.Name.ToDalamudString().TextValue);
        ItemId = item.RowId;
        Type   = item.ToEquipType();
        var offType = Type.Offhand();
        var model   = offhand && offType == Type ? item.ModelSub : item.ModelMain;
        Valid = Type.ToSlot() switch
        {
            EquipSlot.MainHand when !offhand                   => true,
            EquipSlot.MainHand when offhand && offType == Type => true,
            EquipSlot.OffHand when offhand                     => true,
            _                                                  => false,
        };
        Model.Set     = (SetId)model;
        Model.Type    = (WeaponType)(model >> 16);
        Model.Variant = (byte)(model >> 32);
    }
}
