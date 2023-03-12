using System;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

[Flags]
public enum EquipFlag : uint
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
}

public static class EquipFlagExtensions
{
    public const EquipFlag All = (EquipFlag)(((uint)EquipFlag.OffhandStain << 1) - 1);

    public static EquipFlag ToFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand            => EquipFlag.Mainhand,
            EquipSlot.OffHand             => EquipFlag.Offhand,
            EquipSlot.Head                => EquipFlag.Head,
            EquipSlot.Body                => EquipFlag.Body,
            EquipSlot.Hands               => EquipFlag.Hands,
            EquipSlot.Legs                => EquipFlag.Legs,
            EquipSlot.Feet                => EquipFlag.Feet,
            EquipSlot.Ears                => EquipFlag.Ears,
            EquipSlot.Neck                => EquipFlag.Neck,
            EquipSlot.Wrists              => EquipFlag.Wrist,
            EquipSlot.RFinger             => EquipFlag.RFinger,
            EquipSlot.LFinger             => EquipFlag.LFinger,
            _                             => 0,
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
}
