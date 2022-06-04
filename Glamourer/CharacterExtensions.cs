using Dalamud.Game.ClientState.Objects.Types;

namespace Glamourer;

public static class CharacterExtensions
{
    public static unsafe bool IsWet(this Character a)
        => (*((byte*)a.Address + Offsets.Character.Wetness) & Offsets.Character.Flags.IsWet) != 0;

    public static unsafe bool SetWetness(this Character a, bool value)
    {
        var current = a.IsWet();
        if (current == value)
            return false;

        if (value)
            *((byte*)a.Address + Offsets.Character.Wetness) =
                (byte)(*((byte*)a.Address + Offsets.Character.Wetness) | Offsets.Character.Flags.IsWet);
        else
            *((byte*)a.Address + Offsets.Character.Wetness) =
                (byte)(*((byte*)a.Address + Offsets.Character.Wetness) & ~Offsets.Character.Flags.IsWet);
        return true;
    }

    public static unsafe bool IsHatVisible(this Character a)
        => (*((byte*)a.Address + Offsets.Character.HatVisible) & Offsets.Character.Flags.IsHatHidden) == 0;

    public static unsafe bool SetHatVisible(this Character a, bool visible)
    {
        var current = IsHatVisible(a);
        if (current == visible)
            return false;

        if (visible)
            *((byte*)a.Address + Offsets.Character.HatVisible) =
                (byte)(*((byte*)a.Address + Offsets.Character.HatVisible) & ~Offsets.Character.Flags.IsHatHidden);
        else
            *((byte*)a.Address + Offsets.Character.HatVisible) =
                (byte)(*((byte*)a.Address + Offsets.Character.HatVisible) | Offsets.Character.Flags.IsHatHidden);
        return true;
    }

    public static unsafe bool IsVisorToggled(this Character a)
        => (*((byte*)a.Address + Offsets.Character.VisorToggled) & Offsets.Character.Flags.IsVisorToggled)
         == Offsets.Character.Flags.IsVisorToggled;

    public static unsafe bool SetVisorToggled(this Character a, bool toggled)
    {
        var current = IsVisorToggled(a);
        if (current == toggled)
            return false;

        if (toggled)
            *((byte*)a.Address + Offsets.Character.VisorToggled) =
                (byte)(*((byte*)a.Address + Offsets.Character.VisorToggled) | Offsets.Character.Flags.IsVisorToggled);
        else
            *((byte*)a.Address + Offsets.Character.VisorToggled) =
                (byte)(*((byte*)a.Address + Offsets.Character.VisorToggled) & ~Offsets.Character.Flags.IsVisorToggled);
        return true;
    }

    public static unsafe bool IsWeaponHidden(this Character a)
        => (*((byte*)a.Address + Offsets.Character.WeaponHidden1) & Offsets.Character.Flags.IsWeaponHidden1)
         == Offsets.Character.Flags.IsWeaponHidden1
         && (*((byte*)a.Address + Offsets.Character.WeaponHidden2) & Offsets.Character.Flags.IsWeaponHidden2)
         == Offsets.Character.Flags.IsWeaponHidden2;

    public static unsafe bool SetWeaponHidden(this Character a, bool value)
    {
        var hidden = IsWeaponHidden(a);
        if (hidden == value)
            return false;

        var val1 = *((byte*)a.Address + Offsets.Character.WeaponHidden1);
        var val2 = *((byte*)a.Address + Offsets.Character.WeaponHidden2);
        if (value)
        {
            *((byte*)a.Address + Offsets.Character.WeaponHidden1) = (byte)(val1 | Offsets.Character.Flags.IsWeaponHidden1);
            *((byte*)a.Address + Offsets.Character.WeaponHidden2) = (byte)(val2 | Offsets.Character.Flags.IsWeaponHidden2);
        }
        else
        {
            *((byte*)a.Address + Offsets.Character.WeaponHidden1) = (byte)(val1 & ~Offsets.Character.Flags.IsWeaponHidden1);
            *((byte*)a.Address + Offsets.Character.WeaponHidden2) = (byte)(val2 & ~Offsets.Character.Flags.IsWeaponHidden2);
        }

        return true;
    }

    public static unsafe ref float Alpha(this Character a)
        => ref *(float*)((byte*)a.Address + Offsets.Character.Alpha);
}
