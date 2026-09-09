using System.Text.Json;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.Services;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.GameData.DataContainers;
using Luna;

namespace Glamourer.Designs;

public class DesignBase : JsonObjectConversion.IJsonWritable<DesignBase>
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
    internal DesignBase(CustomizeService customize, in DesignData designData, EquipFlag equipFlags, CustomizeFlag customizeFlags,
        BonusItemFlag bonusFlags)
    {
        _designData           = designData;
        ApplyCustomize        = customizeFlags & CustomizeFlagExtensions.AllRelevant;
        Application.Equip     = equipFlags & EquipFlagExtensions.All;
        Application.BonusItem = bonusFlags & BonusExtensions.All;
        Application.Meta      = 0;
        CustomizeSet          = SetCustomizationSet(customize);
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

    public IEnumerable<string> FilteredItemNames
        => _designData.FilteredItemNames(Application.Equip, Application.BonusItem);

    internal FlagRestrictionResetter TemporarilyRestrictApplication(ApplicationCollection restrictions)
        => new(this, restrictions);

    internal readonly struct FlagRestrictionResetter : IDisposable
    {
        public static readonly FlagRestrictionResetter Nothing = default;

        private readonly DesignBase            _design;
        private readonly ApplicationCollection _oldFlags;
        private readonly bool                  _alive;

        public FlagRestrictionResetter(DesignBase d, ApplicationCollection restrictions)
        {
            _design             = d;
            _oldFlags           = d.Application;
            _design.Application = restrictions.Restrict(_oldFlags);
            _alive              = true;
        }

        public void Dispose()
        {
            if (_alive)
                _design.Application = _oldFlags;
        }
    }

    private CustomizeSet SetCustomizationSet(CustomizeService customize)
        => !_designData.IsHuman
            ? customize.Manager.GetSet(SubRace.Midlander,          Gender.Male)
            : customize.Manager.GetSet(_designData.Customize.Clan, _designData.Customize.Gender);

    #endregion

    #region Serialization

    public void Serialize(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteNumber("FileVersion"u8, FileVersion);
        SerializeEquipment(j);
        SerializeBonusItems(j);
        SerializeCustomize(j);
        SerializeParameters(j);
        SerializeMaterials(j);
        j.WriteEndObject();
    }

    protected void SerializeEquipment(Utf8JsonWriter j)
    {
        j.WriteStartObject("Equipment"u8);
        if (_designData.IsHuman)
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                var item      = _designData.Item(slot);
                var stains    = _designData.Stain(slot);
                var crestSlot = slot.ToCrestFlag();
                var crest     = _designData.Crest(crestSlot);
                j.WriteStartObject(slot.StringU8);
                j.WriteNumber("ItemId"u8, item.Id.Id);
                j.WriteIfNot("Crest"u8,      crest,                   false);
                j.WriteIfNot("Apply"u8,      DoApplyEquip(slot),      false);
                j.WriteIfNot("ApplyStain"u8, DoApplyStain(slot),      false);
                j.WriteIfNot("ApplyCrest"u8, DoApplyCrest(crestSlot), false);
                stains.AddToObject(j);
                j.WriteEndObject();
            }

            new QuadBool(_designData.IsHatVisible(),    DoApplyMeta(MetaIndex.HatState)).WriteJson(j, "Hat"u8, "Show"u8, "Apply"u8);
            new QuadBool(_designData.AreEarsVisible(),  DoApplyMeta(MetaIndex.EarState)).WriteJson(j, "VieraEars"u8, "Show"u8, "Apply"u8);
            new QuadBool(_designData.IsVisorToggled(),  DoApplyMeta(MetaIndex.VisorState)).WriteJson(j, "Visor"u8, "IsToggled"u8, "Apply"u8);
            new QuadBool(_designData.IsWeaponVisible(), DoApplyMeta(MetaIndex.WeaponState)).WriteJson(j, "Weapon"u8, "Show"u8, "Apply"u8);
        }
        else
        {
            j.WriteString("Array"u8, _designData.WriteEquipmentBytesBase64());
        }

        j.WriteEndObject();
    }

    protected void SerializeBonusItems(Utf8JsonWriter j)
    {
        j.WriteStartObject("Bonus"u8);
        foreach (var slot in BonusExtensions.AllFlags)
        {
            var item = _designData.BonusItem(slot);
            j.WriteStartObject(slot.StringU8);
            j.WriteNumber("BonusId"u8, item.Id.Id);
            j.WriteIfNot("Apply"u8, DoApplyBonusItem(slot), false);
            j.WriteEndObject();
        }

        j.WriteEndObject();
    }

    protected void SerializeCustomize(Utf8JsonWriter j)
    {
        j.WriteStartObject("Customize"u8);
        j.WriteIfNot("ModelId"u8, _designData.ModelId, 0u);

        var customize = _designData.Customize;
        if (_designData.IsHuman)
            foreach (var idx in CustomizeIndex.Values)
            {
                j.WriteStartObject(idx.StringU8);
                j.WriteNumber("Value"u8, customize[idx].Value);
                j.WriteIfNot("Apply"u8, Application.Customize.HasFlag(idx.ToFlag()), false);
                j.WriteEndObject();
            }
        else
            j.WriteString("Array"u8, customize.WriteBase64());

        new QuadBool(_designData.IsWet(), DoApplyMeta(MetaIndex.Wetness)).WriteJson(j, "Wetness"u8, "Value"u8, "Apply"u8);
        j.WriteEndObject();
    }

    protected void SerializeParameters(Utf8JsonWriter j)
    {
        j.WriteStartObject("Parameters"u8);

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
        {
            j.WriteStartObject(flag.StringU8);
            j.WriteNumber("Value"u8, DesignData.Parameters[flag][0]);
            j.WriteIfNot("Apply"u8, DoApplyParameter(flag), false);
            j.WriteEndObject();
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
        {
            j.WriteStartObject(flag.StringU8);
            j.WriteNumber("Percentage"u8, DesignData.Parameters[flag][0]);
            j.WriteIfNot("Apply"u8, DoApplyParameter(flag), false);
            j.WriteEndObject();
        }

        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
        {
            j.WriteStartObject(flag.StringU8);
            j.WriteNumber("Red"u8,   DesignData.Parameters[flag][0]);
            j.WriteNumber("Green"u8, DesignData.Parameters[flag][1]);
            j.WriteNumber("Blue"u8,  DesignData.Parameters[flag][2]);
            j.WriteIfNot("Apply"u8, DoApplyParameter(flag), false);
            j.WriteEndObject();
        }

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
        {
            j.WriteStartObject(flag.StringU8);
            j.WriteNumber("Red"u8,   DesignData.Parameters[flag][0]);
            j.WriteNumber("Green"u8, DesignData.Parameters[flag][1]);
            j.WriteNumber("Blue"u8,  DesignData.Parameters[flag][2]);
            j.WriteNumber("Alpha"u8, DesignData.Parameters[flag][3]);
            j.WriteIfNot("Apply"u8, DoApplyParameter(flag), false);
            j.WriteEndObject();
        }

        j.WriteEndObject();
    }

    protected void SerializeMaterials(Utf8JsonWriter j)
    {
        if (Materials.Count is 0)
            return;

        j.WriteStartObject("Materials"u8);
        foreach (var (key, value) in Materials)
        {
            j.WritePropertyName($"{key:X8}");
            value.WriteJson(j);
        }

        j.WriteEndObject();
    }

    #endregion

    #region Deserialization

    public static DesignBase LoadDesignBase(CustomizeService customizations, ItemManager items, in JsonElement json)
    {
        var version = json.PropertyOrDefault("FileVersion"u8, 0);
        return version switch
        {
            FileVersion => LoadDesignV1Base(customizations, items, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static DesignBase LoadDesignV1Base(CustomizeService customizations, ItemManager items, in JsonElement json)
    {
        var ret = new DesignBase(customizations, items);
        LoadCustomize(customizations, json.TryReadObject("Customize"u8, out var c) ? c : null, ret, "Temporary Design", false, true);
        LoadEquip(items, json.TryReadObject("Equipment"u8,              out var e) ? e : null, ret, "Temporary Design", true);
        LoadParameters(json.TryReadObject("Parameters"u8,               out var p) ? p : null, ret, "Temporary Design");
        LoadMaterials(json.TryReadObject("Materials"u8,                 out var m) ? m : null, ret, "Temporary Design");
        LoadBonus(items, ret, json.TryReadObject("Bonus"u8,             out var b) ? b : null);
        return ret;
    }

    protected static void LoadBonus(ItemManager items, DesignBase design, in JsonElement? json)
    {
        if (json is not { } j)
        {
            design.Application.BonusItem = 0;
            return;
        }

        foreach (var slot in BonusExtensions.AllFlags)
        {
            if (!j.TryReadObject(slot.StringU8, out var itemJson))
            {
                design.Application.BonusItem &= ~slot;
                design.GetDesignDataRef().SetBonusItem(slot, EquipItem.BonusItemNothing(slot));
                continue;
            }

            design.SetApplyBonusItem(slot, itemJson.PropertyOrDefault("Apply"u8, false));
            var id   = itemJson.PropertyOrDefault("BonusId"u8,                   0UL);
            var item = items.Resolve(slot, id);
            design.GetDesignDataRef().SetBonusItem(slot, item);
        }
    }

    protected static void LoadParameters(in JsonElement? json, DesignBase design, string name)
    {
        if (json is not { } j)
        {
            design.Application.Parameters        = 0;
            design.GetDesignDataRef().Parameters = default;
            return;
        }

        foreach (var flag in CustomizeParameterExtensions.ValueFlags)
        {
            if (!TryGetElement(flag, out var element))
                continue;

            var value = element.PropertyOrDefault("Value"u8, 0f);
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(value);
        }

        foreach (var flag in CustomizeParameterExtensions.PercentageFlags)
        {
            if (!TryGetElement(flag, out var element))
                continue;

            var value = element.PropertyOrDefault("Percentage"u8, 0f);
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(value);
        }

        foreach (var flag in CustomizeParameterExtensions.RgbFlags)
        {
            if (!TryGetElement(flag, out var element))
                continue;

            var r = element.PropertyOrDefault("Red"u8,   0f);
            var g = element.PropertyOrDefault("Green"u8, 0f);
            var b = element.PropertyOrDefault("Blue"u8,  0f);
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(r, g, b);
        }

        foreach (var flag in CustomizeParameterExtensions.RgbaFlags)
        {
            if (!TryGetElement(flag, out var element))
                continue;

            var r = element.PropertyOrDefault("Red"u8,   0f);
            var g = element.PropertyOrDefault("Green"u8, 0f);
            var b = element.PropertyOrDefault("Blue"u8,  0f);
            var a = element.PropertyOrDefault("Alpha"u8, 0f);
            design.GetDesignDataRef().Parameters[flag] = new CustomizeParameterValue(r, g, b, a);
        }

        MigrateLipOpacity();
        return;

        // Load the token and set application.
        bool TryGetElement(CustomizeParameterFlag flag, out JsonElement sub)
        {
            if (!j.TryReadObject(flag.StringU8, out sub))
            {
                design.Application.Parameters              &= ~flag;
                design.GetDesignDataRef().Parameters[flag] =  CustomizeParameterValue.Zero;
                return false;
            }

            var apply = sub.PropertyOrDefault("Apply"u8, false);
            design.SetApplyParameter(flag, apply);
            return true;
        }

        void MigrateLipOpacity()
        {
            float? token =
                j.TryReadObject("LipOpacity"u8, out var lip)
             && lip.TryGetProperty("Percentage"u8, out var lipPercentage)
             && lipPercentage.ValueKind is JsonValueKind.Number
             && lipPercentage.TryGetSingle(out var t)
                    ? t
                    : null;
            float? actualToken =
                j.TryReadObject(CustomizeParameterFlag.LipDiffuse.StringU8, out var diff)
             && diff.TryGetProperty("Alpha"u8, out var lipDiff)
             && lipDiff.ValueKind is JsonValueKind.Number
             && lipDiff.TryGetSingle(out var d)
                    ? d
                    : null;
            if (token is not null && actualToken is null)
                design.GetDesignDataRef().Parameters.LipDiffuse.W = token.Value;
        }
    }

    protected static void LoadEquip(ItemManager items, in JsonElement? equip, DesignBase design, string name, bool allowUnknown)
    {
        if (equip is not { } j)
        {
            design._designData.SetDefaultEquipment(items);
            Glamourer.Messager.NotificationMessage("The loaded design does not contain any equipment data, reset to default.",
                NotificationType.Warning);
            return;
        }

        if (!design._designData.IsHuman)
        {
            var textArray = j.PropertyOrDefault("Array"u8, string.Empty);
            design._designData.SetEquipmentBytesFromBase64(textArray);
            return;
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stains, crest, apply, applyStain, applyCrest) = ParseItem(slot, j.TryReadObject(slot.StringU8, out var o) ? o : null);

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
            var (id, stains, crest, apply, applyStain, applyCrest) =
                ParseItem(EquipSlot.MainHand, j.TryReadObject(EquipSlot.MainHand.StringU8, out var m) ? m : null);
            if (id == ItemManager.NothingId(EquipSlot.MainHand))
                id = items.DefaultSword.ItemId;
            var (idOff, stainsOff, crestOff, applyOff, applyStainOff, applyCrestOff) =
                ParseItem(EquipSlot.OffHand, j.TryReadObject(EquipSlot.OffHand.StringU8, out var o) ? o : null);
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
        var metaValue = QuadBool.FromJsonElement(j.TryReadObject("Hat"u8, out var h) ? h : null, "Show"u8, "Apply"u8, QuadBool.NullFalse);
        design.SetApplyMeta(MetaIndex.HatState, metaValue.Set);
        design._designData.SetHatVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJsonElement(j.TryReadObject("Weapon"u8, out var w) ? w : null, "Show"u8, "Apply"u8, QuadBool.NullFalse);
        design.SetApplyMeta(MetaIndex.WeaponState, metaValue.Set);
        design._designData.SetWeaponVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJsonElement(j.TryReadObject("Visor"u8, out var v) ? v : null, "IsToggled"u8, "Apply"u8, QuadBool.NullFalse);
        design.SetApplyMeta(MetaIndex.VisorState, metaValue.Set);
        design._designData.SetVisor(metaValue.ForcedValue);

        metaValue = QuadBool.FromJsonElement(j.TryReadObject("VieraEars"u8, out var e) ? e : null, "Show"u8, "Apply"u8, QuadBool.NullTrue);
        design.SetApplyMeta(MetaIndex.EarState, metaValue.Set);
        design._designData.SetEarsVisible(metaValue.ForcedValue);
        return;

        void PrintWarning(string msg)
        {
            if (msg.Length > 0 && name != "Temporary Design")
                Glamourer.Messager.NotificationMessage($"{msg} ({name})", NotificationType.Warning);
        }

        static (CustomItemId, StainIds, bool, bool, bool, bool) ParseItem(EquipSlot slot, in JsonElement? item)
        {
            var ret = ((CustomItemId)ItemManager.NothingId(slot).Id, StainIds.None, false, false, false, false);
            if (item is not { } j)
                return ret;

            ret.Item1 = j.PropertyOrDefault("ItemId"u8, ret.Item1.Id);
            ret.Item2 = StainIds.ParseFromElement(j);
            ret.Item3 = j.PropertyOrDefault("Crest"u8,      false);
            ret.Item4 = j.PropertyOrDefault("Apply"u8,      false);
            ret.Item5 = j.PropertyOrDefault("ApplyStain"u8, false);
            ret.Item6 = j.PropertyOrDefault("ApplyCrest"u8, false);
            return ret;
        }
    }

    protected static void LoadCustomize(CustomizeService customizations, in JsonElement? json, DesignBase design, string name,
        bool forbidNonHuman, bool allowUnknown)
    {
        if (json is not { } j)
        {
            design._designData.ModelId = 0;
            design._designData.IsHuman = true;
            design.SetCustomize(customizations, CustomizeArray.Default);
            Glamourer.Messager.NotificationMessage("The loaded design does not contain any customization data, reset to default.",
                NotificationType.Warning);
            return;
        }

        var wetness = QuadBool.FromJsonElement(j.TryReadObject("Wetness"u8, out var w) ? w : null, "Value"u8, "Apply"u8, QuadBool.NullFalse);
        design._designData.SetIsWet(wetness.ForcedValue);
        design.SetApplyMeta(MetaIndex.Wetness, wetness.Set);

        design._designData.ModelId = j.PropertyOrDefault("ModelId"u8, 0u);
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
            var arrayText = j.PropertyOrDefault("Array"u8, string.Empty);
            design._designData.Customize.LoadBase64(arrayText);
            design.CustomizeSet = design.SetCustomizationSet(customizations);
            return;
        }

        var race = (Race)(j.TryReadObject(CustomizeIndex.Race.StringU8,    out var r) ? r.PropertyOrDefault("Value"u8, (byte)0) : (byte)0);
        var clan = (SubRace)(j.TryReadObject(CustomizeIndex.Clan.StringU8, out var c) ? c.PropertyOrDefault("Value"u8, (byte)0) : (byte)0);
        PrintWarning(customizations.ValidateClan(clan, race, out race, out clan));
        var gender = (Gender)((j.TryReadObject(CustomizeIndex.Gender.StringU8, out var g) ? g.PropertyOrDefault("Value"u8, (byte)0) : 0) + 1);
        PrintWarning(customizations.ValidateGender(race, gender, out gender));
        var bodyType = (CustomizeValue)(j.TryReadObject(CustomizeIndex.BodyType.StringU8, out var b)
            ? b.PropertyOrDefault("Value"u8, (byte)1)
            : (byte)1);
        design._designData.Customize.Race     = race;
        design._designData.Customize.Clan     = clan;
        design._designData.Customize.Gender   = gender;
        design._designData.Customize.BodyType = bodyType;
        design.CustomizeSet                   = design.SetCustomizationSet(customizations);
        design.SetApplyCustomize(CustomizeIndex.Race,
            j.TryReadObject(CustomizeIndex.Race.StringU8, out var r2) && r2.PropertyOrDefault("Apply"u8, false));
        design.SetApplyCustomize(CustomizeIndex.Clan,
            j.TryReadObject(CustomizeIndex.Clan.StringU8, out var c2) && c2.PropertyOrDefault("Apply"u8, false));
        design.SetApplyCustomize(CustomizeIndex.Gender,
            j.TryReadObject(CustomizeIndex.Gender.StringU8, out var g2) && g2.PropertyOrDefault("Apply"u8, false));
        design.SetApplyCustomize(CustomizeIndex.BodyType, bodyType.Value is not 0);
        var set = design.CustomizeSet;

        foreach (var idx in CustomizationExtensions.AllBasic)
        {
            var data  = CustomizeValue.Zero;
            var apply = false;

            if (j.TryReadObject(idx.StringU8, out var token))
            {
                data  = (CustomizeValue)token.PropertyOrDefault("Value"u8, (byte)0);
                apply = token.PropertyOrDefault("Apply"u8, false);
            }

            if (set.IsAvailable(idx) && design._designData.Customize.BodyType == 1)
                PrintWarning(CustomizeService.ValidateCustomizeValue(set, design._designData.Customize.Face, idx, data, out data,
                    allowUnknown));
            design._designData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }

        return;

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Messager.NotificationMessage(
                    $"{msg} ({name})\nThis change is not saved automatically. If you want this replacement to stick and the warning to stop appearing, please save the design manually once by changing something in it.",
                    NotificationType.Warning);
        }
    }

    protected static void LoadMaterials(in JsonElement? materials, DesignBase design, string name)
    {
        if (materials is not { } j)
            return;

        design.GetMaterialDataRef().Clear();
        foreach (var property in j.EnumerateObject())
        {
            try
            {
                var k = uint.Parse(property.Name, NumberStyles.HexNumber);
                var v = property.Value.Deserialize<MaterialValueDesign>();
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

    public void MigrateBase64Data(CustomizeService customize, ItemManager items, HumanModelList humans, ReadOnlySpan<byte> bytes)
    {
        try
        {
            _designData = DesignBase64Migration.MigrateBase64(items, humans, bytes, out var equipFlags, out var customizeFlags,
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

    public static void Write(Utf8JsonWriter writer, in DesignBase value, JsonSerializerOptions options)
        => value.Serialize(writer);
}
