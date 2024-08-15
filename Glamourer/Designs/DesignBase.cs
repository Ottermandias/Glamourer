using Dalamud.Interface.ImGuiNotification;
using Glamourer.GameData;
using Glamourer.Interop.Material;
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

    private          DesignData            _designData = new();
    private readonly DesignMaterialManager _materials  = new();

    /// <summary> For read-only information about custom material color changes. </summary>
    public IReadOnlyList<(uint, MaterialValueDesign)> Materials
        => _materials.Values;

    /// <summary> To make it clear something is edited here. </summary>
    public DesignMaterialManager GetMaterialDataRef()
        => _materials;

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

    /// <summary> Used when importing .cma or .chara files. </summary>
    internal DesignBase(CustomizeService customize, in DesignData designData, EquipFlag equipFlags, CustomizeFlag customizeFlags)
    {
        _designData       = designData;
        ApplyCustomize    = customizeFlags & CustomizeFlagExtensions.AllRelevant;
        Application.Equip = equipFlags & EquipFlagExtensions.All;
        Application.Meta  = 0;
        CustomizeSet      = SetCustomizationSet(customize);
    }

    internal DesignBase(DesignBase clone)
    {
        _designData  = clone._designData;
        _materials   = clone._materials.Clone();
        CustomizeSet = clone.CustomizeSet;
        Application  = clone.Application.CloneSecure();
    }

    /// <summary> Ensure that the customization set is updated when the design data changes. </summary>
    internal void SetDesignData(CustomizeService customize, in DesignData other)
    {
        _designData  = other;
        CustomizeSet = SetCustomizationSet(customize);
    }

    #region Application Data

    public CustomizeSet CustomizeSet { get; private set; }

    public ApplicationCollection Application = ApplicationCollection.Default;

    internal CustomizeFlag ApplyCustomize
    {
        get => Application.Customize.FixApplication(CustomizeSet);
        set => Application.Customize = (value & CustomizeFlagExtensions.AllRelevant) | CustomizeFlag.BodyType;
    }

    internal CustomizeFlag ApplyCustomizeExcludingBodyType
        => Application.Customize.FixApplication(CustomizeSet) & ~CustomizeFlag.BodyType;

    private bool _writeProtected;

    public bool SetCustomize(CustomizeService customizeService, CustomizeArray customize)
    {
        if (customize.Equals(_designData.Customize))
            return false;

        _designData.Customize = customize;
        CustomizeSet          = customizeService.Manager.GetSet(customize.Clan, customize.Gender);
        return true;
    }

    public bool DoApplyMeta(MetaIndex index)
        => Application.Meta.HasFlag(index.ToFlag());

    public bool WriteProtected()
        => _writeProtected;

    public bool SetApplyMeta(MetaIndex index, bool value)
    {
        var newFlag = value ? Application.Meta | index.ToFlag() : Application.Meta & ~index.ToFlag();
        if (newFlag == Application.Meta)
            return false;

        Application.Meta = newFlag;
        return true;
    }

    public bool SetWriteProtected(bool value)
    {
        if (value == _writeProtected)
            return false;

        _writeProtected = value;
        return true;
    }

    public bool DoApplyEquip(EquipSlot slot)
        => Application.Equip.HasFlag(slot.ToFlag());

    public bool DoApplyStain(EquipSlot slot)
        => Application.Equip.HasFlag(slot.ToStainFlag());

    public bool DoApplyCustomize(CustomizeIndex idx)
        => Application.Customize.HasFlag(idx.ToFlag());

    public bool DoApplyCrest(CrestFlag slot)
        => Application.Crest.HasFlag(slot);

    public bool DoApplyParameter(CustomizeParameterFlag flag)
        => Application.Parameters.HasFlag(flag);

    public bool DoApplyBonusItem(BonusItemFlag slot)
        => Application.BonusItem.HasFlag(slot);

    internal bool SetApplyEquip(EquipSlot slot, bool value)
    {
        var newValue = value ? Application.Equip | slot.ToFlag() : Application.Equip & ~slot.ToFlag();
        if (newValue == Application.Equip)
            return false;

        Application.Equip = newValue;
        return true;
    }

    internal bool SetApplyBonusItem(BonusItemFlag slot, bool value)
    {
        var newValue = value ? Application.BonusItem | slot : Application.BonusItem & ~slot;
        if (newValue == Application.BonusItem)
            return false;

        Application.BonusItem = newValue;
        return true;
    }

    internal bool SetApplyStain(EquipSlot slot, bool value)
    {
        var newValue = value ? Application.Equip | slot.ToStainFlag() : Application.Equip & ~slot.ToStainFlag();
        if (newValue == Application.Equip)
            return false;

        Application.Equip = newValue;
        return true;
    }

    internal bool SetApplyCustomize(CustomizeIndex idx, bool value)
    {
        var newValue = value ? Application.Customize | idx.ToFlag() : Application.Customize & ~idx.ToFlag();
        if (newValue == Application.Customize)
            return false;

        Application.Customize = newValue;
        return true;
    }

    internal bool SetApplyCrest(CrestFlag slot, bool value)
    {
        var newValue = value ? Application.Crest | slot : Application.Crest & ~slot;
        if (newValue == Application.Crest)
            return false;

        Application.Crest = newValue;
        return true;
    }

    internal bool SetApplyParameter(CustomizeParameterFlag flag, bool value)
    {
        var newValue = value ? Application.Parameters | flag : Application.Parameters & ~flag;
        if (newValue == Application.Parameters)
            return false;

        Application.Parameters = newValue;
        return true;
    }

    internal FlagRestrictionResetter TemporarilyRestrictApplication(ApplicationCollection restrictions)
        => new(this, restrictions);

    internal readonly struct FlagRestrictionResetter : IDisposable
    {
        private readonly DesignBase            _design;
        private readonly ApplicationCollection _oldFlags;

        public FlagRestrictionResetter(DesignBase d, ApplicationCollection restrictions)
        {
            _design             = d;
            _oldFlags           = d.Application;
            _design.Application = restrictions.Restrict(_oldFlags);
        }

        public void Dispose()
            => _design.Application = _oldFlags;
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
            ["Bonus"]       = SerializeBonusItems(),
            ["Customize"]   = SerializeCustomize(),
            ["Parameters"]  = SerializeParameters(),
            ["Materials"]   = SerializeMaterials(),
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
                var stains    = _designData.Stain(slot);
                var crestSlot = slot.ToCrestFlag();
                var crest     = _designData.Crest(crestSlot);
                ret[slot.ToString()] = Serialize(item.Id, stains, crest, DoApplyEquip(slot), DoApplyStain(slot), DoApplyCrest(crestSlot));
            }

            ret["Hat"]    = new QuadBool(_designData.IsHatVisible(),    DoApplyMeta(MetaIndex.HatState)).ToJObject("Show", "Apply");
            ret["Visor"]  = new QuadBool(_designData.IsVisorToggled(),  DoApplyMeta(MetaIndex.VisorState)).ToJObject("IsToggled", "Apply");
            ret["Weapon"] = new QuadBool(_designData.IsWeaponVisible(), DoApplyMeta(MetaIndex.WeaponState)).ToJObject("Show", "Apply");
        }
        else
        {
            ret["Array"] = _designData.WriteEquipmentBytesBase64();
        }

        return ret;

        static JObject Serialize(CustomItemId id, StainIds stains, bool crest, bool apply, bool applyStain, bool applyCrest)
            => stains.AddToObject(new JObject
            {
                ["ItemId"]     = id.Id,
                ["Crest"]      = crest,
                ["Apply"]      = apply,
                ["ApplyStain"] = applyStain,
                ["ApplyCrest"] = applyCrest,
            });
    }

    protected JObject SerializeBonusItems()
    {
        var ret = new JObject();
        foreach (var slot in BonusExtensions.AllFlags)
        {
            var item = _designData.BonusItem(slot);
            ret[slot.ToString()] = new JObject()
            {
                ["BonusId"] = item.CustomId.Id,
                ["Apply"]   = DoApplyBonusItem(slot),
            };
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
                    ["Apply"] = Application.Customize.HasFlag(idx.ToFlag()),
                };
            }
        else
            ret["Array"] = customize.WriteBase64();

        ret["Wetness"] = new JObject()
        {
            ["Value"] = _designData.IsWet(),
            ["Apply"] = DoApplyMeta(MetaIndex.Wetness),
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

    protected JObject SerializeMaterials()
    {
        var ret = new JObject();
        foreach (var (key, value) in Materials)
            ret[key.ToString("X16")] = JToken.FromObject(value);
        return ret;
    }

    protected static void LoadMaterials(JToken? materials, DesignBase design, string name)
    {
        if (materials is not JObject obj)
            return;

        design.GetMaterialDataRef().Clear();
        foreach (var (key, value) in obj.Properties().Zip(obj.PropertyValues()))
        {
            try
            {
                var k = uint.Parse(key.Name, NumberStyles.HexNumber);
                var v = value.ToObject<MaterialValueDesign>();
                if (!MaterialValueIndex.FromKey(k, out _))
                {
                    Glamourer.Messager.NotificationMessage($"Invalid material value key {k} for design {name}, skipped.",
                        NotificationType.Warning);
                    continue;
                }

                if (!design.GetMaterialDataRef().TryAddValue(MaterialValueIndex.FromKey(k), v))
                    Glamourer.Messager.NotificationMessage($"Duplicate material value key {k} for design {name}, skipped.",
                        NotificationType.Warning);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Error parsing material value for design {name}, skipped",
                    NotificationType.Warning);
            }
        }
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
        LoadMaterials(json["Materials"], ret, "Temporary Design");
        LoadBonus(items, ret, json["Bonus"]);
        return ret;
    }

    protected static void LoadBonus(ItemManager items, DesignBase design, JToken? json)
    {
        if (json is not JObject)
        {
            design.Application.BonusItem = 0;
            return;
        }

        foreach (var slot in BonusExtensions.AllFlags)
        {
            if (json[slot.ToString()] is not JObject itemJson)
            {
                design.Application.BonusItem &= ~slot;
                design.GetDesignDataRef().SetBonusItem(slot, BonusItem.Empty(slot));
                continue;
            }

            design.SetApplyBonusItem(slot, itemJson["Apply"]?.ToObject<bool>() ?? false);
            var id   = itemJson["BonusId"]?.ToObject<ulong>() ?? 0;
            var item = items.Resolve(slot, id);
            design.GetDesignDataRef().SetBonusItem(slot, item);
        }
    }

    protected static void LoadParameters(JToken? parameters, DesignBase design, string name)
    {
        if (parameters == null)
        {
            design.Application.Parameters        = 0;
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
            token = parameters[flag.ToString()];
            if (token != null)
            {
                var apply = token["Apply"]?.ToObject<bool>() ?? false;
                design.SetApplyParameter(flag, apply);
                return true;
            }

            design.Application.Parameters              &= ~flag;
            design.GetDesignDataRef().Parameters[flag] =  CustomizeParameterValue.Zero;
            return false;
        }

        void MigrateLipOpacity()
        {
            var token       = parameters["LipOpacity"]?["Percentage"]?.ToObject<float>();
            var actualToken = parameters[CustomizeParameterFlag.LipDiffuse.ToString()]?["Alpha"];
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

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stains, crest, apply, applyStain, applyCrest) = ParseItem(slot, equip[slot.ToString()]);

            PrintWarning(items.ValidateItem(slot, id, out var item, allowUnknown));
            PrintWarning(items.ValidateStain(stains, out stains, allowUnknown));
            var crestSlot = slot.ToCrestFlag();
            design._designData.SetItem(slot, item);
            design._designData.SetStain(slot, stains);
            design._designData.SetCrest(crestSlot, crest);
            design.SetApplyEquip(slot, apply);
            design.SetApplyStain(slot, applyStain);
            design.SetApplyCrest(crestSlot, applyCrest);
        }

        {
            var (id, stains, crest, apply, applyStain, applyCrest) = ParseItem(EquipSlot.MainHand, equip[EquipSlot.MainHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.MainHand))
                id = items.DefaultSword.ItemId;
            var (idOff, stainsOff, crestOff, applyOff, applyStainOff, applyCrestOff) =
                ParseItem(EquipSlot.OffHand, equip[EquipSlot.OffHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.OffHand))
                id = ItemManager.NothingId(FullEquipType.Shield);

            PrintWarning(items.ValidateWeapons(id, idOff, out var main, out var off, allowUnknown));
            PrintWarning(items.ValidateStain(stains,    out stains,    allowUnknown));
            PrintWarning(items.ValidateStain(stainsOff, out stainsOff, allowUnknown));
            design._designData.SetItem(EquipSlot.MainHand, main);
            design._designData.SetItem(EquipSlot.OffHand,  off);
            design._designData.SetStain(EquipSlot.MainHand, stains);
            design._designData.SetStain(EquipSlot.OffHand,  stainsOff);
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
        design.SetApplyMeta(MetaIndex.HatState, metaValue.Enabled);
        design._designData.SetHatVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Weapon"], "Show", "Apply", QuadBool.NullFalse);
        design.SetApplyMeta(MetaIndex.WeaponState, metaValue.Enabled);
        design._designData.SetWeaponVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Visor"], "IsToggled", "Apply", QuadBool.NullFalse);
        design.SetApplyMeta(MetaIndex.VisorState, metaValue.Enabled);
        design._designData.SetVisor(metaValue.ForcedValue);
        return;

        void PrintWarning(string msg)
        {
            if (msg.Length > 0 && name != "Temporary Design")
                Glamourer.Messager.NotificationMessage($"{msg} ({name})", NotificationType.Warning);
        }

        static (CustomItemId, StainIds, bool, bool, bool, bool) ParseItem(EquipSlot slot, JToken? item)
        {
            var id         = item?["ItemId"]?.ToObject<ulong>() ?? ItemManager.NothingId(slot).Id;
            var stains     = StainIds.ParseFromObject(item as JObject);
            var crest      = item?["Crest"]?.ToObject<bool>() ?? false;
            var apply      = item?["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = item?["ApplyStain"]?.ToObject<bool>() ?? false;
            var applyCrest = item?["ApplyCrest"]?.ToObject<bool>() ?? false;
            return (id, stains, crest, apply, applyStain, applyCrest);
        }
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
        design.SetApplyMeta(MetaIndex.Wetness, wetness.Enabled);

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
        var bodyType = (CustomizeValue)(json[CustomizeIndex.BodyType.ToString()]?["Value"]?.ToObject<byte>() ?? 1);
        design._designData.Customize.Race     = race;
        design._designData.Customize.Clan     = clan;
        design._designData.Customize.Gender   = gender;
        design._designData.Customize.BodyType = bodyType;
        design.CustomizeSet                   = design.SetCustomizationSet(customizations);
        design.SetApplyCustomize(CustomizeIndex.Race,     json[CustomizeIndex.Race.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Clan,     json[CustomizeIndex.Clan.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Gender,   json[CustomizeIndex.Gender.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.BodyType, bodyType != 0);
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
                out var writeProtected, out var applyMeta);
            Application.Equip      = equipFlags;
            ApplyCustomize         = customizeFlags;
            Application.Parameters = 0;
            Application.Crest      = 0;
            Application.Meta       = applyMeta;
            Application.BonusItem  = 0;
            SetWriteProtected(writeProtected);
            CustomizeSet = SetCustomizationSet(customize);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, "Could not parse Base64 design.", NotificationType.Error);
        }
    }

    #endregion
}
