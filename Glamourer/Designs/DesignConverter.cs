using Glamourer.Designs.Links;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs;

public class DesignConverter(
    ItemManager _items,
    DesignManager _designs,
    CustomizeService _customize,
    HumanModelList _humans,
    DesignLinkLoader _linkLoader)
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
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(DesignBase design)
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(ActorState state, in ApplicationRules rules)
        => ShareBase64(state.ModelData, state.Materials, rules);

    public string ShareBase64(in DesignData data, in StateMaterialManager materials, in ApplicationRules rules)
    {
        var design = Convert(data, materials, rules);
        return ShareBase64(ShareJObject(design));
    }

    public DesignBase Convert(ActorState state, in ApplicationRules rules)
        => Convert(state.ModelData, state.Materials, rules);

    public DesignBase Convert(in DesignData data, in StateMaterialManager materials, in ApplicationRules rules)
    {
        var design = _designs.CreateTemporary();
        rules.Apply(design);
        design.SetDesignData(_customize, data);
        if (rules.Materials)
            ComputeMaterials(design.GetMaterialDataRef(), materials, rules.Equip);
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
                        ? Design.LoadDesign(_customize, _items, _linkLoader, jObj1)
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
                        ? Design.LoadDesign(_customize, _items, _linkLoader, jObj2)
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
                        ? Design.LoadDesign(_customize, _items, _linkLoader, jObj2)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj2);
                    break;
                }
                case 6:
                {
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == 6);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, _linkLoader, jObj2)
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

        ret.SetApplyMeta(MetaIndex.Wetness, customize);
        if (!customize)
            ret.ApplyCustomize = 0;

        if (!equip)
        {
            ret.ApplyEquip =  0;
            ret.ApplyCrest =  0;
            ret.ApplyMeta  &= ~(MetaFlag.HatState | MetaFlag.WeaponState | MetaFlag.VisorState);
        }

        return ret;
    }

    private static string ShareBase64(JToken jObject)
    {
        var json       = jObject.ToString(Formatting.None);
        var compressed = json.Compress(Version);
        return System.Convert.ToBase64String(compressed);
    }

    public IEnumerable<(EquipSlot Slot, EquipItem Item, StainId Stain)> FromDrawData(IReadOnlyList<CharacterArmor> armors,
        CharacterWeapon mainhand, CharacterWeapon offhand, bool skipWarnings)
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
                if (!skipWarnings)
                    Glamourer.Log.Warning($"Appearance data {armor} for slot {slot} invalid, item could not be identified.");
                item = ItemManager.NothingItem(slot);
            }

            yield return (slot, item, armor.Stain);
        }

        var mh = _items.Identify(EquipSlot.MainHand, mainhand.Skeleton, mainhand.Weapon, mainhand.Variant);
        if (!skipWarnings && !mh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {mainhand} for mainhand weapon invalid, item could not be identified.");
            mh = _items.DefaultSword;
        }

        yield return (EquipSlot.MainHand, mh, mainhand.Stain);

        var oh = _items.Identify(EquipSlot.OffHand, offhand.Skeleton, offhand.Weapon, offhand.Variant, mh.Type);
        if (!skipWarnings && !oh.Valid)
        {
            Glamourer.Log.Warning($"Appearance data {offhand} for offhand weapon invalid, item could not be identified.");
            oh = _items.GetDefaultOffhand(mh);
            if (!oh.Valid)
                oh = ItemManager.NothingItem(FullEquipType.Shield);
        }

        yield return (EquipSlot.OffHand, oh, offhand.Stain);
    }

    private static void ComputeMaterials(DesignMaterialManager manager, in StateMaterialManager materials,
        EquipFlag equipFlags = EquipFlagExtensions.All)
    {
        foreach (var (key, value) in materials.Values)
        {
            var idx = MaterialValueIndex.FromKey(key);
            if (idx.RowIndex >= MtrlFile.ColorTable.NumRows)
                continue;
            if (idx.MaterialIndex >= MaterialService.MaterialsPerModel)
                continue;

            var slot = idx.DrawObject switch
            {
                MaterialValueIndex.DrawObjectType.Human => idx.SlotIndex < 10 ? ((uint)idx.SlotIndex).ToEquipSlot() : EquipSlot.Unknown,
                MaterialValueIndex.DrawObjectType.Mainhand when idx.SlotIndex == 0 => EquipSlot.MainHand,
                MaterialValueIndex.DrawObjectType.Offhand when idx.SlotIndex == 0 => EquipSlot.OffHand,
                _ => EquipSlot.Unknown,
            };
            if (slot is EquipSlot.Unknown || (slot.ToBothFlags() & equipFlags) == 0)
                continue;

            manager.AddOrUpdateValue(idx, value.Convert());
        }
    }
}
