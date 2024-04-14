namespace Glamourer.Api.Enums;

[Flags]
public enum ApplyFlag : ulong
{
    Once          = 0x01,
    Equipment     = 0x02,
    Customization = 0x04,
    Lock          = 0x08,
}

public static class ApplyFlagEx
{
    public const ApplyFlag DesignDefault = ApplyFlag.Once | ApplyFlag.Equipment | ApplyFlag.Customization;
    public const ApplyFlag StateDefault  = ApplyFlag.Equipment | ApplyFlag.Customization | ApplyFlag.Lock;
    public const ApplyFlag RevertDefault = ApplyFlag.Equipment | ApplyFlag.Customization;
}

public enum ApiEquipSlot : byte
{
    Unknown           = 0,
    MainHand          = 1,
    OffHand           = 2,
    Head              = 3,
    Body              = 4,
    Hands             = 5,
    Legs              = 7,
    Feet              = 8,
    Ears              = 9,
    Neck              = 10,
    Wrists            = 11,
    RFinger           = 12,
    LFinger           = 14, // Not officially existing, means "weapon could be equipped in either hand" for the game.
}