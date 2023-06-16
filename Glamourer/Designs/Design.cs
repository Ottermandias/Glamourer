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
    internal Design(ItemManager items)
    { }

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

    #region Application Data

    [Flags]
    private enum DesignFlags : byte
    {
        ApplyHatVisible    = 0x01,
        ApplyVisorState    = 0x02,
        ApplyWeaponVisible = 0x04,
        WriteProtected     = 0x08,
    }

    private CustomizeFlag _applyCustomize;
    private EquipFlag     _applyEquip;
    private DesignFlags   _designFlags;

    public bool DoApplyHatVisible()
        => _designFlags.HasFlag(DesignFlags.ApplyHatVisible);

    public bool DoApplyVisorToggle()
        => _designFlags.HasFlag(DesignFlags.ApplyVisorState);

    public bool DoApplyWeaponVisible()
        => _designFlags.HasFlag(DesignFlags.ApplyWeaponVisible);

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

    public bool SetWriteProtected(bool value)
    {
        var newFlag = value ? _designFlags | DesignFlags.WriteProtected : _designFlags & ~DesignFlags.WriteProtected;
        if (newFlag == _designFlags)
            return false;

        _designFlags = newFlag;
        return true;
    }


    public bool DoApplyEquip(EquipSlot slot)
        => _applyEquip.HasFlag(slot.ToFlag());

    public bool DoApplyStain(EquipSlot slot)
        => _applyEquip.HasFlag(slot.ToStainFlag());

    public bool DoApplyCustomize(CustomizeIndex idx)
        => _applyCustomize.HasFlag(idx.ToFlag());

    internal bool SetApplyEquip(EquipSlot slot, bool value)
    {
        var newValue = value ? _applyEquip | slot.ToFlag() : _applyEquip & ~slot.ToFlag();
        if (newValue == _applyEquip)
            return false;

        _applyEquip = newValue;
        return true;
    }

    internal bool SetApplyStain(EquipSlot slot, bool value)
    {
        var newValue = value ? _applyEquip | slot.ToStainFlag() : _applyEquip & ~slot.ToStainFlag();
        if (newValue == _applyEquip)
            return false;

        _applyEquip = newValue;
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

    #endregion

    #region ISavable

    public JObject JsonSerialize()
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

    public JObject SerializeEquipment()
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

    public JObject SerializeCustomize()
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

        ret["IsWet"] = DesignData.IsWet();
        return ret;
    }

    public static Design LoadDesign(CustomizationManager customizeManager, ItemManager items, JObject json, out bool changes)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            1 => LoadDesignV1(customizeManager, items, json, out changes),
            _ => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Design LoadDesignV1(CustomizationManager customizeManager, ItemManager items, JObject json, out bool changes)
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

        changes =  LoadEquip(items, json["Equipment"], design);
        changes |= LoadCustomize(customizeManager, json["Customize"], design);
        return design;
    }

    private static bool ValidateItem(ItemManager items, EquipSlot slot, uint itemId, out EquipItem item)
    {
        item = items.Resolve(slot, itemId);
        if (item.Valid)
            return true;

        Glamourer.Chat.NotificationMessage($"The {slot.ToName()} item {itemId} does not exist, reset to Nothing.", "Warning",
            NotificationType.Warning);
        item = ItemManager.NothingItem(slot);
        return false;
    }

    private static bool ValidateStain(ItemManager items, StainId stain, out StainId ret)
    {
        if (stain.Value != 0 && !items.Stains.ContainsKey(stain))
        {
            ret = 0;
            Glamourer.Chat.NotificationMessage($"The Stain {stain} does not exist, reset to unstained.");
            return false;
        }

        ret = stain;
        return true;
    }

    private static bool ValidateWeapons(ItemManager items, uint mainId, uint offId, out EquipItem main, out EquipItem off)
    {
        var ret = true;
        main = items.Resolve(EquipSlot.MainHand, mainId);
        if (!main.Valid)
        {
            Glamourer.Chat.NotificationMessage($"The mainhand weapon {mainId} does not exist, reset to default sword.", "Warning",
                NotificationType.Warning);
            main = items.DefaultSword;
            ret  = false;
        }

        off = items.Resolve(main.Type.Offhand(), offId);
        if (off.Valid)
            return ret;

        ret = false;
        off = items.Resolve(main.Type.Offhand(), mainId);
        if (off.Valid)
        {
            Glamourer.Chat.NotificationMessage($"The offhand weapon {offId} does not exist, reset to implied offhand.", "Warning",
                NotificationType.Warning);
        }
        else
        {
            off = ItemManager.NothingItem(FullEquipType.Shield);
            if (main.Type.Offhand() == FullEquipType.Shield)
            {
                Glamourer.Chat.NotificationMessage($"The offhand weapon {offId} does not exist, reset to no offhand.", "Warning",
                    NotificationType.Warning);
            }
            else
            {
                main = items.DefaultSword;
                Glamourer.Chat.NotificationMessage(
                    $"The offhand weapon {offId} does not exist, but no default could be restored, reset mainhand to default sword and offhand to nothing.",
                    "Warning",
                    NotificationType.Warning);
            }
        }

        return ret;
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
            changes                            |= !ValidateItem(items, slot, id, out var item);
            changes                            |= !ValidateStain(items, stain, out stain);
            design.DesignData.SetItem(item);
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
            changes |= ValidateWeapons(items, id, idOff, out var main, out var off);
            changes |= ValidateStain(items, stain,    out stain);
            changes |= ValidateStain(items, stainOff, out stainOff);
            design.DesignData.SetItem(main);
            design.DesignData.SetItem(off);
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

        return changes;
    }

    private static bool ValidateCustomize(CustomizationManager manager, ref Customize customize)
    {
        var ret = true;
        if (!manager.Races.Contains(customize.Race))
        {
            ret = false;
            if (manager.Clans.Contains(customize.Clan))
            {
                Glamourer.Chat.NotificationMessage(
                    $"The race {customize.Race.ToName()} is unknown, reset to {customize.Clan.ToRace().ToName()} from Clan.", "Warning",
                    NotificationType.Warning);
                customize.Race = customize.Clan.ToRace();
            }
            else
            {
                Glamourer.Chat.NotificationMessage(
                    $"The race {customize.Race.ToName()} is unknown, reset to {Race.Hyur.ToName()} {SubRace.Midlander.ToName()}.", "Warning",
                    NotificationType.Warning);
                customize.Race = Race.Hyur;
                customize.Clan = SubRace.Midlander;
            }
        }

        if (!manager.Clans.Contains(customize.Clan))
        {
            ret = false;
            var oldClan = customize.Clan;
            customize.Clan = (SubRace)((byte)customize.Race * 2 - 1);
            if (manager.Clans.Contains(customize.Clan))
            {
                Glamourer.Chat.NotificationMessage($"The clan {oldClan.ToName()} is unknown, reset to {customize.Clan.ToName()} from race.",
                    "Warning", NotificationType.Warning);
            }
            else
            {
                customize.Race = Race.Hyur;
                customize.Clan = SubRace.Midlander;
                Glamourer.Chat.NotificationMessage(
                    $"The clan {oldClan.ToName()} is unknown, reset to {customize.Race.ToName()} {customize.Clan.ToName()}.", "Warning",
                    NotificationType.Warning);
            }
        }

        if (!manager.Genders.Contains(customize.Gender))
        {
            ret = false;
            Glamourer.Chat.NotificationMessage($"The gender {customize.Gender} is unknown, reset to {Gender.Male.ToName()}.", "Warning",
                NotificationType.Warning);
            customize.Gender = Gender.Male;
        }

        // TODO: Female Hrothgar
        if (customize.Gender == Gender.Female && customize.Race == Race.Hrothgar)
        {
            ret = false;
            Glamourer.Chat.NotificationMessage($"Hrothgar do not currently support female characters, reset to male.", "Warning",
                NotificationType.Warning);
            customize.Gender = Gender.Male;
        }

        var list = manager.GetList(customize.Clan, customize.Gender);

        // Face is handled first automatically so it should not conflict with other customizations when corrupt.
        foreach (var index in Enum.GetValues<CustomizeIndex>().Where(list.IsAvailable))
        {
            var value = customize.Get(index);
            var count = list.Count(index, customize.Face);
            var idx   = list.DataByValue(index, value, out var data, customize.Face);
            if (idx >= 0 && idx < count)
                continue;

            ret = false;
            var name     = list.Option(index);
            var newValue = list.Data(index, 0, customize.Face);
            Glamourer.Chat.NotificationMessage(
                $"Customization {name} for {customize.Race.ToName()} {customize.Gender.ToName()}s does not support value {value.Value}, reset to {newValue.Value.Value}");
            customize.Set(index, newValue.Value);
        }

        return ret;
    }

    private static bool ValidateModelId(ref uint modelId)
    {
        if (modelId != 0)
        {
            Glamourer.Chat.NotificationMessage($"Model IDs different from 0 are not currently allowed, reset {modelId} to 0.", "Warning",
                NotificationType.Warning);
            modelId = 0;
            return false;
        }

        return true;
    }

    private static bool LoadCustomize(CustomizationManager manager, JToken? json, Design design)
    {
        if (json == null)
            return true;

        design.DesignData.ModelId = json["ModelId"]?.ToObject<uint>() ?? 0;
        var ret = !ValidateModelId(ref design.DesignData.ModelId);

        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var tok   = json[idx.ToString()];
            var data  = (CustomizeValue)(tok?["Value"]?.ToObject<byte>() ?? 0);
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            design.DesignData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }

        design.DesignData.SetIsWet(json["IsWet"]?.ToObject<bool>() ?? false);
        ret |= !ValidateCustomize(manager, ref design.DesignData.Customize);

        return ret;
    }

    //public void MigrateBase64(ItemManager items, string base64)
    //{
    //    var data = DesignBase64Migration.MigrateBase64(items, base64, out var applyEquip, out var applyCustomize, out var writeProtected, out var wet,
    //        out var hat,
    //        out var visor, out var weapon);
    //    UpdateMainhand(items, data.MainHand);
    //    UpdateOffhand(items, data.OffHand);
    //    foreach (var slot in EquipSlotExtensions.EqdpSlots)
    //        UpdateArmor(items, slot, data.Armor(slot), true);
    //    ModelData.Customize = data.Customize;
    //    _applyEquip         = applyEquip;
    //    _applyCustomize     = applyCustomize;
    //    WriteProtected      = writeProtected;
    //    Wetness             = wet;
    //    Hat                 = hat;
    //    Visor               = visor;
    //    Weapon              = weapon;
    //}
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
