using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Penumbra.GameData.Enums;

namespace Glamourer.Structs;

[Flags]
public enum CrestFlag : ushort
{
    Head     = 0x0001,
    Body     = 0x0002,
    Hands    = 0x0004,
    Legs     = 0x0008,
    Feet     = 0x0010,
    Ears     = 0x0020,
    Neck     = 0x0040,
    Wrists   = 0x0080,
    RFinger  = 0x0100,
    LFinger  = 0x0200,
    MainHand = 0x0400,
    OffHand  = 0x0800,
}

public static class CrestExtensions
{
    public const CrestFlag All         = (CrestFlag)(((ulong)EquipFlag.Offhand << 1) - 1);
    public const CrestFlag AllRelevant = CrestFlag.Head | CrestFlag.Body | CrestFlag.OffHand;

    public static readonly IReadOnlyList<CrestFlag> AllRelevantSet = Enum.GetValues<CrestFlag>().Where(f => f.ToRelevantIndex() >= 0).ToArray();

    public static int ToIndex(this CrestFlag flag)
        => BitOperations.TrailingZeroCount((uint)flag);

    public static int ToRelevantIndex(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head    => 0,
            CrestFlag.Body    => 1,
            CrestFlag.OffHand => 2,
            _                 => -1,
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

    public static EquipSlot ToSlot(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.MainHand => EquipSlot.MainHand,
            CrestFlag.OffHand  => EquipSlot.OffHand,
            CrestFlag.Head     => EquipSlot.Head,
            CrestFlag.Body     => EquipSlot.Body,
            CrestFlag.Hands    => EquipSlot.Hands,
            CrestFlag.Legs     => EquipSlot.Legs,
            CrestFlag.Feet     => EquipSlot.Feet,
            CrestFlag.Ears     => EquipSlot.Ears,
            CrestFlag.Neck     => EquipSlot.Neck,
            CrestFlag.Wrists   => EquipSlot.Wrists,
            CrestFlag.RFinger  => EquipSlot.RFinger,
            CrestFlag.LFinger  => EquipSlot.LFinger,
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
