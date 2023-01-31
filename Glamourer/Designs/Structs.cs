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


    public Item(string name, uint itemId, CharacterArmor armor)
    {
        Name          = name;
        ItemId        = itemId;
        Model.Set     = armor.Set;
        Model.Variant = armor.Variant;
        Model.Stain   = armor.Stain;
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
}
