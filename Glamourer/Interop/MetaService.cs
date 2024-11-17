using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

public unsafe class MetaService : IDisposable
{
    private readonly HeadGearVisibilityChanged _headGearEvent;
    private readonly WeaponVisibilityChanged   _weaponEvent;
    private readonly VisorStateChanged         _visorEvent;

    private delegate void HideHatGearDelegate(DrawDataContainer* drawData, uint id, byte value);
    private delegate void HideWeaponsDelegate(DrawDataContainer* drawData, byte value);

    private readonly Hook<HideHatGearDelegate> _hideHatGearHook;
    private readonly Hook<HideWeaponsDelegate> _hideWeaponsHook;
    private readonly Hook<HideWeaponsDelegate> _toggleVisorHook;

    public MetaService(WeaponVisibilityChanged weaponEvent, HeadGearVisibilityChanged headGearEvent, VisorStateChanged visorEvent,
        IGameInteropProvider interop)
    {
        _weaponEvent   = weaponEvent;
        _headGearEvent = headGearEvent;
        _visorEvent    = visorEvent;
        _hideHatGearHook =
            interop.HookFromAddress<HideHatGearDelegate>((nint)DrawDataContainer.MemberFunctionPointers.HideHeadgear, HideHatDetour);
        _hideWeaponsHook =
            interop.HookFromAddress<HideWeaponsDelegate>((nint)DrawDataContainer.MemberFunctionPointers.HideWeapons, HideWeaponsDetour);
        _toggleVisorHook =
            interop.HookFromAddress<HideWeaponsDelegate>((nint)DrawDataContainer.MemberFunctionPointers.SetVisor, ToggleVisorDetour);
        _hideHatGearHook.Enable();
        _hideWeaponsHook.Enable();
        _toggleVisorHook.Enable();
    }

    public void Dispose()
    {
        _hideHatGearHook.Dispose();
        _hideWeaponsHook.Dispose();
        _toggleVisorHook.Dispose();
    }

    public void SetHatState(Actor actor, bool value)
    {
        if (!actor.IsCharacter)
            return;

        // The function seems to not do anything if the head is 0, but also breaks for carbuncles turned human, sometimes?
        var old = actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id;
        if (old == 0 && actor.AsCharacter->ModelContainer.ModelCharaId == 0)
            actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id = 1;
        _hideHatGearHook.Original(&actor.AsCharacter->DrawData, 0, (byte)(value ? 0 : 1));
        actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id = old;
    }

    public void SetWeaponState(Actor actor, bool value)
    {
        if (!actor.IsCharacter)
            return;

        var old = actor.AsCharacter->DrawData.IsWeaponHidden;
        _hideWeaponsHook.Original(&actor.AsCharacter->DrawData, (byte)(value ? 0 : 1));
        actor.AsCharacter->DrawData.IsWeaponHidden = old;
    }

    private void HideHatDetour(DrawDataContainer* drawData, uint id, byte value)
    {
        if (id != 0)
        {
            _hideHatGearHook.Original(drawData, id, value);
            return;
        }

        Actor actor = drawData->OwnerObject;
        var   v     = value == 0;
        _headGearEvent.Invoke(actor, ref v);
        value = (byte)(v ? 0 : 1);
        Glamourer.Log.Verbose($"[MetaService] Hide Hat triggered with 0x{(nint)drawData:X} {id} {value} for {actor.Utf8Name}.");
        _hideHatGearHook.Original(drawData, id, value);
    }

    private void HideWeaponsDetour(DrawDataContainer* drawData, byte value)
    {
        Actor actor = drawData->OwnerObject;
        var   v     = value == 0;
        _weaponEvent.Invoke(actor, ref v);
        Glamourer.Log.Verbose($"[MetaService] Hide Weapon triggered with 0x{(nint)drawData:X} {value} for {actor.Utf8Name}.");
        _hideWeaponsHook.Original(drawData, (byte)(v ? 0 : 1));
    }

    private void ToggleVisorDetour(DrawDataContainer* drawData, byte value)
    {
        Actor actor = drawData->OwnerObject;
        var   v     = value != 0;
        _visorEvent.Invoke(actor.Model, true, ref v);
        Glamourer.Log.Verbose($"[MetaService] Toggle Visor triggered with 0x{(nint)drawData:X} {value} for {actor.Utf8Name}.");
        _toggleVisorHook.Original(drawData, (byte)(v ? 1 : 0));
    }
}
