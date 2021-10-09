using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer
{
    public readonly struct Item
    {
        private static (SetId id, WeaponType type, ushort variant) ParseModel(EquipSlot slot, ulong data)
        {
            if (slot == EquipSlot.MainHand || slot == EquipSlot.OffHand)
            {
                var id      = (SetId) (data & 0xFFFF);
                var type    = (WeaponType) ((data >> 16) & 0xFFFF);
                var variant = (ushort) ((data >> 32) & 0xFFFF);
                return (id, type, variant);
            }
            else
            {
                var id      = (SetId) (data & 0xFFFF);
                var variant = (byte) ((data >> 16) & 0xFF);
                return (id, new WeaponType(), variant);
            }
        }

        public readonly Lumina.Excel.GeneratedSheets.Item Base;
        public readonly string                            Name;
        public readonly EquipSlot                         EquippableTo;

        public (SetId id, WeaponType type, ushort variant) MainModel
            => ParseModel(EquippableTo, Base.ModelMain);

        public bool HasSubModel
            => Base.ModelSub != 0;

        public (SetId id, WeaponType type, ushort variant) SubModel
            => ParseModel(EquippableTo, Base.ModelSub);

        public Item(Lumina.Excel.GeneratedSheets.Item item, string name, EquipSlot slot = EquipSlot.Unknown)
        {
            Base         = item;
            Name         = name;
            EquippableTo = slot == EquipSlot.Unknown ? ((EquipSlot) item.EquipSlotCategory.Row).ToSlot() : slot;
        }

        public static Item Nothing(EquipSlot slot)
            => new("Nothing", slot);

        private Item(string name, EquipSlot slot)
        {
            Name         = name;
            Base         = new Lumina.Excel.GeneratedSheets.Item();
            EquippableTo = slot;
        }
    }
}
