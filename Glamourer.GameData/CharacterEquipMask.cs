using System;

namespace Glamourer
{
    [Flags]
    public enum CharacterEquipMask : ushort
    {
        None     = 0,
        MainHand = 0b000000000001,
        OffHand  = 0b000000000010,
        Head     = 0b000000000100,
        Body     = 0b000000001000,
        Hands    = 0b000000010000,
        Legs     = 0b000000100000,
        Feet     = 0b000001000000,
        Ears     = 0b000010000000,
        Neck     = 0b000100000000,
        Wrists   = 0b001000000000,
        RFinger  = 0b010000000000,
        LFinger  = 0b100000000000,
        All      = 0b111111111111,
    }
}