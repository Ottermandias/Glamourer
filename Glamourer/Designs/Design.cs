using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;

namespace Glamourer.Designs;

public sealed class Design : DesignBase, ISavable
{
    #region Data
    internal Design(ItemManager items)
        : base(items)
    { }

    internal Design(DesignBase other)
        : base(other)
    { }

    internal Design(Design other)
        : base(other)
    {
        Tags           = Tags.ToArray();
        Description    = Description;
        AssociatedMods = new SortedList<Mod, ModSettings>(other.AssociatedMods);
    }

    // Metadata
    public new const int FileVersion = 1;

    public Guid                         Identifier     { get; internal init; }
    public DateTimeOffset               CreationDate   { get; internal init; }
    public DateTimeOffset               LastEdit       { get; internal set; }
    public LowerString                  Name           { get; internal set; } = LowerString.Empty;
    public string                       Description    { get; internal set; } = string.Empty;
    public string[]                     Tags           { get; internal set; } = Array.Empty<string>();
    public int                          Index          { get; internal set; }
    public SortedList<Mod, ModSettings> AssociatedMods { get; private set; } = new();

    public string Incognito
        => Identifier.ToString()[..8];

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
                ["Tags"]           = JArray.FromObject(Tags),
                ["WriteProtected"] = WriteProtected(),
                ["Equipment"]      = SerializeEquipment(),
                ["Customize"]      = SerializeCustomize(),
                ["Mods"]           = SerializeMods(),
            }
            ;
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

    public static Design LoadDesign(CustomizationService customizations, ItemManager items, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            FileVersion => LoadDesignV1(customizations, items, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Design LoadDesignV1(CustomizationService customizations, ItemManager items, JObject json)
    {
        static string[] ParseTags(JObject json)
        {
            var tags = json["Tags"]?.ToObject<string[]>() ?? Array.Empty<string>();
            return tags.OrderBy(t => t).Distinct().ToArray();
        }

        var creationDate = json["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate");

        var design = new Design(items)
        {
            CreationDate = creationDate,
            Identifier   = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Name         = new LowerString(json["Name"]?.ToObject<string>() ?? throw new ArgumentNullException("Name")),
            Description  = json["Description"]?.ToObject<string>() ?? string.Empty,
            Tags         = ParseTags(json),
            LastEdit     = json["LastEdit"]?.ToObject<DateTimeOffset>() ?? creationDate,
        };
        if (design.LastEdit < creationDate)
            design.LastEdit = creationDate;
        design.SetWriteProtected(json["WriteProtected"]?.ToObject<bool>() ?? false);
        LoadCustomize(customizations, json["Customize"], design, design.Name, true, false);
        LoadEquip(items, json["Equipment"], design, design.Name, false);
        LoadMods(json["Mods"], design);
        return design;
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
                Glamourer.Chat.NotificationMessage("The loaded design contains an invalid mod, skipped.", "Warning", NotificationType.Warning);
                continue;
            }

            var settingsDict = tok["Settings"]?.ToObject<Dictionary<string, string[]>>() ?? new Dictionary<string, string[]>();
            var settings     = new SortedList<string, IList<string>>(settingsDict.Count);
            foreach (var (key, value) in settingsDict)
                settings.Add(key, value);
            var priority = tok["Priority"]?.ToObject<int>() ?? 0;
            if (!design.AssociatedMods.TryAdd(new Mod(name, directory), new ModSettings(settings, priority, enabled.Value)))
                Glamourer.Chat.NotificationMessage("The loaded design contains a mod more than once, skipped.", "Warning",
                    NotificationType.Warning);
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
