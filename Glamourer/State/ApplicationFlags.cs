using Penumbra.GameData.Enums;
using System;

namespace Glamourer.State;

[Flags]
public enum ApplicationFlags : uint
{
    Customizations = 0x000001,
    MainHand       = 0x000002,
    OffHand        = 0x000004,
    Head           = 0x000008,
    Body           = 0x000010,
    Hands          = 0x000020,
    Legs           = 0x000040,
    Feet           = 0x000080,
    Ears           = 0x000100,
    Neck           = 0x000200,
    Wrist          = 0x000400,
    RFinger        = 0x000800,
    LFinger        = 0x001000,
    SetVisor       = 0x002000,
    Visor          = 0x004000,
    SetWeapon      = 0x008000,
    Weapon         = 0x010000,
    SetWet         = 0x020000,
    Wet            = 0x040000,
}

public static class ApplicationFlagExtensions
{
    public static ApplicationFlags ToApplicationFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand          => ApplicationFlags.MainHand,
            EquipSlot.OffHand           => ApplicationFlags.OffHand,
            EquipSlot.Head              => ApplicationFlags.Head,
            EquipSlot.Body              => ApplicationFlags.Body,
            EquipSlot.Hands             => ApplicationFlags.Hands,
            EquipSlot.Legs              => ApplicationFlags.Legs,
            EquipSlot.Feet              => ApplicationFlags.Feet,
            EquipSlot.Ears              => ApplicationFlags.Ears,
            EquipSlot.Neck              => ApplicationFlags.Neck,
            EquipSlot.Wrists            => ApplicationFlags.Wrist,
            EquipSlot.RFinger           => ApplicationFlags.RFinger,
            EquipSlot.BothHand          => ApplicationFlags.MainHand | ApplicationFlags.OffHand,
            EquipSlot.LFinger           => ApplicationFlags.LFinger,
            EquipSlot.HeadBody          => ApplicationFlags.Body,
            EquipSlot.BodyHandsLegsFeet => ApplicationFlags.Body,
            EquipSlot.LegsFeet          => ApplicationFlags.Legs,
            EquipSlot.FullBody          => ApplicationFlags.Body,
            EquipSlot.BodyHands         => ApplicationFlags.Body,
            EquipSlot.BodyLegsFeet      => ApplicationFlags.Body,
            EquipSlot.ChestHands        => ApplicationFlags.Body,
            _                           => 0,
        };

    public static EquipSlot ToSlot(this ApplicationFlags flags)
        => flags switch
        {
            ApplicationFlags.MainHand => EquipSlot.MainHand,
            ApplicationFlags.OffHand  => EquipSlot.OffHand,
            ApplicationFlags.Head     => EquipSlot.Head,
            ApplicationFlags.Body     => EquipSlot.Body,
            ApplicationFlags.Hands    => EquipSlot.Hands,
            ApplicationFlags.Legs     => EquipSlot.Legs,
            ApplicationFlags.Feet     => EquipSlot.Feet,
            ApplicationFlags.Ears     => EquipSlot.Ears,
            ApplicationFlags.Neck     => EquipSlot.Neck,
            ApplicationFlags.Wrist    => EquipSlot.Wrists,
            ApplicationFlags.RFinger  => EquipSlot.RFinger,
            ApplicationFlags.LFinger  => EquipSlot.LFinger,
            _                         => EquipSlot.Unknown,
        };
}
