using Dalamud.Interface.Internal.Notifications;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System;
using System.Linq;
using Penumbra.GameData.DataContainers;

namespace Glamourer.Designs;

public class DesignBase
{
    public const int FileVersion = 1;

    private DesignData _designData = new();

    /// <summary> For read-only information about the actual design. </summary>
    public ref readonly DesignData DesignData
        => ref _designData;

    /// <summary> To make it clear that something is edited here. </summary>
    public ref DesignData GetDesignDataRef()
        => ref _designData;

    internal DesignBase(CustomizationService customize, ItemManager items)
    {
        _designData.SetDefaultEquipment(items);
        CustomizationSet = SetCustomizationSet(customize);
    }

    internal DesignBase(CustomizationService customize, in DesignData designData, EquipFlag equipFlags, CustomizeFlag customizeFlags)
    {
        _designData      = designData;
        ApplyCustomize   = customizeFlags & CustomizeFlagExtensions.AllRelevant;
        ApplyEquip       = equipFlags & EquipFlagExtensions.All;
        _designFlags     = 0;
        CustomizationSet = SetCustomizationSet(customize);
    }

    internal DesignBase(DesignBase clone)
    {
        _designData      = clone._designData;
        CustomizationSet = clone.CustomizationSet;
        ApplyCustomize   = clone.ApplyCustomizeRaw;
        ApplyEquip       = clone.ApplyEquip & EquipFlagExtensions.All;
        _designFlags     = clone._designFlags & (DesignFlags)0x0F;
    }

    /// <summary> Ensure that the customization set is updated when the design data changes. </summary>
    internal void SetDesignData(CustomizationService customize, in DesignData other)
    {
        _designData      = other;
        CustomizationSet = SetCustomizationSet(customize);
    }

    #region Application Data

    [Flags]
    private enum DesignFlags : byte
    {
        ApplyHatVisible    = 0x01,
        ApplyVisorState    = 0x02,
        ApplyWeaponVisible = 0x04,
        ApplyWetness       = 0x08,
        WriteProtected     = 0x10,
    }

    private CustomizeFlag    _applyCustomize = CustomizeFlagExtensions.AllRelevant;
    public  CustomizationSet CustomizationSet { get; private set; }

    internal CustomizeFlag ApplyCustomize
    {
        get => _applyCustomize.FixApplication(CustomizationSet);
        set => _applyCustomize = value & CustomizeFlagExtensions.AllRelevant;
    }

    internal CustomizeFlag ApplyCustomizeRaw
        => _applyCustomize;

    internal EquipFlag   ApplyEquip   = EquipFlagExtensions.All;
    internal CrestFlag   ApplyCrest   = CrestExtensions.AllRelevant;
    private  DesignFlags _designFlags = DesignFlags.ApplyHatVisible | DesignFlags.ApplyVisorState | DesignFlags.ApplyWeaponVisible;

    public bool SetCustomize(CustomizationService customizationService, Customize customize)
    {
        if (customize.Equals(_designData.Customize))
            return false;

        _designData.Customize.Load(customize);
        CustomizationSet = customizationService.Service.GetList(customize.Clan, customize.Gender);
        return true;
    }

    public bool DoApplyHatVisible()
        => _designFlags.HasFlag(DesignFlags.ApplyHatVisible);

    public bool DoApplyVisorToggle()
        => _designFlags.HasFlag(DesignFlags.ApplyVisorState);

    public bool DoApplyWeaponVisible()
        => _designFlags.HasFlag(DesignFlags.ApplyWeaponVisible);

    public bool DoApplyWetness()
        => _designFlags.HasFlag(DesignFlags.ApplyWetness);

    public bool WriteProtected()
        => _designFlags.HasFlag(DesignFlags.WriteProtected);

    public bool SetApplyHatVisible(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.ApplyHatVisible : _designFlags & ~DesignFlags.ApplyHatVisible;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }

    public bool SetApplyVisorToggle(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.ApplyVisorState : _designFlags & ~DesignFlags.ApplyVisorState;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }

    public bool SetApplyWeaponVisible(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.ApplyWeaponVisible : _designFlags & ~DesignFlags.ApplyWeaponVisible;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }

    public bool SetApplyWetness(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.ApplyWetness : _designFlags & ~DesignFlags.ApplyWetness;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }

    public bool SetWriteProtected(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.WriteProtected : _designFlags & ~DesignFlags.WriteProtected;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }

    public bool DoApplyEquip(EquipSlot slot)
        => ApplyEquip.HasFlag(slot.ToFlag());

    public bool DoApplyStain(EquipSlot slot)
        => ApplyEquip.HasFlag(slot.ToStainFlag());

