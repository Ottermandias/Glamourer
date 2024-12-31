using Dalamud.Interface.ImGuiNotification;
using Glamourer.Automation;
using Glamourer.Designs.Links;
using Glamourer.Interop.Material;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Structs;
using Notification = OtterGui.Classes.Notification;

namespace Glamourer.Designs;

public sealed class Design : DesignBase, ISavable, IDesignStandIn
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
        Color                  = other.Color;
        AssociatedMods         = new SortedList<Mod, ModSettings>(other.AssociatedMods);
        Links                  = Links.Clone();
    }

    // Metadata
    public new const int FileVersion = 2;

    public Guid                         Identifier             { get; internal init; }
    public DateTimeOffset               CreationDate           { get; internal init; }
    public DateTimeOffset               LastEdit               { get; internal set; }
    public LowerString                  Name                   { get; internal set; } = LowerString.Empty;
    public string                       Description            { get; internal set; } = string.Empty;
    public string[]                     Tags                   { get; internal set; } = [];
    public int                          Index                  { get; internal set; }
    public bool                         ForcedRedraw           { get; internal set; }
    public bool                         ResetAdvancedDyes      { get; internal set; }
    public bool                         ResetTemporarySettings { get; internal set; }
    public bool                         QuickDesign            { get; internal set; } = true;
    public string                       Color                  { get; internal set; } = string.Empty;
    public SortedList<Mod, ModSettings> AssociatedMods         { get; private set; }  = [];
    public LinkContainer                Links                  { get; private set; }  = [];

    public string Incognito
        => Identifier.ToString()[..8];

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks(bool newApplication)
        => LinkContainer.GetAllLinks(this).Select(t => ((IDesignStandIn)t.Link.Link, t.Link.Type, JobFlag.All));

    #endregion

    #region IDesignStandIn

    public string ResolveName(bool incognito)
        => incognito ? Incognito : Name.Text;

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

    public void AddData(JObject _)
    { }

    public void ParseData(JObject _)
    { }

    public bool ChangeData(object data)
        => false;

    #endregion

    #region Serialization

    public new JObject JsonSerialize()
    {
        var ret = new JObject()
        {
            ["FileVersion"]            = FileVersion,
            ["Identifier"]             = Identifier,
            ["CreationDate"]           = CreationDate,
            ["LastEdit"]               = LastEdit,
            ["Name"]                   = Name.Text,
            ["Description"]            = Description,
            ["ForcedRedraw"]           = ForcedRedraw,
            ["ResetAdvancedDyes"]      = ResetAdvancedDyes,
            ["ResetTemporarySettings"] = ResetTemporarySettings,
            ["Color"]                  = Color,
            ["QuickDesign"]            = QuickDesign,
            ["Tags"]                   = JArray.FromObject(Tags),
            ["WriteProtected"]         = WriteProtected(),
            ["Equipment"]              = SerializeEquipment(),
            ["Bonus"]                  = SerializeBonusItems(),
            ["Customize"]              = SerializeCustomize(),
            ["Parameters"]             = SerializeParameters(),
            ["Materials"]              = SerializeMaterials(),
            ["Mods"]                   = SerializeMods(),
            ["Links"]                  = Links.Serialize(),
        };
        return ret;
    }

    private JArray SerializeMods()
    {
        var ret = new JArray();
        foreach (var (mod, settings) in AssociatedMods)
        {
            var obj = new JObject()
            {
                ["Name"]      = mod.Name,
                ["Directory"] = mod.DirectoryName,
                ["Enabled"]   = settings.Enabled,
            };
            if (settings.Enabled)
            {
                obj["Priority"] = settings.Priority;
                obj["Settings"] = JObject.FromObject(settings.Settings);
            }

            ret.Add(obj);
        }

        return ret;
    }

    #endregion

    #region Deserialization

    public static Design LoadDesign(SaveService saveService, CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader,
        JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            1           => LoadDesignV1(saveService, customizations, items, linkLoader, json),
            FileVersion => LoadDesignV2(customizations, items, linkLoader, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    /// <summary> The values for gloss and specular strength were switched. Swap them for all appropriate designs. </summary>
    private static Design LoadDesignV1(SaveService saveService, CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader,
        JObject json)
    {
        var design             = LoadDesignV2(customizations, items, linkLoader, json);
        var materialDesignData = design.GetMaterialDataRef();
        if (materialDesignData.Values.Count == 0)
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
            foreach (var (key, value) in materialData.GetValues(MaterialValueIndex.Min(), MaterialValueIndex.Max()))
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

    private static Design LoadDesignV2(CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader, JObject json)
    {
        var creationDate = json["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate");

        var design = new Design(customizations, items)
        {
            CreationDate = creationDate,
            Identifier   = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Name         = new LowerString(json["Name"]?.ToObject<string>() ?? throw new ArgumentNullException("Name")),
            Description  = json["Description"]?.ToObject<string>() ?? string.Empty,
            Tags         = ParseTags(json),
            LastEdit     = json["LastEdit"]?.ToObject<DateTimeOffset>() ?? creationDate,
            QuickDesign  = json["QuickDesign"]?.ToObject<bool>() ?? true,
        };
        if (design.LastEdit < creationDate)
            design.LastEdit = creationDate;
        design.SetWriteProtected(json["WriteProtected"]?.ToObject<bool>() ?? false);
        LoadCustomize(customizations, json["Customize"], design, design.Name, true, false);
        LoadEquip(items, json["Equipment"], design, design.Name, true);
        LoadBonus(items, design, json["Bonus"]);
        LoadMods(json["Mods"], design);
        LoadParameters(json["Parameters"], design, design.Name);
        LoadMaterials(json["Materials"], design, design.Name);
        LoadLinks(linkLoader, json["Links"], design);
        design.Color                  = json["Color"]?.ToObject<string>() ?? string.Empty;
        design.ForcedRedraw           = json["ForcedRedraw"]?.ToObject<bool>() ?? false;
        design.ResetAdvancedDyes      = json["ResetAdvancedDyes"]?.ToObject<bool>() ?? false;
        design.ResetTemporarySettings = json["ResetTemporarySettings"]?.ToObject<bool>() ?? false;
        return design;

        static string[] ParseTags(JObject json)
        {
            var tags = json["Tags"]?.ToObject<string[]>() ?? [];
            return tags.OrderBy(t => t).Distinct().ToArray();
        }
    }

    private static void LoadMods(JToken? mods, Design design)
    {
        if (mods is not JArray array)
            return;

        foreach (var tok in array)
        {
            var name      = tok["Name"]?.ToObject<string>();
            var directory = tok["Directory"]?.ToObject<string>();
            var enabled   = tok["Enabled"]?.ToObject<bool>();
            if (name == null || directory == null || enabled == null)
            {
                Glamourer.Messager.NotificationMessage("The loaded design contains an invalid mod, skipped.", NotificationType.Warning);
                continue;
            }

            var forceInherit  = tok["Inherit"]?.ToObject<bool>() ?? false;
            var removeSetting = tok["Remove"]?.ToObject<bool>() ?? false;
            var settingsDict  = tok["Settings"]?.ToObject<Dictionary<string, List<string>>>() ?? [];
            var settings      = new Dictionary<string, List<string>>(settingsDict.Count);
            foreach (var (key, value) in settingsDict)
                settings.Add(key, value);
            var priority = tok["Priority"]?.ToObject<int>() ?? 0;
            if (!design.AssociatedMods.TryAdd(new Mod(name, directory),
                    new ModSettings(settings, priority, enabled.Value, forceInherit, removeSetting)))
                Glamourer.Messager.NotificationMessage("The loaded design contains a mod more than once, skipped.", NotificationType.Warning);
        }
    }

    private static void LoadLinks(DesignLinkLoader linkLoader, JToken? links, Design design)
    {
        if (links is not JObject obj)
            return;

        Parse(obj["Before"] as JArray, LinkOrder.Before);
        Parse(obj["After"] as JArray,  LinkOrder.After);
        return;

        void Parse(JArray? array, LinkOrder order)
        {
            if (array == null)
                return;

            foreach (var jObj in array.OfType<JObject>())
            {
                var identifier = jObj["Design"]?.ToObject<Guid>() ?? throw new ArgumentNullException(nameof(design));
                var type       = (ApplicationType)(jObj["Type"]?.ToObject<uint>() ?? 0);
                linkLoader.AddObject(design, new LinkData(identifier, type, order));
            }
        }
    }

    #endregion

    #region ISavable

    public string ToFilename(FilenameService fileNames)
        => fileNames.DesignFile(this);

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
        };
        var obj = JsonSerialize();
        obj.WriteTo(j);
    }

    public string LogName(string fileName)
        => Path.GetFileNameWithoutExtension(fileName);

    #endregion
}
