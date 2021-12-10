using Dalamud.Game.ClientState.Objects.Types;

namespace Glamourer;

public static class CharacterExtensions
{
    public const int  WetnessOffset      = 0x19E4;
    public const byte WetnessFlag        = 0x08;
    public const int  StateFlagsOffset   = 0xDF6;
    public const byte HatHiddenFlag      = 0x01;
    public const byte VisorToggledFlag   = 0x10;
    public const int  AlphaOffset        = 0x18B8;
    public const int  WeaponHiddenOffset = 0xCD4;
    public const byte WeaponHiddenFlag   = 0x02;

    public static unsafe bool IsWet(this Character a)
        => (*((byte*)a.Address + WetnessOffset) & WetnessFlag) != 0;

    public static unsafe bool SetWetness(this Character a, bool value)
    {
        var current = a.IsWet();
        if (current == value)
            return false;

        if (value)
            *((byte*)a.Address + WetnessOffset) = (byte)(*((byte*)a.Address + WetnessOffset) | WetnessFlag);
        else
            *((byte*)a.Address + WetnessOffset) = (byte)(*((byte*)a.Address + WetnessOffset) & ~WetnessFlag);
        return true;
    }

    public static unsafe ref byte StateFlags(this Character a)
        => ref *((byte*)a.Address + StateFlagsOffset);

    public static bool SetStateFlag(this Character a, bool value, byte flag)
    {
        var current       = a.StateFlags();
        var previousValue = (current & flag) != 0;
        if (previousValue == value)
            return false;

        if (value)
            a.StateFlags() = (byte)(current | flag);
        else
            a.StateFlags() = (byte)(current & ~flag);
        return true;
    }

    public static bool IsHatHidden(this Character a)
        => (a.StateFlags() & HatHiddenFlag) != 0;

    public static unsafe bool IsWeaponHidden(this Character a)
        => (a.StateFlags() & WeaponHiddenFlag) != 0
         && (*((byte*)a.Address + WeaponHiddenOffset) & WeaponHiddenFlag) != 0;

    public static bool IsVisorToggled(this Character a)
        => (a.StateFlags() & VisorToggledFlag) != 0;

    public static bool SetHatHidden(this Character a, bool value)
        => SetStateFlag(a, value, HatHiddenFlag);

    public static unsafe bool SetWeaponHidden(this Character a, bool value)
    {
        var ret = SetStateFlag(a, value, WeaponHiddenFlag);
        var val = *((byte*)a.Address + WeaponHiddenOffset);
        if (value)
            *((byte*)a.Address + WeaponHiddenOffset) = (byte)(val | WeaponHiddenFlag);
        else
            *((byte*)a.Address + WeaponHiddenOffset) = (byte)(val & ~WeaponHiddenFlag);
        return ret || (val & WeaponHiddenFlag) != 0 != value;
    }

    public static bool SetVisorToggled(this Character a, bool value)
        => SetStateFlag(a, value, VisorToggledFlag);

    public static unsafe ref float Alpha(this Character a)
        => ref *(float*)((byte*)a.Address + AlphaOffset);
}
