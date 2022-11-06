using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Saves;

public partial class Design
{
    private unsafe struct EquipmentData
    {
        public const int  NumEquipment = 12;
        public fixed uint Ids[NumEquipment];
        public fixed byte Stains[NumEquipment];
    }

    private EquipmentData _equipmentData = default;
    public  ushort        EquipmentFlags { get; private set; }
    public  ushort        StainFlags     { get; private set; }

    // @formatter:off
    public Slot Head     => new(this, 0);
    public Slot Body     => new(this, 1);
    public Slot Hands    => new(this, 2);
    public Slot Legs     => new(this, 3);
    public Slot Feet     => new(this, 4);
    public Slot Ears     => new(this, 5);
    public Slot Neck     => new(this, 6);
    public Slot Wrists   => new(this, 7);
    public Slot RFinger  => new(this, 8);
    public Slot LFinger  => new(this, 9);
    public Slot MainHand => new(this, 10);
    public Slot OffHand  => new(this, 11);
    // @formatter:on

    public Slot this[EquipSlot slot]
        => new(this, (int)slot.ToIndex());


    public static readonly string[] SlotName =
    {
        EquipSlot.Head.ToName(),
        EquipSlot.Body.ToName(),
        EquipSlot.Hands.ToName(),
        EquipSlot.Legs.ToName(),
        EquipSlot.Feet.ToName(),
        EquipSlot.Ears.ToName(),
        EquipSlot.Neck.ToName(),
        EquipSlot.Wrists.ToName(),
        EquipSlot.RFinger.ToName(),
        EquipSlot.LFinger.ToName(),
        EquipSlot.MainHand.ToName(),
        EquipSlot.OffHand.ToName(),
    };


    public readonly unsafe struct Slot
    {
        private readonly Design _data;
        public readonly  int    Index;
        public readonly  ushort Flag;

        public Slot(Design design, int idx)
        {
            _data = design;
            Index = idx;
            Flag  = (ushort)(1 << idx);
        }

        public uint ItemId
        {
            get => _data._equipmentData.Ids[Index];
            set => _data._equipmentData.Ids[Index] = value;
        }

        public StainId StainId
        {
            get => _data._equipmentData.Stains[Index];
            set => _data._equipmentData.Stains[Index] = value.Value;
        }

        public bool ApplyItem
        {
            get => (_data.EquipmentFlags & Flag) != 0;
            set => _data.EquipmentFlags = (ushort)(value ? _data.EquipmentFlags | Flag : _data.EquipmentFlags & ~Flag);
        }

        public bool ApplyStain
        {
            get => (_data.StainFlags & Flag) != 0;
            set => _data.StainFlags = (ushort)(value ? _data.StainFlags | Flag : _data.StainFlags & ~Flag);
        }
    }

    public IEnumerable<Slot> Equipment
        => Enumerable.Range(0, EquipmentData.NumEquipment).Select(i => new Slot(this, i));

    private void WriteEquipment(JObject obj)
    {
        var tok = new JObject();
        foreach (var slot in Equipment)
        {
            tok[SlotName] = new JObject
            {
                [nameof(Slot.ItemId)]     = slot.ItemId,
                [nameof(Slot.ApplyItem)]  = slot.ApplyItem,
                [nameof(Slot.StainId)]    = slot.StainId.Value,
                [nameof(Slot.ApplyStain)] = slot.ApplyStain,
            };
        }

        obj[nameof(Equipment)] = tok;
    }
}
