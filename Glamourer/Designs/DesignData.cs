using System;
using System.Runtime.InteropServices;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Services;
using OtterGui.Classes;
using OtterGui;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = FFXIVClientStructs.FFXIV.Client.Game.Character.CustomizeData;

namespace Glamourer.Designs;

public class DesignData
{
    internal ModelData     ModelData;
    public   FullEquipType MainhandType { get; internal set; }

    public uint Head    = ItemManager.NothingId(EquipSlot.Head);
    public uint Body    = ItemManager.NothingId(EquipSlot.Body);
    public uint Hands   = ItemManager.NothingId(EquipSlot.Hands);
    public uint Legs    = ItemManager.NothingId(EquipSlot.Legs);
    public uint Feet    = ItemManager.NothingId(EquipSlot.Feet);
    public uint Ears    = ItemManager.NothingId(EquipSlot.Ears);
    public uint Neck    = ItemManager.NothingId(EquipSlot.Neck);
    public uint Wrists  = ItemManager.NothingId(EquipSlot.Wrists);
    public uint RFinger = ItemManager.NothingId(EquipSlot.RFinger);
    public uint LFinger = ItemManager.NothingId(EquipSlot.RFinger);
    public uint MainHandId;
    public uint OffHandId;

    public string HeadName    = ItemManager.Nothing;
    public string BodyName    = ItemManager.Nothing;
    public string HandsName   = ItemManager.Nothing;
    public string LegsName    = ItemManager.Nothing;
    public string FeetName    = ItemManager.Nothing;
    public string EarsName    = ItemManager.Nothing;
    public string NeckName    = ItemManager.Nothing;
    public string WristsName  = ItemManager.Nothing;
    public string RFingerName = ItemManager.Nothing;
    public string LFingerName = ItemManager.Nothing;
    public string MainhandName;
    public string OffhandName;

    public DesignData(ItemManager items)
    {
        MainHandId                                                      = items.DefaultSword.RowId;
        (_, var set, var type, var variant, MainhandName, MainhandType) = items.Resolve(MainHandId, items.DefaultSword);

        ModelData = new ModelData(new CharacterWeapon(set, type, variant, 0));
        OffHandId = ItemManager.NothingId(MainhandType.Offhand());
        (_, ModelData.OffHand.Set, ModelData.OffHand.Type, ModelData.OffHand.Variant, OffhandName, _) =
            items.Resolve(OffHandId, MainhandType);
    }

    public uint ModelId
        => ModelData.ModelId;

