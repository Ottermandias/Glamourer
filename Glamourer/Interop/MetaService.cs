using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Glamourer.Interop.Structs;

namespace Glamourer.Interop;

public unsafe class MetaService : IDisposable
{
    private readonly HeadGearVisibilityChanged _headGearEvent;
    private readonly WeaponVisibilityChanged   _weaponEvent;

    private delegate void HideHatGearDelegate(DrawDataContainer* drawData, uint id, byte value);
    private delegate void HideWeaponsDelegate(DrawDataContainer* drawData, bool value);

    private readonly Hook<HideHatGearDelegate> _hideHatGearHook;
    private readonly Hook<HideWeaponsDelegate> _hideWeaponsHook;

    public MetaService(WeaponVisibilityChanged weaponEvent, HeadGearVisibilityChanged headGearEvent)
    {
        _weaponEvent     = weaponEvent;
        _headGearEvent   = headGearEvent;
        _hideHatGearHook = Hook<HideHatGearDelegate>.FromAddress((nint)DrawDataContainer.MemberFunctionPointers.HideHeadgear, HideHatDetour);
        _hideWeaponsHook = Hook<HideWeaponsDelegate>.FromAddress((nint)DrawDataContainer.MemberFunctionPointers.HideWeapons, HideWeaponsDetour);
        _hideHatGearHook.Enable();
        _hideWeaponsHook.Enable();
    }

    public void Dispose()
    {
        _hideHatGearHook.Dispose();
        _hideWeaponsHook.Dispose();
    }

    public void SetHatState(Actor actor, bool value)
    {
        if (!actor.IsCharacter)
            return;

        // The function seems to not do anything if the head is 0, sometimes?
        var old = actor.AsCharacter->DrawData.Head.Id;
        if (old == 0)
            actor.AsCharacter->DrawData.Head.Id = 1;
        _hideHatGearHook.Original(&actor.AsCharacter->DrawData, 0, (byte)(value ? 0 : 1));
        actor.AsCharacter->DrawData.Head.Id = old;
    }

    public void SetWeaponState(Actor actor, bool value)
    {
        if (!actor.IsCharacter)
            return;

        _hideWeaponsHook.Original(&actor.AsCharacter->DrawData, !value);
    }

    private void HideHatDetour(DrawDataContainer* drawData, uint id, byte value)
    {
        Actor actor = drawData->Parent;
        var   v     = value == 0;
        _headGearEvent.Invoke(actor, ref v);
        value = (byte)(v ? 0 : 1);
        Glamourer.Log.Verbose($"[MetaService] Hide Hat triggered with 0x{(nint)drawData:X} {id} {value} for {actor.Utf8Name}.");
        _hideHatGearHook.Original(drawData, id, value);
    }

    private void HideWeaponsDetour(DrawDataContainer* drawData, bool value)
    {
        Actor actor = drawData->Parent;
        value = !value;
        _weaponEvent.Invoke(actor, ref value);
        value = !value;
        Glamourer.Log.Verbose($"[MetaService] Hide Weapon triggered with 0x{(nint)drawData:X} {value} for {actor.Utf8Name}.");
        _hideWeaponsHook.Original(drawData, value);
    }
}