    public bool DoApplyCustomize(CustomizeIndex idx)
        => ApplyCustomize.HasFlag(idx.ToFlag());

    public bool DoApplyCrest(CrestFlag slot)
        => ApplyCrest.HasFlag(slot);

    internal bool SetApplyEquip(EquipSlot slot, bool value)
    {
        var newValue = value ? ApplyEquip | slot.ToFlag() : ApplyEquip & ~slot.ToFlag();
        if (newValue == ApplyEquip)
            return false;

        ApplyEquip = newValue;
        return true;
    }

    internal bool SetApplyStain(EquipSlot slot, bool value)
    {
        var newValue = value ? ApplyEquip | slot.ToStainFlag() : ApplyEquip & ~slot.ToStainFlag();
        if (newValue == ApplyEquip)
            return false;

        ApplyEquip = newValue;
        return true;
    }

    internal bool SetApplyCustomize(CustomizeIndex idx, bool value)
    {
        var newValue = value ? _applyCustomize | idx.ToFlag() : _applyCustomize & ~idx.ToFlag();
        if (newValue == _applyCustomize)
            return false;

        _applyCustomize = newValue;
        return true;
    }

    internal bool SetApplyCrest(CrestFlag slot, bool value)
    {
        var newValue = value ? ApplyCrest | slot : ApplyCrest & ~slot;
        if (newValue == ApplyCrest)
            return false;

        ApplyCrest = newValue;
        return true;
    }

    internal FlagRestrictionResetter TemporarilyRestrictApplication(EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags)
        => new(this, equipFlags, customizeFlags, crestFlags);

    internal readonly struct FlagRestrictionResetter : IDisposable
    {
        private readonly DesignBase    _design;
        private readonly EquipFlag     _oldEquipFlags;
        private readonly CustomizeFlag _oldCustomizeFlags;
        private readonly CrestFlag     _oldCrestFlags;

        public FlagRestrictionResetter(DesignBase d, EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags)
        {
            _design            =  d;
            _oldEquipFlags     =  d.ApplyEquip;
            _oldCustomizeFlags =  d.ApplyCustomizeRaw;
            _oldCrestFlags     =  d.ApplyCrest;
            d.ApplyEquip       &= equipFlags;
            d.ApplyCustomize   &= customizeFlags;
            d.ApplyCrest       &= crestFlags;
        }

        public void Dispose()
        {
            _design.ApplyEquip     = _oldEquipFlags;
            _design.ApplyCustomize = _oldCustomizeFlags;
            _design.ApplyCrest     = _oldCrestFlags;
        }
    }

    private CustomizationSet SetCustomizationSet(CustomizationService customize)
        => !_designData.IsHuman
            ? customize.Service.GetList(SubRace.Midlander,          Gender.Male)
            : customize.Service.GetList(_designData.Customize.Clan, _designData.Customize.Gender);

    #endregion

    #region Serialization

    public JObject JsonSerialize()
    {
        var ret = new JObject
        {
            ["FileVersion"] = FileVersion,
            ["Equipment"]   = SerializeEquipment(),
            ["Customize"]   = SerializeCustomize(),
        };
        return ret;
    }

    protected JObject SerializeEquipment()
    {
        static JObject Serialize(CustomItemId id, StainId stain, bool crest, bool apply, bool applyStain, bool applyCrest)
            => new()
            {
                ["ItemId"]     = id.Id,
                ["Stain"]      = stain.Id,
                ["Crest"]      = crest,
                ["Apply"]      = apply,
                ["ApplyStain"] = applyStain,
                ["ApplyCrest"] = applyCrest,
            };

        var ret = new JObject();
        if (_designData.IsHuman)
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                var item      = _designData.Item(slot);
                var stain     = _designData.Stain(slot);
                var crestSlot = slot.ToCrestFlag();
                var crest     = _designData.Crest(crestSlot);
                ret[slot.ToString()] = Serialize(item.Id, stain, crest, DoApplyEquip(slot), DoApplyStain(slot), DoApplyCrest(crestSlot));
            }

