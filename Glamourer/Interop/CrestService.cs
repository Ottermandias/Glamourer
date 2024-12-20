﻿using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using OtterGui.Classes;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

/// <summary>
/// Triggered when the crest visibility is updated on a model.
/// <list type="number">
///     <item>Parameter is the model with an update. </item>
///     <item>Parameter is the equipment slot changed. </item>
///     <item>Parameter is whether the crest will be shown. </item>
/// </list>
/// </summary>
public sealed unsafe class CrestService : EventWrapperRef3<Actor, CrestFlag, bool, CrestService.Priority>
{
    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCrestChange"/>
        StateListener = 0,
    }

    public CrestService(IGameInteropProvider interop)
        : base(nameof(CrestService))
    {
        interop.InitializeFromAttributes(this);
        _humanSetFreeCompanyCrestVisibleOnSlot =
            interop.HookFromAddress<SetCrestDelegateIntern>(_humanVTable[109], HumanSetFreeCompanyCrestVisibleOnSlotDetour);
        _weaponSetFreeCompanyCrestVisibleOnSlot =
            interop.HookFromAddress<SetCrestDelegateIntern>(_weaponVTable[109], WeaponSetFreeCompanyCrestVisibleOnSlotDetour);
        _humanSetFreeCompanyCrestVisibleOnSlot.Enable();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Enable();
        _crestChangeHook.Enable();
        _crestChangeCallerHook.Enable();
    }

    public void UpdateCrests(Actor gameObject, CrestFlag flags)
    {
        if (!gameObject.IsCharacter)
            return;

        flags &= CrestExtensions.AllRelevant;
        var       currentCrests = gameObject.CrestBitfield;
        using var update        = _inUpdate.EnterMethod();
        _crestChangeHook.Original(&gameObject.AsCharacter->DrawData, (byte) flags);
        gameObject.CrestBitfield = currentCrests;
    }

    public delegate void DrawObjectCrestUpdateDelegate(Model drawObject, CrestFlag slot, ref bool value);

    public event DrawObjectCrestUpdateDelegate? ModelCrestSetup;

    protected override void Dispose(bool _)
    {
        _humanSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _crestChangeHook.Dispose();
        _crestChangeCallerHook.Dispose();
    }

    private delegate void CrestChangeDelegate(DrawDataContainer* container, byte crestFlags);

    [Signature(Sigs.CrestChange, DetourName = nameof(CrestChangeDetour))]
    private readonly Hook<CrestChangeDelegate> _crestChangeHook = null!;

    private void CrestChangeDetour(DrawDataContainer* container, byte crestFlags)
    {
        var actor = (Actor)container->OwnerObject;
        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            var newValue = ((CrestFlag)crestFlags).HasFlag(slot);
            Invoke(actor, slot, ref newValue);
            crestFlags = (byte)(newValue ? crestFlags | (byte)slot : crestFlags & (byte)~slot);
        }

        Glamourer.Log.Verbose(
            $"Called CrestChange on {(ulong)container:X} with {crestFlags:X} and prior flags {actor.CrestBitfield}.");
        using var _ = _inUpdate.EnterMethod();
        _crestChangeHook.Original(container, crestFlags);
    }

    [Signature(Sigs.CrestChangeCaller, DetourName = nameof(CrestChangeCallerDetour))]
    private readonly Hook<CrestChangeCallerDelegate> _crestChangeCallerHook = null!;

    private delegate void CrestChangeCallerDelegate(DrawDataContainer* container, byte* data);

    private void CrestChangeCallerDetour(DrawDataContainer* container, byte* data)
    {
        var     actor = (Actor)container->OwnerObject;
        ref var flags = ref data[16];
        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            var newValue = ((CrestFlag)flags).HasFlag(slot);
            Invoke(actor, slot, ref newValue);
            flags = (byte)(newValue ? flags | (byte)slot : flags & (byte)~slot);
        }
        Glamourer.Log.Verbose(
            $"Called inlined CrestChange via CrestChangeCaller on {(ulong)container:X} with {(flags & 0x1F):X} and prior flags {actor.CrestBitfield}.");

        using var _ = _inUpdate.EnterMethod();
        _crestChangeCallerHook.Original(container, data);
    }

    public static bool GetModelCrest(Actor gameObject, CrestFlag slot)
    {
        if (!gameObject.IsCharacter)
            return false;

        var (type, index) = slot.ToIndex();
        switch (type)
        {
            case CrestType.Human:
            {
                var model = gameObject.Model;
                if (!model.IsHuman)
                    return false;

                return model.AsHuman->IsFreeCompanyCrestVisibleOnSlot(index) != 0;
            }
            case CrestType.Offhand:
            {
                var model = (Model)gameObject.AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawObject;
                if (!model.IsWeapon)
                    return false;

                return model.AsWeapon->IsFreeCompanyCrestVisibleOnSlot(index) != 0;
            }
        }

        return false;
    }

    private readonly InMethodChecker _inUpdate = new();

    private delegate void SetCrestDelegateIntern(DrawObject* drawObject, byte slot, byte visible);

    [Signature(Sigs.HumanVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _humanVTable = null!;

    [Signature(Sigs.WeaponVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _weaponVTable = null!;

    private readonly Hook<SetCrestDelegateIntern> _humanSetFreeCompanyCrestVisibleOnSlot;
    private readonly Hook<SetCrestDelegateIntern> _weaponSetFreeCompanyCrestVisibleOnSlot;

    private void HumanSetFreeCompanyCrestVisibleOnSlotDetour(DrawObject* drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        var slot     = (CrestFlag)((ushort)CrestFlag.Head << slotIdx);
        if (!inUpdate)
            ModelCrestSetup?.Invoke(drawObject, slot, ref rVisible);

        Glamourer.Log.Excessive(
            $"[Human.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{(ulong)drawObject:X} for slot {slot} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _humanSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }

    private void WeaponSetFreeCompanyCrestVisibleOnSlotDetour(DrawObject* drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        if (!inUpdate && slotIdx == 0)
            ModelCrestSetup?.Invoke(drawObject, CrestFlag.OffHand, ref rVisible);
        Glamourer.Log.Excessive(
            $"[Weapon.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{(ulong)drawObject:X} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _weaponSetFreeCompanyCrestVisibleOnSlot.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }
}
