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

// Can be removed once merged with client structs and referenced directly. See: https://github.com/aers/FFXIVClientStructs/pull/1277/files
[StructLayout(LayoutKind.Explicit)]
public readonly struct GearsetDataStruct
{
    // Stores the weapon data. Includes both dyes in the data. </summary>
    [FieldOffset(0)] public readonly WeaponModelId MainhandWeaponData;
    [FieldOffset(8)] public readonly WeaponModelId OffhandWeaponData;

    [FieldOffset(16)] public readonly byte CrestBitField; // A Bitfield:: ShieldCrest == 1, HeadCrest == 2, Chest Crest == 4
    [FieldOffset(17)] public readonly byte JobId; // Job ID associated with the gearset change.

    // Flicks from 0 to 127 (anywhere inbetween), have yet to associate what it is linked to. Remains the same when flicking between gearsets of the same job.
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
    public readonly GearsetDataLoaded GearsetDataLoadedEvent;
    private readonly DictBonusItems _bonusItems;
    public UpdateSlotService(EquipSlotUpdating equipSlotUpdating, BonusSlotUpdating bonusSlotUpdating, GearsetDataLoaded gearsetDataLoaded,
        IGameInteropProvider interop, DictBonusItems bonusItems)
    {
        EquipSlotUpdatingEvent = equipSlotUpdating;
        BonusSlotUpdatingEvent = bonusSlotUpdating;
        GearsetDataLoadedEvent = gearsetDataLoaded;
        _bonusItems = bonusItems;

        // Usable after the merge with client structs.
        //_loadGearsetDataHook = interop.HookFromAddress<LoadGearsetDataDelegate>((nint)DrawDataContainer.MemberFunctionPointers.LoadGearsetData, LoadGearsetDataDetour);
        interop.InitializeFromAttributes(this);

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

    // This signature is what calls the weapon/equipment/crest load functions in the drawData container inherited from a human/characterBase.
    //
    // Contrary to assumption, this is not frequently fired when any slot changes, and is instead only called when another player
    // initially loads, or when the client player changes gearsets. (Does not fire when another player or self is redrawn)
    //
    // This functions purpose is to iterate all Equipment/Weapon/Crest data on gearset change / initial player load, and determine which slots need to fire FlagSlotForUpdate.
    //
    // Because Glamourer processes GameState changes by detouring this method, this means by returning original after detour, any logic performed after will occur
    // AFTER Glamourer finishes applying all changes to the game State, providing a gearset endpoint. (MetaData not included)
    // Currently pending a merge to clientStructs, after which it can be removed, along with the explicit struct. See: https://github.com/aers/FFXIVClientStructs/pull/1277/files
    public const string LoadGearsetDataSig = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 44 0F B6 B9";
    private delegate Int64 LoadGearsetDataDelegate(DrawDataContainer* drawDataContainer, GearsetDataStruct* gearsetData);

    [Signature(LoadGearsetDataSig, DetourName = nameof(LoadGearsetDataDetour))]
    private readonly Hook<LoadGearsetDataDelegate> _loadGearsetDataHook = null!;

    private ulong FlagSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToEquipSlot();
        var returnValue = ulong.MaxValue;
        EquipSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        returnValue = returnValue == ulong.MaxValue ? _flagSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
        return returnValue;
    }

    private ulong FlagBonusSlotForUpdateDetour(nint drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot        = slotIdx.ToBonusSlot();
        var returnValue = ulong.MaxValue;
        BonusSlotUpdatingEvent.Invoke(drawObject, slot, ref *data, ref returnValue);
        Glamourer.Log.Excessive($"[FlagBonusSlotForUpdate] Called with 0x{drawObject:X} for slot {slot} with {*data} ({returnValue:X}).");
        returnValue = returnValue == ulong.MaxValue ? _flagBonusSlotForUpdateHook.Original(drawObject, slotIdx, data) : returnValue;
        return returnValue;
    }

    private ulong FlagSlotForUpdateInterop(Model drawObject, EquipSlot slot, CharacterArmor armor)
    {
        Glamourer.Log.Excessive($"[FlagBonusSlotForUpdate] Invoked by Glamourer on 0x{drawObject.Address:X} on {slot} with itemdata {armor}.");
        return _flagSlotForUpdateHook.Original(drawObject.Address, slot.ToIndex(), &armor);
    }
    private Int64 LoadGearsetDataDetour(DrawDataContainer* drawDataContainer, GearsetDataStruct* gearsetData)
    {
        // Let the gearset data process all of its loads and slot flag update calls first.
        var ret = _loadGearsetDataHook.Original(drawDataContainer, gearsetData);
        Model drawObject = drawDataContainer->OwnerObject->DrawObject;
        Glamourer.Log.Verbose($"[LoadAllEquipmentDetour] Owner: 0x{drawObject.Address:X} Finished Applying its GameState!");
        GearsetDataLoadedEvent.Invoke(drawObject);
        // Can use for debugging, if desired.
        // Glamourer.Log.Verbose($"[LoadAllEquipmentDetour] GearsetItemData: {FormatGearsetItemDataStruct(*gearsetData)}");
        return ret;
    }

    // If you ever care to debug this, here is a formatted string output of this new gearsetData struct.
    private string FormatGearsetItemDataStruct(GearsetDataStruct gearsetData)
    {
        string ret =
            $"\nMainhandWeaponData: Id: {gearsetData.MainhandWeaponData.Id}, Type: {gearsetData.MainhandWeaponData.Type}, " +
            $"Variant: {gearsetData.MainhandWeaponData.Variant}, Stain0: {gearsetData.MainhandWeaponData.Stain0}, Stain1: {gearsetData.MainhandWeaponData.Stain1}" +
            $"\nOffhandWeaponData: Id: {gearsetData.OffhandWeaponData.Id}, Type: {gearsetData.OffhandWeaponData.Type}, " +
            $"Variant: {gearsetData.OffhandWeaponData.Variant}, Stain0: {gearsetData.OffhandWeaponData.Stain0}, Stain1: {gearsetData.OffhandWeaponData.Stain1}" +
            $"\nCrestBitField: {gearsetData.CrestBitField} | JobId: {gearsetData.JobId} | UNK_18: {gearsetData.UNK_18} | UNK_19: {gearsetData.UNK_19}";
        for (int offset = 20; offset <= 56; offset += sizeof(LegacyCharacterArmor))
        {
            LegacyCharacterArmor* equipSlotPtr = (LegacyCharacterArmor*)((byte*)&gearsetData + offset);
            int dyeOffset = (offset - 20) / sizeof(LegacyCharacterArmor) + 60; // Calculate the corresponding dye offset
            byte* dyePtr = (byte*)&gearsetData + dyeOffset;
            ret += $"\nEquipSlot {((EquipSlot)(dyeOffset - 60)).ToString()}:: Id: {(*equipSlotPtr).Set}, Variant: {(*equipSlotPtr).Variant}, Stain0: {(*equipSlotPtr).Stain.Id}, Stain1: {*dyePtr}";
        }
        return ret;
    }
}
