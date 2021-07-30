using System;
using System.ComponentModel;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer
{
    public static class WriteExtensions
    {
        private static unsafe void Write(IntPtr actorPtr, EquipSlot slot, SetId? id, WeaponType? type, ushort? variant, StainId? stain)
        {
            void WriteWeapon(int offset)
            {
                var address = (byte*) actorPtr + offset;
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
                var address = (byte*) actorPtr + offset;
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
                    WriteWeapon(ActorEquipment.MainWeaponOffset);
                    break;
                case EquipSlot.OffHand:
                    WriteWeapon(ActorEquipment.OffWeaponOffset);
                    break;
                case EquipSlot.Head:
                    WriteEquip(ActorEquipment.EquipmentOffset);
                    break;
                case EquipSlot.Body:
                    WriteEquip(ActorEquipment.EquipmentOffset + 4);
                    break;
                case EquipSlot.Hands:
                    WriteEquip(ActorEquipment.EquipmentOffset + 8);
                    break;
                case EquipSlot.Legs:
                    WriteEquip(ActorEquipment.EquipmentOffset + 12);
                    break;
                case EquipSlot.Feet:
                    WriteEquip(ActorEquipment.EquipmentOffset + 16);
                    break;
                case EquipSlot.Ears:
                    WriteEquip(ActorEquipment.EquipmentOffset + 20);
                    break;
                case EquipSlot.Neck:
                    WriteEquip(ActorEquipment.EquipmentOffset + 24);
                    break;
                case EquipSlot.Wrists:
                    WriteEquip(ActorEquipment.EquipmentOffset + 28);
                    break;
                case EquipSlot.RFinger:
                    WriteEquip(ActorEquipment.EquipmentOffset + 32);
                    break;
                case EquipSlot.LFinger:
                    WriteEquip(ActorEquipment.EquipmentOffset + 36);
                    break;
                default: throw new InvalidEnumArgumentException();
            }
        }

        public static void Write(this Stain stain, IntPtr actorPtr, EquipSlot slot)
            => Write(actorPtr, slot, null, null, null, stain.RowIndex);

        public static void Write(this Item item, IntPtr actorAddress)
        {
            var (id, type, variant) = item.MainModel;
            Write(actorAddress, item.EquippableTo, id, type, variant, null);
            if (item.EquippableTo == EquipSlot.MainHand && item.HasSubModel)
            {
                var (subId, subType, subVariant) = item.SubModel;
                Write(actorAddress, EquipSlot.OffHand, subId, subType, subVariant, null);
            }
        }

        public static void Write(this ActorArmor armor, IntPtr actorAddress, EquipSlot slot)
            => Write(actorAddress, slot, armor.Set, null, armor.Variant, armor.Stain);

        public static void Write(this ActorWeapon weapon, IntPtr actorAddress, EquipSlot slot)
            => Write(actorAddress, slot, weapon.Set, weapon.Type, weapon.Variant, weapon.Stain);

        public static unsafe void Write(this ActorEquipment equip, IntPtr actorAddress)
        {
            if (equip.IsSet == 0)
                return;

            Write(actorAddress, EquipSlot.MainHand, equip.MainHand.Set, equip.MainHand.Type, equip.MainHand.Variant, equip.MainHand.Stain);
            Write(actorAddress, EquipSlot.OffHand,  equip.OffHand.Set,  equip.OffHand.Type,  equip.OffHand.Variant,  equip.OffHand.Stain);

            fixed (ActorArmor* equipment = &equip.Head)
            {
                Buffer.MemoryCopy(equipment, (byte*) actorAddress + ActorEquipment.EquipmentOffset,
                    ActorEquipment.EquipmentSlots * sizeof(ActorArmor), ActorEquipment.EquipmentSlots * sizeof(ActorArmor));
            }
        }

        public static void Write(this ActorEquipment equip, IntPtr actorAddress, ActorEquipMask models, ActorEquipMask stains)
        {
            if (models == ActorEquipMask.All && stains == ActorEquipMask.All)
            {
                equip.Write(actorAddress);
                return;
            }

            if (models.HasFlag(ActorEquipMask.MainHand))
                Write(actorAddress, EquipSlot.MainHand, equip.MainHand.Set, equip.MainHand.Type, equip.MainHand.Variant, null);
            if (stains.HasFlag(ActorEquipMask.MainHand))
                Write(actorAddress, EquipSlot.MainHand, null, null, null, equip.MainHand.Stain);
            if (models.HasFlag(ActorEquipMask.OffHand))
                Write(actorAddress, EquipSlot.OffHand, equip.OffHand.Set, equip.OffHand.Type, equip.OffHand.Variant, null);
            if (stains.HasFlag(ActorEquipMask.OffHand))
                Write(actorAddress, EquipSlot.OffHand, null, null, null, equip.OffHand.Stain);

            if (models.HasFlag(ActorEquipMask.Head))
                Write(actorAddress, EquipSlot.Head, equip.Head.Set, null, equip.Head.Variant, null);
            if (stains.HasFlag(ActorEquipMask.Head))
                Write(actorAddress, EquipSlot.Head, null, null, null, equip.Head.Stain);
            if (models.HasFlag(ActorEquipMask.Body))
                Write(actorAddress, EquipSlot.Body, equip.Body.Set, null, equip.Body.Variant, null);
            if (stains.HasFlag(ActorEquipMask.Body))
                Write(actorAddress, EquipSlot.Body, null, null, null, equip.Body.Stain);
            if (models.HasFlag(ActorEquipMask.Hands))
                Write(actorAddress, EquipSlot.Hands, equip.Hands.Set, null, equip.Hands.Variant, null);
            if (stains.HasFlag(ActorEquipMask.Hands))
                Write(actorAddress, EquipSlot.Hands, null, null, null, equip.Hands.Stain);
            if (models.HasFlag(ActorEquipMask.Legs))
                Write(actorAddress, EquipSlot.Legs, equip.Legs.Set, null, equip.Legs.Variant, null);
            if (stains.HasFlag(ActorEquipMask.Legs))
                Write(actorAddress, EquipSlot.Legs, null, null, null, equip.Legs.Stain);
            if (models.HasFlag(ActorEquipMask.Feet))
                Write(actorAddress, EquipSlot.Feet, equip.Feet.Set, null, equip.Feet.Variant, null);
            if (stains.HasFlag(ActorEquipMask.Feet))
                Write(actorAddress, EquipSlot.Feet, null, null, null, equip.Feet.Stain);

            if (models.HasFlag(ActorEquipMask.Ears))
                Write(actorAddress, EquipSlot.Ears, equip.Ears.Set, null, equip.Ears.Variant, null);
            if (models.HasFlag(ActorEquipMask.Neck))
                Write(actorAddress, EquipSlot.Neck, equip.Neck.Set, null, equip.Neck.Variant, null);
            if (models.HasFlag(ActorEquipMask.Wrists))
                Write(actorAddress, EquipSlot.Wrists, equip.Wrists.Set, null, equip.Wrists.Variant, null);
            if (models.HasFlag(ActorEquipMask.LFinger))
                Write(actorAddress, EquipSlot.LFinger, equip.LFinger.Set, null, equip.LFinger.Variant, null);
            if (models.HasFlag(ActorEquipMask.RFinger))
                Write(actorAddress, EquipSlot.RFinger, equip.RFinger.Set, null, equip.RFinger.Variant, null);
        }
    }
}
