using Dalamud.Game.ClientState.Objects.Types;
using Glamourer.Interop;

namespace Glamourer.Util;

public static class CharacterExtensions
{
    public static unsafe bool IsWet(this Character a)
        => ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*) a.Address)->IsGPoseWet;

    public static unsafe bool SetWetness(this Character a, bool value)
    {
        var current = a.IsWet();
        if (current == value)
            return false;

        ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->IsGPoseWet = value;
        return true;
    }

    public static unsafe bool IsHatVisible(this Character a)
        => !((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsHatHidden;

    public static unsafe bool SetHatVisible(this Character a, bool visible)
    {
        var current = a.IsHatVisible();
        if (current == visible)
            return false;

        ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsHatHidden = !visible;
        return true;
    }

    public static unsafe bool IsVisorToggled(this Character a)
        => ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsVisorToggled;

    public static unsafe bool SetVisorToggled(this Character a, bool toggled)
    {
        var current = a.IsVisorToggled();
        if (current == toggled)
            return false;

        ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsVisorToggled = toggled;
        return true;
    }

    public static unsafe bool IsWeaponHidden(this Character a)
        => ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsWeaponHidden 
        && ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData.IsMainHandHidden;

    public static unsafe bool SetWeaponHidden(this Character a, bool value)
    {
        var hidden = a.IsWeaponHidden();
        if (hidden == value)
            return false;

        var drawData = &((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->DrawData;


        drawData->IsWeaponHidden = value; 
        drawData->IsMainHandHidden = value;
        // TODO
        if (value)
            *(&drawData->MainHandState + ((long) &drawData->OffHandModel - (long) &drawData->MainHandModel)) |= 0x02;
        else
            *(&drawData->MainHandState + ((long) &drawData->OffHandModel - (long) &drawData->MainHandModel)) &= 0xFD;
        return true;
    }

    public static unsafe ref float Alpha(this Character a)
        => ref ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)a.Address)->Alpha;
}
