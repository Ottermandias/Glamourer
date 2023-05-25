using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.PlayerWatch;

namespace Glamourer;

public static class WriteExtensions
{
    private static unsafe void Write(IntPtr characterPtr, EquipSlot slot, SetId? id, WeaponType? type, ushort? variant, StainId? stain)
    {
        void WriteWeapon(WeaponModelId* address)
        {
            if (id.HasValue)
                address->Id = (ushort)id.Value;

            if (type.HasValue)
                address->Type = (ushort)type.Value;

            if (variant.HasValue)
                address->Variant = variant.Value;

            if (*(ushort*)address == 0)
                address->Stain = 0;
            else if (stain.HasValue)
                address->Stain = (byte)stain.Value;
        }

        void WriteEquip(EquipmentModelId* address)
        {
            if (id.HasValue)
                address->Id = (ushort)id.Value;

            if (variant < byte.MaxValue)
                address->Variant = (byte)variant.Value;

            if (stain.HasValue)
                address->Stain = (byte)stain.Value;
        }

        var ptr = (Character*)characterPtr;
        switch (slot)
        {
            case EquipSlot.MainHand:
                WriteWeapon(&ptr->DrawData.MainHandModel);
                break;
            case EquipSlot.OffHand:
                WriteWeapon(&ptr->DrawData.OffHandModel);
                break;
            case EquipSlot.Head:
                WriteEquip(&ptr->DrawData.Head);
                break;
            case EquipSlot.Body:
                WriteEquip(&ptr->DrawData.Top);
                break;
            case EquipSlot.Hands:
                WriteEquip(&ptr->DrawData.Arms);
                break;
            case EquipSlot.Legs:
                WriteEquip(&ptr->DrawData.Legs);
                break;
            case EquipSlot.Feet:
                WriteEquip(&ptr->DrawData.Feet);
                break;
            case EquipSlot.Ears:
                WriteEquip(&ptr->DrawData.Ear);
                break;
            case EquipSlot.Neck:
                WriteEquip(&ptr->DrawData.Neck);
                break;
            case EquipSlot.Wrists:
                WriteEquip(&ptr->DrawData.Wrist);
                break;
            case EquipSlot.RFinger:
                WriteEquip(&ptr->DrawData.RFinger);
                break;
            case EquipSlot.LFinger:
                WriteEquip(&ptr->DrawData.LFinger);
                break;
            default: throw new InvalidEnumArgumentException();
        }
    }

    public static void Write(this Stain stain, IntPtr characterPtr, EquipSlot slot)
        => Write(characterPtr, slot, null, null, null, stain.RowIndex);

    public static void Write(this Item item, IntPtr characterAddress)
    {
        var (id, type, variant) = item.MainModel;
        Write(characterAddress, item.EquippableTo, id, type, variant, null);
        if (item.EquippableTo == EquipSlot.MainHand && item.HasSubModel)
        {
            var (subId, subType, subVariant) = item.SubModel;
            Write(characterAddress, EquipSlot.OffHand, subId, subType, subVariant, null);
        }
    }

    public static void Write(this CharacterArmor armor, IntPtr characterAddress, EquipSlot slot)
        => Write(characterAddress, slot, armor.Set, null, armor.Variant, armor.Stain);

    public static void Write(this CharacterWeapon weapon, IntPtr characterAddress, EquipSlot slot)
        => Write(characterAddress, slot, weapon.Set, weapon.Type, weapon.Variant, weapon.Stain);

    public static unsafe void Write(this CharacterEquipment equip, IntPtr characterAddress)
    {
        if (equip.IsSet == 0)
            return;

        Write(characterAddress, EquipSlot.MainHand, equip.MainHand.Set, equip.MainHand.Type, equip.MainHand.Variant, equip.MainHand.Stain);
        Write(characterAddress, EquipSlot.OffHand,  equip.OffHand.Set,  equip.OffHand.Type,  equip.OffHand.Variant,  equip.OffHand.Stain);

        fixed (CharacterArmor* equipment = &equip.Head)
        {
            Buffer.MemoryCopy(equipment, &((Character*)characterAddress)->DrawData.Head,
                CharacterEquipment.EquipmentSlots * sizeof(CharacterArmor), CharacterEquipment.EquipmentSlots * sizeof(CharacterArmor));
        }
    }

