using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using OtterGui.Classes;
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
            interop.HookFromAddress<SetCrestDelegateIntern>(_humanVTable[96], HumanSetFreeCompanyCrestVisibleOnSlotDetour);
        _weaponSetFreeCompanyCrestVisibleOnSlot =
            interop.HookFromAddress<SetCrestDelegateIntern>(_weaponVTable[96], WeaponSetFreeCompanyCrestVisibleOnSlotDetour);
        _humanSetFreeCompanyCrestVisibleOnSlot.Enable();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Enable();
        _crestChangeHook.Enable();
    }

    public void UpdateCrests(Actor gameObject, CrestFlag flags)
    {
        if (!gameObject.IsCharacter)
            return;

        flags &= CrestExtensions.AllRelevant;
        var       currentCrests = gameObject.CrestBitfield;
        using var update        = _inUpdate.EnterMethod();
        _crestChangeHook.Original(gameObject.AsCharacter, (byte) flags);
        gameObject.CrestBitfield = currentCrests;
    }

    public delegate void DrawObjectCrestUpdateDelegate(Model drawObject, CrestFlag slot, ref bool value);

    public event DrawObjectCrestUpdateDelegate? ModelCrestSetup;

    protected override void Dispose(bool _)
    {
        _humanSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _weaponSetFreeCompanyCrestVisibleOnSlot.Dispose();
        _crestChangeHook.Dispose();
    }

    private delegate void CrestChangeDelegate(Character* character, byte crestFlags);

    [Signature("E8 ?? ?? ?? ?? 48 8B 55 ?? 49 8B CE E8", DetourName = nameof(CrestChangeDetour))]
    private readonly Hook<CrestChangeDelegate> _crestChangeHook = null!;

    private void CrestChangeDetour(Character* character, byte crestFlags)
    {
        var actor = (Actor)character;
        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            var newValue = ((CrestFlag)crestFlags).HasFlag(slot);
            Invoke(actor, slot, ref newValue);
            crestFlags = (byte)(newValue ? crestFlags | (byte)slot : crestFlags & (byte)~slot);
        }

        Glamourer.Log.Verbose(
            $"Called CrestChange on {(ulong)character:X} with {crestFlags:X} and prior flags {((Actor)character).CrestBitfield}.");
        using var _ = _inUpdate.EnterMethod();
        _crestChangeHook.Original(character, crestFlags);
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

                var getter = (delegate* unmanaged<Human*, byte, byte>)((nint*)model.AsCharacterBase->VTable)[95];
                return getter(model.AsHuman, index) != 0;
            }
            case CrestType.Offhand:
            {
                var model = (Model)gameObject.AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawObject;
                if (!model.IsWeapon)
                    return false;

                var getter = (delegate* unmanaged<Weapon*, byte, byte>)((nint*)model.AsCharacterBase->VTable)[95];
                return getter(model.AsWeapon, index) != 0;
            }
        }

        return false;
    }

    private readonly InMethodChecker _inUpdate = new();

    private delegate void SetCrestDelegateIntern(DrawObject* drawObject, byte slot, byte visible);

    [Signature(global::Penumbra.GameData.Sigs.HumanVTable, ScanType = ScanType.StaticAddress)]
    private readonly nint* _humanVTable = null!;

    [Signature(global::Penumbra.GameData.Sigs.WeaponVTable, ScanType = ScanType.StaticAddress)]
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
