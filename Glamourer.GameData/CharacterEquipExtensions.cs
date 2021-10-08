using System;
using System.ComponentModel;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer
{
    public static class WriteExtensions
    {
        private static unsafe void Write(IntPtr characterPtr, EquipSlot slot, SetId? id, WeaponType? type, ushort? variant, StainId? stain)
        {
            void WriteWeapon(int offset)
            {
                var address = (byte*) characterPtr + offset;
                if (id.HasValue)
                    *(ushort*) address = (ushort) id.Value;

                if (type.HasValue)
                    *(ushort*) (address + 2) = (ushort) type.Value;

                if (variant.HasValue)
                    *(ushort*) (address + 4) = variant.Value;

                if (stain.HasValue)
                    *(address + 6) = (byte) stain.Value;
            }

            void WriteEquip(int offset)
            {
                var address = (byte*) characterPtr + offset;
                if (id.HasValue)
                    *(ushort*) address = (ushort) id.Value;

                if (variant < byte.MaxValue)
                    *(address + 2) = (byte) variant.Value;

                if (stain.HasValue)
                    *(address + 3) = (byte) stain.Value;
            }

            switch (slot)
            {
                case EquipSlot.MainHand:
                    WriteWeapon(CharacterEquipment.MainWeaponOffset);
                    break;
                case EquipSlot.OffHand:
                    WriteWeapon(CharacterEquipment.OffWeaponOffset);
                    break;
                case EquipSlot.Head:
                    WriteEquip(CharacterEquipment.EquipmentOffset);
                    break;
                case EquipSlot.Body:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 4);
                    break;
                case EquipSlot.Hands:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 8);
                    break;
                case EquipSlot.Legs:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 12);
                    break;
                case EquipSlot.Feet:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 16);
                    break;
                case EquipSlot.Ears:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 20);
                    break;
                case EquipSlot.Neck:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 24);
                    break;
                case EquipSlot.Wrists:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 28);
                    break;
                case EquipSlot.RFinger:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 32);
                    break;
                case EquipSlot.LFinger:
                    WriteEquip(CharacterEquipment.EquipmentOffset + 36);
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
                Buffer.MemoryCopy(equipment, (byte*) characterAddress + CharacterEquipment.EquipmentOffset,
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
            if (models.HasFlag(CharacterEquipMask.Neck))
                Write(characterAddress, EquipSlot.Neck, equip.Neck.Set, null, equip.Neck.Variant, null);
            if (models.HasFlag(CharacterEquipMask.Wrists))
                Write(characterAddress, EquipSlot.Wrists, equip.Wrists.Set, null, equip.Wrists.Variant, null);
            if (models.HasFlag(CharacterEquipMask.LFinger))
                Write(characterAddress, EquipSlot.LFinger, equip.LFinger.Set, null, equip.LFinger.Variant, null);
            if (models.HasFlag(CharacterEquipMask.RFinger))
                Write(characterAddress, EquipSlot.RFinger, equip.RFinger.Set, null, equip.RFinger.Variant, null);
        }
    }
}
