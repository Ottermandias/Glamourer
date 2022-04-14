using Dalamud.Game.ClientState.Objects.Types;

namespace Glamourer;

public static class CharacterExtensions
{
    public const int  WetnessOffset       = 0x1ADA;
    public const byte WetnessFlag         = 0x80;
    public const int  HatVisibleOffset    = 0x84E;
    public const int  VisorToggledOffset  = 0x84F;
    public const byte HatHiddenFlag       = 0x01;
    public const byte VisorToggledFlag    = 0x08;
    public const int  AlphaOffset         = 0x19E0;
    public const int  WeaponHiddenOffset1 = 0x84F;
    public const int  WeaponHiddenOffset2 = 0x72C; // maybe
    public const byte WeaponHiddenFlag1   = 0x01;
    public const byte WeaponHiddenFlag2   = 0x02;

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

    public static unsafe bool IsHatVisible(this Character a)
        => (*((byte*)a.Address + HatVisibleOffset) & HatHiddenFlag) == 0;

    public static unsafe bool SetHatVisible(this Character a, bool visible)
    {
        var current = IsHatVisible(a);
        if (current == visible)
            return false;

        if (visible)
            *((byte*)a.Address + HatVisibleOffset) = (byte)(*((byte*)a.Address + HatVisibleOffset) & ~HatHiddenFlag);
        else
            *((byte*)a.Address + HatVisibleOffset) = (byte)(*((byte*)a.Address + HatVisibleOffset) | HatHiddenFlag);
        return true;
    }

    public static unsafe bool IsVisorToggled(this Character a)
        => (*((byte*)a.Address + VisorToggledOffset) & VisorToggledFlag) == VisorToggledFlag;

    public static unsafe bool SetVisorToggled(this Character a, bool toggled)
    {
        var current = IsVisorToggled(a);
        if (current == toggled)
            return false;

        if (toggled)
            *((byte*)a.Address + VisorToggledOffset) = (byte)(*((byte*)a.Address + VisorToggledOffset) | VisorToggledFlag);
        else
            *((byte*)a.Address + VisorToggledOffset) = (byte)(*((byte*)a.Address + VisorToggledOffset) & ~VisorToggledFlag);
        return true;
    }

    public static unsafe bool IsWeaponHidden(this Character a)
        => (*((byte*)a.Address + WeaponHiddenOffset1) & WeaponHiddenFlag1) == WeaponHiddenFlag1
         && (*((byte*)a.Address + WeaponHiddenOffset2) & WeaponHiddenFlag2) == WeaponHiddenFlag2;

    public static unsafe bool SetWeaponHidden(this Character a, bool value)
    {
        var hidden = IsWeaponHidden(a);
        if (hidden == value)
            return false;

        var val1 = *((byte*)a.Address + WeaponHiddenOffset1);
        var val2 = *((byte*)a.Address + WeaponHiddenOffset2);
        if (value)
        {
            *((byte*)a.Address + WeaponHiddenOffset1) = (byte)(val1 | WeaponHiddenFlag1);
            *((byte*)a.Address + WeaponHiddenOffset2) = (byte)(val2 | WeaponHiddenFlag2);
        }
        else
        {
            *((byte*)a.Address + WeaponHiddenOffset1) = (byte)(val1 & ~WeaponHiddenFlag1);
            *((byte*)a.Address + WeaponHiddenOffset2) = (byte)(val2 & ~WeaponHiddenFlag2);
        }
        return true;
    }

    public static unsafe ref float Alpha(this Character a)
        => ref *(float*)((byte*)a.Address + AlphaOffset);
}
