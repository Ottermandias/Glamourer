using System;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;

namespace Glamourer.Designs;

public struct ItemPiece
{
    public string Name;
    public uint   ItemId;
    public SetId  ModelId;
}

[Flags]
public enum ModelFlags : ushort
{
    HatVisible    = 0x01,
    WeaponVisible = 0x02,
    VisorToggled  = 0x04,
}

public struct ModelData
{
    public readonly unsafe ModelData Clone()
    {
        var data = new ModelData(MainHand);
        fixed (void* ptr = &this)
        {
            MemoryUtility.MemCpyUnchecked(&data, ptr, sizeof(ModelData));
        }

        return data;
    }

    public Customize       Customize = Customize.Default;
    public ModelFlags      Flags     = ModelFlags.HatVisible | ModelFlags.WeaponVisible;
    public CharacterWeapon MainHand;
    public CharacterWeapon OffHand = CharacterWeapon.Empty;

    public uint           ModelId = 0;
    public CharacterArmor Head    = CharacterArmor.Empty;
    public CharacterArmor Body    = CharacterArmor.Empty;
    public CharacterArmor Hands   = CharacterArmor.Empty;
    public CharacterArmor Legs    = CharacterArmor.Empty;
    public CharacterArmor Feet    = CharacterArmor.Empty;
    public CharacterArmor Ears    = CharacterArmor.Empty;
    public CharacterArmor Neck    = CharacterArmor.Empty;
    public CharacterArmor Wrists  = CharacterArmor.Empty;
    public CharacterArmor RFinger = CharacterArmor.Empty;
    public CharacterArmor LFinger = CharacterArmor.Empty;

    public ModelData(CharacterWeapon mainHand)
        => MainHand = mainHand;

    public readonly CharacterArmor Armor(EquipSlot slot) 
        => slot switch
        {
            EquipSlot.MainHand => MainHand.ToArmor(),
            EquipSlot.OffHand  => OffHand.ToArmor(),
            EquipSlot.Head     => Head,
            EquipSlot.Body     => Body,
            EquipSlot.Hands    => Hands,
            EquipSlot.Legs     => Legs,
            EquipSlot.Feet     => Feet,
            EquipSlot.Ears     => Ears,
            EquipSlot.Neck     => Neck,
            EquipSlot.Wrists   => Wrists,
            EquipSlot.RFinger  => RFinger,
            EquipSlot.LFinger  => LFinger,
            _                  => CharacterArmor.Empty,
        };

    public readonly CharacterWeapon Piece(EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => MainHand,
            EquipSlot.OffHand  => OffHand,
            EquipSlot.Head     => Head.ToWeapon(),
            EquipSlot.Body     => Body.ToWeapon(),
            EquipSlot.Hands    => Hands.ToWeapon(),
            EquipSlot.Legs     => Legs.ToWeapon(),
            EquipSlot.Feet     => Feet.ToWeapon(),
            EquipSlot.Ears     => Ears.ToWeapon(),
            EquipSlot.Neck     => Neck.ToWeapon(),
            EquipSlot.Wrists   => Wrists.ToWeapon(),
            EquipSlot.RFinger  => RFinger.ToWeapon(),
            EquipSlot.LFinger  => LFinger.ToWeapon(),
            _                  => CharacterWeapon.Empty,
        };

