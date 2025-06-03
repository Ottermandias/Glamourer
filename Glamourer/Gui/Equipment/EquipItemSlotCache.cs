using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

[InlineArray(13)]
public struct EquipItemSlotCache
{
    private EquipItem _element;

    public EquipItem Dragged
    {
        get => this[^1];
        set => this[^1] = value;
    }

    public void Clear()
        => ((Span<EquipItem>)this).Clear();

    public EquipItem this[EquipSlot slot]
    {
        get => this[(int)slot.ToIndex()];
        set => this[(int)slot.ToIndex()] = value;
    }

    public void Update(ItemManager items, in EquipItem item, EquipSlot startSlot)
    {
        if (item.Id == Dragged.Id && item.Type == Dragged.Type)
            return;

        switch (startSlot)
        {
            case EquipSlot.MainHand:
            {
                Clear();
                this[EquipSlot.MainHand] = item;
                if (item.Type is FullEquipType.Sword)
                    this[EquipSlot.OffHand] = items.FindClosestShield(item.ItemId, out var shield) ? shield : default;
                else
                    this[EquipSlot.OffHand] = items.ItemData.Secondary.GetValueOrDefault(item.ItemId);
                break;
            }
            case EquipSlot.OffHand:
            {
                Clear();
                if (item.Type is FullEquipType.Shield)
                    this[EquipSlot.MainHand] = items.FindClosestSword(item.ItemId, out var sword) ? sword : default;
                else
                    this[EquipSlot.MainHand] = items.ItemData.Primary.GetValueOrDefault(item.ItemId);
                this[EquipSlot.OffHand] = item;
                break;
            }
            default:
            {
                this[EquipSlot.MainHand] = default;
                this[EquipSlot.OffHand]  = default;
                foreach (var slot in EquipSlotExtensions.EqdpSlots)
                {
                    if (startSlot == slot)
                    {
                        this[slot] = item;
                        continue;
                    }

                    var slotItem = items.Identify(slot, item.PrimaryId, item.Variant);
                    if (!slotItem.Valid || slotItem.ItemId.Id is not 0 != item.ItemId.Id is not 0)
                    {
                        slotItem = items.Identify(EquipSlot.OffHand, item.PrimaryId, item.SecondaryId, 1, item.Type);
                        if (slotItem.ItemId.Id is not 0 != item.ItemId.Id is not 0)
                            slotItem = default;
                    }

                    this[slot] = slotItem;
                }

                break;
            }
        }

        Dragged = item;
    }
}
