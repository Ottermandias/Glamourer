using System;
using System.Linq;
using Glamourer.Customization;
using Glamourer.Util;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public partial class Design : DesignBase
{
    public const int FileVersion = 1;

    public Guid           Identifier   { get; private init; }
    public DateTimeOffset CreationDate { get; private init; }
    public LowerString    Name         { get; private set; } = LowerString.Empty;
    public string         Description  { get; private set; } = string.Empty;
    public string[]       Tags         { get; private set; } = Array.Empty<string>();
    public int            Index        { get; private set; }

    private EquipFlag     _applyEquip;
    private CustomizeFlag _applyCustomize;
    public  QuadBool      Wetness        { get; private set; } = QuadBool.NullFalse;
    public  QuadBool      Visor          { get; private set; } = QuadBool.NullFalse;
    public  QuadBool      Hat            { get; private set; } = QuadBool.NullFalse;
    public  QuadBool      Weapon         { get; private set; } = QuadBool.NullFalse;
    public  bool          WriteProtected { get; private set; }

    public bool DoApplyEquip(EquipSlot slot)
        => _applyEquip.HasFlag(slot.ToFlag());

    public bool DoApplyStain(EquipSlot slot)
        => _applyEquip.HasFlag(slot.ToStainFlag());

    public bool DoApplyCustomize(CustomizeIndex idx)
        => _applyCustomize.HasFlag(idx.ToFlag());

    private bool SetApplyEquip(EquipSlot slot, bool value)
    {
        var newValue = value ? _applyEquip | slot.ToFlag() : _applyEquip & ~slot.ToFlag();
        if (newValue == _applyEquip)
            return false;

        _applyEquip = newValue;
        return true;
    }

    private bool SetApplyStain(EquipSlot slot, bool value)
    {
        var newValue = value ? _applyEquip | slot.ToStainFlag() : _applyEquip & ~slot.ToStainFlag();
        if (newValue == _applyEquip)
            return false;

        _applyEquip = newValue;
        return true;
    }

    private bool SetApplyCustomize(CustomizeIndex idx, bool value)
    {
        var newValue = value ? _applyCustomize | idx.ToFlag() : _applyCustomize & ~idx.ToFlag();
        if (newValue == _applyCustomize)
            return false;

        _applyCustomize = newValue;
        return true;
    }


    private Design()
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
            [nameof(OffHand)] = Serialize(OffHand, CharacterData.OffHand.Stain, DoApplyEquip(EquipSlot.OffHand), DoApplyStain(EquipSlot.OffHand)),
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

    public static Design LoadDesign(JObject json, out bool changes)
    {
        var version = json[nameof(FileVersion)]?.ToObject<int>() ?? 0;
        return version switch
        {
            1 => LoadDesignV1(json, out changes),
            _ => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Design LoadDesignV1(JObject json, out bool changes)
    {
        static string[] ParseTags(JObject json)
        {
            var tags = json["Tags"]?.ToObject<string[]>() ?? Array.Empty<string>();
            return tags.OrderBy(t => t).Distinct().ToArray();
        }

        var design = new Design()
        {
            CreationDate = json["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate"),
            Identifier   = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
            Name         = new LowerString(json["Name"]?.ToObject<string>() ?? throw new ArgumentNullException("Name")),
            Description  = json["Description"]?.ToObject<string>() ?? string.Empty,
            Tags         = ParseTags(json),
        };

        changes =  LoadEquip(json["Equipment"], design);
        changes |= LoadCustomize(json["Customize"], design);
        return design;
    }

    private static bool LoadEquip(JToken? equip, Design design)
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
            changes                            |= !design.SetArmor(slot, id);
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
            var id         = main["ItemId"]?.ToObject<uint>() ?? Glamourer.Items.DefaultSword.RowId;
            var stain      = (StainId)(main["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = main["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = main["ApplyStain"]?.ToObject<bool>() ?? false;
            changes |= !design.SetMainhand(id);
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
            changes |= !design.SetOffhand(id);
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


    public void MigrateBase64(string base64)
    {
        static void CheckSize(int length, int requiredLength)
        {
            if (length != requiredLength)
                throw new Exception(
                    $"Can not parse Base64 string into CharacterSave:\n\tInvalid size {length} instead of {requiredLength}.");
        }

        var bytes = Convert.FromBase64String(base64);

        byte   applicationFlags;
        ushort equipFlags;

        switch (bytes[0])
        {
            case 1:
            {
                CheckSize(bytes.Length, 86);
                applicationFlags = bytes[1];
                equipFlags       = BitConverter.ToUInt16(bytes, 2);
                break;
            }
            case 2:
            {
                CheckSize(bytes.Length, 91);
                applicationFlags = bytes[1];
                equipFlags       = BitConverter.ToUInt16(bytes, 2);
                Hat              = Hat.SetValue((bytes[90] & 0x01) == 0);
                Visor            = Visor.SetValue((bytes[90] & 0x10) != 0);
                Weapon           = Weapon.SetValue((bytes[90] & 0x02) == 0);
                break;
            }
            default: throw new Exception($"Can not parse Base64 string into design for migration:\n\tInvalid Version {bytes[0]}.");
        }

        _applyCustomize = (applicationFlags & 0x01) != 0 ? CustomizeFlagExtensions.All : 0;
        Wetness         = (applicationFlags & 0x02) != 0 ? QuadBool.True : QuadBool.NullFalse;
        Hat             = Hat.SetEnabled((applicationFlags & 0x04) != 0);
        Weapon          = Weapon.SetEnabled((applicationFlags & 0x08) != 0);
        Visor           = Visor.SetEnabled((applicationFlags & 0x10) != 0);
        WriteProtected  = (applicationFlags & 0x20) != 0;

        CharacterData.ModelId = 0;

        SetApplyEquip(EquipSlot.MainHand, (equipFlags & 0x0001) != 0);
        SetApplyEquip(EquipSlot.OffHand,  (equipFlags & 0x0002) != 0);
        SetApplyStain(EquipSlot.MainHand, (equipFlags & 0x0001) != 0);
        SetApplyStain(EquipSlot.OffHand,  (equipFlags & 0x0002) != 0);
        var flag = 0x0002u;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            flag <<= 1;
            var apply = (equipFlags & flag) != 0;
            SetApplyEquip(slot, apply);
            SetApplyStain(slot, apply);
        }
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                CharacterData.CustomizeData.Read(ptr + 4);
                var cur = (CharacterWeapon*)(ptr + 30);

                UpdateMainhand(cur[0]);
                SetStain(EquipSlot.MainHand, cur[0].Stain);
                UpdateOffhand(cur[1]);
                SetStain(EquipSlot.OffHand, cur[1].Stain);
                var eq = (CharacterArmor*)(cur + 2);
                foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
                {
                    UpdateArmor(slot, eq[idx], true);
                    SetStain(slot, eq[idx].Stain);
                }
            }
        }
    }
}
