using System;
using System.IO;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public partial class Design : DesignBase, ISavable
{
    public const int FileVersion = 1;

    public Guid           Identifier   { get; private init; }
    public DateTimeOffset CreationDate { get; private init; }
    public LowerString    Name         { get; private set; } = LowerString.Empty;
    public string         Description  { get; private set; } = string.Empty;
    public string[]       Tags         { get; private set; } = Array.Empty<string>();
    public int            Index        { get; private set; }

    public EquipFlag     ApplyEquip     { get; private set; }
    public CustomizeFlag ApplyCustomize { get; private set; }
    public QuadBool      Wetness        { get; private set; } = QuadBool.NullFalse;
    public QuadBool      Visor          { get; private set; } = QuadBool.NullFalse;
    public QuadBool      Hat            { get; private set; } = QuadBool.NullFalse;
    public QuadBool      Weapon         { get; private set; } = QuadBool.NullFalse;
    public bool          WriteProtected { get; private set; }

    public bool DoApplyEquip(EquipSlot slot)
        => ApplyEquip.HasFlag(slot.ToFlag());

    public bool DoApplyStain(EquipSlot slot)
        => ApplyEquip.HasFlag(slot.ToStainFlag());

    public bool DoApplyCustomize(CustomizeIndex idx)
        => ApplyCustomize.HasFlag(idx.ToFlag());

    private bool SetApplyEquip(EquipSlot slot, bool value)
    {
        var newValue = value ? ApplyEquip | slot.ToFlag() : ApplyEquip & ~slot.ToFlag();
        if (newValue == ApplyEquip)
            return false;

        ApplyEquip = newValue;
        return true;
    }

    private bool SetApplyStain(EquipSlot slot, bool value)
    {
        var newValue = value ? ApplyEquip | slot.ToStainFlag() : ApplyEquip & ~slot.ToStainFlag();
        if (newValue == ApplyEquip)
            return false;

        ApplyEquip = newValue;
        return true;
    }

    private bool SetApplyCustomize(CustomizeIndex idx, bool value)
    {
        var newValue = value ? ApplyCustomize | idx.ToFlag() : ApplyCustomize & ~idx.ToFlag();
        if (newValue == ApplyCustomize)
            return false;

        ApplyCustomize = newValue;
        return true;
    }


    private Design(ItemManager items)
        : base(items)
    { }

    public JObject JsonSerialize()
    {
        var ret = new JObject
        {
            [nameof(FileVersion)]             = FileVersion,
            [nameof(Identifier)]              = Identifier,
            [nameof(CreationDate)]            = CreationDate,
            [nameof(Name)]                    = Name.Text,
            [nameof(Description)]             = Description,
            [nameof(Tags)]                    = JArray.FromObject(Tags),
            [nameof(WriteProtected)]          = WriteProtected,
            [nameof(CharacterData.Equipment)] = SerializeEquipment(),
            [nameof(CharacterData.Customize)] = SerializeCustomize(),
        };
        return ret;
    }

    public JObject SerializeEquipment()
    {
        static JObject Serialize(uint itemId, StainId stain, bool apply, bool applyStain)
            => new()
            {
                [nameof(Item.ItemId)] = itemId,
                [nameof(Item.Stain)]  = stain.Value,
                ["Apply"]             = apply,
                ["ApplyStain"]        = applyStain,
            };

        var ret = new JObject()
        {
            [nameof(MainHand)] =
                Serialize(MainHand, CharacterData.MainHand.Stain, DoApplyEquip(EquipSlot.MainHand), DoApplyStain(EquipSlot.MainHand)),
            [nameof(OffHand)] = Serialize(OffHand, CharacterData.OffHand.Stain, DoApplyEquip(EquipSlot.OffHand),
                DoApplyStain(EquipSlot.OffHand)),
        };

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var armor = Armor(slot);
            ret[slot.ToString()] = Serialize(armor.ItemId, armor.Stain, DoApplyEquip(slot), DoApplyStain(slot));
        }

        ret[nameof(Hat)]    = Hat.ToJObject("Show", "Apply");
        ret[nameof(Weapon)] = Weapon.ToJObject("Show", "Apply");
        ret[nameof(Visor)]  = Visor.ToJObject("IsToggled", "Apply");

        return ret;
    }

    public JObject SerializeCustomize()
    {
        var ret = new JObject()
        {
            [nameof(ModelId)] = ModelId,
        };
        var customize = CharacterData.Customize;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var data = customize[idx];
            ret[idx.ToString()] = new JObject()
            {
                ["Value"] = data.Value,
                ["Apply"] = true,
            };
        }

        ret[nameof(Wetness)] = Wetness.ToJObject("IsWet", "Apply");
        return ret;
    }

    public static Design LoadDesign(ItemManager items, JObject json, out bool changes)
    {
        var version = json[nameof(FileVersion)]?.ToObject<int>() ?? 0;
        return version switch
        {
            1 => LoadDesignV1(items, json, out changes),
            _ => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Design LoadDesignV1(ItemManager items, JObject json, out bool changes)
    {
        static string[] ParseTags(JObject json)
        {
            var tags = json["Tags"]?.ToObject<string[]>() ?? Array.Empty<string>();
            return tags.OrderBy(t => t).Distinct().ToArray();
        }

        var design = new Design(items)
        {
            CreationDate = json["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate"),
            Identifier   = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Name         = new LowerString(json["Name"]?.ToObject<string>() ?? throw new ArgumentNullException("Name")),
            Description  = json["Description"]?.ToObject<string>() ?? string.Empty,
            Tags         = ParseTags(json),
        };

        changes =  LoadEquip(items, json["Equipment"], design);
        changes |= LoadCustomize(json["Customize"], design);
        return design;
    }

    private static bool LoadEquip(ItemManager items, JToken? equip, Design design)
    {
        if (equip == null)
            return true;

        static (uint, StainId, bool, bool) ParseItem(EquipSlot slot, JToken? item)
        {
            var id         = item?["ItemId"]?.ToObject<uint>() ?? ItemManager.NothingId(slot);
            var stain      = (StainId)(item?["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = item?["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = item?["ApplyStain"]?.ToObject<bool>() ?? false;
            return (id, stain, apply, applyStain);
        }

        var changes = false;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stain, apply, applyStain) =  ParseItem(slot, equip[slot.ToString()]);
            changes                            |= !design.SetArmor(items, slot, id);
            changes                            |= !design.SetStain(slot, stain);
            design.SetApplyEquip(slot, apply);
            design.SetApplyStain(slot, applyStain);
        }

        var main = equip["MainHand"];
        if (main == null)
        {
            changes = true;
        }
        else
        {
            var id         = main["ItemId"]?.ToObject<uint>() ?? items.DefaultSword.RowId;
            var stain      = (StainId)(main["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = main["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = main["ApplyStain"]?.ToObject<bool>() ?? false;
            changes |= !design.SetMainhand(items, id);
            changes |= !design.SetStain(EquipSlot.MainHand, stain);
            design.SetApplyEquip(EquipSlot.MainHand, apply);
            design.SetApplyStain(EquipSlot.MainHand, applyStain);
        }

        var off = equip["OffHand"];
        if (off == null)
        {
            changes = true;
        }
        else
        {
            var id         = off["ItemId"]?.ToObject<uint>() ?? ItemManager.NothingId(design.MainhandType.Offhand());
            var stain      = (StainId)(off["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = off["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = off["ApplyStain"]?.ToObject<bool>() ?? false;
            changes |= !design.SetOffhand(items, id);
            changes |= !design.SetStain(EquipSlot.OffHand, stain);
            design.SetApplyEquip(EquipSlot.OffHand, apply);
            design.SetApplyStain(EquipSlot.OffHand, applyStain);
        }

        design.Hat    = QuadBool.FromJObject(equip["Hat"],    "Show",      "Apply", QuadBool.NullFalse);
        design.Weapon = QuadBool.FromJObject(equip["Weapon"], "Show",      "Apply", QuadBool.NullFalse);
        design.Visor  = QuadBool.FromJObject(equip["Visor"],  "IsToggled", "Apply", QuadBool.NullFalse);

        return changes;
    }

    private static bool LoadCustomize(JToken? json, Design design)
    {
        if (json == null)
            return true;

        var customize = design.CharacterData.Customize;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var tok   = json[idx.ToString()];
            var data  = (CustomizeValue)(tok?["Value"]?.ToObject<byte>() ?? 0);
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }

        design.Wetness = QuadBool.FromJObject(json["Wetness"], "IsWet", "Apply", QuadBool.NullFalse);

        return false;
    }

    public void MigrateBase64(ItemManager items, string base64)
    {
        var data = MigrateBase64(base64, out var applyEquip, out var applyCustomize, out var writeProtected, out var wet, out var hat,
            out var visor,               out var weapon);
        UpdateMainhand(items, data.MainHand);
        UpdateOffhand(items, data.OffHand);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            UpdateArmor(items, slot, data.Equipment[slot], true);
        CharacterData.CustomizeData = data.CustomizeData;
        ApplyEquip                  = applyEquip;
        ApplyCustomize              = applyCustomize;
        WriteProtected              = writeProtected;
        Wetness                     = wet;
        Hat                         = hat;
        Visor                       = visor;
        Weapon                      = weapon;
    }

    public static Design CreateTemporaryFromBase64(ItemManager items, string base64, bool customize, bool equip)
    {
        var ret = new Design(items);
        ret.MigrateBase64(items, base64);
        if (!customize)
            ret.ApplyCustomize = 0;
        if (!equip)
            ret.ApplyEquip = 0;
        ret.Wetness = ret.Wetness.SetEnabled(customize);
        ret.Visor   = ret.Visor.SetEnabled(equip);
        ret.Hat     = ret.Hat.SetEnabled(equip);
        ret.Weapon  = ret.Weapon.SetEnabled(equip);
        return ret;
    }

    // Outdated.
    public string CreateOldBase64()
        => CreateOldBase64(in CharacterData, ApplyEquip,    ApplyCustomize,     Wetness == QuadBool.True, Hat.ForcedValue, Hat.Enabled,
            Visor.ForcedValue,               Visor.Enabled, Weapon.ForcedValue, Weapon.Enabled,           WriteProtected,  1f);

    public string ToFilename(FilenameService fileNames)
        => fileNames.DesignFile(this);

    public void   Save(StreamWriter writer)
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
}
