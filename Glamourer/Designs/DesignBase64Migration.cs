using Glamourer.Api.Enums;
using Glamourer.Services;
using OtterGui;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignBase64Migration
{
    public const int Base64SizeV1 = 86;
    public const int Base64SizeV2 = 91;
    public const int Base64SizeV4 = 95;

    public static unsafe DesignData MigrateBase64(ItemManager items, HumanModelList humans, string base64, out EquipFlag equipFlags,
        out CustomizeFlag customizeFlags, out bool writeProtected, out MetaFlag metaFlags)
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
        metaFlags = MetaFlag.Wetness;
        var data = new DesignData();
        switch (bytes[0])
        {
            case 1:
            {
                CheckSize(bytes.Length, Base64SizeV1);
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                break;
            }
            case 2:
            {
                CheckSize(bytes.Length, Base64SizeV2);
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                data.SetHatVisible((bytes[90] & 0x01) == 0);
                data.SetVisor((bytes[90] & 0x10) != 0);
                data.SetWeaponVisible((bytes[90] & 0x02) == 0);
                break;
            }
            case 3: // does not exist as old base64.
                throw new Exception(
                    $"Can not parse Base64 string into design for migration:\n\tInvalid Version {bytes[0]} can not be migrated.");
            case 4:
            {
                CheckSize(bytes.Length, Base64SizeV4); // contains model id
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                data.SetHatVisible((bytes[90] & 0x01) == 0);
                data.SetVisor((bytes[90] & 0x10) != 0);
                data.SetWeaponVisible((bytes[90] & 0x02) == 0);
                data.ModelId = bytes[91] | ((uint)bytes[92] << 8) | ((uint)bytes[93] << 16) | ((uint)bytes[94] << 24);
                break;
            }
            case 5:
                bytes = bytes[..Base64SizeV4];
                CheckSize(bytes.Length, Base64SizeV4); // contains model id
                applicationFlags = bytes[1];
                equipFlagsS      = BitConverter.ToUInt16(bytes, 2);
                data.SetHatVisible((bytes[90] & 0x01) == 0);
                data.SetVisor((bytes[90] & 0x10) != 0);
                data.SetWeaponVisible((bytes[90] & 0x02) == 0);
                data.ModelId = bytes[91] | ((uint)bytes[92] << 8) | ((uint)bytes[93] << 16) | ((uint)bytes[94] << 24);
                break;
            default: throw new Exception($"Can not parse Base64 string into design for migration:\n\tInvalid Version {bytes[0]}.");
        }

        customizeFlags = (applicationFlags & 0x01) != 0 ? CustomizeFlagExtensions.All : 0;
        data.SetIsWet((applicationFlags & 0x02) != 0);
        if ((applicationFlags & 0x04) != 0)
            metaFlags |= MetaFlag.HatState;
        if ((applicationFlags & 0x08) != 0)
            metaFlags |= MetaFlag.WeaponState;
        if ((applicationFlags & 0x10) != 0)
            metaFlags |= MetaFlag.VisorState;
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

        fixed (byte* ptr = bytes)
        {
            var cur = (LegacyCharacterWeapon*)(ptr + 30);
            var eq  = (LegacyCharacterArmor*)(cur + 2);

            if (!humans.IsHuman(data.ModelId))
            {
                data.LoadNonHuman(data.ModelId, *(CustomizeArray*)(ptr + 4), (nint)eq);
                return data;
            }

            data.Customize = *(CustomizeArray*)(ptr + 4);
            foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
            {
                var mdl  = eq[idx];
                var item = items.Identify(slot, mdl.Set, mdl.Variant);
                if (!item.Valid)
                {
                    Glamourer.Log.Warning("Base64 string invalid, item could not be identified.");
                    item = ItemManager.NothingItem(slot);
                }

                data.SetItem(slot, item);
                data.SetStain(slot, mdl.Stain);
            }

            var main = cur[0].Skeleton.Id == 0
                ? items.DefaultSword
                : items.Identify(EquipSlot.MainHand, cur[0].Skeleton, cur[0].Weapon, cur[0].Variant);
            if (!main.Valid)
            {
                Glamourer.Log.Warning("Base64 string invalid, weapon could not be identified.");
                main = items.DefaultSword;
            }

            data.SetItem(EquipSlot.MainHand, main);
            data.SetStain(EquipSlot.MainHand, cur[0].Stain);

            EquipItem off;
            // Fist weapon hack
            if (main.PrimaryId.Id is > 1600 and < 1651 && cur[1].Variant == 0)
            {
                off = items.Identify(EquipSlot.OffHand, (PrimaryId)(main.PrimaryId.Id + 50), main.SecondaryId, main.Variant, main.Type);
                var gauntlet = items.Identify(EquipSlot.Hands, cur[1].Skeleton, (Variant)cur[1].Weapon.Id);
                if (gauntlet.Valid)
                {
                    data.SetItem(EquipSlot.Hands, gauntlet);
                    data.SetStain(EquipSlot.Hands, cur[0].Stain);
                }
            }
            else
            {
                off = cur[0].Skeleton.Id == 0
                    ? ItemManager.NothingItem(FullEquipType.Shield)
                    : items.Identify(EquipSlot.OffHand, cur[1].Skeleton, cur[1].Weapon, cur[1].Variant, main.Type);
            }

            if (main.Type.ValidOffhand() != FullEquipType.Unknown && !off.Valid)
            {
                Glamourer.Log.Warning("Base64 string invalid, weapon could not be identified.");
                off = items.GetDefaultOffhand(main);
            }

            data.SetItem(EquipSlot.OffHand, off);
            data.SetStain(EquipSlot.OffHand, cur[1].Stain);
            return data;
        }
    }

    public static unsafe string CreateOldBase64(in DesignData save, EquipFlag equipFlags, CustomizeFlag customizeFlags, MetaFlag meta,
        bool writeProtected, float alpha = 1.0f)
    {
        var data = stackalloc byte[Base64SizeV4];
        data[0] = 5;
        data[1] = (byte)((customizeFlags == CustomizeFlagExtensions.All ? 0x01 : 0)
          | (save.IsWet() ? 0x02 : 0)
          | (meta.HasFlag(MetaFlag.HatState) ? 0x04 : 0)
          | (meta.HasFlag(MetaFlag.WeaponState) ? 0x08 : 0)
          | (meta.HasFlag(MetaFlag.VisorState) ? 0x10 : 0)
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
        save.Customize.Write(data + 4);
        ((LegacyCharacterWeapon*)(data + 30))[0] =
            new LegacyCharacterWeapon(save.Item(EquipSlot.MainHand).Weapon(save.Stain(EquipSlot.MainHand)));
        ((LegacyCharacterWeapon*)(data + 30))[1] =
            new LegacyCharacterWeapon(save.Item(EquipSlot.OffHand).Weapon(save.Stain(EquipSlot.OffHand)));
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            ((LegacyCharacterArmor*)(data + 44))[slot.ToIndex()] = new LegacyCharacterArmor(save.Item(slot).Armor(save.Stain(slot)));
        *(ushort*)(data + 84) = 1; // IsSet.
        *(float*)(data + 86)  = 1f;
        data[90] = (byte)((save.IsHatVisible() ? 0x00 : 0x01)
          | (save.IsVisorToggled() ? 0x10 : 0)
          | (save.IsWeaponVisible() ? 0x00 : 0x02));

        data[91] = (byte)save.ModelId;
        data[92] = (byte)(save.ModelId >> 8);
        data[93] = (byte)(save.ModelId >> 16);
        data[94] = (byte)(save.ModelId >> 24);

        return Convert.ToBase64String(new Span<byte>(data, Base64SizeV4));
    }
}