    public Item Armor(EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.Head    => new Item(HeadName,    Head,    ModelData.Head),
            EquipSlot.Body    => new Item(BodyName,    Body,    ModelData.Body),
            EquipSlot.Hands   => new Item(HandsName,   Hands,   ModelData.Hands),
            EquipSlot.Legs    => new Item(LegsName,    Legs,    ModelData.Legs),
            EquipSlot.Feet    => new Item(FeetName,    Feet,    ModelData.Feet),
            EquipSlot.Ears    => new Item(EarsName,    Ears,    ModelData.Ears),
            EquipSlot.Neck    => new Item(NeckName,    Neck,    ModelData.Neck),
            EquipSlot.Wrists  => new Item(WristsName,  Wrists,  ModelData.Wrists),
            EquipSlot.RFinger => new Item(RFingerName, RFinger, ModelData.RFinger),
            EquipSlot.LFinger => new Item(LFingerName, LFinger, ModelData.LFinger),
            _                 => throw new Exception("Invalid equip slot for item."),
        };
    }


    public Weapon WeaponMain
        => new(MainhandName, MainHandId, ModelData.MainHand, MainhandType);

    public Weapon WeaponOff
        => Weapon.Offhand(OffhandName, OffHandId, ModelData.OffHand, MainhandType);

    public CustomizeValue GetCustomize(CustomizeIndex idx)
        => ModelData.Customize[idx];

    internal bool SetCustomize(CustomizeIndex idx, CustomizeValue value)
        => ModelData.Customize.Set(idx, value);

    internal bool SetArmor(ItemManager items, EquipSlot slot, uint itemId, Lumina.Excel.GeneratedSheets.Item? item = null)
    {
        var (valid, set, variant, name) = items.Resolve(slot, itemId, item);
        if (!valid)
            return false;

        return SetArmor(slot, set, variant, name, itemId);
    }

    internal bool SetArmor(EquipSlot slot, Item item)
        => SetArmor(slot, item.ModelBase, item.Variant, item.Name, item.ItemId);

    internal bool UpdateArmor(ItemManager items, EquipSlot slot, CharacterArmor armor, bool force)
    {
        var (valid, id, name) = items.Identify(slot, armor.Set, armor.Variant);
        if (!valid)
            return false;

        return SetArmor(slot, armor.Set, armor.Variant, name, id) | SetStain(slot, armor.Stain);
    }

    internal bool SetMainhand(ItemManager items, uint mainId, Lumina.Excel.GeneratedSheets.Item? main = null)
    {
        if (mainId == MainHandId)
            return false;

        var (valid, set, weapon, variant, name, type) = items.Resolve(mainId, main);
        if (!valid)
            return false;

        var fixOffhand = type.Offhand() != MainhandType.Offhand();

        MainHandId                 = mainId;
        MainhandName               = name;
        MainhandType               = type;
        ModelData.MainHand.Set     = set;
        ModelData.MainHand.Type    = weapon;
        ModelData.MainHand.Variant = variant;
        if (fixOffhand)
            SetOffhand(items, ItemManager.NothingId(type.Offhand()));
        return true;
    }

    internal bool SetOffhand(ItemManager items, uint offId, Lumina.Excel.GeneratedSheets.Item? off = null)
    {
        if (offId == OffHandId)
            return false;

        var (valid, set, weapon, variant, name, type) = items.Resolve(offId, MainhandType, off);
        if (!valid)
            return false;

        OffHandId                 = offId;
        OffhandName               = name;
        ModelData.OffHand.Set     = set;
        ModelData.OffHand.Type    = weapon;
        ModelData.OffHand.Variant = variant;
        return true;
    }

    internal bool UpdateMainhand(ItemManager items, CharacterWeapon weapon)
    {
        if (weapon.Value == ModelData.MainHand.Value)
            return false;

        var (valid, id, name, type) = items.Identify(EquipSlot.MainHand, weapon.Set, weapon.Type, (byte)weapon.Variant);
        if (!valid || id == MainHandId)
            return false;

        var fixOffhand = type.Offhand() != MainhandType.Offhand();

        MainHandId                 = id;
        MainhandName               = name;
        MainhandType               = type;
        ModelData.MainHand.Set     = weapon.Set;
        ModelData.MainHand.Type    = weapon.Type;
        ModelData.MainHand.Variant = weapon.Variant;
        ModelData.MainHand.Stain   = weapon.Stain;
        if (fixOffhand)
            SetOffhand(items, ItemManager.NothingId(type.Offhand()));
        return true;
    }

    internal bool UpdateOffhand(ItemManager items, CharacterWeapon weapon)
    {
        if (weapon.Value == ModelData.OffHand.Value)
            return false;

        var (valid, id, name, _) = items.Identify(EquipSlot.OffHand, weapon.Set, weapon.Type, (byte)weapon.Variant, MainhandType);
        if (!valid || id == OffHandId)
            return false;

        OffHandId                 = id;
        OffhandName               = name;
        ModelData.OffHand.Set     = weapon.Set;
        ModelData.OffHand.Type    = weapon.Type;
        ModelData.OffHand.Variant = weapon.Variant;
        ModelData.OffHand.Stain   = weapon.Stain;
        return true;
    }

    internal bool SetStain(EquipSlot slot, StainId id)
    {
        return slot switch
        {
            EquipSlot.MainHand => SetIfDifferent(ref ModelData.MainHand.Stain, id),
            EquipSlot.OffHand  => SetIfDifferent(ref ModelData.OffHand.Stain,  id),
            EquipSlot.Head     => SetIfDifferent(ref ModelData.Head.Stain,     id),
            EquipSlot.Body     => SetIfDifferent(ref ModelData.Body.Stain,     id),
            EquipSlot.Hands    => SetIfDifferent(ref ModelData.Hands.Stain,    id),
            EquipSlot.Legs     => SetIfDifferent(ref ModelData.Legs.Stain,     id),
            EquipSlot.Feet     => SetIfDifferent(ref ModelData.Feet.Stain,     id),
            EquipSlot.Ears     => SetIfDifferent(ref ModelData.Ears.Stain,     id),
            EquipSlot.Neck     => SetIfDifferent(ref ModelData.Neck.Stain,     id),
            EquipSlot.Wrists   => SetIfDifferent(ref ModelData.Wrists.Stain,   id),
            EquipSlot.RFinger  => SetIfDifferent(ref ModelData.RFinger.Stain,  id),
            EquipSlot.LFinger  => SetIfDifferent(ref ModelData.LFinger.Stain,  id),
            _                  => false,
        };
    }

    internal static bool SetIfDifferent<T>(ref T old, T value) where T : IEquatable<T>
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
                changes  |= SetIfDifferent(ref ModelData.Head.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Head.Variant, variant);
                changes  |= HeadName != name;
                HeadName =  name;
                changes  |= Head != id;
                Head     =  id;
                return changes;
            case EquipSlot.Body:
                changes  |= SetIfDifferent(ref ModelData.Body.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Body.Variant, variant);
                changes  |= BodyName != name;
                BodyName =  name;
                changes  |= Body != id;
                Body     =  id;
                return changes;
            case EquipSlot.Hands:
                changes   |= SetIfDifferent(ref ModelData.Hands.Set,     set);
                changes   |= SetIfDifferent(ref ModelData.Hands.Variant, variant);
                changes   |= HandsName != name;
                HandsName =  name;
                changes   |= Hands != id;
                Hands     =  id;
                return changes;
            case EquipSlot.Legs:
                changes  |= SetIfDifferent(ref ModelData.Legs.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Legs.Variant, variant);
                changes  |= LegsName != name;
                LegsName =  name;
                changes  |= Legs != id;
                Legs     =  id;
                return changes;
            case EquipSlot.Feet:
                changes  |= SetIfDifferent(ref ModelData.Feet.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Feet.Variant, variant);
                changes  |= FeetName != name;
                FeetName =  name;
                changes  |= Feet != id;
                Feet     =  id;
                return changes;
            case EquipSlot.Ears:
                changes  |= SetIfDifferent(ref ModelData.Ears.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Ears.Variant, variant);
                changes  |= EarsName != name;
                EarsName =  name;
                changes  |= Ears != id;
                Ears     =  id;
                return changes;
            case EquipSlot.Neck:
                changes  |= SetIfDifferent(ref ModelData.Neck.Set,     set);
                changes  |= SetIfDifferent(ref ModelData.Neck.Variant, variant);
                changes  |= NeckName != name;
                NeckName =  name;
                changes  |= Neck != id;
                Neck     =  id;
                return changes;
            case EquipSlot.Wrists:
                changes    |= SetIfDifferent(ref ModelData.Wrists.Set,     set);
                changes    |= SetIfDifferent(ref ModelData.Wrists.Variant, variant);
                changes    |= WristsName != name;
                WristsName =  name;
                changes    |= Wrists != id;
                Wrists     =  id;
                return changes;
            case EquipSlot.RFinger:
                changes     |= SetIfDifferent(ref ModelData.RFinger.Set,     set);
                changes     |= SetIfDifferent(ref ModelData.RFinger.Variant, variant);
                changes     |= RFingerName != name;
                RFingerName =  name;
                changes     |= RFinger != id;
                RFinger     =  id;
                return changes;
            case EquipSlot.LFinger:
                changes     |= SetIfDifferent(ref ModelData.LFinger.Set,     set);
                changes     |= SetIfDifferent(ref ModelData.LFinger.Variant, variant);
                changes     |= LFingerName != name;
                LFingerName =  name;
                changes     |= LFinger != id;
                LFinger     =  id;
                return changes;
            default: return false;
        }
    }
}

