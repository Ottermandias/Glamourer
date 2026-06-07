using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

public sealed unsafe class MetaService : IDisposable, IRequiredService
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

        var old       = actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id;
        var oldHidden = actor.AsCharacter->DrawData.IsHatHidden;
        if (actor.AsCharacter->ModelContainer.ModelCharaId is 0)
        {
            // The function seems to not do anything if the head is 0, but also breaks for carbuncles turned human, sometimes?
            if (old is 0)
                actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id = 1;

            // It also seems to not do anything if the value is the same as before.
            if (oldHidden != value)
                actor.AsCharacter->DrawData.IsHatHidden = value;
        }

        _hideHatGearHook.Original(&actor.AsCharacter->DrawData, 0, (byte)(value ? 0 : 1));

        // Restore game object data.
        actor.AsCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Id = old;
        actor.AsCharacter->DrawData.IsHatHidden                                        = oldHidden;
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
        var   v     = value is 0;
        _headGearEvent.Invoke(new HeadGearVisibilityChanged.Arguments(actor, ref v));
        value = (byte)(v ? 0 : 1);
        Glamourer.Log.Verbose($"[MetaService] Hide Hat triggered with 0x{(nint)drawData:X} {id} {value} for {actor.Utf8Name}.");
        _hideHatGearHook.Original(drawData, id, value);
    }

    private void HideWeaponsDetour(DrawDataContainer* drawData, byte value)
    {
        Actor actor = drawData->OwnerObject;
        var   v     = value is 0;
        _weaponEvent.Invoke(new WeaponVisibilityChanged.Arguments(actor, ref v));
        Glamourer.Log.Verbose($"[MetaService] Hide Weapon triggered with 0x{(nint)drawData:X} {value} for {actor.Utf8Name}.");
        _hideWeaponsHook.Original(drawData, (byte)(v ? 0 : 1));
    }

    private void ToggleVisorDetour(DrawDataContainer* drawData, byte value)
    {
        Actor actor = drawData->OwnerObject;
        var   v     = value is not 0;
        _visorEvent.Invoke(new VisorStateChanged.Arguments(actor.Model, true, ref v));
        Glamourer.Log.Verbose($"[MetaService] Toggle Visor triggered with 0x{(nint)drawData:X} {value} for {actor.Utf8Name}.");
        _toggleVisorHook.Original(drawData, (byte)(v ? 1 : 0));
    }
}
