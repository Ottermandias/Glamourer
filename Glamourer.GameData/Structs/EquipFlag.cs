using System;
using Penumbra.GameData.Enums;

namespace Glamourer.Structs;

[Flags]
public enum EquipFlag : ulong
{
    Head          = 0x00000001,
    Body          = 0x00000002,
    Hands         = 0x00000004,
    Legs          = 0x00000008,
    Feet          = 0x00000010,
    Ears          = 0x00000020,
    Neck          = 0x00000040,
    Wrist         = 0x00000080,
    RFinger       = 0x00000100,
    LFinger       = 0x00000200,
    Mainhand      = 0x00000400,
    Offhand       = 0x00000800,
    HeadStain     = 0x00001000,
    BodyStain     = 0x00002000,
    HandsStain    = 0x00004000,
    LegsStain     = 0x00008000,
    FeetStain     = 0x00010000,
    EarsStain     = 0x00020000,
    NeckStain     = 0x00040000,
    WristStain    = 0x00080000,
    RFingerStain  = 0x00100000,
    LFingerStain  = 0x00200000,
    MainhandStain = 0x00400000,
    OffhandStain  = 0x00800000,
    HeadCrest     = 0x01000000,
    BodyCrest     = 0x02000000,
    HandsCrest    = 0x04000000,
    LegsCrest     = 0x08000000,
    FeetCrest     = 0x10000000,
    EarsCrest     = 0x20000000,
    NeckCrest     = 0x40000000,
    WristCrest    = 0x80000000,
    RFingerCrest  = 0x100000000,
    LFingerCrest  = 0x200000000,
    MainhandCrest = 0x400000000,
    OffhandCrest  = 0x800000000,
}

public static class EquipFlagExtensions
{
    public const EquipFlag All           = (EquipFlag)(((ulong)EquipFlag.OffhandCrest << 1) - 1);
    public const EquipFlag AllRelevant   = All
        & ~EquipFlag.HandsCrest
        & ~EquipFlag.LegsCrest
        & ~EquipFlag.FeetCrest
        & ~EquipFlag.EarsCrest
        & ~EquipFlag.NeckCrest
        & ~EquipFlag.WristCrest
        & ~EquipFlag.RFingerCrest
        & ~EquipFlag.LFingerCrest
        & ~EquipFlag.MainhandCrest;
    public const int       NumEquipFlags = 36;

    public static EquipFlag ToFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.Mainhand,
            EquipSlot.OffHand  => EquipFlag.Offhand,
            EquipSlot.Head     => EquipFlag.Head,
            EquipSlot.Body     => EquipFlag.Body,
            EquipSlot.Hands    => EquipFlag.Hands,
            EquipSlot.Legs     => EquipFlag.Legs,
            EquipSlot.Feet     => EquipFlag.Feet,
            EquipSlot.Ears     => EquipFlag.Ears,
            EquipSlot.Neck     => EquipFlag.Neck,
            EquipSlot.Wrists   => EquipFlag.Wrist,
            EquipSlot.RFinger  => EquipFlag.RFinger,
            EquipSlot.LFinger  => EquipFlag.LFinger,
            _                  => 0,
        };

    public static EquipFlag ToStainFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.MainhandStain,
            EquipSlot.OffHand  => EquipFlag.OffhandStain,
            EquipSlot.Head     => EquipFlag.HeadStain,
            EquipSlot.Body     => EquipFlag.BodyStain,
            EquipSlot.Hands    => EquipFlag.HandsStain,
            EquipSlot.Legs     => EquipFlag.LegsStain,
            EquipSlot.Feet     => EquipFlag.FeetStain,
            EquipSlot.Ears     => EquipFlag.EarsStain,
            EquipSlot.Neck     => EquipFlag.NeckStain,
            EquipSlot.Wrists   => EquipFlag.WristStain,
            EquipSlot.RFinger  => EquipFlag.RFingerStain,
            EquipSlot.LFinger  => EquipFlag.LFingerStain,
            _                  => 0,
        };

    public static EquipFlag ToCrestFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.MainhandCrest,
            EquipSlot.OffHand  => EquipFlag.OffhandCrest,
            EquipSlot.Head     => EquipFlag.HeadCrest,
            EquipSlot.Body     => EquipFlag.BodyCrest,
            EquipSlot.Hands    => EquipFlag.HandsCrest,
            EquipSlot.Legs     => EquipFlag.LegsCrest,
            EquipSlot.Feet     => EquipFlag.FeetCrest,
            EquipSlot.Ears     => EquipFlag.EarsCrest,
            EquipSlot.Neck     => EquipFlag.NeckCrest,
            EquipSlot.Wrists   => EquipFlag.WristCrest,
            EquipSlot.RFinger  => EquipFlag.RFingerCrest,
            EquipSlot.LFinger  => EquipFlag.LFingerCrest,
            _                  => 0,
        };

    public static EquipFlag ToBothFlags(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.Mainhand | EquipFlag.MainhandStain | EquipFlag.MainhandCrest,
            EquipSlot.OffHand  => EquipFlag.Offhand | EquipFlag.OffhandStain | EquipFlag.OffhandCrest,
            EquipSlot.Head     => EquipFlag.Head | EquipFlag.HeadStain | EquipFlag.HeadCrest,
            EquipSlot.Body     => EquipFlag.Body | EquipFlag.BodyStain | EquipFlag.BodyCrest,
            EquipSlot.Hands    => EquipFlag.Hands | EquipFlag.HandsStain | EquipFlag.HandsCrest,
            EquipSlot.Legs     => EquipFlag.Legs | EquipFlag.LegsStain | EquipFlag.LegsCrest,
            EquipSlot.Feet     => EquipFlag.Feet | EquipFlag.FeetStain | EquipFlag.FeetCrest,
            EquipSlot.Ears     => EquipFlag.Ears | EquipFlag.EarsStain | EquipFlag.EarsCrest,
            EquipSlot.Neck     => EquipFlag.Neck | EquipFlag.NeckStain | EquipFlag.NeckCrest,
            EquipSlot.Wrists   => EquipFlag.Wrist | EquipFlag.WristStain | EquipFlag.WristCrest,
            EquipSlot.RFinger  => EquipFlag.RFinger | EquipFlag.RFingerStain | EquipFlag.RFingerCrest,
            EquipSlot.LFinger  => EquipFlag.LFinger | EquipFlag.LFingerStain | EquipFlag.LFingerCrest,
            _                  => 0,
        };
}
