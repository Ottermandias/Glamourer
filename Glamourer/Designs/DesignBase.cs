using System;
using System.Linq;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignBase
{
    public const int FileVersion = 1;

    internal DesignBase(ItemManager items)
    {
        DesignData.SetDefaultEquipment(items);
    }

    internal DesignBase(DesignBase clone)
    {
        DesignData     = clone.DesignData;
        ApplyCustomize = clone.ApplyCustomize & CustomizeFlagExtensions.All;
        ApplyEquip     = clone.ApplyEquip & EquipFlagExtensions.All;
        _designFlags   = clone._designFlags & (DesignFlags)0x0F;
    }

    internal DesignData DesignData = new();

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
        => idx is not CustomizeIndex.Race and not CustomizeIndex.BodyType && ApplyCustomize.HasFlag(idx.ToFlag());

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

    public JObject JsonSerialize()
    {
        var ret = new JObject
        {
            ["FileVersion"] = FileVersion,
            ["Equipment"]   = SerializeEquipment(),
            ["Customize"]   = SerializeCustomize(),
        };
        return ret;
    }

    protected JObject SerializeEquipment()
    {
        static JObject Serialize(ulong id, StainId stain, bool apply, bool applyStain)
            => new()
            {
                ["ItemId"]     = id,
                ["Stain"]      = stain.Value,
                ["Apply"]      = apply,
                ["ApplyStain"] = applyStain,
            };

        var ret = new JObject();
        if (DesignData.IsHuman)
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                var item  = DesignData.Item(slot);
                var stain = DesignData.Stain(slot);
                ret[slot.ToString()] = Serialize(item.Id, stain, DoApplyEquip(slot), DoApplyStain(slot));
            }

            ret["Hat"]    = new QuadBool(DesignData.IsHatVisible(),    DoApplyHatVisible()).ToJObject("Show", "Apply");
            ret["Visor"]  = new QuadBool(DesignData.IsVisorToggled(),  DoApplyVisorToggle()).ToJObject("IsToggled", "Apply");
            ret["Weapon"] = new QuadBool(DesignData.IsWeaponVisible(), DoApplyWeaponVisible()).ToJObject("Show", "Apply");
        }
        else
        {
            ret["Array"] = DesignData.WriteEquipmentBytesBase64();
        }

        return ret;
    }

    protected JObject SerializeCustomize()
    {
        var ret = new JObject()
        {
            ["ModelId"] = DesignData.ModelId,
        };

        var customize = DesignData.Customize;
        if (DesignData.IsHuman)
            foreach (var idx in Enum.GetValues<CustomizeIndex>())
            {
                ret[idx.ToString()] = new JObject()
                {
                    ["Value"] = customize[idx].Value,
                    ["Apply"] = DoApplyCustomize(idx),
                };
            }
        else
            ret["Array"] = customize.WriteBase64();

        ret["Wetness"] = new JObject()
        {
            ["Value"] = DesignData.IsWet(),
            ["Apply"] = DoApplyWetness(),
        };

        return ret;
    }

    #endregion

    #region Deserialization

    public static DesignBase LoadDesignBase(CustomizationService customizations, ItemManager items, JObject json)
    {
        var version = json["FileVersion"]?.ToObject<int>() ?? 0;
        return version switch
        {
            FileVersion => LoadDesignV1Base(customizations, items, json),
            _           => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static DesignBase LoadDesignV1Base(CustomizationService customizations, ItemManager items, JObject json)
    {
        var ret = new DesignBase(items);
        LoadCustomize(customizations, json["Customize"], ret, "Temporary Design", false, true);
        LoadEquip(items, json["Equipment"], ret, "Temporary Design", true);
        return ret;
    }

    protected static void LoadEquip(ItemManager items, JToken? equip, DesignBase design, string name, bool allowUnknown)
    {
        if (equip == null)
        {
            design.DesignData.SetDefaultEquipment(items);
            Glamourer.Chat.NotificationMessage("The loaded design does not contain any equipment data, reset to default.", "Warning",
                NotificationType.Warning);
            return;
        }

        if (!design.DesignData.IsHuman)
        {
            var textArray = equip["Array"]?.ToObject<string>() ?? string.Empty;
            design.DesignData.SetEquipmentBytesFromBase64(textArray);
            return;
        }

        static (ulong, StainId, bool, bool) ParseItem(EquipSlot slot, JToken? item)
        {
            var id         = item?["ItemId"]?.ToObject<ulong>() ?? ItemManager.NothingId(slot);
            var stain      = (StainId)(item?["Stain"]?.ToObject<byte>() ?? 0);
            var apply      = item?["Apply"]?.ToObject<bool>() ?? false;
            var applyStain = item?["ApplyStain"]?.ToObject<bool>() ?? false;
            return (id, stain, apply, applyStain);
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Chat.NotificationMessage($"{msg} ({name})", "Warning", NotificationType.Warning);
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var (id, stain, apply, applyStain) = ParseItem(slot, equip[slot.ToString()]);

            PrintWarning(items.ValidateItem(slot, id, out var item, allowUnknown));
            PrintWarning(items.ValidateStain(stain, out stain, allowUnknown));
            design.DesignData.SetItem(slot, item);
            design.DesignData.SetStain(slot, stain);
            design.SetApplyEquip(slot, apply);
            design.SetApplyStain(slot, applyStain);
        }

        {
            var (id, stain, apply, applyStain) = ParseItem(EquipSlot.MainHand, equip[EquipSlot.MainHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.MainHand))
                id = items.DefaultSword.ItemId;
            var (idOff, stainOff, applyOff, applyStainOff) = ParseItem(EquipSlot.OffHand, equip[EquipSlot.OffHand.ToString()]);
            if (id == ItemManager.NothingId(EquipSlot.OffHand))
                id = ItemManager.NothingId(FullEquipType.Shield);

            PrintWarning(items.ValidateWeapons((uint)id, (uint)idOff, out var main, out var off));
            PrintWarning(items.ValidateStain(stain,    out stain,    allowUnknown));
            PrintWarning(items.ValidateStain(stainOff, out stainOff, allowUnknown));
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

    protected static void LoadCustomize(CustomizationService customizations, JToken? json, DesignBase design, string name, bool forbidNonHuman,
        bool allowUnknown)
    {
        if (json == null)
        {
            design.DesignData.ModelId   = 0;
            design.DesignData.IsHuman   = true;
            design.DesignData.Customize = Customize.Default;
            Glamourer.Chat.NotificationMessage("The loaded design does not contain any customization data, reset to default.", "Warning",
                NotificationType.Warning);
            return;
        }

        void PrintWarning(string msg)
        {
            if (msg.Length > 0)
                Glamourer.Chat.NotificationMessage($"{msg} ({name})", "Warning", NotificationType.Warning);
        }

        var wetness = QuadBool.FromJObject(json["Wetness"], "Value", "Apply", QuadBool.NullFalse);
        design.DesignData.SetIsWet(wetness.ForcedValue);
        design.SetApplyWetness(wetness.Enabled);

        design.DesignData.ModelId = json["ModelId"]?.ToObject<uint>() ?? 0;
        PrintWarning(customizations.ValidateModelId(design.DesignData.ModelId, out design.DesignData.ModelId, out design.DesignData.IsHuman));
        if (design.DesignData.ModelId != 0 && forbidNonHuman)
        {
            PrintWarning("Model IDs different from 0 are not currently allowed, reset model id to 0.");
            design.DesignData.ModelId = 0;
            design.DesignData.IsHuman = true;
        }
        else if (!design.DesignData.IsHuman)
        {
            var arrayText = json["Array"]?.ToObject<string>() ?? string.Empty;
            design.DesignData.Customize.LoadBase64(arrayText);
            return;
        }

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
            PrintWarning(CustomizationService.ValidateCustomizeValue(set, design.DesignData.Customize.Face, idx, data, out data, allowUnknown));
            var apply = tok?["Apply"]?.ToObject<bool>() ?? false;
            design.DesignData.Customize[idx] = data;
            design.SetApplyCustomize(idx, apply);
        }
    }

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

    #endregion
}
