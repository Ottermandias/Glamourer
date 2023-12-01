using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Glamourer.Interop.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace Glamourer.Interop;

/// <summary>
/// Triggered when the crest visibility is updated on a model.
/// <list type="number">
///     <item>Parameter is the model with an update. </item>
///     <item>Parameter is the equipment slot changed. </item>
///     <item>Parameter is the whether the crest will be shown. </item>
/// </list>
/// </summary>
public sealed unsafe class CrestService : EventWrapper<Action<Model, EquipSlot, Ref<bool>>, CrestService.Priority>, IDisposable
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCrestVisibilityUpdating"/>
        StateListener = 0,
    }

    public CrestService(IGameInteropProvider interop)
        : base(nameof(CrestService))
    {
        _humanSetFreeCompanyCrestVisibleOnSlot =
            interop.HookFromAddress<SetCrestDelegateIntern>(_humanVTable[96], HumanSetFreeCompanyCrestVisibleOnSlotDetour);
        _weaponSetFreeCompanyCrestVisibleOnSlot =
            interop.HookFromAddress<SetCrestDelegateIntern>(_weaponVTable[96], WeaponSetFreeCompanyCrestVisibleOnSlotDetour);
        _humanSetFreeCompanyCrestVisibleOnSlot.Enable();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Enable();
    }

    protected override void Dispose(bool _)
    {
        _humanSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Dispose();
    }

    public void Invoke(Model model, EquipSlot slot, ref bool visible)
    {
        var ret = new Ref<bool>(visible);
        Invoke(this, model, slot, ret);
        visible = ret;
    }

    public void UpdateCrest(Model drawObject, EquipSlot slot, bool crest)
    {
        using var _ = _inUpdate.EnterMethod();
        drawObject.SetFreeCompanyCrestVisibleOnSlot(slot, crest);
    }

    private readonly InMethodChecker _inUpdate = new();

    private delegate void SetCrestDelegateIntern(nint drawObject, byte slot, byte visible);

    [Signature(global::Penumbra.GameData.Sigs.HumanVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _humanVTable = null!;

    [Signature(global::Penumbra.GameData.Sigs.WeaponVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _weaponVTable = null!;

    private readonly Hook<SetCrestDelegateIntern> _humanSetFreeCompanyCrestVisibleOnSlot;
    private readonly Hook<SetCrestDelegateIntern> _weaponSetFreeCompanyCrestVisibleOnSlot;

    private void HumanSetFreeCompanyCrestVisibleOnSlotDetour(nint drawObject, byte slotIdx, byte visible)
    {
        var slot     = ((uint)slotIdx).ToEquipSlot();
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        if (!inUpdate)
            Invoke(drawObject, slot, ref rVisible);
        Glamourer.Log.Excessive(
            $"[Human.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{drawObject:X} for slot {slot} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _humanSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }

    private void WeaponSetFreeCompanyCrestVisibleOnSlotDetour(nint drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        if (!inUpdate)
            Invoke(drawObject, EquipSlot.BothHand, ref rVisible);
        Glamourer.Log.Excessive(
            $"[Weapon.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{drawObject:X} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _weaponSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }
}
