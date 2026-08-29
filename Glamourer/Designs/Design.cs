using System.Text.Json;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Automation;
using Glamourer.Designs.Links;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.GameData.Structs;
using Luna;
using Penumbra.Api.Preset;
using Penumbra.GameData.Enums;
using Notification = Luna.Notification;

namespace Glamourer.Designs;

public sealed class Design : DesignBase, ISavable, IDesignStandIn, IFileSystemValue<Design>, JsonObjectConversion.IJsonWritable<Design>
{
    #region Data

    internal Design(CustomizeService customize, ItemManager items)
        : base(customize, items)
    { }

    internal Design(DesignBase other)
        : base(other)
    { }

    internal Design(Design other)
        : base(other)
    {
        Tags                   = [.. other.Tags];
        Description            = other.Description;
        QuickDesign            = other.QuickDesign;
        ForcedRedraw           = other.ForcedRedraw;
        ResetAdvancedDyes      = other.ResetAdvancedDyes;
        ResetTemporarySettings = other.ResetTemporarySettings;
        RevertAdvancedDyes     = other.RevertAdvancedDyes;
        Color                  = other.Color;
        AssociatedMods         = new SortedList<ModIdentifier, SettingPresetData>(other.AssociatedMods, ModIdentifierComparer.Instance);
        Links                  = Links.Clone();
    }

    // Metadata
    public new const int FileVersion = 3;

    public Guid                     Identifier             { get; internal init; }
    public IFileSystemData<Design>? Node                   { get; set; }
    public DateTimeOffset           CreationDate           { get; internal init; }
    public DateTimeOffset           LastEdit               { get; internal set; }
    public string                   Name                   { get; internal set; } = string.Empty;
    public string                   Description            { get; internal set; } = string.Empty;
    public string[]                 Tags                   { get; internal set; } = [];
    public int                      Index                  { get; internal set; }
    public bool                     ForcedRedraw           { get; internal set; }
    public ModelCombinedSlots       ResetAdvancedDyes      { get; internal set; }
    public ModelCombinedSlots       RevertAdvancedDyes     { get; internal set; }
    public bool                     ResetTemporarySettings { get; internal set; }
    public bool                     QuickDesign            { get; internal set; } = true;
    public string                   Color                  { get; internal set; } = string.Empty;
    public LinkContainer            Links                  { get; private set; }  = [];
    public DataPath                 Path                   { get; }               = new();

    public SortedList<ModIdentifier, SettingPresetData> AssociatedMods { get; private set; } = new(ModIdentifierComparer.Instance);

