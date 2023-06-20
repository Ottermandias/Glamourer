using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public readonly SlotUpdating Event;

    public UpdateSlotService(SlotUpdating slotUpdating)
    {
        Event = slotUpdating;
        SignatureHelper.Initialise(this);
        _flagSlotForUpdateHook.Enable();
    }

    public void Dispose()
        => _flagSlotForUpdateHook.Dispose();

    private delegate ulong FlagSlotForUpdateDelegateIntern(nint drawObject, uint slot, CharacterArmor* data);

    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagSlotForUpdateHook = null!;

    public void UpdateSlot(Model drawObject, EquipSlot slot, CharacterArmor data)
    {
        if (!drawObject.IsCharacterBase)
            return;
        FlagSlotForUpdateInterop(drawObject, slot, data);
    }

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor data)
    {
        if (!drawObject.IsCharacterBase)
            return;

        FlagSlotForUpdateInterop(drawObject, slot, data.With(drawObject.GetArmor(slot).Stain));
    }

    public void UpdateStain(Model drawObject, EquipSlot slot, StainId stain)
    {
        if (!drawObject.IsHuman)
            return;

        FlagSlotForUpdateInterop(drawObject, slot, drawObject.GetArmor(slot).With(stain));
    }

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        Event.Invoke(drawObject, slot, ref *data, ref returnValue);
        return returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
}
