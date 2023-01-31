using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Structs;

// An Item wrapper struct that contains the item table, a precomputed name and the associated equip slot.
public readonly struct Item2
{
    public readonly Lumina.Excel.GeneratedSheets.Item Base;
    public readonly string                            Name;
    public readonly EquipSlot                         EquippableTo;

    // Obtain the main model info used by the item.
    public (SetId id, WeaponType type, ushort variant) MainModel
        => ParseModel(EquippableTo, Base.ModelMain);

    // Obtain the sub model info used by the item. Will be 0 if the item has no sub model.
    public (SetId id, WeaponType type, ushort variant) SubModel
        => ParseModel(EquippableTo, Base.ModelSub);

    public bool HasSubModel
        => Base.ModelSub != 0;

    public bool IsBothHand
        => (EquipSlot)Base.EquipSlotCategory.Row == EquipSlot.BothHand;

    public FullEquipType WeaponCategory
        => ((WeaponCategory) (Base.ItemUICategory?.Row ?? 0)).ToEquipType();

    // Create a new item from its sheet list with the given name and either the inferred equip slot or the given one.
    public Item2(Lumina.Excel.GeneratedSheets.Item item, string name, EquipSlot slot = EquipSlot.Unknown)
    {
        Base         = item;
        Name         = name;
        EquippableTo = slot == EquipSlot.Unknown ? ((EquipSlot)item.EquipSlotCategory.Row).ToSlot() : slot;
    }

    // Create empty Nothing items.
    public static Item2 Nothing(EquipSlot slot)
        => new("Nothing", slot);

    // Produce the relevant model information for a given item and equip slot.
    private static (SetId id, WeaponType type, ushort variant) ParseModel(EquipSlot slot, ulong data)
    {
        if (slot is EquipSlot.MainHand or EquipSlot.OffHand)
        {
            var id      = (SetId)(data & 0xFFFF);
            var type    = (WeaponType)((data >> 16) & 0xFFFF);
            var variant = (ushort)((data >> 32) & 0xFFFF);
            return (id, type, variant);
        }
        else
        {
            var id      = (SetId)(data & 0xFFFF);
            var variant = (byte)((data >> 16) & 0xFF);
            return (id, new WeaponType(), variant);
        }
    }

    // Used for 'Nothing' items.
    private Item2(string name, EquipSlot slot)
    {
        Name         = name;
        Base         = new Lumina.Excel.GeneratedSheets.Item();
        EquippableTo = slot;
    }

    public override string ToString()
        => Name;
}