    public string Incognito
        => Identifier.ToString()[..8];

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool newApplication,
        Predicate<DesignConditions>? condition)
        => LinkContainer.GetAllLinks(this, condition).Select(t => ((IDesignStandIn)t.Link.Link, t.Link.Type, JobFlag.All));

    #endregion

    #region IDesignStandIn

    public string ResolveName(bool incognito)
        => incognito ? Incognito : Name;

    public string SerializeName()
        => Identifier.ToString();

    public ref readonly DesignData GetDesignData(in DesignData baseData)
        => ref GetDesignDataRef();

    public IReadOnlyList<(uint, MaterialValueDesign)> GetMaterialData()
        => Materials;

    public bool Equals(IDesignStandIn? other)
        => other is Design d && d.Identifier == Identifier;

    public StateSource AssociatedSource()
        => StateSource.Manual;

    public void AddData(Utf8JsonWriter _)
    { }

    public void ParseData(in JsonElement _)
    { }

    public bool ChangeData(object data)
        => false;

    #endregion

    #region Serialization

    public new void Serialize(Utf8JsonWriter j)
    {
        j.WriteStartObject();
        j.WriteNumber("FileVersion"u8, FileVersion);
        j.WriteString("Identifier"u8, Identifier);
        j.WriteNumber("CreationDate"u8, CreationDate.ToUnixTimeMilliseconds());
        j.WriteNumber("LastEdit"u8,     LastEdit.ToUnixTimeMilliseconds());
        j.WriteString("Name"u8, Name);
        j.WriteNonEmptyString("Description"u8, Description);
        j.WriteIfNot("ForcedRedraw"u8,           ForcedRedraw,           false);
        j.WriteIfNot("ResetTemporarySettings"u8, ResetTemporarySettings, false);
        SerializeCombinedSlots(j, "ResetAdvancedDyes"u8,  ResetAdvancedDyes);
        SerializeCombinedSlots(j, "RevertAdvancedDyes"u8, RevertAdvancedDyes);
        j.WriteNonEmptyString("Color"u8, Color);
        j.WriteIfNot("QuickDesign"u8,    QuickDesign,      true);
        j.WriteIfNot("WriteProtected"u8, WriteProtected(), false);
        j.WriteNonEmptyString("FileSystemFolder"u8, Path.Folder);
        j.WriteNonEmptyString("SortOrderName"u8,    Path.SortName);
        if (Tags.Length > 0)
        {
            j.WriteStartArray("Tags"u8);
            foreach (var tag in Tags)
                j.WriteStringValue(tag);
            j.WriteEndArray();
        }

        SerializeEquipment(j);
        SerializeBonusItems(j);
        SerializeCustomize(j);
        SerializeParameters(j);
        SerializeMaterials(j);
        SerializeMods(j);
        Links.Serialize(j, "Links"u8);
        j.WriteEndObject();
    }

    private void SerializeMods(Utf8JsonWriter j)
    {
        if (AssociatedMods.Count is 0)
            return;

        j.WriteStartArray("Mods"u8);
        foreach (var (mod, settings) in AssociatedMods)
        {
            j.WriteStartObject();
            j.WriteNonEmptyString("Name"u8,      mod.Name);
            j.WriteNonEmptyString("Directory"u8, mod.Identifier);
            settings.WriteJsonProperties(j);
            j.WriteEndObject();
        }

        j.WriteEndArray();
    }

    private static void SerializeCombinedSlots(Utf8JsonWriter j, ReadOnlySpan<byte> property, ModelCombinedSlots slots)
    {
        if (slots is 0)
            return;

        j.WritePropertyName(property);
        // If all the existing slots are included in the mask, save the intent of "All",
        // so that future slots get included as well on deserialization.
        if (slots.HasFlag(ModelCombinedSlotsExtensions.All))
        {
            j.WriteBooleanValue(true);
        }
        // If we only have AllEquipmentPieces (or a subset), this format is not ambiguous.
        else if ((slots & ~ModelCombinedSlotsExtensions.AllEquipmentPieces) is 0)
        {
            j.WriteNumberValue((ulong)slots);
        }
        else
        {
            j.WriteStartObject();
            j.WriteUnsignedIfNot("Human"u8,    unchecked((uint)slots),                0u);
            j.WriteUnsignedIfNot("Mainhand"u8, unchecked((byte)((ulong)slots >> 32)), 0u);
            j.WriteUnsignedIfNot("Offhand"u8,  unchecked((byte)((ulong)slots >> 40)), 0u);
            j.WriteEndObject();
        }
    }

    #endregion

    #region Deserialization

    public static Design LoadDesign(SaveService saveService, CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader,
        in JsonElement json)
    {
        var version = json.PropertyOrDefault("FileVersion"u8, 0);
        return version switch
        {
            1           => LoadDesignV1(saveService, customizations, items, linkLoader, json),
            2           => LoadDesignV2Or3(customizations, items, linkLoader, json, version),
            FileVersion => LoadDesignV2Or3(customizations, items, linkLoader, json, version),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    /// <summary> The values for gloss and specular strength were switched. Swap them for all appropriate designs. </summary>
    private static Design LoadDesignV1(SaveService saveService, CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader,
        in JsonElement json)
    {
        var design             = LoadDesignV2Or3(customizations, items, linkLoader, json, 2);
        var materialDesignData = design.GetMaterialDataRef();
        if (materialDesignData.Values.Count is 0)
            return design;

        var materialData = materialDesignData.Clone();
        // Guesstimate whether to migrate material rows:
        // Update 1.3.0.10 released at that time, so any design last updated before that can be migrated.
        if (design.LastEdit <= new DateTime(2024, 8, 7, 16, 0, 0, DateTimeKind.Utc))
        {
            Migrate("because it was saved the wrong way around before 1.3.0.10, and this design was not changed since that release.");
        }
        else
        {
            var hasNegativeGloss    = false;
            var hasNonPositiveGloss = false;
            var specularLarger      = 0;
            foreach (var (_, value) in materialData.GetValues(MaterialValueIndex.Min(), MaterialValueIndex.Max()))
            {
                hasNegativeGloss    |= value.Value.GlossStrength < 0;
                hasNonPositiveGloss |= value.Value.GlossStrength <= 0;
                if (value.Value.SpecularStrength > value.Value.GlossStrength)
                    ++specularLarger;
            }

            // If there is any negative gloss, this is wrong and can be migrated.
            if (hasNegativeGloss)
                Migrate("because it had a negative Gloss value, which is not supported and thus probably outdated.");
            // If there is any non-positive Gloss and some specular values that are larger, it is probably wrong and can be migrated.
            else if (hasNonPositiveGloss && specularLarger > 0)
                Migrate("because it had a zero Gloss value, and at least one Specular Strength larger than the Gloss, which is unusual.");
            // If most of the specular strengths are larger, it is probably wrong and can be migrated.
            else if (specularLarger > materialData.Values.Count / 2)
                Migrate("because most of its Specular Strength values were larger than the Gloss values, which is unusual.");
        }

        return design;

        void Migrate(string reason)
        {
            materialDesignData.Clear();
            foreach (var (key, value) in materialData.GetValues(MaterialValueIndex.Min(), MaterialValueIndex.Max()))
            {
                var gloss            = Math.Clamp(value.Value.SpecularStrength, 0, (float)Half.MaxValue);
                var specularStrength = Math.Clamp(value.Value.GlossStrength,    0, (float)Half.MaxValue);
                var colorRow = value.Value with
                {
                    GlossStrength = gloss,
                    SpecularStrength = specularStrength,
                };
                materialDesignData.AddOrUpdateValue(MaterialValueIndex.FromKey(key), value with { Value = colorRow });
            }

            Glamourer.Messager.AddMessage(new Notification(
                $"Swapped Gloss and Specular Strength in {materialDesignData.Values.Count} Rows in design {design.Incognito} {reason}",
                NotificationType.Info));
            saveService.Save(SaveType.ImmediateSync, design);
        }
    }

    private static Design LoadDesignV2Or3(CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader, in JsonElement json,
        int version)
    {
        var creationDate = json.TryReadProperty("CreationDate"u8, out DateTimeOffset? cd)
            ? cd!.Value
            : throw new Exception("Design creation date can not be null or unset.");

        var design = new Design(customizations, items)
        {
            CreationDate = creationDate,
            Identifier =
                json.TryReadProperty("Identifier"u8, out Guid? id)
                    ? id!.Value
                    : throw new Exception("Design identifier can not be null or unset."),
            Name        = json.TryReadProperty("Name"u8, out string? n) ? n! : throw new Exception("Design name can not be null or unset."),
            Description = json.PropertyOrDefault("Description"u8, string.Empty),
            Tags        = ParseTags(json),
            LastEdit    = json.PropertyOrDefault("LastEdit"u8,    creationDate),
            QuickDesign = json.PropertyOrDefault("QuickDesign"u8, true),
        };
        if (design.LastEdit < creationDate)
            design.LastEdit = creationDate;
        design.Path.Folder   = json.PropertyOrDefault("FileSystemFolder"u8, string.Empty);
        design.Path.SortName = json.TryReadProperty("SortOrderName"u8, out string? sn, true) ? sn?.FixName() : null;

        design.SetWriteProtected(json.PropertyOrDefault("WriteProtected"u8, false));
        LoadCustomize(customizations, json.TryReadObject("Customize"u8, out var c) ? c : null, design, design.Name, true, false);
        LoadEquip(items, json.TryReadObject("Equipment"u8,              out var e) ? e : null, design, design.Name, true);
        LoadBonus(items, design, json.TryReadObject("Bonus"u8,          out var b) ? b : null);
        LoadMods(json.TryReadArray("Mods"u8, out var m) ? m : null, design, version);
        LoadParameters(json.TryReadObject("Parameters"u8,   out var p) ? p : null, design, design.Name);
        LoadMaterials(json.TryReadObject("Materials"u8,     out var mat) ? mat : null, design, design.Name);
        LoadLinks(linkLoader, json.TryReadObject("Links"u8, out var l) ? l : null, design);
        design.Color                  = json.PropertyOrDefault("Color"u8,        string.Empty);
        design.ForcedRedraw           = json.PropertyOrDefault("ForcedRedraw"u8, false);
        design.ResetAdvancedDyes      = ParseCombinedSlots(json.TryGetProperty("ResetAdvancedDyes"u8, out var reset) ? reset : null);
        design.ResetTemporarySettings = json.PropertyOrDefault("ResetTemporarySettings"u8, false);
        design.RevertAdvancedDyes     = ParseCombinedSlots(json.TryGetProperty("RevertAdvancedDyes"u8, out var revert) ? revert : null);
        return design;

        static string[] ParseTags(in JsonElement json)
        {
            if (!json.TryReadArray("Tags"u8, out var array))
                return [];

            List<string> entries = [];
            foreach (var prop in array.EnumerateArray())
            {
                if (prop.ValueKind is JsonValueKind.String)
                    entries.Add(prop.GetString()!);
            }

            return entries.OrderBy(t => t).Distinct().ToArray();
        }
    }

    private static void LoadMods(in JsonElement? mods, Design design, int version)
    {
        if (mods?.ValueKind is not JsonValueKind.Array)
            return;

        foreach (var mod in mods.Value.EnumerateArray())
        {
            if (!mod.TryReadProperty("Name"u8, out string? name) || !mod.TryReadProperty("Directory"u8, out string? directory))
            {
                Glamourer.Messager.NotificationMessage("The loaded design contains an invalid mod, skipped.", NotificationType.Warning);
                continue;
            }

            var preset = version <= 2 ? ReadV2Mods(mod) : ReadV3Mods(mod);
            if (!design.AssociatedMods.TryAdd(new ModIdentifier(directory!, name!), preset))
                Glamourer.Messager.NotificationMessage("The loaded design contains a mod more than once, skipped.", NotificationType.Warning);
        }

        return;

        static SettingPresetData ReadV3Mods(in JsonElement mod)
            => SettingPresetData.FromElement(mod);

        static SettingPresetData ReadV2Mods(in JsonElement mod)
        {
            var preset = SettingPresetData.Create();
            if (mod.TryReadProperty("Priority"u8, out int? priority, true))
            {
                preset._hasPriority = priority.HasValue;
                preset._priority    = priority.GetValueOrDefault(0);
            }

            if (mod.PropertyOrDefault("Remove"u8, false))
                preset._state = (byte)ModState.RemoveTemporary;
            else if (mod.PropertyOrDefault("Inherit"u8, false))
                preset._state = (byte)ModState.Inherited;
            else
                preset._state = mod.TryReadProperty("Enabled"u8, out bool? value, true)
                    ? value switch
                    {
                        null  => (byte)ModState.Ignored,
                        true  => (byte)ModState.Enabled,
                        false => (byte)ModState.Disabled,
                    }
                    : (byte)ModState.Ignored;

            if (mod.TryReadObject("Settings"u8, out var settings))
                foreach (var prop in settings.EnumerateObject())
                {
                    if (prop.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Array)
                        continue;

                    var data = GroupSettingDataExtensions.Create();
                    data.DisableAllUnknown = true;
                    if (prop.Value.ValueKind is JsonValueKind.Array)
                        foreach (var value in prop.Value.EnumerateArray())
                        {
                            if (value.ValueKind is JsonValueKind.String)
                                data.Options.Add((Guid.Empty, value.GetString()!), (byte)OptionState.Enabled);
                        }

                    preset.Settings.Add((Guid.Empty, prop.Name), data);
                }

            return preset;
        }
    }

    private static void LoadLinks(DesignLinkLoader linkLoader, in JsonElement? links, Design design)
    {
        if (links is not { } j)
            return;

        Parse(j.TryReadArray("Before"u8, out var b) ? b : null, LinkOrder.Before);
        Parse(j.TryReadArray("After"u8,  out var a) ? a : null, LinkOrder.After);
        return;

        void Parse(in JsonElement? array, LinkOrder order)
        {
            if (array is not { } data)
                return;

            foreach (var obj in data.EnumerateArray())
            {
                if (!obj.TryReadProperty("Design"u8, out Guid? identifier))
                    throw new ArgumentNullException(nameof(design));

                var type       = (ApplicationType)obj.PropertyOrDefault("Type"u8, 0u);
                var conditions = DesignConditionData.Deserialize(obj.TryReadObject("Conditions"u8, out var c) ? c : null);
                linkLoader.AddObject(design, new LinkData(identifier!.Value, type, conditions, order));
            }
        }
    }

    private static ModelCombinedSlots ParseCombinedSlots(in JsonElement? json)
    {
        if (json is not { } j || j.ValueKind is JsonValueKind.Null)
            return 0;

        return j.ValueKind switch
        {
            JsonValueKind.True                                  => ModelCombinedSlotsExtensions.All,
            JsonValueKind.False                                 => 0,
            JsonValueKind.Number when j.TryGetUInt64(out var v) => ParseLegacyOrCompactValue(v),
            JsonValueKind.Object                                => ParseObjectValue(j),
            _                                                   => 0,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ModelCombinedSlots ParseLegacyOrCompactValue(ulong value)
        {
            // Legacy (CombinedItemSlotFlag) mask, or compact form for unambiguous values.
            if ((value & ~(ulong)ModelCombinedSlotsExtensions.AllEquipmentPieces) is 0)
                return (ModelCombinedSlots)value;

            // Treat the legacy "All" value as the current "All" (that will be serialized as `true`).
            if ((value & 0x1FFF) is 0x1FFF)
                return ModelCombinedSlotsExtensions.All;

            var equipment = (ModelCombinedSlots)value & ModelCombinedSlotsExtensions.AllEquipmentPieces;
            var bonus     = (ModelCombinedSlots)(value << 4) & ModelCombinedSlotsExtensions.BonusItemFlagMask;
            var mainhand  = (ModelCombinedSlots)(value << 22) & ModelCombinedSlots.Mainhand;
            var offhand   = (ModelCombinedSlots)(value << 29) & ModelCombinedSlots.Offhand;
            return equipment | bonus | mainhand | offhand;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ModelCombinedSlots ParseObjectValue(in JsonElement json)
        {
            var human    = json.PropertyOrDefault("Human"u8,    0UL);
            var mainhand = json.PropertyOrDefault("Mainhand"u8, 0UL);
            var offhand  = json.PropertyOrDefault("Offhand"u8,  0UL);
            return (ModelCombinedSlots)(human | (mainhand << 32) | (offhand << 40));
        }
    }

    #endregion

    #region ISavable

    public string ToFilePath(FilenameService fileNames)
        => fileNames.DesignFile(this);

    public void Save(Stream stream)
    {
        using var j = new Utf8JsonWriter(stream, JsonFunctions.WriterOptions);
        Serialize(j);
    }

    public string LogName(string fileName)
        => System.IO.Path.GetFileNameWithoutExtension(fileName);

    #endregion

    string IFileSystemValue.Identifier
        => Identifier.ToString();

    public string DisplayName
        => Name;

    public static void Write(Utf8JsonWriter writer, in Design value, JsonSerializerOptions options)
        => value.Serialize(writer);
}
