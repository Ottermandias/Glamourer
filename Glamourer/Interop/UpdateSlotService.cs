using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Glamourer.Events;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public readonly SlotUpdating SlotUpdatingEvent;

    public UpdateSlotService(SlotUpdating slotUpdating, IGameInteropProvider interop)
    {
        SlotUpdatingEvent = slotUpdating;
        interop.InitializeFromAttributes(this);
        _flagSlotForUpdateHook.Enable();
    }

    public void Dispose()
        => _flagSlotForUpdateHook.Dispose();

    public void UpdateSlot(Model drawObject, EquipSlot slot, CharacterArmor data)
    {
        if (!drawObject.IsCharacterBase)
            return;

        FlagSlotForUpdateInterop(drawObject, slot, data);
    }

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor, StainId stain)
        => UpdateSlot(drawObject, slot, armor.With(stain));

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => UpdateArmor(drawObject, slot, armor, drawObject.GetArmor(slot).Stain);

    public void UpdateStain(Model drawObject, EquipSlot slot, StainId stain)
        => UpdateArmor(drawObject, slot, drawObject.GetArmor(slot), stain);

    private delegate ulong FlagSlotForUpdateDelegateIntern(nint drawObject, uint slot, CharacterArmor* data);

    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagSlotForUpdateHook = null!;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        SlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        return returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
}
