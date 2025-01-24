using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
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
    public readonly  GearsetDataLoaded GearsetDataLoadedEvent;
    private readonly DictBonusItems    _bonusItems;

    public UpdateSlotService(EquipSlotUpdating equipSlotUpdating, BonusSlotUpdating bonusSlotUpdating, GearsetDataLoaded gearsetDataLoaded,
        IGameInteropProvider interop, DictBonusItems bonusItems)
    {
        EquipSlotUpdatingEvent = equipSlotUpdating;
        BonusSlotUpdatingEvent = bonusSlotUpdating;
        GearsetDataLoadedEvent = gearsetDataLoaded;
        _bonusItems            = bonusItems;

        interop.InitializeFromAttributes(this);
        _loadGearsetDataHook = interop.HookFromAddress<LoadGearsetDataDelegate>((nint)DrawDataContainer.MemberFunctionPointers.LoadGearsetData, LoadGearsetDataDetour);
        _flagSlotForUpdateHook.Enable();
        _flagBonusSlotForUpdateHook.Enable();
        _loadGearsetDataHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _flagBonusSlotForUpdateHook.Dispose();
        _loadGearsetDataHook.Dispose();
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

    /// <summary> Detours the func that makes all FlagSlotForUpdate calls on a gearset change or initial render of a given actor (Only Cases this is Called).
    /// <para> Logic done after returning the original hook executes <b>After</b> all equipment/weapon/crest data is loaded into the Actors BaseData. </para>
    /// </summary>
    private delegate ulong LoadGearsetDataDelegate(DrawDataContainer* drawDataContainer, PacketPlayerGearsetData* gearsetData);
    private readonly Hook<LoadGearsetDataDelegate> _loadGearsetDataHook;

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
    {
        Glamourer.Log.Excessive($"[FlagBonusSlotForUpdate] Glamourer-Invoked on 0x{drawObject.Address:X} on {slot} with item data {armor}.");
        return _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
    }
    private ulong LoadGearsetDataDetour(DrawDataContainer* drawDataContainer, PacketPlayerGearsetData* gearsetData)
    {
        var ret = _loadGearsetDataHook.Original(drawDataContainer, gearsetData);
        var drawObject = drawDataContainer->OwnerObject->DrawObject;
        GearsetDataLoadedEvent.Invoke(drawObject);
        Glamourer.Log.Excessive($"[LoadAllEquipmentDetour] GearsetItemData: {FormatGearsetItemDataStruct(*gearsetData)}");
        return ret;
    }


    private static string FormatGearsetItemDataStruct(PacketPlayerGearsetData gearsetData)
    {
        var ret =
            $"\nMainhandWeaponData: Id: {gearsetData.MainhandWeaponData.Id}, Type: {gearsetData.MainhandWeaponData.Type}, " +
            $"Variant: {gearsetData.MainhandWeaponData.Variant}, Stain0: {gearsetData.MainhandWeaponData.Stain0}, Stain1: {gearsetData.MainhandWeaponData.Stain1}" +
            $"\nOffhandWeaponData: Id: {gearsetData.OffhandWeaponData.Id}, Type: {gearsetData.OffhandWeaponData.Type}, " +
            $"Variant: {gearsetData.OffhandWeaponData.Variant}, Stain0: {gearsetData.OffhandWeaponData.Stain0}, Stain1: {gearsetData.OffhandWeaponData.Stain1}" +
            $"\nCrestBitField: {gearsetData.CrestBitField} | JobId: {gearsetData.JobId}";
        for (var offset = 20; offset <= 56; offset += sizeof(LegacyCharacterArmor))
        {
            var equipSlotPtr = (LegacyCharacterArmor*)((byte*)&gearsetData + offset);
            var dyeOffset = (offset - 20) / sizeof(LegacyCharacterArmor) + 60; // Calculate the corresponding dye offset
            var dyePtr = (byte*)&gearsetData + dyeOffset;
            ret += $"\nEquipSlot {(EquipSlot)(dyeOffset - 60)}:: Id: {(*equipSlotPtr).Set}, Variant: {(*equipSlotPtr).Variant}, Stain0: {(*equipSlotPtr).Stain.Id}, Stain1: {*dyePtr}";
        }
        return ret;
    }
}
