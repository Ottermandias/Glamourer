using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Penumbra.GameData;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
namespace Glamourer.Interop;

/// <summary>
/// This struct is the struct that loadallequipment passes in as its gearsetData container.
/// </summary>
[StructLayout(LayoutKind.Explicit)] // Size of 70 bytes maybe?
public readonly struct GearsetItemDataStruct
{
    // Stores the weapon data. Includes both dyes in the data. </summary>
    [FieldOffset(0)] public readonly WeaponModelId MainhandWeaponData;
    [FieldOffset(8)] public readonly WeaponModelId OffhandWeaponData;

    [FieldOffset(16)] public readonly byte CrestBitField; // A Bitfield:: ShieldCrest == 1, HeadCrest == 2, Chest Crest == 4
    [FieldOffset(17)] public readonly byte JobId; // Job ID associated with the gearset change.

    // Flicks from 0 to 128 (anywhere inbetween), have yet to associate what it is linked to. Remains the same when flicking between gearsets of the same job.
    [FieldOffset(18)] public readonly byte UNK_18;
    [FieldOffset(19)] public readonly byte UNK_19; // I have never seen this be anything other than 0.

    // Legacy helmet equip slot armor for a character.
    [FieldOffset(20)] public readonly LegacyCharacterArmor HeadSlotArmor;
    [FieldOffset(24)] public readonly LegacyCharacterArmor TopSlotArmor;
    [FieldOffset(28)] public readonly LegacyCharacterArmor ArmsSlotArmor;
    [FieldOffset(32)] public readonly LegacyCharacterArmor LegsSlotArmor;
    [FieldOffset(26)] public readonly LegacyCharacterArmor FeetSlotArmor;
    [FieldOffset(40)] public readonly LegacyCharacterArmor EarSlotArmor;
    [FieldOffset(44)] public readonly LegacyCharacterArmor NeckSlotArmor;
    [FieldOffset(48)] public readonly LegacyCharacterArmor WristSlotArmor;
    [FieldOffset(52)] public readonly LegacyCharacterArmor RFingerSlotArmor;
    [FieldOffset(56)] public readonly LegacyCharacterArmor LFingerSlotArmor;

    // Byte array of all slot's secondary dyes.
    [FieldOffset(60)] public readonly byte HeadSlotSecondaryDye;
    [FieldOffset(61)] public readonly byte TopSlotSecondaryDye;
    [FieldOffset(62)] public readonly byte ArmsSlotSecondaryDye;
    [FieldOffset(63)] public readonly byte LegsSlotSecondaryDye;
    [FieldOffset(64)] public readonly byte FeetSlotSecondaryDye;
    [FieldOffset(65)] public readonly byte EarSlotSecondaryDye;
    [FieldOffset(66)] public readonly byte NeckSlotSecondaryDye;
    [FieldOffset(67)] public readonly byte WristSlotSecondaryDye;
    [FieldOffset(68)] public readonly byte RFingerSlotSecondaryDye;
    [FieldOffset(69)] public readonly byte LFingerSlotSecondaryDye;
}

public unsafe class UpdateSlotService : IDisposable
{
    public readonly EquipSlotUpdating EquipSlotUpdatingEvent;
    public readonly BonusSlotUpdating BonusSlotUpdatingEvent;
    private readonly DictBonusItems _bonusItems;

