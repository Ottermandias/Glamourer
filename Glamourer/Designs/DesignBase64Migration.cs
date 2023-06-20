using System;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public static class DesignBase64Migration
{
    public const int Base64Size = 91;

    public static DesignData MigrateBase64(ItemManager items, string base64, out EquipFlag equipFlags, out CustomizeFlag customizeFlags,
        out bool writeProtected, out bool applyHat, out bool applyVisor, out bool applyWeapon)
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
        applyHat     = false;
        applyVisor   = false;
        applyWeapon  = false;
        var data = new DesignData();
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
                data.SetHatVisible((bytes[90] & 0x01) == 0);
                data.SetVisor((bytes[90] & 0x10) != 0);
                data.SetWeaponVisible((bytes[90] & 0x02) == 0);
                break;
            }
            default: throw new Exception($"Can not parse Base64 string into design for migration:\n\tInvalid Version {bytes[0]}.");
        }

        customizeFlags = (applicationFlags & 0x01) != 0 ? CustomizeFlagExtensions.All : 0;
        data.SetIsWet((applicationFlags & 0x02) != 0);
        applyHat       = (applicationFlags & 0x04) != 0;
        applyWeapon    = (applicationFlags & 0x08) != 0;
        applyVisor     = (applicationFlags & 0x10) != 0;
        writeProtected = (applicationFlags & 0x20) != 0;

        equipFlags =  0;
        equipFlags |= (equipFlagsS & 0x0001) != 0 ? EquipFlag.Mainhand | EquipFlag.MainhandStain : 0;
        equipFlags |= (equipFlagsS & 0x0002) != 0 ? EquipFlag.Offhand | EquipFlag.OffhandStain : 0;
        var flag = 0x0002u;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            flag       <<= 1;
            equipFlags |=  (equipFlagsS & flag) != 0 ? slot.ToFlag() | slot.ToStainFlag() : 0;
        }

        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                data.Customize.Load(*(Customize*)(ptr + 4));
                var cur  = (CharacterWeapon*)(ptr + 30);
                var main = items.Identify(EquipSlot.MainHand, cur[0].Set, cur[0].Type, (byte)cur[0].Variant);
                if (!main.Valid)
                    throw new Exception($"Base64 string invalid, weapon could not be identified.");

                data.SetItem(EquipSlot.MainHand, main);
                data.SetStain(EquipSlot.MainHand, cur[0].Stain);
                var off = items.Identify(EquipSlot.OffHand, cur[1].Set, cur[1].Type, (byte)cur[1].Variant, main.Type);
                if (!off.Valid)
                    throw new Exception($"Base64 string invalid, weapon could not be identified.");

                data.SetItem(EquipSlot.OffHand, off);
                data.SetStain(EquipSlot.OffHand, cur[1].Stain);

                var eq = (CharacterArmor*)(cur + 2);
                foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
                {
                    var mdl  = eq[idx];
                    var item = items.Identify(slot, mdl.Set, mdl.Variant);
                    if (!item.Valid)
                        throw new Exception($"Base64 string invalid, item could not be identified.");

                    data.SetItem(slot, item);
                    data.SetStain(slot, mdl.Stain);
                }
            }
        }

        return data;
    }

    public static unsafe string CreateOldBase64(in DesignData save, EquipFlag equipFlags, CustomizeFlag customizeFlags,
        bool setHat, bool setVisor, bool setWeapon, bool writeProtected, float alpha = 1.0f)
    {
        var data = stackalloc byte[Base64Size];
        data[0] = 2;
        data[1] = (byte)((customizeFlags == CustomizeFlagExtensions.All ? 0x01 : 0)
          | (save.IsWet() ? 0x02 : 0)
          | (setHat ? 0x04 : 0)
          | (setWeapon ? 0x08 : 0)
          | (setVisor ? 0x10 : 0)
          | (writeProtected ? 0x20 : 0));
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
        save.Customize.Write((nint)data + 4);
        ((CharacterWeapon*)(data + 30))[0] = save.Item(EquipSlot.MainHand).Weapon(save.Stain(EquipSlot.MainHand));
        ((CharacterWeapon*)(data + 30))[1] = save.Item(EquipSlot.OffHand).Weapon(save.Stain(EquipSlot.OffHand));
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            ((CharacterArmor*)(data + 44))[slot.ToIndex()] = save.Item(slot).Armor(save.Stain(slot));
        *(ushort*)(data + 84) = 1; // IsSet.
        *(float*)(data + 86)  = 1f;
        data[90] = (byte)((save.IsHatVisible() ? 0x00 : 0x01)
          | (save.IsVisorToggled() ? 0x10 : 0)
          | (save.IsWeaponVisible() ? 0x00 : 0x02));

        return Convert.ToBase64String(new Span<byte>(data, Base64Size));
    }
}
