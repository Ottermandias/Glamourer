using System;
using System.Diagnostics.CodeAnalysis;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
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
    Wrist    = 0x0080,
    RFinger  = 0x0100,
    LFinger  = 0x0200,
    Mainhand = 0x0400,
    Offhand  = 0x0800,
}

public static class CrestExtensions
{
    public const CrestFlag All         = (CrestFlag)(((ulong)EquipFlag.Offhand << 1) - 1);
    public const CrestFlag AllRelevant = CrestFlag.Body;

    public static CrestFlag ToCrestFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => CrestFlag.Mainhand,
            EquipSlot.OffHand  => CrestFlag.Offhand,
            EquipSlot.Head     => CrestFlag.Head,
            EquipSlot.Body     => CrestFlag.Body,
            EquipSlot.Hands    => CrestFlag.Hands,
            EquipSlot.Legs     => CrestFlag.Legs,
            EquipSlot.Feet     => CrestFlag.Feet,
            EquipSlot.Ears     => CrestFlag.Ears,
            EquipSlot.Neck     => CrestFlag.Neck,
            EquipSlot.Wrists   => CrestFlag.Wrist,
            EquipSlot.RFinger  => CrestFlag.RFinger,
            EquipSlot.LFinger  => CrestFlag.LFinger,
            _                  => 0,
        };

    public static bool Valid(this CrestFlag crest)
        => AllRelevant.HasFlag(crest);
}