public static class DesignBase64Migration
{
    public const int Base64Size = 91;

    public static ModelData MigrateBase64(string base64, out EquipFlag equipFlags, out CustomizeFlag customizeFlags,
        out bool writeinternal, out QuadBool wet, out QuadBool hat, out QuadBool visor, out QuadBool weapon)
    {
        static void CheckSize(int length, int requiredLength)
        {
            if (length != requiredLength)
                throw new Exception(
                    $"Can not parse Base64 string into CharacterSave:\n\tInvalid size {length} instead of {requiredLength}.");
        }

        byte   applicationFlags;
        ushort equipFlagsS;
        var    bytes = Convert.FromBase64String(base64);
        hat    = QuadBool.Null;
        visor  = QuadBool.Null;
        weapon = QuadBool.Null;
        switch (bytes[0])
        {
            case 1:
            {
                CheckSize(bytes.Length, 86);
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                break;
            }
            case 2:
            {
                CheckSize(bytes.Length, Base64Size);
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                hat              = hat.SetValue((bytes[90] & 0x01) == 0);
                visor            = visor.SetValue((bytes[90] & 0x10) != 0);
                weapon           = weapon.SetValue((bytes[90] & 0x02) == 0);
                break;
            }
            default: throw new Exception($"Can not parse Base64 string into design for migration:\n\tInvalid Version {bytes[0]}.");
        }

        customizeFlags = (applicationFlags & 0x01) != 0 ? CustomizeFlagExtensions.All : 0;
        wet            = (applicationFlags & 0x02) != 0 ? QuadBool.True : QuadBool.NullFalse;
        hat            = hat.SetEnabled((applicationFlags & 0x04) != 0);
        weapon         = weapon.SetEnabled((applicationFlags & 0x08) != 0);
        visor          = visor.SetEnabled((applicationFlags & 0x10) != 0);
        writeinternal  = (applicationFlags & 0x20) != 0;

        equipFlags =  0;
        equipFlags |= (equipFlagsS & 0x0001) != 0 ? EquipFlag.Mainhand | EquipFlag.MainhandStain : 0;
        equipFlags |= (equipFlagsS & 0x0002) != 0 ? EquipFlag.Offhand | EquipFlag.OffhandStain : 0;
        var flag = 0x0002u;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            flag       <<= 1;
            equipFlags |=  (equipFlagsS & flag) != 0 ? slot.ToFlag() | slot.ToStainFlag() : 0;
        }

