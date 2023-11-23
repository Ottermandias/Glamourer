using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using PenumbraSigs = Penumbra.GameData.Sigs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public  readonly SlotUpdating            SlotUpdatingEvent;
    public  readonly CrestVisibilityUpdating CrestVisibilityUpdatingEvent;
    private readonly ThreadLocal<uint>       _crestVisibilityUpdate = new(() => 0, false);

    public UpdateSlotService(SlotUpdating slotUpdating, CrestVisibilityUpdating crestVisibilityUpdating, IGameInteropProvider interop)
    {
        SlotUpdatingEvent            = slotUpdating;
        CrestVisibilityUpdatingEvent = crestVisibilityUpdating;
        interop.InitializeFromAttributes(this);
        _humanSetFreeCompanyCrestVisibleOnSlot  = interop.HookFromAddress<SetCrestDelegateIntern>(_humanVTable[96],  HumanSetFreeCompanyCrestVisibleOnSlotDetour);
        _weaponSetFreeCompanyCrestVisibleOnSlot = interop.HookFromAddress<SetCrestDelegateIntern>(_weaponVTable[96], WeaponSetFreeCompanyCrestVisibleOnSlotDetour);
        _flagSlotForUpdateHook.Enable();
        _humanSetFreeCompanyCrestVisibleOnSlot.Enable();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _humanSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Dispose();
    }

    public void UpdateSlot(Model drawObject, EquipSlot slot, CharacterArmor data, bool? crest)
    {
        if (!drawObject.IsCharacterBase)
            return;

        FlagSlotForUpdateInterop(drawObject, slot, data);
        if (crest.HasValue)
        {
            using var _ = EnterCrestVisibilityUpdate();
            drawObject.SetFreeCompanyCrestVisibleOnSlot((byte)slot.ToIndex(), crest.Value);
        }
    }

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor, StainId stain, bool? crest)
        => UpdateSlot(drawObject, slot, armor.With(stain), crest);

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => UpdateArmor(drawObject, slot, armor, drawObject.GetArmor(slot).Stain, null);

    public void UpdateStain(Model drawObject, EquipSlot slot, StainId stain)
        => UpdateArmor(drawObject, slot, drawObject.GetArmor(slot), stain, null);

    public void UpdateCrest(Model drawObject, EquipSlot slot, bool crest)
    {
        using var _ = EnterCrestVisibilityUpdate();
        drawObject.SetFreeCompanyCrestVisibleOnSlot((byte)slot.ToIndex(), crest);
    }

    private delegate ulong FlagSlotForUpdateDelegateIntern(nint drawObject, uint slot, CharacterArmor* data);
    private delegate void SetCrestDelegateIntern(nint drawObject, byte slot, byte visible);

    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagSlotForUpdateHook = null!;

    [Signature(PenumbraSigs.HumanVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _humanVTable = null!;

    [Signature(PenumbraSigs.WeaponVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _weaponVTable = null!;

    private readonly Hook<SetCrestDelegateIntern> _humanSetFreeCompanyCrestVisibleOnSlot;
    private readonly Hook<SetCrestDelegateIntern> _weaponSetFreeCompanyCrestVisibleOnSlot;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        SlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        return returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private void HumanSetFreeCompanyCrestVisibleOnSlotDetour(nint drawObject, byte slotIdx, byte visible)
    {
        var slot     = ((uint)slotIdx).ToEquipSlot();
        var rVisible = visible != 0;
        var inUpdate = _crestVisibilityUpdate.IsValueCreated && _crestVisibilityUpdate.Value > 0;
        if (!inUpdate)
            CrestVisibilityUpdatingEvent.Invoke(drawObject, slot, ref rVisible);
        Glamourer.Log.Excessive($"[Human.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{drawObject:X} for slot {slot} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _humanSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }

    private void WeaponSetFreeCompanyCrestVisibleOnSlotDetour(nint drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _crestVisibilityUpdate.IsValueCreated && _crestVisibilityUpdate.Value > 0;
        if (!inUpdate)
            CrestVisibilityUpdatingEvent.Invoke(drawObject, EquipSlot.BothHand, ref rVisible);
        Glamourer.Log.Excessive($"[Weapon.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{drawObject:X} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _weaponSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);

    /// <summary>
    /// Temporarily disables the crest update hooks on the current thread.
    /// </summary>
    /// <returns> A struct that will undo this operation when disposed. Best used with: <code>using (var _ = updateSlotService.EnterCrestVisibilityUpdate()) { ... }</code> </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CrestVisibilityUpdateRaii EnterCrestVisibilityUpdate()
        => new(this);

    public readonly ref struct CrestVisibilityUpdateRaii
    {
        private readonly ThreadLocal<uint> _crestVisibilityUpdate;

        public CrestVisibilityUpdateRaii(UpdateSlotService parent)
        {
            _crestVisibilityUpdate = parent._crestVisibilityUpdate;
            ++_crestVisibilityUpdate.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly void Dispose()
        {
            --_crestVisibilityUpdate.Value;
        }
    }
}
