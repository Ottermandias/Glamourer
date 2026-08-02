using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop;

/// <summary> Triggered when the crest visibility is updated on a model. </summary>
public sealed unsafe class CrestService : EventBase<CrestService.Arguments, CrestService.Priority>
{
    private readonly HookManager _hooks;

    public enum Priority
    {
        /// <seealso cref="State.StateListener.OnCrestChange"/>
        StateListener = 0,
    }

    public ref struct Arguments(Actor actor, CrestFlag slot, ref bool value)
    {
        /// <summary> The game object with a crest update. </summary>
        public readonly Actor Actor = actor;

        /// <summary> The equipment slot changed. </summary>
        public readonly CrestFlag Slot = slot;

        /// <summary> The new value. </summary>
        public ref bool Value = ref value;
    }

    public CrestService(LunaLogger log, HookManager hooks)
        : base(nameof(CrestService), log)
    {
        _hooks        = hooks;
        _humanVTable  = (nint*)_hooks.SigScanner.GetStaticAddressFromSig(Sigs.HumanVTable);
        _weaponVTable = (nint*)_hooks.SigScanner.GetStaticAddressFromSig(Sigs.WeaponVTable);
        _humanSetFreeCompanyCrestVisibleOnSlot = _hooks.CreateHook<SetCrestDelegateIntern>("Human.SetFreeCompanyCrestVisibleOnSlot",
            _humanVTable[109], HumanSetFreeCompanyCrestVisibleOnSlotDetour, true)!;
        _weaponSetFreeCompanyCrestVisibleOnSlot = _hooks.CreateHook<SetCrestDelegateIntern>("Weapon.SetFreeCompanyCrestVisibleOnSlot",
            _weaponVTable[109], WeaponSetFreeCompanyCrestVisibleOnSlotDetour, true)!;
        _crestChangeHook = _hooks.CreateHook<CrestChangeDelegate>("CrestChange", Sigs.SetFreeCompanyCrestBitfield, CrestChangeDetour, true)!;
        _crestChangeCallerHook =
            _hooks.CreateHook<CrestChangeCallerDelegate>("CrestChangeCaller", Sigs.CrestChangeCaller, CrestChangeCallerDetour, true)!;
    }

    public void UpdateCrests(Actor gameObject, CrestFlag flags)
    {
        if (!gameObject.IsCharacter)
            return;

        flags &= CrestExtensions.AllRelevant;
        var       currentCrests = gameObject.CrestBitfield;
        using var update        = _inUpdate.EnterMethod();
        _crestChangeHook.Result.Original(&gameObject.AsCharacter->DrawData, (byte)flags);
        gameObject.CrestBitfield = currentCrests;
    }

    public delegate void DrawObjectCrestUpdateDelegate(Model drawObject, CrestFlag slot, ref bool value);

    public event DrawObjectCrestUpdateDelegate? ModelCrestSetup;

    protected override void Dispose(bool _)
    {
        _hooks.DisposeHook("Human.SetFreeCompanyCrestVisibleOnSlot");
        _hooks.DisposeHook("Weapon.SetFreeCompanyCrestVisibleOnSlot");
        _hooks.DisposeHook("CrestChange");
        _hooks.DisposeHook("CrestChangeCaller");
    }

    private delegate void                            CrestChangeDelegate(DrawDataContainer* container, byte crestFlags);
    private readonly Task<Hook<CrestChangeDelegate>> _crestChangeHook;

    private void CrestChangeDetour(DrawDataContainer* container, byte crestFlags)
    {
        var actor = (Actor)container->OwnerObject;
        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            var newValue = ((CrestFlag)crestFlags).HasFlag(slot);
            Invoke(new Arguments(actor, slot, ref newValue));
            crestFlags = (byte)(newValue ? crestFlags | (byte)slot : crestFlags & (byte)~slot);
        }

        Glamourer.Log.Verbose(
            $"Called CrestChange on {(ulong)container:X} with {crestFlags:X} and prior flags {actor.CrestBitfield}.");
        using var _ = _inUpdate.EnterMethod();
        _crestChangeHook.Result.Original(container, crestFlags);
    }

    private readonly Task<Hook<CrestChangeCallerDelegate>> _crestChangeCallerHook;

    private delegate void CrestChangeCallerDelegate(DrawDataContainer* container, byte* data);

    private void CrestChangeCallerDetour(DrawDataContainer* container, byte* data)
    {
        var     actor = (Actor)container->OwnerObject;
        ref var flags = ref data[16];
        foreach (var slot in CrestExtensions.AllRelevantSet)
        {
            var newValue = ((CrestFlag)flags).HasFlag(slot);
            Invoke(new Arguments(actor, slot, ref newValue));
            flags = (byte)(newValue ? flags | (byte)slot : flags & (byte)~slot);
        }

        Glamourer.Log.Verbose(
            $"Called inlined CrestChange via CrestChangeCaller on {(ulong)container:X} with {flags & 0x1F:X} and prior flags {actor.CrestBitfield}.");

        using var _ = _inUpdate.EnterMethod();
        _crestChangeCallerHook.Result.Original(container, data);
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

                return model.AsHuman->IsFreeCompanyCrestVisibleOnSlot(index);
            }
            case CrestType.Offhand:
            {
                var model = (Model)gameObject.AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawData.DrawObject;
                if (!model.IsWeapon)
                    return false;

                return model.AsWeapon->IsFreeCompanyCrestVisibleOnSlot(index);
            }
        }

        return false;
    }

    private readonly InMethodChecker _inUpdate = new();

    private delegate void SetCrestDelegateIntern(DrawObject* drawObject, byte slot, byte visible);

    private readonly nint* _humanVTable  = null!;
    private readonly nint* _weaponVTable = null!;

    private readonly Task<Hook<SetCrestDelegateIntern>> _humanSetFreeCompanyCrestVisibleOnSlot;
    private readonly Task<Hook<SetCrestDelegateIntern>> _weaponSetFreeCompanyCrestVisibleOnSlot;

    private void HumanSetFreeCompanyCrestVisibleOnSlotDetour(DrawObject* drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        var slot     = (CrestFlag)((ushort)CrestFlag.Head << slotIdx);
        if (!inUpdate)
            ModelCrestSetup?.Invoke(drawObject, slot, ref rVisible);

        Glamourer.Log.Excessive(
            $"[Human.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{(ulong)drawObject:X} for slot {slot} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _humanSetFreeCompanyCrestVisibleOnSlot.Result.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }

    private void WeaponSetFreeCompanyCrestVisibleOnSlotDetour(DrawObject* drawObject, byte slotIdx, byte visible)
    {
        var rVisible = visible != 0;
        var inUpdate = _inUpdate.InMethod;
        if (!inUpdate && slotIdx == 0)
            ModelCrestSetup?.Invoke(drawObject, CrestFlag.OffHand, ref rVisible);
        Glamourer.Log.Excessive(
            $"[Weapon.SetFreeCompanyCrestVisibleOnSlot] Called with 0x{(ulong)drawObject:X} with {rVisible} (original: {visible != 0}, in update: {inUpdate}).");
        _weaponSetFreeCompanyCrestVisibleOnSlot.Result.Original(drawObject, slotIdx, rVisible ? (byte)1 : (byte)0);
    }
}
