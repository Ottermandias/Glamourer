using System;
using Glamourer.Customization;
using Glamourer.Util;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignBase
{
    protected CharacterData CharacterData = CharacterData.Default;
    public    FullEquipType MainhandType { get; protected set; }

    public uint Head     { get; protected set; } = ItemManager.NothingId(EquipSlot.Head);
    public uint Body     { get; protected set; } = ItemManager.NothingId(EquipSlot.Body);
    public uint Hands    { get; protected set; } = ItemManager.NothingId(EquipSlot.Hands);
    public uint Legs     { get; protected set; } = ItemManager.NothingId(EquipSlot.Legs);
    public uint Feet     { get; protected set; } = ItemManager.NothingId(EquipSlot.Feet);
    public uint Ears     { get; protected set; } = ItemManager.NothingId(EquipSlot.Ears);
    public uint Neck     { get; protected set; } = ItemManager.NothingId(EquipSlot.Neck);
    public uint Wrists   { get; protected set; } = ItemManager.NothingId(EquipSlot.Wrists);
    public uint RFinger  { get; protected set; } = ItemManager.NothingId(EquipSlot.RFinger);
    public uint LFinger  { get; protected set; } = ItemManager.NothingId(EquipSlot.RFinger);
    public uint MainHand { get; protected set; }
    public uint OffHand  { get; protected set; }

    public string HeadName     { get; protected set; } = ItemManager.Nothing;
    public string BodyName     { get; protected set; } = ItemManager.Nothing;
    public string HandsName    { get; protected set; } = ItemManager.Nothing;
    public string LegsName     { get; protected set; } = ItemManager.Nothing;
    public string FeetName     { get; protected set; } = ItemManager.Nothing;
    public string EarsName     { get; protected set; } = ItemManager.Nothing;
    public string NeckName     { get; protected set; } = ItemManager.Nothing;
    public string WristsName   { get; protected set; } = ItemManager.Nothing;
    public string RFingerName  { get; protected set; } = ItemManager.Nothing;
    public string LFingerName  { get; protected set; } = ItemManager.Nothing;
    public string MainhandName { get; protected set; }
    public string OffhandName  { get; protected set; }

    public Customize Customize()
        => CharacterData.Customize;

    public CharacterEquip Equipment()
        => CharacterData.Equipment;

    public DesignBase()
    {
        MainHand = Glamourer.Items.DefaultSword.RowId;
        (_, CharacterData.MainHand.Set, CharacterData.MainHand.Type, CharacterData.MainHand.Variant, MainhandName, MainhandType) =
            Glamourer.Items.Resolve(MainHand, Glamourer.Items.DefaultSword);
        OffHand = ItemManager.NothingId(MainhandType.Offhand());
        (_, CharacterData.OffHand.Set, CharacterData.OffHand.Type, CharacterData.OffHand.Variant, OffhandName, _) =
            Glamourer.Items.Resolve(OffHand, MainhandType);
    }

    public uint ModelId
        => CharacterData.ModelId;

    public Item Armor(EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.Head    => new Item(HeadName,    Head,    CharacterData.Head),
            EquipSlot.Body    => new Item(BodyName,    Body,    CharacterData.Body),
            EquipSlot.Hands   => new Item(HandsName,   Hands,   CharacterData.Hands),
            EquipSlot.Legs    => new Item(LegsName,    Legs,    CharacterData.Legs),
            EquipSlot.Feet    => new Item(FeetName,    Feet,    CharacterData.Feet),
            EquipSlot.Ears    => new Item(EarsName,    Ears,    CharacterData.Ears),
            EquipSlot.Neck    => new Item(NeckName,    Neck,    CharacterData.Neck),
            EquipSlot.Wrists  => new Item(WristsName,  Wrists,  CharacterData.Wrists),
            EquipSlot.RFinger => new Item(RFingerName, RFinger, CharacterData.RFinger),
            EquipSlot.LFinger => new Item(LFingerName, LFinger, CharacterData.LFinger),
            _                 => throw new Exception("Invalid equip slot for item."),
        };
    }

    public Weapon WeaponMain
        => new(MainhandName, MainHand, CharacterData.MainHand, MainhandType);

    public Weapon WeaponOff
        => Designs.Weapon.Offhand(OffhandName, OffHand, CharacterData.OffHand, MainhandType);

    public CustomizeValue GetCustomize(CustomizeIndex idx)
        => Customize()[idx];

    protected bool SetCustomize(CustomizeIndex idx, CustomizeValue value)
    {
        var c = Customize();
        if (c[idx] == value)
            return false;

        c[idx] = value;
        return true;
    }

    protected bool SetArmor(EquipSlot slot, uint itemId, Lumina.Excel.GeneratedSheets.Item? item = null)
    {
        var (valid, set, variant, name) = Glamourer.Items.Resolve(slot, itemId, item);
        if (!valid)
            return false;

        return SetArmor(slot, set, variant, name, itemId);
    }

    protected bool SetArmor(EquipSlot slot, Item item)
        => SetArmor(slot, item.ModelBase, item.Variant, item.Name, item.ItemId);

    protected bool UpdateArmor(EquipSlot slot, CharacterArmor armor, bool force)
    {
        if (!force)
        {
            switch (slot)
            {
                case EquipSlot.Head when CharacterData.Head.Value == armor.Value:       return false;
                case EquipSlot.Body when CharacterData.Body.Value == armor.Value:       return false;
                case EquipSlot.Hands when CharacterData.Hands.Value == armor.Value:     return false;
                case EquipSlot.Legs when CharacterData.Legs.Value == armor.Value:       return false;
                case EquipSlot.Feet when CharacterData.Feet.Value == armor.Value:       return false;
                case EquipSlot.Ears when CharacterData.Ears.Value == armor.Value:       return false;
                case EquipSlot.Neck when CharacterData.Neck.Value == armor.Value:       return false;
                case EquipSlot.Wrists when CharacterData.Wrists.Value == armor.Value:   return false;
                case EquipSlot.RFinger when CharacterData.RFinger.Value == armor.Value: return false;
                case EquipSlot.LFinger when CharacterData.LFinger.Value == armor.Value: return false;
            }
        }

        var (valid, id, name) = Glamourer.Items.Identify(slot, armor.Set, armor.Variant);
        if (!valid)
            return false;

        return SetArmor(slot, armor.Set, armor.Variant, name, id);
    }

    protected bool SetMainhand(uint mainId, Lumina.Excel.GeneratedSheets.Item? main = null)
    {
        if (mainId == MainHand)
            return false;

        var (valid, set, weapon, variant, name, type) = Glamourer.Items.Resolve(mainId, main);
        if (!valid)
            return false;

        var fixOffhand = type.Offhand() != MainhandType.Offhand();

        MainHand                       = mainId;
        MainhandName                   = name;
        MainhandType                   = type;
        CharacterData.MainHand.Set     = set;
        CharacterData.MainHand.Type    = weapon;
        CharacterData.MainHand.Variant = variant;
        if (fixOffhand)
            SetOffhand(ItemManager.NothingId(type.Offhand()));
        return true;
    }

    protected bool SetOffhand(uint offId, Lumina.Excel.GeneratedSheets.Item? off = null)
    {
        if (offId == OffHand)
            return false;

        var (valid, set, weapon, variant, name, type) = Glamourer.Items.Resolve(offId, MainhandType, off);
        if (!valid)
            return false;

        OffHand                       = offId;
        OffhandName                   = name;
        CharacterData.OffHand.Set     = set;
        CharacterData.OffHand.Type    = weapon;
        CharacterData.OffHand.Variant = variant;
        return true;
    }

    protected bool UpdateMainhand(CharacterWeapon weapon)
    {
        if (weapon.Value == CharacterData.MainHand.Value)
            return false;

        var (valid, id, name, type) = Glamourer.Items.Identify(EquipSlot.MainHand, weapon.Set, weapon.Type, (byte)weapon.Variant);
        if (!valid || id == MainHand)
            return false;

        var fixOffhand = type.Offhand() != MainhandType.Offhand();

        MainHand                       = id;
        MainhandName                   = name;
        MainhandType                   = type;
        CharacterData.MainHand.Set     = weapon.Set;
        CharacterData.MainHand.Type    = weapon.Type;
        CharacterData.MainHand.Variant = weapon.Variant;
        CharacterData.MainHand.Stain   = weapon.Stain;
        if (fixOffhand)
            SetOffhand(ItemManager.NothingId(type.Offhand()));
        return true;
    }

    protected bool UpdateOffhand(CharacterWeapon weapon)
    {
        if (weapon.Value == CharacterData.OffHand.Value)
            return false;

        var (valid, id, name, _) = Glamourer.Items.Identify(EquipSlot.OffHand, weapon.Set, weapon.Type, (byte)weapon.Variant, MainhandType);
        if (!valid || id == OffHand)
            return false;

        OffHand                       = id;
        OffhandName                   = name;
        CharacterData.OffHand.Set     = weapon.Set;
        CharacterData.OffHand.Type    = weapon.Type;
        CharacterData.OffHand.Variant = weapon.Variant;
        CharacterData.OffHand.Stain   = weapon.Stain;
        return true;
    }

    protected bool SetStain(EquipSlot slot, StainId id)
    {
        return slot switch
        {
            EquipSlot.MainHand => SetIfDifferent(ref CharacterData.MainHand.Stain, id),
            EquipSlot.OffHand  => SetIfDifferent(ref CharacterData.OffHand.Stain,  id),
            EquipSlot.Head     => SetIfDifferent(ref CharacterData.Head.Stain,     id),
            EquipSlot.Body     => SetIfDifferent(ref CharacterData.Body.Stain,     id),
            EquipSlot.Hands    => SetIfDifferent(ref CharacterData.Hands.Stain,    id),
            EquipSlot.Legs     => SetIfDifferent(ref CharacterData.Legs.Stain,     id),
            EquipSlot.Feet     => SetIfDifferent(ref CharacterData.Feet.Stain,     id),
            EquipSlot.Ears     => SetIfDifferent(ref CharacterData.Ears.Stain,     id),
            EquipSlot.Neck     => SetIfDifferent(ref CharacterData.Neck.Stain,     id),
            EquipSlot.Wrists   => SetIfDifferent(ref CharacterData.Wrists.Stain,   id),
            EquipSlot.RFinger  => SetIfDifferent(ref CharacterData.RFinger.Stain,  id),
            EquipSlot.LFinger  => SetIfDifferent(ref CharacterData.LFinger.Stain,  id),
            _                  => false,
        };
    }

    protected static bool SetIfDifferent<T>(ref T old, T value) where T : IEquatable<T>
    {
        if (old.Equals(value))
            return false;

        old = value;
        return true;
    }


    private bool SetArmor(EquipSlot slot, SetId set, byte variant, string name, uint id)
    {
        var changes = false;
        switch (slot)
        {
            case EquipSlot.Head:
                changes  |= SetIfDifferent(ref CharacterData.Head.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Head.Variant, variant);
                changes  |= HeadName != name;
                HeadName =  name;
                changes  |= Head != id;
                Head     =  id;
                return changes;
            case EquipSlot.Body:
                changes  |= SetIfDifferent(ref CharacterData.Body.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Body.Variant, variant);
                changes  |= BodyName != name;
                BodyName =  name;
                changes  |= Body != id;
                Body     =  id;
                return changes;
            case EquipSlot.Hands:
                changes   |= SetIfDifferent(ref CharacterData.Hands.Set,     set);
                changes   |= SetIfDifferent(ref CharacterData.Hands.Variant, variant);
                changes   |= HandsName != name;
                HandsName =  name;
                changes   |= Hands != id;
                Hands     =  id;
                return changes;
            case EquipSlot.Legs:
                changes  |= SetIfDifferent(ref CharacterData.Legs.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Legs.Variant, variant);
                changes  |= LegsName != name;
                LegsName =  name;
                changes  |= Legs != id;
                Legs     =  id;
                return changes;
            case EquipSlot.Feet:
                changes  |= SetIfDifferent(ref CharacterData.Feet.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Feet.Variant, variant);
                changes  |= FeetName != name;
                FeetName =  name;
                changes  |= Feet != id;
                Feet     =  id;
                return changes;
            case EquipSlot.Ears:
                changes  |= SetIfDifferent(ref CharacterData.Ears.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Ears.Variant, variant);
                changes  |= EarsName != name;
                EarsName =  name;
                changes  |= Ears != id;
                Ears     =  id;
                return changes;
            case EquipSlot.Neck:
                changes  |= SetIfDifferent(ref CharacterData.Neck.Set,     set);
                changes  |= SetIfDifferent(ref CharacterData.Neck.Variant, variant);
                changes  |= NeckName != name;
                NeckName =  name;
                changes  |= Neck != id;
                Neck     =  id;
                return changes;
            case EquipSlot.Wrists:
                changes    |= SetIfDifferent(ref CharacterData.Wrists.Set,     set);
                changes    |= SetIfDifferent(ref CharacterData.Wrists.Variant, variant);
                changes    |= WristsName != name;
                WristsName =  name;
                changes    |= Wrists != id;
                Wrists     =  id;
                return changes;
            case EquipSlot.RFinger:
                changes     |= SetIfDifferent(ref CharacterData.RFinger.Set,     set);
                changes     |= SetIfDifferent(ref CharacterData.RFinger.Variant, variant);
                changes     |= RFingerName != name;
                RFingerName =  name;
                changes     |= RFinger != id;
                RFinger     =  id;
                return changes;
            case EquipSlot.LFinger:
                changes     |= SetIfDifferent(ref CharacterData.LFinger.Set,     set);
                changes     |= SetIfDifferent(ref CharacterData.LFinger.Variant, variant);
                changes     |= LFingerName != name;
                LFingerName =  name;
                changes     |= LFinger != id;
                LFinger     =  id;
                return changes;
            default: return false;
        }
    }
}
