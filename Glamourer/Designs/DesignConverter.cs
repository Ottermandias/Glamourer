using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignConverter(ItemManager _items, DesignManager _designs, CustomizationService _customize, HumanModelList _humans)
{
    public const byte Version = 6;

    public JObject ShareJObject(DesignBase design)
        => design.JsonSerialize();

    public JObject ShareJObject(Design design)
        => design.JsonSerialize();

    public JObject ShareJObject(ActorState state, EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags)
    {
        var design = Convert(state, equipFlags, customizeFlags, crestFlags);
        return ShareJObject(design);
    }

    public string ShareBase64(Design design)
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(DesignBase design)
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(ActorState state)
        => ShareBase64(state, EquipFlagExtensions.All, CustomizeFlagExtensions.All, CrestExtensions.All);

    public string ShareBase64(ActorState state, EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags)
    {
        var design = Convert(state, equipFlags, customizeFlags, crestFlags);
        return ShareBase64(ShareJObject(design));
    }

    public DesignBase Convert(ActorState state, EquipFlag equipFlags, CustomizeFlag customizeFlags, CrestFlag crestFlags)
    {
        var design = _designs.CreateTemporary();
        design.ApplyEquip     = equipFlags & EquipFlagExtensions.All;
        design.ApplyCustomize = customizeFlags & CustomizeFlagExtensions.AllRelevant;
        design.ApplyCrest     = crestFlags & CrestExtensions.All;
        design.SetApplyHatVisible(design.DoApplyEquip(EquipSlot.Head));
        design.SetApplyVisorToggle(design.DoApplyEquip(EquipSlot.Head));
        design.SetApplyWeaponVisible(design.DoApplyEquip(EquipSlot.MainHand) || design.DoApplyEquip(EquipSlot.OffHand));
        design.SetApplyWetness(true);
        design.SetDesignData(_customize, state.ModelData);
        return design;
    }

    public DesignBase? FromBase64(string base64, bool customize, bool equip, out byte version)
    {
        DesignBase ret;
        version = 0;
        try
        {
            var bytes = System.Convert.FromBase64String(base64);
            version = bytes[0];
            switch (version)
            {
                case (byte)'{':
                    var jObj1 = JObject.Parse(Encoding.UTF8.GetString(bytes));
                    ret = jObj1["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj1)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj1);
                    break;
                case 1:
                case 2:
                case 4:
                    ret = _designs.CreateTemporary();
                    ret.MigrateBase64(_customize, _items, _humans, base64);
                    break;
                case 3:
                {
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 3);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj2)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj2);
                    break;
                }
                case 5:
                {
                    bytes   = bytes[DesignBase64Migration.Base64SizeV4..];
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 5);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj2)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj2);
                    break;
                }
                case 6:
                {
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 6);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj2)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj2);
                    break;
                }

                default: throw new Exception($"Unknown Version {bytes[0]}.");
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"[DesignConverter] Could not parse base64 string [{base64}]:\n{ex}");
            return null;
        }

        ret.SetApplyWetness(customize);
        ret.ApplyCustomize = customize ? CustomizeFlagExtensions.AllRelevant : 0;

        if (!equip)
        {
            ret.ApplyEquip = 0;
            ret.ApplyCrest = 0;
            ret.SetApplyHatVisible(false);
            ret.SetApplyWeaponVisible(false);
            ret.SetApplyVisorToggle(false);
        }

        return ret;
    }

    private static string ShareBase64(JObject jObject)
    {
        var json       = jObject.ToString(Formatting.None);
        var compressed = json.Compress(Version);
        return System.Convert.ToBase64String(compressed);
    }

    public IEnumerable<(EquipSlot Slot, EquipItem Item, StainId Stain)> FromDrawData(IReadOnlyList<CharacterArmor> armors,
        CharacterWeapon mainhand, CharacterWeapon offhand)
    {
        if (armors.Count != 10)
            throw new ArgumentException("Invalid length of armor array.");

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var index = (int)slot.ToIndex();
            var armor = armors[index];
            var item  = _items.Identify(slot, armor.Set, armor.Variant);
            if (!item.Valid)
            {
                Glamourer.Log.Warning($"Appearance data {armor} for slot {slot} invalid, item could not be identified.");
                item = ItemManager.NothingItem(slot);
            }

            yield return (slot, item, armor.Stain);
        }

        var mh = _items.Identify(EquipSlot.MainHand, mainhand.Skeleton, mainhand.Weapon, mainhand.Variant);
        if (!mh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {mainhand} for mainhand weapon invalid, item could not be identified.");
            mh = _items.DefaultSword;
        }

        yield return (EquipSlot.MainHand, mh, mainhand.Stain);

        var oh = _items.Identify(EquipSlot.OffHand, offhand.Skeleton, offhand.Weapon, offhand.Variant, mh.Type);
        if (!oh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {offhand} for offhand weapon invalid, item could not be identified.");
            oh = _items.GetDefaultOffhand(mh);
            if (!oh.Valid)
                oh = ItemManager.NothingItem(FullEquipType.Shield);
        }

        yield return (EquipSlot.OffHand, oh, offhand.Stain);
    }
}
