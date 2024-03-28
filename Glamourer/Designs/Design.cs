using Dalamud.Interface.Internal.Notifications;
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
        Tags           = [.. other.Tags];
        Description    = other.Description;
        QuickDesign    = other.QuickDesign;
        AssociatedMods = new SortedList<Mod, ModSettings>(other.AssociatedMods);
    }

    // Metadata
    public new const int FileVersion = 1;

    public Guid                         Identifier     { get; internal init; }
    public DateTimeOffset               CreationDate   { get; internal init; }
    public DateTimeOffset               LastEdit       { get; internal set; }
    public LowerString                  Name           { get; internal set; } = LowerString.Empty;
    public string                       Description    { get; internal set; } = string.Empty;
    public string[]                     Tags           { get; internal set; } = [];
    public int                          Index          { get; internal set; }
    public bool                         QuickDesign    { get; internal set; } = true;
    public string                       Color          { get; internal set; } = string.Empty;
    public SortedList<Mod, ModSettings> AssociatedMods { get; private set; }  = [];
    public LinkContainer                Links          { get; private set; }  = [];

    public string Incognito
        => Identifier.ToString()[..8];

    public IEnumerable<(IDesignStandIn Design, ApplicationType Flags, JobFlag Jobs)> AllLinks
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
            ["FileVersion"]    = FileVersion,
            ["Identifier"]     = Identifier,
            ["CreationDate"]   = CreationDate,
            ["LastEdit"]       = LastEdit,
            ["Name"]           = Name.Text,
            ["Description"]    = Description,
            ["Color"]          = Color,
            ["QuickDesign"]    = QuickDesign,
            ["Tags"]           = JArray.FromObject(Tags),
            ["WriteProtected"] = WriteProtected(),
            ["Equipment"]      = SerializeEquipment(),
            ["Customize"]      = SerializeCustomize(),
            ["Parameters"]     = SerializeParameters(),
            ["Materials"]      = SerializeMaterials(),
            ["Mods"]           = SerializeMods(),
            ["Links"]          = Links.Serialize(),
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

    public static Design LoadDesign(CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            FileVersion => LoadDesignV1(customizations, items, linkLoader, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Design LoadDesignV1(CustomizeService customizations, ItemManager items, DesignLinkLoader linkLoader, JObject json)
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
        LoadMods(json["Mods"], design);
        LoadParameters(json["Parameters"], design, design.Name);
        LoadMaterials(json["Materials"], design, design.Name);
        LoadLinks(linkLoader, json["Links"], design);
        design.Color = json["Color"]?.ToObject<string>() ?? string.Empty;
        return design;

        static string[] ParseTags(JObject json)
        {
            var tags = json["Tags"]?.ToObject<string[]>() ?? Array.Empty<string>();
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

            var settingsDict = tok["Settings"]?.ToObject<Dictionary<string, string[]>>() ?? new Dictionary<string, string[]>();
            var settings     = new SortedList<string, IList<string>>(settingsDict.Count);
            foreach (var (key, value) in settingsDict)
                settings.Add(key, value);
            var priority = tok["Priority"]?.ToObject<int>() ?? 0;
            if (!design.AssociatedMods.TryAdd(new Mod(name, directory), new ModSettings(settings, priority, enabled.Value)))
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

            foreach (var obj in array.OfType<JObject>())
            {
                var identifier = obj["Design"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Design");
                var type       = (ApplicationType)(obj["Type"]?.ToObject<uint>() ?? 0);
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