    public static void Write(this CharacterEquipment equip, IntPtr characterAddress, CharacterEquipMask models, CharacterEquipMask stains)
    {
        if (models == CharacterEquipMask.All && stains == CharacterEquipMask.All)
        {
            equip.Write(characterAddress);
            return;
        }

        if (models.HasFlag(CharacterEquipMask.MainHand))
            Write(characterAddress, EquipSlot.MainHand, equip.MainHand.Set, equip.MainHand.Type, equip.MainHand.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.MainHand))
            Write(characterAddress, EquipSlot.MainHand, null, null, null, equip.MainHand.Stain);
        if (models.HasFlag(CharacterEquipMask.OffHand))
            Write(characterAddress, EquipSlot.OffHand, equip.OffHand.Set, equip.OffHand.Type, equip.OffHand.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.OffHand))
            Write(characterAddress, EquipSlot.OffHand, null, null, null, equip.OffHand.Stain);

        if (models.HasFlag(CharacterEquipMask.Head))
            Write(characterAddress, EquipSlot.Head, equip.Head.Set, null, equip.Head.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Head))
            Write(characterAddress, EquipSlot.Head, null, null, null, equip.Head.Stain);
        if (models.HasFlag(CharacterEquipMask.Body))
            Write(characterAddress, EquipSlot.Body, equip.Body.Set, null, equip.Body.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Body))
            Write(characterAddress, EquipSlot.Body, null, null, null, equip.Body.Stain);
        if (models.HasFlag(CharacterEquipMask.Hands))
            Write(characterAddress, EquipSlot.Hands, equip.Hands.Set, null, equip.Hands.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Hands))
            Write(characterAddress, EquipSlot.Hands, null, null, null, equip.Hands.Stain);
        if (models.HasFlag(CharacterEquipMask.Legs))
            Write(characterAddress, EquipSlot.Legs, equip.Legs.Set, null, equip.Legs.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Legs))
            Write(characterAddress, EquipSlot.Legs, null, null, null, equip.Legs.Stain);
        if (models.HasFlag(CharacterEquipMask.Feet))
            Write(characterAddress, EquipSlot.Feet, equip.Feet.Set, null, equip.Feet.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Feet))
            Write(characterAddress, EquipSlot.Feet, null, null, null, equip.Feet.Stain);

        if (models.HasFlag(CharacterEquipMask.Ears))
            Write(characterAddress, EquipSlot.Ears, equip.Ears.Set, null, equip.Ears.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Ears))
            Write(characterAddress, EquipSlot.Ears, null, null, null, equip.Ears.Stain);
        if (models.HasFlag(CharacterEquipMask.Neck))
            Write(characterAddress, EquipSlot.Neck, equip.Neck.Set, null, equip.Neck.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Neck))
            Write(characterAddress, EquipSlot.Neck, null, null, null, equip.Neck.Stain);
        if (models.HasFlag(CharacterEquipMask.Wrists))
            Write(characterAddress, EquipSlot.Wrists, equip.Wrists.Set, null, equip.Wrists.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.Wrists))
            Write(characterAddress, EquipSlot.Wrists, null, null, null, equip.Wrists.Stain);
        if (models.HasFlag(CharacterEquipMask.LFinger))
            Write(characterAddress, EquipSlot.LFinger, equip.LFinger.Set, null, equip.LFinger.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.LFinger))
            Write(characterAddress, EquipSlot.LFinger, null, null, null, equip.LFinger.Stain);
        if (models.HasFlag(CharacterEquipMask.RFinger))
            Write(characterAddress, EquipSlot.RFinger, equip.RFinger.Set, null, equip.RFinger.Variant, null);
        if (stains.HasFlag(CharacterEquipMask.RFinger))
            Write(characterAddress, EquipSlot.RFinger, null, null, null, equip.RFinger.Stain);
    }
}
