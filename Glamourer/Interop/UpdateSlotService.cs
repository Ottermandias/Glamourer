using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public readonly SlotUpdating     SlotUpdatingEvent;
    public readonly EquipmentLoading EquipmentLoadingEvent;

    public UpdateSlotService(SlotUpdating slotUpdating, EquipmentLoading equipmentLoadingEvent)
    {
        SlotUpdatingEvent     = slotUpdating;
        EquipmentLoadingEvent = equipmentLoadingEvent;
        SignatureHelper.Initialise(this);
        _flagSlotForUpdateHook.Enable();
        _loadEquipmentHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _loadEquipmentHook.Dispose();
    }

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

    private delegate void LoadEquipmentDelegateIntern(DrawDataContainer* drawDataContainer, uint slotIdx, CharacterArmor data, bool force);

    // TODO: use client structs.
    [Signature("E8 ?? ?? ?? ?? 41 B5 ?? FF C6", DetourName = nameof(LoadEquipmentDetour))]
    private readonly Hook<LoadEquipmentDelegateIntern> _loadEquipmentHook = null!;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        SlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Information($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue}).");
        return returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private void LoadEquipmentDetour(DrawDataContainer* drawDataContainer, uint slotIdx, CharacterArmor data, bool force)
    {
        var slot = slotIdx.ToEquipSlot();
        EquipmentLoadingEvent.Invoke(drawDataContainer->Parent, slot, data);
        Glamourer.Log.Information($"[LoadEquipment] Called with 0x{(ulong)drawDataContainer:X} for slot {slot} with {data} ({force}).");
        _loadEquipmentHook.Original(drawDataContainer, slotIdx, data, force);
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
}
