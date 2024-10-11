using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Glamourer.Events;
using Penumbra.GameData;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class UpdateSlotService : IDisposable
{
    public readonly  EquipSlotUpdating EquipSlotUpdatingEvent;
    public readonly  BonusSlotUpdating BonusSlotUpdatingEvent;
    private readonly DictBonusItems       _bonusItems;

    public UpdateSlotService(EquipSlotUpdating equipSlotUpdating, BonusSlotUpdating bonusSlotUpdating, IGameInteropProvider interop,
        DictBonusItems bonusItems)
    {
        EquipSlotUpdatingEvent = equipSlotUpdating;
        BonusSlotUpdatingEvent = bonusSlotUpdating;
        _bonusItems               = bonusItems;
        interop.InitializeFromAttributes(this);
        _flagSlotForUpdateHook.Enable();
        _flagBonusSlotForUpdateHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _flagBonusSlotForUpdateHook.Dispose();
    }

    public void UpdateEquipSlot(Model drawObject, EquipSlot slot, CharacterArmor data)
    {
        if (!drawObject.IsCharacterBase)
            return;

        FlagSlotForUpdateInterop(drawObject, slot, data);
    }

    public void UpdateBonusSlot(Model drawObject, BonusItemFlag slot, CharacterArmor data)
    {
        if (!drawObject.IsCharacterBase)
            return;

        var index = slot.ToIndex();
        if (index == uint.MaxValue)
            return;

        _flagBonusSlotForUpdateHook.Original(drawObject.Address, index, &data);
    }

    public void UpdateGlasses(Model drawObject, BonusItemId id)
    {
        if (!_bonusItems.TryGetValue(id, out var glasses))
            return;

        var armor = new CharacterArmor(glasses.PrimaryId, glasses.Variant, StainIds.None);
        _flagBonusSlotForUpdateHook.Original(drawObject.Address, BonusItemFlag.Glasses.ToIndex(), &armor);
    }

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor, StainIds stains)
        => UpdateEquipSlot(drawObject, slot, armor.With(stains));

    public void UpdateArmor(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => UpdateArmor(drawObject, slot, armor, drawObject.GetArmor(slot).Stains);

    public void UpdateStain(Model drawObject, EquipSlot slot, StainIds stains)
        => UpdateArmor(drawObject, slot, drawObject.GetArmor(slot), stains);

    private delegate ulong FlagSlotForUpdateDelegateIntern(nint drawObject, uint slot, CharacterArmor* data);

    [Signature(Sigs.FlagSlotForUpdate, DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagSlotForUpdateHook = null!;

    [Signature(Sigs.FlagBonusSlotForUpdate, DetourName = nameof(FlagBonusSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegateIntern> _flagBonusSlotForUpdateHook = null!;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        EquipSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        return returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private ulong FlagBonusSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToBonusSlot();
        var returnValue = ulong.MaxValue;
        BonusSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagBonusSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        return returnValue == ulong.MaxValue ? _flagBonusSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
        => _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
}
