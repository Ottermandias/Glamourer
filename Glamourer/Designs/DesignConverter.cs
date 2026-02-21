using Glamourer.Designs.Links;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Utility;
using Luna;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public sealed class DesignConverter(
    SaveService saveService,
    ItemManager items,
    DesignManager designs,
    CustomizeService customizeService,
    HumanModelList humans,
    DesignLinkLoader linkLoader) : IService
{
    public const byte Version = 6;

    public JObject ShareJObject(DesignBase design)
        => design.JsonSerialize();

    public JObject ShareJObject(Design design)
        => design.JsonSerialize();

    public JObject ShareJObject(ActorState state, in ApplicationRules rules)
    {
        var design = Convert(state, rules);
        return ShareJObject(design);
    }

    public string ShareBase64(Design design)
        => ToBase64(ShareJObject(design));

    public string ShareBase64(DesignBase design)
        => ToBase64(ShareJObject(design));

    public string ShareBase64(ActorState state, in ApplicationRules rules)
        => ShareBase64(state.ModelData, state.Materials, rules);

    public string ShareBase64(in DesignData data, in StateMaterialManager materials, in ApplicationRules rules)
    {
        var design = Convert(data, materials, rules);
        return ToBase64(ShareJObject(design));
    }

    public DesignBase Convert(ActorState state, in ApplicationRules rules)
        => Convert(state.ModelData, state.Materials, rules);

    public DesignBase Convert(in DesignData data, in StateMaterialManager materials, in ApplicationRules rules)
    {
        var design = designs.CreateTemporary();
        rules.Apply(design);
        design.SetDesignData(customizeService, data);
        if (rules.Materials)
            ComputeMaterials(design.GetMaterialDataRef(), materials, rules.Equip);
        return design;
    }

    public DesignBase? FromJObject(JObject? jObject, bool customize, bool equip)
    {
        if (jObject == null)
            return null;

        try
        {
            var ret = jObject["Identifier"] != null
                ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObject)
                : DesignBase.LoadDesignBase(customizeService, items, jObject);

            if (!customize)
                ret.Application.RemoveCustomize();

            if (!equip)
                ret.Application.RemoveEquip();

            return ret;
        }
        catch (Exception ex)
        {
            Glamourer.Log.Warning($"Failure to parse JObject to design:\n{ex}");
            return null;
        }
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
                        ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj1)
                        : DesignBase.LoadDesignBase(customizeService, items, jObj1);
                    break;
                case 1:
                case 2:
                case 4:
                    ret = designs.CreateTemporary();
                    ret.MigrateBase64(customizeService, items, humans, base64);
                    break;
                case 3:
                {
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 3);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                        : DesignBase.LoadDesignBase(customizeService, items, jObj2);
                    break;
                }
                case 5:
                {
                    bytes   = bytes[DesignBase64Migration.Base64SizeV4..];
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 5);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                        : DesignBase.LoadDesignBase(customizeService, items, jObj2);
                    break;
                }
                case 6:
                {
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 6);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                        : DesignBase.LoadDesignBase(customizeService, items, jObj2);
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

        if (!customize)
            ret.Application.RemoveCustomize();

        if (!equip)
            ret.Application.RemoveEquip();

        return ret;
    }

    public static string ToBase64(JToken jObject)
    {
        var json       = jObject.ToString(Formatting.None);
        var compressed = json.Compress(Version);
        return System.Convert.ToBase64String(compressed);
    }

    public IEnumerable<(EquipSlot Slot, EquipItem Item, StainIds Stains)> FromDrawData(IReadOnlyList<CharacterArmor> armors,
        CharacterWeapon mainhand, CharacterWeapon offhand, bool skipWarnings)
    {
        if (armors.Count != 10)
            throw new ArgumentException("Invalid length of armor array.");

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var index = (int)slot.ToIndex();
            var armor = armors[index];
            var item  = items.Identify(slot, armor.Set, armor.Variant);
            if (!item.Valid)
            {
                if (!skipWarnings)
                    Glamourer.Log.Warning($"Appearance data {armor} for slot {slot} invalid, item could not be identified.");
                item = ItemManager.NothingItem(slot);
            }

            yield return (slot, item, armor.Stains);
        }

        var mh = items.Identify(EquipSlot.MainHand, mainhand.Skeleton, mainhand.Weapon, mainhand.Variant);
        if (!skipWarnings && !mh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {mainhand} for mainhand weapon invalid, item could not be identified.");
            mh = items.DefaultSword;
        }

        yield return (EquipSlot.MainHand, mh, mainhand.Stains);

        var oh = items.Identify(EquipSlot.OffHand, offhand.Skeleton, offhand.Weapon, offhand.Variant, mh.Type);
        if (!skipWarnings && !oh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {offhand} for offhand weapon invalid, item could not be identified.");
            oh = items.GetDefaultOffhand(mh);
            if (!oh.Valid)
                oh = ItemManager.NothingItem(FullEquipType.Shield);
        }

        yield return (EquipSlot.OffHand, oh, offhand.Stains);
    }

    private static void ComputeMaterials(DesignMaterialManager manager, in StateMaterialManager materials,
        EquipFlag equipFlags = EquipFlagExtensions.All, BonusItemFlag bonusFlags = BonusExtensions.All)
    {
        foreach (var (key, value) in materials.Values)
        {
            var idx = MaterialValueIndex.FromKey(key);
            if (idx.RowIndex >= ColorTable.NumRows)
                continue;
            if (idx.MaterialIndex >= MaterialService.MaterialsPerModel)
                continue;

            switch (idx.DrawObject)
            {
                case MaterialValueIndex.DrawObjectType.Mainhand when idx.SlotIndex == 0:
                    if ((equipFlags & (EquipFlag.Mainhand | EquipFlag.MainhandStain)) == 0)
                        continue;

                    break;
                case MaterialValueIndex.DrawObjectType.Offhand when idx.SlotIndex == 0:
                    if ((equipFlags & (EquipFlag.Offhand | EquipFlag.OffhandStain)) == 0)
                        continue;

                    break;
                case MaterialValueIndex.DrawObjectType.Human:
                    if (idx.SlotIndex < 10)
                    {
                        if ((((uint)idx.SlotIndex).ToEquipSlot().ToBothFlags() & equipFlags) == 0)
                            continue;
                    }
                    else if (idx.SlotIndex >= 16)
                    {
                        if (((idx.SlotIndex - 16u).ToBonusSlot() & bonusFlags) == 0)
                            continue;
                    }

                    break;
                default: continue;
            }

            manager.AddOrUpdateValue(idx, value.Convert());
        }
    }
}
