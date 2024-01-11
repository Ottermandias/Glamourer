using Dalamud.Interface.Internal.Notifications;
using Glamourer.GameData;
using Glamourer.Services;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
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

    internal DesignBase(CustomizeService customize, ItemManager items)
    {
        _designData.SetDefaultEquipment(items);
        CustomizeSet = SetCustomizationSet(customize);
    }

    internal DesignBase(CustomizeService customize, in DesignData designData, EquipFlag equipFlags, CustomizeFlag customizeFlags)
    {
        _designData    = designData;
        ApplyCustomize = customizeFlags & CustomizeFlagExtensions.AllRelevant;
        ApplyEquip     = equipFlags & EquipFlagExtensions.All;
        _designFlags   = 0;
        CustomizeSet   = SetCustomizationSet(customize);
    }

    internal DesignBase(DesignBase clone)
    {
        _designData    = clone._designData;
        CustomizeSet   = clone.CustomizeSet;
        ApplyCustomize = clone.ApplyCustomizeRaw;
        ApplyEquip     = clone.ApplyEquip & EquipFlagExtensions.All;
        _designFlags   = clone._designFlags & (DesignFlags)0x0F;
    }

    /// <summary> Ensure that the customization set is updated when the design data changes. </summary>
    internal void SetDesignData(CustomizeService customize, in DesignData other)
    {
        _designData  = other;
        CustomizeSet = SetCustomizationSet(customize);
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

    private CustomizeFlag _applyCustomize = CustomizeFlagExtensions.AllRelevant;
    public  CustomizeSet  CustomizeSet { get; private set; }

    public CustomizeParameterFlag ApplyParameters { get; internal set; }

    internal CustomizeFlag ApplyCustomize
    {
        get => _applyCustomize.FixApplication(CustomizeSet);
        set => _applyCustomize = (value & CustomizeFlagExtensions.AllRelevant) | CustomizeFlag.BodyType;
    }

    internal CustomizeFlag ApplyCustomizeExcludingBodyType
        => _applyCustomize.FixApplication(CustomizeSet) & ~CustomizeFlag.BodyType;

    internal CustomizeFlag ApplyCustomizeRaw
        => _applyCustomize;

    internal EquipFlag   ApplyEquip   = EquipFlagExtensions.All;
    internal CrestFlag   ApplyCrest   = CrestExtensions.AllRelevant;
    private  DesignFlags _designFlags = DesignFlags.ApplyHatVisible | DesignFlags.ApplyVisorState | DesignFlags.ApplyWeaponVisible;

    public bool SetCustomize(CustomizeService customizeService, CustomizeArray customize)
    {
        if (customize.Equals(_designData.Customize))
            return false;

        _designData.Customize = customize;
        CustomizeSet          = customizeService.Manager.GetSet(customize.Clan, customize.Gender);
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

    public bool DoApplyParameter(CustomizeParameterFlag flag)
        => ApplyParameters.HasFlag(flag);

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

    internal bool SetApplyParameter(CustomizeParameterFlag flag, bool value)
    {
        var newValue = value ? ApplyParameters | flag : ApplyParameters & ~flag;
        if (newValue == ApplyParameters)
            return false;

        ApplyParameters = newValue;
        return true;
    }

    internal FlagRestrictionResetter TemporarilyRestrictApplication(EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags,
        CustomizeParameterFlag parameterFlags)
        => new(this, equipFlags, customizeFlags, crestFlags, parameterFlags);

    internal readonly struct FlagRestrictionResetter : IDisposable
    {
        private readonly DesignBase             _design;
        private readonly EquipFlag              _oldEquipFlags;
        private readonly CustomizeFlag          _oldCustomizeFlags;
        private readonly CrestFlag              _oldCrestFlags;
        private readonly CustomizeParameterFlag _oldParameterFlags;

        public FlagRestrictionResetter(DesignBase d, EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags,
            CustomizeParameterFlag parameterFlags)
        {
            _design            =  d;
            _oldEquipFlags     =  d.ApplyEquip;
            _oldCustomizeFlags =  d.ApplyCustomizeRaw;
            _oldCrestFlags     =  d.ApplyCrest;
            _oldParameterFlags =  d.ApplyParameters;
            d.ApplyEquip       &= equipFlags;
            d.ApplyCustomize   &= customizeFlags;
            d.ApplyCrest       &= crestFlags;
            d.ApplyParameters  &= parameterFlags;
        }

        public void Dispose()
        {
            _design.ApplyEquip      = _oldEquipFlags;
            _design.ApplyCustomize  = _oldCustomizeFlags;
            _design.ApplyCrest      = _oldCrestFlags;
            _design.ApplyParameters = _oldParameterFlags;
        }
    }

    private CustomizeSet SetCustomizationSet(CustomizeService customize)
        => !_designData.IsHuman
            ? customize.Manager.GetSet(SubRace.Midlander,          Gender.Male)
            : customize.Manager.GetSet(_designData.Customize.Clan, _designData.Customize.Gender);

    #endregion

    #region Serialization

    public JObject JsonSerialize()
    {
        var ret = new JObject
        {
            ["FileVersion"] = FileVersion,
            ["Equipment"]   = SerializeEquipment(),
            ["Customize"]   = SerializeCustomize(),
            ["Parameters"]  = SerializeParameters(),
        };
        return ret;
    }

    protected JObject SerializeEquipment()
    {
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

    protected JObject SerializeParameters()
    {
        var ret = new JObject();

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
        {
            ret[flag.ToString()] = new JObject()
            {
                ["Value"] = DesignData.Parameters[flag][0],
                ["Apply"] = DoApplyParameter(flag),
            };
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
        {
            ret[flag.ToString()] = new JObject()
            {
                ["Percentage"] = DesignData.Parameters[flag][0],
                ["Apply"]      = DoApplyParameter(flag),
            };
        }

        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
        {
            ret[flag.ToString()] = new JObject()
            {
                ["Red"]   = DesignData.Parameters[flag][0],
                ["Green"] = DesignData.Parameters[flag][1],
                ["Blue"]  = DesignData.Parameters[flag][2],
                ["Apply"] = DoApplyParameter(flag),
            };
        }

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
        {
            ret[flag.ToString()] = new JObject()
            {
                ["Red"]   = DesignData.Parameters[flag][0],
                ["Green"] = DesignData.Parameters[flag][1],
                ["Blue"]  = DesignData.Parameters[flag][2],
                ["Alpha"] = DesignData.Parameters[flag][3],
                ["Apply"] = DoApplyParameter(flag),
            };
        }

        return ret;
    }

    #endregion

    #region Deserialization

    public static DesignBase LoadDesignBase(CustomizeService customizations, ItemManager items, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            FileVersion => LoadDesignV1Base(customizations, items, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static DesignBase LoadDesignV1Base(CustomizeService customizations, ItemManager items, JObject json)
    {
        var ret = new DesignBase(customizations, items);
        LoadCustomize(customizations, json["Customize"], ret, "Temporary Design", false, true);
        LoadEquip(items, json["Equipment"], ret, "Temporary Design", true);
        LoadParameters(json["Parameters"], ret, "Temporary Design");
        return ret;
    }

    protected static void LoadParameters(JToken? parameters, DesignBase design, string name)
    {
        if (parameters == null)
        {
            design.ApplyParameters               = 0;
            design.GetDesignDataRef().Parameters = default;
            return;
        }


        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
        {
            if (!TryGetToken(flag, out var token))
                continue;

            var value = token["Value"]?.ToObject<float>() ?? 0f;
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(value);
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
        {
            if (!TryGetToken(flag, out var token))
                continue;

            var value = token["Percentage"]?.ToObject<float>() ?? 0f;
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(value);
        }

        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
        {
            if (!TryGetToken(flag, out var token))
                continue;

            var r = token["Red"]?.ToObject<float>() ?? 0f;
            var g = token["Green"]?.ToObject<float>() ?? 0f;
            var b = token["Blue"]?.ToObject<float>() ?? 0f;
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(r, g, b);
        }

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
        {
            if (!TryGetToken(flag, out var token))
                continue;

            var r = token["Red"]?.ToObject<float>() ?? 0f;
            var g = token["Green"]?.ToObject<float>() ?? 0f;
            var b = token["Blue"]?.ToObject<float>() ?? 0f;
            var a = token["Alpha"]?.ToObject<float>() ?? 0f;
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(r, g, b, a);
        }

        MigrateLipOpacity();
        return;

        // Load the token and set application.
        bool TryGetToken(CustomizeParameterFlag flag, [NotNullWhen(true)] out JToken? token)
        {
            token = parameters![flag.ToString()];
            if (token != null)
            {
                var apply = token["Apply"]?.ToObject<bool>() ?? false;
                design.SetApplyParameter(flag, apply);
                return true;
            }

            design.ApplyParameters                     &= ~flag;
            design.GetDesignDataRef().Parameters[flag] =  CustomizeParameterValue.Zero;
            return false;
        }

        void MigrateLipOpacity()
        {
            var token       = parameters!["LipOpacity"]?["Percentage"]?.ToObject<float>();
            var actualToken = parameters![CustomizeParameterFlag.LipDiffuse]?["Alpha"];
            if (token != null && actualToken == null)
                design.GetDesignDataRef().Parameters.LipDiffuse.W = token.Value;
        }
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

    protected static void LoadCustomize(CustomizeService customizations, JToken? json, DesignBase design, string name, bool forbidNonHuman,
        bool allowUnknown)
    {
        if (json == null)
        {
            design._designData.ModelId = 0;
            design._designData.IsHuman = true;
            design.SetCustomize(customizations, CustomizeArray.Default);
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
            design.CustomizeSet = design.SetCustomizationSet(customizations);
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
        design.CustomizeSet                 = design.SetCustomizationSet(customizations);
        design.SetApplyCustomize(CustomizeIndex.Race,   json[CustomizeIndex.Race.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Clan,   json[CustomizeIndex.Clan.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Gender, json[CustomizeIndex.Gender.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        var set = design.CustomizeSet;

        foreach (var idx in CustomizationExtensions.AllBasic)
        {
            var tok  = json[idx.ToString()];
            var data = (CustomizeValue)(tok?["Value"]?.ToObject<byte>() ?? 0);
            if (set.IsAvailable(idx) && design._designData.Customize.BodyType == 1)
                PrintWarning(CustomizeService.ValidateCustomizeValue(set, design._designData.Customize.Face, idx, data, out data,
                    allowUnknown));
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            design._designData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }
    }

    public void MigrateBase64(CustomizeService customize, ItemManager items, HumanModelList humans, string base64)
    {
        try
        {
            _designData = DesignBase64Migration.MigrateBase64(items, humans, base64, out var equipFlags, out var customizeFlags,
                out var writeProtected,
                out var applyHat, out var applyVisor, out var applyWeapon);
            ApplyEquip      = equipFlags;
            ApplyCustomize  = customizeFlags;
            ApplyParameters = 0;
            ApplyCrest      = 0;
            SetWriteProtected(writeProtected);
            SetApplyHatVisible(applyHat);
            SetApplyVisorToggle(applyVisor);
            SetApplyWeaponVisible(applyWeapon);
            SetApplyWetness(true);
            CustomizeSet = SetCustomizationSet(customize);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not parse Base64 design.", NotificationType.Error);
        }
    }

    #endregion
}
