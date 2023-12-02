using System;
using System.Collections.Generic;
using System.Linq;
using Penumbra.GameData.Enums;

namespace Glamourer.Structs;

[Flags]
public enum CrestFlag : ushort
{
    OffHand  = 0x0001,
    Head     = 0x0002,
    Body     = 0x0004,
    Hands    = 0x0008,
    Legs     = 0x0010,
    Feet     = 0x0020,
    Ears     = 0x0040,
    Neck     = 0x0080,
    Wrists   = 0x0100,
    RFinger  = 0x0200,
    LFinger  = 0x0400,
    MainHand = 0x0800,
}

public enum CrestType : byte
{
    None,
    Human,
    Mainhand,
    Offhand,
};

public static class CrestExtensions
{
    public const CrestFlag All         = (CrestFlag)(((ulong)EquipFlag.Mainhand << 1) - 1);
    public const CrestFlag AllRelevant = CrestFlag.Head | CrestFlag.Body | CrestFlag.OffHand;

    public static readonly IReadOnlyList<CrestFlag> AllRelevantSet = Enum.GetValues<CrestFlag>().Where(f => AllRelevant.HasFlag(f)).ToArray();

    public static int ToInternalIndex(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head    => 0,
            CrestFlag.Body    => 1,
            CrestFlag.OffHand => 2,
            _                 => -1,
        };

    public static (CrestType Type, byte Index) ToIndex(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head     => (CrestType.Human, 0),
            CrestFlag.Body     => (CrestType.Human, 1),
            CrestFlag.Hands    => (CrestType.Human, 2),
            CrestFlag.Legs     => (CrestType.Human, 3),
            CrestFlag.Feet     => (CrestType.Human, 4),
            CrestFlag.Ears     => (CrestType.None, 0),
            CrestFlag.Neck     => (CrestType.None, 0),
            CrestFlag.Wrists   => (CrestType.None, 0),
            CrestFlag.RFinger  => (CrestType.None, 0),
            CrestFlag.LFinger  => (CrestType.None, 0),
            CrestFlag.MainHand => (CrestType.None, 0),
            CrestFlag.OffHand  => (CrestType.Offhand, 0),
            _                  => (CrestType.None, 0),
        };

    public static CrestFlag ToCrestFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => CrestFlag.MainHand,
            EquipSlot.OffHand  => CrestFlag.OffHand,
            EquipSlot.Head     => CrestFlag.Head,
            EquipSlot.Body     => CrestFlag.Body,
            EquipSlot.Hands    => CrestFlag.Hands,
            EquipSlot.Legs     => CrestFlag.Legs,
            EquipSlot.Feet     => CrestFlag.Feet,
            EquipSlot.Ears     => CrestFlag.Ears,
            EquipSlot.Neck     => CrestFlag.Neck,
            EquipSlot.Wrists   => CrestFlag.Wrists,
            EquipSlot.RFinger  => CrestFlag.RFinger,
            EquipSlot.LFinger  => CrestFlag.LFinger,
            _                  => 0,
        };

    public static string ToLabel(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head     => "Head",
            CrestFlag.Body     => "Chest",
            CrestFlag.Hands    => "Gauntlets",
            CrestFlag.Legs     => "Pants",
            CrestFlag.Feet     => "Boot",
            CrestFlag.Ears     => "Earrings",
            CrestFlag.Neck     => "Necklace",
            CrestFlag.Wrists   => "Bracelet",
            CrestFlag.RFinger  => "Right Ring",
            CrestFlag.LFinger  => "Left Ring",
            CrestFlag.MainHand => "Weapon",
            CrestFlag.OffHand  => "Shield",
            _                  => string.Empty,
        };
}