        var data = new ModelData();
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                data.CustomizeData.Read(ptr + 4);
                var cur = (CharacterWeapon*)(ptr + 30);
                data.MainHand = cur[0];
                data.OffHand  = cur[1];
                var eq = (CharacterArmor*)(cur + 2);
                foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
                    data.Equipment[slot] = eq[idx];
            }
        }

        return data;
    }

    public static unsafe string CreateOldBase64(in ModelData save, EquipFlag equipFlags, CustomizeFlag customizeFlags, bool wet, bool hat,
        bool setHat, bool visor, bool setVisor, bool weapon, bool setWeapon, bool writeinternal, float alpha)
    {
        var data = stackalloc byte[Base64Size];
        data[0] = 2;
        data[1] = (byte)((customizeFlags == CustomizeFlagExtensions.All ? 0x01 : 0)
          | (wet ? 0x02 : 0)
          | (setHat ? 0x04 : 0)
          | (setWeapon ? 0x08 : 0)
          | (setVisor ? 0x10 : 0)
          | (writeinternal ? 0x20 : 0));
        data[2] = (byte)((equipFlags.HasFlag(EquipFlag.Mainhand) ? 0x01 : 0)
          | (equipFlags.HasFlag(EquipFlag.Offhand) ? 0x02 : 0)
          | (equipFlags.HasFlag(EquipFlag.Head) ? 0x04 : 0)
          | (equipFlags.HasFlag(EquipFlag.Body) ? 0x08 : 0)
          | (equipFlags.HasFlag(EquipFlag.Hands) ? 0x10 : 0)
          | (equipFlags.HasFlag(EquipFlag.Legs) ? 0x20 : 0)
          | (equipFlags.HasFlag(EquipFlag.Feet) ? 0x40 : 0)
          | (equipFlags.HasFlag(EquipFlag.Ears) ? 0x80 : 0));
        data[3] = (byte)((equipFlags.HasFlag(EquipFlag.Neck) ? 0x01 : 0)
          | (equipFlags.HasFlag(EquipFlag.Wrist) ? 0x02 : 0)
          | (equipFlags.HasFlag(EquipFlag.RFinger) ? 0x04 : 0)
          | (equipFlags.HasFlag(EquipFlag.LFinger) ? 0x08 : 0));
        save.CustomizeData.Write(data + 4);
        ((CharacterWeapon*)(data + 30))[0] = save.MainHand;
        ((CharacterWeapon*)(data + 30))[1] = save.OffHand;
        ((CharacterArmor*)(data + 44))[0]  = save.Head;
        ((CharacterArmor*)(data + 44))[1]  = save.Body;
        ((CharacterArmor*)(data + 44))[2]  = save.Hands;
        ((CharacterArmor*)(data + 44))[3]  = save.Legs;
        ((CharacterArmor*)(data + 44))[4]  = save.Feet;
        ((CharacterArmor*)(data + 44))[5]  = save.Ears;
        ((CharacterArmor*)(data + 44))[6]  = save.Neck;
        ((CharacterArmor*)(data + 44))[7]  = save.Wrists;
        ((CharacterArmor*)(data + 44))[8]  = save.RFinger;
        ((CharacterArmor*)(data + 44))[9]  = save.LFinger;
        *(float*)(data + 84)               = 1f;
        data[88] = (byte)((hat ? 0x01 : 0)
          | (visor ? 0x10 : 0)
          | (weapon ? 0x02 : 0));

        return Convert.ToBase64String(new Span<byte>(data, Base64Size));
    }
}
