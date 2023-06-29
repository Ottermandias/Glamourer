using System;
using System.IO;
using System.Linq;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class Design : ISavable
{
    #region Data

    internal Design(ItemManager items)
    {
        DesignData.SetDefaultEquipment(items);
    }

    // Metadata
    public const int FileVersion = 1;

    public Guid           Identifier   { get; internal init; }
    public DateTimeOffset CreationDate { get; internal init; }
    public DateTimeOffset LastEdit     { get; internal set; }
    public LowerString    Name         { get; internal set; } = LowerString.Empty;
    public string         Description  { get; internal set; } = string.Empty;
    public string[]       Tags         { get; internal set; } = Array.Empty<string>();
    public int            Index        { get; internal set; }

    internal DesignData DesignData;

    public string Incognito
        => Identifier.ToString()[..8];

    /// <summary> Unconditionally apply a design to a designdata. </summary>
    /// <returns>Whether a redraw is required for the changes to take effect.</returns>
    public (bool, CustomizeFlag, EquipFlag) ApplyDesign(ref DesignData data)
    {
        var modelChanged = data.ModelId != DesignData.ModelId;
        data.ModelId = DesignData.ModelId;

        CustomizeFlag customizeFlags = 0;
        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            if (!DoApplyCustomize(index))
                continue;

            if (data.Customize.Set(index, DesignData.Customize[index]))
                customizeFlags |= index.ToFlag();
        }

        EquipFlag equipFlags = 0;
        foreach (var slot in EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand))
        {
            if (DoApplyEquip(slot))
                if (data.SetItem(slot, DesignData.Item(slot)))
                    equipFlags |= slot.ToFlag();

            if (DoApplyStain(slot))
                if (data.SetStain(slot, DesignData.Stain(slot)))
                    equipFlags |= slot.ToStainFlag();
        }

        if (DoApplyHatVisible())
            data.SetHatVisible(DesignData.IsHatVisible());

        if (DoApplyVisorToggle())
            data.SetVisor(DesignData.IsVisorToggled());

        if (DoApplyWeaponVisible())
            data.SetWeaponVisible(DesignData.IsWeaponVisible());

        if (DoApplyWetness())
            data.SetIsWet(DesignData.IsWet());
        return (modelChanged, customizeFlags, equipFlags);
    }

    #endregion

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

    internal CustomizeFlag ApplyCustomize = CustomizeFlagExtensions.All;
    internal EquipFlag     ApplyEquip     = EquipFlagExtensions.All;
    private  DesignFlags   _designFlags   = DesignFlags.ApplyHatVisible | DesignFlags.ApplyVisorState | DesignFlags.ApplyWeaponVisible;

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
        var newValue = value ? ApplyCustomize | idx.ToFlag() : ApplyCustomize & ~idx.ToFlag();
        if (newValue == ApplyCustomize)
            return false;

        ApplyCustomize = newValue;
        return true;
    }

    #endregion

    #region Serialization

    private JObject JsonSerialize()
    {
        var ret = new JObject
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
        };
        return ret;
    }

    private JObject SerializeEquipment()
    {
        static JObject Serialize(uint itemId, StainId stain, bool apply, bool applyStain)
            => new()
            {
                ["ItemId"]     = itemId,
                ["Stain"]      = stain.Value,
                ["Apply"]      = apply,
                ["ApplyStain"] = applyStain,
            };

        var ret = new JObject();

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item  = DesignData.Item(slot);
            var stain = DesignData.Stain(slot);
            ret[slot.ToString()] = Serialize(item.Id, stain, DoApplyEquip(slot), DoApplyStain(slot));
        }

        ret["Hat"]    = new QuadBool(DesignData.IsHatVisible(),    DoApplyHatVisible()).ToJObject("Show", "Apply");
        ret["Visor"]  = new QuadBool(DesignData.IsVisorToggled(),  DoApplyVisorToggle()).ToJObject("IsToggled", "Apply");
        ret["Weapon"] = new QuadBool(DesignData.IsWeaponVisible(), DoApplyWeaponVisible()).ToJObject("Show", "Apply");

        return ret;
    }

    private JObject SerializeCustomize()
    {
        var ret = new JObject()
        {
            ["ModelId"] = DesignData.ModelId,
        };
        var customize = DesignData.Customize;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            ret[idx.ToString()] = new JObject()
            {
                ["Value"] = customize[idx].Value,
                ["Apply"] = DoApplyCustomize(idx),
            };
        }

        ret["Wetness"] = new JObject()
        {
            ["Value"] = DesignData.IsWet(),
            ["Apply"] = DoApplyWetness(),
        };

        return ret;
    }

    #endregion

    #region Deserialization

    public static Design LoadDesign(CustomizationService customizations, ItemManager items, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            1 => LoadDesignV1(customizations, items, json),
            _ => throw new Exception("The design to be loaded has no valid Version."),
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

        LoadEquip(items, json["Equipment"], design);
        LoadCustomize(customizations, json["Customize"], design);
        return design;
    }

    private static void LoadEquip(ItemManager items, JToken? equip, Design design)
    {
        if (equip == null)
        {
            design.DesignData.SetDefaultEquipment(items);
            Glamourer.Chat.NotificationMessage("The loaded design does not contain any equipment data, reset to default.", "Warning",
                NotificationType.Warning);
            return;
        }

        static (uint, StainId, bool, bool) ParseItem(EquipSlot slot, JToken? item)
        {
            var id         = item?["ItemId"]?.ToObject<uint>() ?? ItemManager.NothingId(slot);
            var stain      = (StainId)(item?["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = item?["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = item?["ApplyStain"]?.ToObject<bool>() ?? false;
            return (id, stain, apply, applyStain);
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Chat.NotificationMessage($"{msg} ({design.Name})", "Warning", NotificationType.Warning);
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stain, apply, applyStain) = ParseItem(slot, equip[slot.ToString()]);

            PrintWarning(items.ValidateItem(slot, id, out var item));
            PrintWarning(items.ValidateStain(stain, out stain));
            design.DesignData.SetItem(slot, item);
            design.DesignData.SetStain(slot, stain);
            design.SetApplyEquip(slot, apply);
            design.SetApplyStain(slot, applyStain);
        }

        {
            var (id, stain, apply, applyStain) = ParseItem(EquipSlot.MainHand, equip[EquipSlot.MainHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.MainHand))
                id = items.DefaultSword.Id;
            var (idOff, stainOff, applyOff, applyStainOff) = ParseItem(EquipSlot.OffHand, equip[EquipSlot.OffHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.OffHand))
                id = ItemManager.NothingId(FullEquipType.Shield);

            PrintWarning(items.ValidateWeapons(id, idOff, out var main, out var off));
            PrintWarning(items.ValidateStain(stain,    out stain));
            PrintWarning(items.ValidateStain(stainOff, out stainOff));
            design.DesignData.SetItem(EquipSlot.MainHand, main);
            design.DesignData.SetItem(EquipSlot.OffHand,  off);
            design.DesignData.SetStain(EquipSlot.MainHand, stain);
            design.DesignData.SetStain(EquipSlot.OffHand,  stainOff);
            design.SetApplyEquip(EquipSlot.MainHand, apply);
            design.SetApplyEquip(EquipSlot.OffHand,  applyOff);
            design.SetApplyStain(EquipSlot.MainHand, applyStain);
            design.SetApplyStain(EquipSlot.OffHand,  applyStainOff);
        }
        var metaValue = QuadBool.FromJObject(equip["Hat"], "Show", "Apply", QuadBool.NullFalse);
        design.SetApplyHatVisible(metaValue.Enabled);
        design.DesignData.SetHatVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Weapon"], "Show", "Apply", QuadBool.NullFalse);
        design.SetApplyWeaponVisible(metaValue.Enabled);
        design.DesignData.SetWeaponVisible(metaValue.ForcedValue);

        metaValue = QuadBool.FromJObject(equip["Visor"], "IsToggled", "Apply", QuadBool.NullFalse);
        design.SetApplyVisorToggle(metaValue.Enabled);
        design.DesignData.SetVisor(metaValue.ForcedValue);
    }

    private static void LoadCustomize(CustomizationService customizations, JToken? json, Design design)
    {
        if (json == null)
        {
            design.DesignData.ModelId   = 0;
            design.DesignData.Customize = Customize.Default;
            Glamourer.Chat.NotificationMessage("The loaded design does not contain any customization data, reset to default.", "Warning",
                NotificationType.Warning);
            return;
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Chat.NotificationMessage($"{msg} ({design.Name})", "Warning", NotificationType.Warning);
        }

        design.DesignData.ModelId = json["ModelId"]?.ToObject<uint>() ?? 0;
        PrintWarning(customizations.ValidateModelId(design.DesignData.ModelId, out design.DesignData.ModelId));

        var race = (Race)(json[CustomizeIndex.Race.ToString()]?["Value"]?.ToObject<byte>() ?? 0);
        var clan = (SubRace)(json[CustomizeIndex.Clan.ToString()]?["Value"]?.ToObject<byte>() ?? 0);
        PrintWarning(customizations.ValidateClan(clan, race, out race, out clan));
        var gender = (Gender)((json[CustomizeIndex.Gender.ToString()]?["Value"]?.ToObject<byte>() ?? 0) + 1);
        PrintWarning(customizations.ValidateGender(race, gender, out gender));
        design.DesignData.Customize.Race   = race;
        design.DesignData.Customize.Clan   = clan;
        design.DesignData.Customize.Gender = gender;
        design.SetApplyCustomize(CustomizeIndex.Race,   json[CustomizeIndex.Race.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Clan,   json[CustomizeIndex.Clan.ToString()]?["Apply"]?.ToObject<bool>() ?? false);
        design.SetApplyCustomize(CustomizeIndex.Gender, json[CustomizeIndex.Gender.ToString()]?["Apply"]?.ToObject<bool>() ?? false);

        var set = customizations.AwaitedService.GetList(clan, gender);

        foreach (var idx in Enum.GetValues<CustomizeIndex>().Where(set.IsAvailable))
        {
            var tok  = json[idx.ToString()];
            var data = (CustomizeValue)(tok?["Value"]?.ToObject<byte>() ?? 0);
            PrintWarning(CustomizationService.ValidateCustomizeValue(set, design.DesignData.Customize.Face, idx, data, out data));
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            design.DesignData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }

        var wetness = QuadBool.FromJObject(json["Wetness"], "Value", "Apply", QuadBool.NullFalse);
        design.DesignData.SetIsWet(wetness.ForcedValue);
        design.SetApplyWetness(wetness.Enabled);
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

    public void MigrateBase64(ItemManager items, string base64)
    {
        DesignData = DesignBase64Migration.MigrateBase64(items, base64, out var equipFlags, out var customizeFlags, out var writeProtected,
            out var applyHat, out var applyVisor, out var applyWeapon);
        ApplyEquip     = equipFlags;
        ApplyCustomize = customizeFlags;
        SetWriteProtected(writeProtected);
        SetApplyHatVisible(applyHat);
        SetApplyVisorToggle(applyVisor);
        SetApplyWeaponVisible(applyWeapon);
        SetApplyWetness(DesignData.IsWet());
    }

    //
    //public static Design CreateTemporaryFromBase64(ItemManager items, string base64, bool customize, bool equip)
    //{
    //    var ret = new Design(items);
    //    ret.MigrateBase64(items, base64);
    //    if (!customize)
    //        ret._applyCustomize = 0;
    //    if (!equip)
    //        ret._applyEquip = 0;
    //    ret.Wetness = ret.Wetness.SetEnabled(customize);
    //    ret.Visor   = ret.Visor.SetEnabled(equip);
    //    ret.Hat     = ret.Hat.SetEnabled(equip);
    //    ret.Weapon  = ret.Weapon.SetEnabled(equip);
    //    return ret;
    //}

    // Outdated.
    //public string CreateOldBase64()
    //    => DesignBase64Migration.CreateOldBase64(in ModelData, _applyEquip, _applyCustomize, Wetness == QuadBool.True, Hat.ForcedValue,
    //        Hat.Enabled,
    //        Visor.ForcedValue, Visor.Enabled, Weapon.ForcedValue, Weapon.Enabled, WriteProtected, 1f);
}