    public bool SetPiece(EquipSlot slot, SetId model, byte variant, out CharacterWeapon ret)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.MainHand:
                changes |= SetIfDifferent(ref MainHand.Set,     model);
                changes |= SetIfDifferent(ref MainHand.Variant, variant);
                ret     =  MainHand;
                return changes;
            case EquipSlot.OffHand:
                changes |= SetIfDifferent(ref OffHand.Set,     model);
                changes |= SetIfDifferent(ref OffHand.Variant, variant);
                ret     =  OffHand;
                return changes;
            case EquipSlot.Head:
                changes |= SetIfDifferent(ref Head.Set,     model);
                changes |= SetIfDifferent(ref Head.Variant, variant);
                ret     =  Head.ToWeapon();
                return changes;
            case EquipSlot.Body:
                changes |= SetIfDifferent(ref Body.Set,     model);
                changes |= SetIfDifferent(ref Body.Variant, variant);
                ret     =  Body.ToWeapon();
                return changes;
            case EquipSlot.Hands:
                changes |= SetIfDifferent(ref Hands.Set,     model);
                changes |= SetIfDifferent(ref Hands.Variant, variant);
                ret     =  Hands.ToWeapon();
                return changes;
            case EquipSlot.Legs:
                changes |= SetIfDifferent(ref Legs.Set,     model);
                changes |= SetIfDifferent(ref Legs.Variant, variant);
                ret     =  Legs.ToWeapon();
                return changes;
            case EquipSlot.Feet:
                changes |= SetIfDifferent(ref Feet.Set,     model);
                changes |= SetIfDifferent(ref Feet.Variant, variant);
                ret     =  Feet.ToWeapon();
                return changes;
            case EquipSlot.Ears:
                changes |= SetIfDifferent(ref Ears.Set,     model);
                changes |= SetIfDifferent(ref Ears.Variant, variant);
                ret     =  Ears.ToWeapon();
                return changes;
            case EquipSlot.Neck:
                changes |= SetIfDifferent(ref Neck.Set,     model);
                changes |= SetIfDifferent(ref Neck.Variant, variant);
                ret     =  Neck.ToWeapon();
                return changes;
            case EquipSlot.Wrists:
                changes |= SetIfDifferent(ref Wrists.Set,     model);
                changes |= SetIfDifferent(ref Wrists.Variant, variant);
                ret     =  Wrists.ToWeapon();
                return changes;
            case EquipSlot.RFinger:
                changes |= SetIfDifferent(ref RFinger.Set,     model);
                changes |= SetIfDifferent(ref RFinger.Variant, variant);
                ret     =  RFinger.ToWeapon();
                return changes;
            case EquipSlot.LFinger:
                changes |= SetIfDifferent(ref LFinger.Set,     model);
                changes |= SetIfDifferent(ref LFinger.Variant, variant);
                ret     =  LFinger.ToWeapon();
                return changes;
            default:
                ret = CharacterWeapon.Empty;
                return changes;
        }
    }

    public bool SetPiece(EquipSlot slot, SetId model, WeaponType type, byte variant, out CharacterWeapon ret)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.MainHand:
                changes |= SetIfDifferent(ref MainHand.Set,     model);
                changes |= SetIfDifferent(ref MainHand.Type,    type);
                changes |= SetIfDifferent(ref MainHand.Variant, variant);
                ret     =  MainHand;
                return changes;
            case EquipSlot.OffHand:
                changes |= SetIfDifferent(ref OffHand.Set,     model);
                changes |= SetIfDifferent(ref OffHand.Type,    type);
                changes |= SetIfDifferent(ref OffHand.Variant, variant);
                ret     =  OffHand;
                return changes;
            case EquipSlot.Head:
                changes |= SetIfDifferent(ref Head.Set,     model);
                changes |= SetIfDifferent(ref Head.Variant, variant);
                ret     =  Head.ToWeapon();
                return changes;
            case EquipSlot.Body:
                changes |= SetIfDifferent(ref Body.Set,     model);
                changes |= SetIfDifferent(ref Body.Variant, variant);
                ret     =  Body.ToWeapon();
                return changes;
            case EquipSlot.Hands:
                changes |= SetIfDifferent(ref Hands.Set,     model);
                changes |= SetIfDifferent(ref Hands.Variant, variant);
                ret     =  Hands.ToWeapon();
                return changes;
            case EquipSlot.Legs:
                changes |= SetIfDifferent(ref Legs.Set,     model);
                changes |= SetIfDifferent(ref Legs.Variant, variant);
                ret     =  Legs.ToWeapon();
                return changes;
            case EquipSlot.Feet:
                changes |= SetIfDifferent(ref Feet.Set,     model);
                changes |= SetIfDifferent(ref Feet.Variant, variant);
                ret     =  Feet.ToWeapon();
                return changes;
            case EquipSlot.Ears:
                changes |= SetIfDifferent(ref Ears.Set,     model);
                changes |= SetIfDifferent(ref Ears.Variant, variant);
                ret     =  Ears.ToWeapon();
                return changes;
            case EquipSlot.Neck:
                changes |= SetIfDifferent(ref Neck.Set,     model);
                changes |= SetIfDifferent(ref Neck.Variant, variant);
                ret     =  Neck.ToWeapon();
                return changes;
            case EquipSlot.Wrists:
                changes |= SetIfDifferent(ref Wrists.Set,     model);
                changes |= SetIfDifferent(ref Wrists.Variant, variant);
                ret     =  Wrists.ToWeapon();
                return changes;
            case EquipSlot.RFinger:
                changes |= SetIfDifferent(ref RFinger.Set,     model);
                changes |= SetIfDifferent(ref RFinger.Variant, variant);
                ret     =  RFinger.ToWeapon();
                return changes;
            case EquipSlot.LFinger:
                changes |= SetIfDifferent(ref LFinger.Set,     model);
                changes |= SetIfDifferent(ref LFinger.Variant, variant);
                ret     =  LFinger.ToWeapon();
                return changes;
            default:
                ret = CharacterWeapon.Empty;
                return changes;
        }
    }

    public bool SetPiece(EquipSlot slot, SetId model, byte variant, StainId stain, out CharacterWeapon ret)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.MainHand:
                changes |= SetIfDifferent(ref MainHand.Set,     model);
                changes |= SetIfDifferent(ref MainHand.Variant, variant);
                changes |= SetIfDifferent(ref MainHand.Stain,   stain);
                ret     =  MainHand;
                return changes;
            case EquipSlot.OffHand:
                changes |= SetIfDifferent(ref OffHand.Set,     model);
                changes |= SetIfDifferent(ref OffHand.Variant, variant);
                changes |= SetIfDifferent(ref OffHand.Stain,   stain);
                ret     =  OffHand;
                return changes;
            case EquipSlot.Head:
                changes |= SetIfDifferent(ref Head.Set,     model);
                changes |= SetIfDifferent(ref Head.Variant, variant);
                changes |= SetIfDifferent(ref Head.Stain,   stain);
                ret     =  Head.ToWeapon();
                return changes;
            case EquipSlot.Body:
                changes |= SetIfDifferent(ref Body.Set,     model);
                changes |= SetIfDifferent(ref Body.Variant, variant);
                changes |= SetIfDifferent(ref Body.Stain,   stain);
                ret     =  Body.ToWeapon();
                return changes;
            case EquipSlot.Hands:
                changes |= SetIfDifferent(ref Hands.Set,     model);
                changes |= SetIfDifferent(ref Hands.Variant, variant);
                changes |= SetIfDifferent(ref Hands.Stain,   stain);
                ret     =  Hands.ToWeapon();
                return changes;
            case EquipSlot.Legs:
                changes |= SetIfDifferent(ref Legs.Set,     model);
                changes |= SetIfDifferent(ref Legs.Variant, variant);
                changes |= SetIfDifferent(ref Legs.Stain,   stain);
                ret     =  Legs.ToWeapon();
                return changes;
            case EquipSlot.Feet:
                changes |= SetIfDifferent(ref Feet.Set,     model);
                changes |= SetIfDifferent(ref Feet.Variant, variant);
                changes |= SetIfDifferent(ref Feet.Stain,   stain);
                ret     =  Feet.ToWeapon();
                return changes;
            case EquipSlot.Ears:
                changes |= SetIfDifferent(ref Ears.Set,     model);
                changes |= SetIfDifferent(ref Ears.Variant, variant);
                changes |= SetIfDifferent(ref Ears.Stain,   stain);
                ret     =  Ears.ToWeapon();
                return changes;
            case EquipSlot.Neck:
                changes |= SetIfDifferent(ref Neck.Set,     model);
                changes |= SetIfDifferent(ref Neck.Variant, variant);
                changes |= SetIfDifferent(ref Neck.Stain,   stain);
                ret     =  Neck.ToWeapon();
                return changes;
            case EquipSlot.Wrists:
                changes |= SetIfDifferent(ref Wrists.Set,     model);
                changes |= SetIfDifferent(ref Wrists.Variant, variant);
                changes |= SetIfDifferent(ref Wrists.Stain,   stain);
                ret     =  Wrists.ToWeapon();
                return changes;
            case EquipSlot.RFinger:
                changes |= SetIfDifferent(ref RFinger.Set,     model);
                changes |= SetIfDifferent(ref RFinger.Variant, variant);
                changes |= SetIfDifferent(ref RFinger.Stain,   stain);
                ret     =  RFinger.ToWeapon();
                return changes;
            case EquipSlot.LFinger:
                changes |= SetIfDifferent(ref LFinger.Set,     model);
                changes |= SetIfDifferent(ref LFinger.Variant, variant);
                changes |= SetIfDifferent(ref LFinger.Stain,   stain);
                ret     =  LFinger.ToWeapon();
                return changes;
            default:
                ret = CharacterWeapon.Empty;
                return changes;
        }
    }

    public bool SetPiece(EquipSlot slot, CharacterWeapon weapon, out CharacterWeapon ret)
        => SetPiece(slot, weapon.Set, weapon.Type, (byte)weapon.Variant, weapon.Stain, out ret);

    public bool SetPiece(EquipSlot slot, CharacterArmor armor, out CharacterWeapon ret)
        => SetPiece(slot, armor.Set, armor.Variant, armor.Stain, out ret);

    public bool SetPiece(EquipSlot slot, SetId model, WeaponType type, byte variant, StainId stain, out CharacterWeapon ret)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.MainHand:
                changes |= SetIfDifferent(ref MainHand.Set,     model);
                changes |= SetIfDifferent(ref MainHand.Type,    type);
                changes |= SetIfDifferent(ref MainHand.Variant, variant);
                changes |= SetIfDifferent(ref MainHand.Stain,   stain);
                ret     =  MainHand;
                return changes;
            case EquipSlot.OffHand:
                changes |= SetIfDifferent(ref OffHand.Set,     model);
                changes |= SetIfDifferent(ref OffHand.Type,    type);
                changes |= SetIfDifferent(ref OffHand.Variant, variant);
                changes |= SetIfDifferent(ref OffHand.Stain,   stain);
                ret     =  OffHand;
                return changes;
            case EquipSlot.Head:
                changes |= SetIfDifferent(ref Head.Set,     model);
                changes |= SetIfDifferent(ref Head.Variant, variant);
                changes |= SetIfDifferent(ref Head.Stain,   stain);
                ret     =  Head.ToWeapon();
                return changes;
            case EquipSlot.Body:
                changes |= SetIfDifferent(ref Body.Set,     model);
                changes |= SetIfDifferent(ref Body.Variant, variant);
                changes |= SetIfDifferent(ref Body.Stain,   stain);
                ret     =  Body.ToWeapon();
                return changes;
            case EquipSlot.Hands:
                changes |= SetIfDifferent(ref Hands.Set,     model);
                changes |= SetIfDifferent(ref Hands.Variant, variant);
                changes |= SetIfDifferent(ref Hands.Stain,   stain);
                ret     =  Hands.ToWeapon();
                return changes;
            case EquipSlot.Legs:
                changes |= SetIfDifferent(ref Legs.Set,     model);
                changes |= SetIfDifferent(ref Legs.Variant, variant);
                changes |= SetIfDifferent(ref Legs.Stain,   stain);
                ret     =  Legs.ToWeapon();
                return changes;
            case EquipSlot.Feet:
                changes |= SetIfDifferent(ref Feet.Set,     model);
                changes |= SetIfDifferent(ref Feet.Variant, variant);
                changes |= SetIfDifferent(ref Feet.Stain,   stain);
                ret     =  Feet.ToWeapon();
                return changes;
            case EquipSlot.Ears:
                changes |= SetIfDifferent(ref Ears.Set,     model);
                changes |= SetIfDifferent(ref Ears.Variant, variant);
                changes |= SetIfDifferent(ref Ears.Stain,   stain);
                ret     =  Ears.ToWeapon();
                return changes;
            case EquipSlot.Neck:
                changes |= SetIfDifferent(ref Neck.Set,     model);
                changes |= SetIfDifferent(ref Neck.Variant, variant);
                changes |= SetIfDifferent(ref Neck.Stain,   stain);
                ret     =  Neck.ToWeapon();
                return changes;
            case EquipSlot.Wrists:
                changes |= SetIfDifferent(ref Wrists.Set,     model);
                changes |= SetIfDifferent(ref Wrists.Variant, variant);
                changes |= SetIfDifferent(ref Wrists.Stain,   stain);
                ret     =  Wrists.ToWeapon();
                return changes;
            case EquipSlot.RFinger:
                changes |= SetIfDifferent(ref RFinger.Set,     model);
                changes |= SetIfDifferent(ref RFinger.Variant, variant);
                changes |= SetIfDifferent(ref RFinger.Stain,   stain);
                ret     =  RFinger.ToWeapon();
                return changes;
            case EquipSlot.LFinger:
                changes |= SetIfDifferent(ref LFinger.Set,     model);
                changes |= SetIfDifferent(ref LFinger.Variant, variant);
                changes |= SetIfDifferent(ref LFinger.Stain,   stain);
                ret     =  LFinger.ToWeapon();
                return changes;
            default:
                ret = CharacterWeapon.Empty;
                return changes;
        }
    }

    public StainId Stain(EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => MainHand.Stain,
            EquipSlot.OffHand  => OffHand.Stain,
            EquipSlot.Head     => Head.Stain,
            EquipSlot.Body     => Body.Stain,
            EquipSlot.Hands    => Hands.Stain,
            EquipSlot.Legs     => Legs.Stain,
            EquipSlot.Feet     => Feet.Stain,
            EquipSlot.Ears     => Ears.Stain,
            EquipSlot.Neck     => Neck.Stain,
            EquipSlot.Wrists   => Wrists.Stain,
            EquipSlot.RFinger  => RFinger.Stain,
            EquipSlot.LFinger  => LFinger.Stain,
            _                  => 0,
        };

    public bool SetStain(EquipSlot slot, StainId stain, out CharacterWeapon ret)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.MainHand:
                changes = SetIfDifferent(ref MainHand.Stain, stain);
                ret     = MainHand;
                return changes;
            case EquipSlot.OffHand:
                changes = SetIfDifferent(ref OffHand.Stain, stain);
                ret     = OffHand;
                return changes;
            case EquipSlot.Head:
                changes = SetIfDifferent(ref Head.Stain, stain);
                ret     = Head.ToWeapon();
                return changes;
            case EquipSlot.Body:
                changes = SetIfDifferent(ref Body.Stain, stain);
                ret     = Body.ToWeapon();
                return changes;
            case EquipSlot.Hands:
                changes = SetIfDifferent(ref Hands.Stain, stain);
                ret     = Hands.ToWeapon();
                return changes;
            case EquipSlot.Legs:
                changes = SetIfDifferent(ref Legs.Stain, stain);
                ret     = Legs.ToWeapon();
                return changes;
            case EquipSlot.Feet:
                changes = SetIfDifferent(ref Feet.Stain, stain);
                ret     = Feet.ToWeapon();
                return changes;
            case EquipSlot.Ears:
                changes = SetIfDifferent(ref Ears.Stain, stain);
                ret     = Ears.ToWeapon();
                return changes;
            case EquipSlot.Neck:
                changes = SetIfDifferent(ref Neck.Stain, stain);
                ret     = Neck.ToWeapon();
                return changes;
            case EquipSlot.Wrists:
                changes = SetIfDifferent(ref Wrists.Stain, stain);
                ret     = Wrists.ToWeapon();
                return changes;
            case EquipSlot.RFinger:
                changes = SetIfDifferent(ref RFinger.Stain, stain);
                ret     = RFinger.ToWeapon();
                return changes;
            case EquipSlot.LFinger:
                changes = SetIfDifferent(ref LFinger.Stain, stain);
                ret     = LFinger.ToWeapon();
                return changes;
            default:
                ret = CharacterWeapon.Empty;
                return false;
        }
    }

    private static bool SetIfDifferent<T>(ref T old, T value) where T : IEquatable<T>
    {
        if (old.Equals(value))
            return false;

        old = value;
        return true;
    }
}