            ret["Hat"]    = new QuadBool(_designData.IsHatVisible(),    DoApplyHatVisible()).ToJObject("Show", "Apply");
            ret["Visor"]  = new QuadBool(_designData.IsVisorToggled(),  DoApplyVisorToggle()).ToJObject("IsToggled", "Apply");
            ret["Weapon"] = new QuadBool(_designData.IsWeaponVisible(), DoApplyWeaponVisible()).ToJObject("Show", "Apply");
        }
        else
        {
            ret["Array"] = _designData.WriteEquipmentBytesBase64();
        }

        return ret;
    }

    protected JObject SerializeCustomize()
    {
        var ret = new JObject()
        {
            ["ModelId"] = _designData.ModelId,
        };

        var customize = _designData.Customize;
        if (_designData.IsHuman)
            foreach (var idx in Enum.GetValues<CustomizeIndex>())
            {
                ret[idx.ToString()] = new JObject()
                {
                    ["Value"] = customize[idx].Value,
                    ["Apply"] = ApplyCustomizeRaw.HasFlag(idx.ToFlag()),
                };
            }
        else
            ret["Array"] = customize.WriteBase64();

        ret["Wetness"] = new JObject()
        {
            ["Value"] = _designData.IsWet(),
            ["Apply"] = DoApplyWetness(),
        };

        return ret;
    }

    #endregion

    #region Deserialization

    public static DesignBase LoadDesignBase(CustomizationService customizations, ItemManager items, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            FileVersion => LoadDesignV1Base(customizations, items, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static DesignBase LoadDesignV1Base(CustomizationService customizations, ItemManager items, JObject json)
    {
        var ret = new DesignBase(customizations, items);
        LoadCustomize(customizations, json["Customize"], ret, "Temporary Design", false, true);
        LoadEquip(items, json["Equipment"], ret, "Temporary Design", true);
        return ret;
    }

    protected static void LoadEquip(ItemManager items, JToken? equip, DesignBase design, string name, bool allowUnknown)
    {
        if (equip == null)
        {
            design._designData.SetDefaultEquipment(items);
            Glamourer.Messager.NotificationMessage("The loaded design does not contain any equipment data, reset to default.",
                NotificationType.Warning);
            return;
        }

        if (!design._designData.IsHuman)
        {
            var textArray = equip["Array"]?.ToObject<string>() ?? string.Empty;
            design._designData.SetEquipmentBytesFromBase64(textArray);
            return;
        }

        static (CustomItemId, StainId, bool, bool, bool, bool) ParseItem(EquipSlot slot, JToken? item)
        {
            var id         = item?["ItemId"]?.ToObject<ulong>() ?? ItemManager.NothingId(slot).Id;
            var stain      = (StainId)(item?["Stain"]?.ToObject<byte>() ?? 0);
            var crest      = item?["Crest"]?.ToObject<bool>() ?? false;
            var apply      = item?["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = item?["ApplyStain"]?.ToObject<bool>() ?? false;
            var applyCrest = item?["ApplyCrest"]?.ToObject<bool>() ?? false;
            return (id, stain, crest, apply, applyStain, applyCrest);
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0 && name != "Temporary Design")
                Glamourer.Messager.NotificationMessage($"{msg} ({name})", NotificationType.Warning);
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stain, crest, apply, applyStain, applyCrest) = ParseItem(slot, equip[slot.ToString()]);

            PrintWarning(items.ValidateItem(slot, id, out var item, allowUnknown));
            PrintWarning(items.ValidateStain(stain, out stain, allowUnknown));
            var crestSlot = slot.ToCrestFlag();
            design._designData.SetItem(slot, item);
            design._designData.SetStain(slot, stain);
            design._designData.SetCrest(crestSlot, crest);
            design.SetApplyEquip(slot, apply);
            design.SetApplyStain(slot, applyStain);
            design.SetApplyCrest(crestSlot, applyCrest);
        }

        {
            var (id, stain, crest, apply, applyStain, applyCrest) = ParseItem(EquipSlot.MainHand, equip[EquipSlot.MainHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.MainHand))
                id = items.DefaultSword.ItemId;
            var (idOff, stainOff, crestOff, applyOff, applyStainOff, applyCrestOff) =
                ParseItem(EquipSlot.OffHand, equip[EquipSlot.OffHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.OffHand))
                id = ItemManager.NothingId(FullEquipType.Shield);

            PrintWarning(items.ValidateWeapons(id, idOff, out var main, out var off, allowUnknown));
            PrintWarning(items.ValidateStain(stain,    out stain,    allowUnknown));
            PrintWarning(items.ValidateStain(stainOff, out stainOff, allowUnknown));
            design._designData.SetItem(EquipSlot.MainHand, main);
            design._designData.SetItem(EquipSlot.OffHand,  off);
            design._designData.SetStain(EquipSlot.MainHand, stain);
            design._designData.SetStain(EquipSlot.OffHand,  stainOff);
            design._designData.SetCrest(CrestFlag.MainHand, crest);
            design._designData.SetCrest(CrestFlag.OffHand,  crestOff);
            design.SetApplyEquip(EquipSlot.MainHand, apply);
            design.SetApplyEquip(EquipSlot.OffHand,  applyOff);
            design.SetApplyStain(EquipSlot.MainHand, applyStain);
            design.SetApplyStain(EquipSlot.OffHand,  applyStainOff);
            design.SetApplyCrest(CrestFlag.MainHand, applyCrest);
            design.SetApplyCrest(CrestFlag.OffHand,  applyCrestOff);
        }
        var metaValue = QuadBool.FromJObject(equip["Hat"], "Show", "Apply", QuadBool.NullFalse);
        design.SetApplyHatVisible(metaValue.Enabled);
        design._designData.SetHatVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Weapon"], "Show", "Apply", QuadBool.NullFalse);
        design.SetApplyWeaponVisible(metaValue.Enabled);
        design._designData.SetWeaponVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Visor"], "IsToggled", "Apply", QuadBool.NullFalse);
        design.SetApplyVisorToggle(metaValue.Enabled);
        design._designData.SetVisor(metaValue.ForcedValue);
    }

    protected static void LoadCustomize(CustomizationService customizations, JToken? json, DesignBase design, string name, bool forbidNonHuman,
        bool allowUnknown)
    {
        if (json == null)
        {
            design._designData.ModelId = 0;
            design._designData.IsHuman = true;
            design.SetCustomize(customizations, Customize.Default);
            Glamourer.Messager.NotificationMessage("The loaded design does not contain any customization data, reset to default.",
                NotificationType.Warning);
            return;
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Messager.NotificationMessage(
                    $"{msg} ({name})\nThis change is not saved automatically. If you want this replacement to stick and the warning to stop appearing, please save the design manually once by changing something in it.",
                    NotificationType.Warning);
        }

        var wetness = QuadBool.FromJObject(json["Wetness"], "Value", "Apply", QuadBool.NullFalse);
        design._designData.SetIsWet(wetness.ForcedValue);
        design.SetApplyWetness(wetness.Enabled);

        design._designData.ModelId = json["ModelId"]?.ToObject<uint>() ?? 0;
        PrintWarning(customizations.ValidateModelId(design._designData.ModelId, out design._designData.ModelId,
            out design._designData.IsHuman));
        if (design._designData.ModelId != 0 && forbidNonHuman)
        {
            PrintWarning("Model IDs different from 0 are not currently allowed, reset model id to 0.");
            design._designData.ModelId = 0;
            design._designData.IsHuman = true;
        }
        else if (!design._designData.IsHuman)
        {
            var arrayText = json["Array"]?.ToObject<string>() ?? string.Empty;
            design._designData.Customize.LoadBase64(arrayText);
            design.CustomizationSet = design.SetCustomizationSet(customizations);
            return;
        }

        var race = (Race)(json[CustomizeIndex.Race.ToString()]?["Value"]?.ToObject<byte>() ?? 0);
        var clan = (SubRace)(json[CustomizeIndex.Clan.ToString()]?["Value"]?.ToObject<byte>() ?? 0);
        PrintWarning(customizations.ValidateClan(clan, race, out race, out clan));
        var gender = (Gender)((json[CustomizeIndex.Gender.ToString()]?["Value"]?.ToObject<byte>() ?? 0) + 1);
        PrintWarning(customizations.ValidateGender(race, gender, out gender));
        design._designData.Customize.Race   = race;
        design._designData.Customize.Clan   = clan;
        design._designData.Customize.Gender = gender;
        design.CustomizationSet             = design.SetCustomizationSet(customizations);
        design.SetApplyCustomize(CustomizeIndex.Race,   json[CustomizeIndex.Race.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Clan,   json[CustomizeIndex.Clan.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Gender, json[CustomizeIndex.Gender.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        var set = design.CustomizationSet;

        foreach (var idx in CustomizationExtensions.AllBasic)
        {
            var tok  = json[idx.ToString()];
            var data = (CustomizeValue)(tok?["Value"]?.ToObject<byte>() ?? 0);
            if (set.IsAvailable(idx))
                PrintWarning(CustomizationService.ValidateCustomizeValue(set, design._designData.Customize.Face, idx, data, out data,
                    allowUnknown));
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            design._designData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }
    }

    public void MigrateBase64(CustomizationService customize, ItemManager items, HumanModelList humans, string base64)
    {
        try
        {
            _designData = DesignBase64Migration.MigrateBase64(items, humans, base64, out var equipFlags, out var customizeFlags,
                out var writeProtected,
                out var applyHat, out var applyVisor, out var applyWeapon);
            ApplyEquip     = equipFlags;
            ApplyCustomize = customizeFlags;
            SetWriteProtected(writeProtected);
            SetApplyHatVisible(applyHat);
            SetApplyVisorToggle(applyVisor);
            SetApplyWeaponVisible(applyWeapon);
            SetApplyWetness(true);
            CustomizationSet = SetCustomizationSet(customize);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not parse Base64 design.", NotificationType.Error);
        }
    }

    #endregion
}