    #region LoadAllEquipData
    ///////////////////////////////////////////////////
    // This is a currently undocumented signature that loads all equipment after changing a gearset.
    // :: Signature Maintainers Note:
    // To obtain this signature, get the stacktrace from FlagSlotForUpdate for human, and find func `sub_140842F50`.
    // This function is what calls the weapon/equipment/crest loads, which call FlagSlotForUpdate if different.
    //
    // By detouring this function, and executing the original, then logic after, we have a consistant point in time where we know all
    // slots have been flagged, meaning a consistant point in time that glamourer has processed all of its updates.
    public const string LoadAllEquipmentSig = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 44 0F B6 B9";
    private delegate Int64 LoadAllEquipmentDelegate(DrawDataContainer* drawDataContainer, GearsetItemDataStruct* gearsetData);
    private Int64 LoadAllEquipmentDetour(DrawDataContainer* drawDataContainer, GearsetItemDataStruct* gearsetData)
    {
        // return original first so we can log the changes after
        var ret = _loadAllEquipmentHook.Original(drawDataContainer, gearsetData);

        // perform logic stuff.
        var owner = drawDataContainer->OwnerObject;
        Glamourer.Log.Warning($"[LoadAllEquipmentDetour] Owner: 0x{(nint)owner->DrawObject:X} Finished Applying its GameState!");
        Glamourer.Log.Warning($"[LoadAllEquipmentDetour] GearsetItemData: {FormatGearsetItemDataStruct(*gearsetData)}");

        // return original.
        return ret;
    }

    private string FormatWeaponModelId(WeaponModelId weaponModelId) => $"Id: {weaponModelId.Id}, Type: {weaponModelId.Type}, Variant: {weaponModelId.Variant}, Stain0: {weaponModelId.Stain0}, Stain1: {weaponModelId.Stain1}";

    private string FormatGearsetItemDataStruct(GearsetItemDataStruct gearsetItemData)
    {
        string ret = $"\nMainhandWeaponData: {FormatWeaponModelId(gearsetItemData.MainhandWeaponData)}," +
               $"\nOffhandWeaponData: {FormatWeaponModelId(gearsetItemData.OffhandWeaponData)}," +
               $"\nCrestBitField: {gearsetItemData.CrestBitField} | JobId: {gearsetItemData.JobId} | UNK_18: {gearsetItemData.UNK_18} | UNK_19: {gearsetItemData.UNK_19}";
        // Iterate through offsets from 20 to 60 and format the CharacterArmor data
        for (int offset = 20; offset <= 56; offset += sizeof(LegacyCharacterArmor))
        {
            LegacyCharacterArmor* equipSlotPtr = (LegacyCharacterArmor*)((byte*)&gearsetItemData + offset);
            int dyeOffset = (offset - 20) / sizeof(LegacyCharacterArmor) + 60; // Calculate the corresponding dye offset
            byte* dyePtr = (byte*)&gearsetItemData + dyeOffset;
            ret += $"\nEquipSlot {((EquipSlot)(dyeOffset-60)).ToString()}:: Id: {(*equipSlotPtr).Set}, Variant: {(*equipSlotPtr).Variant}, Stain0: {(*equipSlotPtr).Stain.Id}, Stain1: {*dyePtr}";
        }
        return ret;
    }
#endregion LoadAllEquipData

    public UpdateSlotService(EquipSlotUpdating equipSlotUpdating, BonusSlotUpdating bonusSlotUpdating, IGameInteropProvider interop,
        DictBonusItems bonusItems)
    {
        EquipSlotUpdatingEvent = equipSlotUpdating;
        BonusSlotUpdatingEvent = bonusSlotUpdating;
        _bonusItems = bonusItems;
        interop.InitializeFromAttributes(this);
        _flagSlotForUpdateHook.Enable();
        _flagBonusSlotForUpdateHook.Enable();
        _loadAllEquipmentHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _flagBonusSlotForUpdateHook.Dispose();
        _loadAllEquipmentHook.Dispose();
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

    [Signature(LoadAllEquipmentSig, DetourName = nameof(LoadAllEquipmentDetour))]
    private readonly Hook<LoadAllEquipmentDelegate> _loadAllEquipmentHook = null!;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;

        EquipSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Information($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        returnValue = returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;

        return returnValue;
    }

    private ulong FlagBonusSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot = slotIdx.ToBonusSlot();
        var returnValue = ulong.MaxValue;

        BonusSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Information($"[FlagBonusSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        returnValue = returnValue == ulong.MaxValue ? _flagBonusSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;

        return returnValue;
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
    {
        Glamourer.Log.Warning($"Glamour-Invoked Equip Slot update for 0x{drawObject.Address:X} with {slot} and {armor}.");
        return _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
    }
}
