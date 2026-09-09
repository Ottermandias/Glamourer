using Glamourer.Designs.Links;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using Luna;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Structs;
using System.Text.Json;

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

    public JsonElement ShareJObject(DesignBase design)
        => design.ToElement();

    public JsonElement ShareJObject(Design design)
        => design.ToElement();

    public JsonElement ShareJObject(ActorState state, in ApplicationRules rules)
    {
        var design = Convert(state, rules);
        return ShareJObject(design);
    }

    public byte[] ShareBase64(Design design)
        => ToBase64(design.ToJson());

    public byte[] ShareBase64(DesignBase design)
        => ToBase64(design.ToJson());

    public byte[] ShareBase64(ActorState state, in ApplicationRules rules)
        => ShareBase64(state.ModelData, state.Materials, rules);

    public byte[] ShareBase64(in DesignData data, in StateMaterialManager materials, in ApplicationRules rules)
    {
        var design = Convert(data, materials, rules);
        return ToBase64(design.ToJson());
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

    public DesignBase? FromJsonElement(in JsonElement? jObject, bool customize, bool equip)
    {
        if (jObject is not { } j)
            return null;

        try
        {
            var ret = j.TryReadProperty("Identifier"u8, out Guid? _)
                ? Design.LoadDesign(saveService, customizeService, items, linkLoader, j)
                : DesignBase.LoadDesignBase(customizeService, items, j);

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

    public DesignBase? FromBase64(ReadOnlySpan<byte> base64, bool customize, bool equip, out byte version)
    {

        try
        {
            if(!CompressionFunctions.Decode(base64, out var compressed))
                throw new Exception("Unknown Error decoding Base64.");

            return FromData(compressed, customize, equip, out version);
        }
        catch (Exception ex)
        {
            version = 0;
            Glamourer.Log.Error($"[DesignConverter] Could not parse base64 string [{base64}]:\n{ex}");
            return null;
        }
    }

    public DesignBase? FromBase64(string base64, bool customize, bool equip, out byte version)
    {
        try
        {
            if (!CompressionFunctions.Decode(base64, out var compressed))
                throw new Exception("Unknown Error decoding Base64.");
            return FromData(compressed, customize, equip, out version);
        }
        catch (Exception ex)
        {
            version = 0;
            Glamourer.Log.Error($"[DesignConverter] Could not parse base64 string [{base64}]:\n{ex}");
            return null;
        }
    }

    private DesignBase FromData(Memory<byte> data, bool customize, bool equip, out byte version)
    {
        DesignBase ret;
        version = 0;
        version = data.Span[0];
        switch (version)
        {
            case (byte)'{':
                var jObj1 = JsonElement.Parse(data.Span);
                ret = jObj1.TryReadProperty("Identifier"u8, out Guid? _)
                    ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj1)
                    : DesignBase.LoadDesignBase(customizeService, items, jObj1);
                break;
            case 1:
            case 2:
            case 4:
                ret = designs.CreateTemporary();
                ret.MigrateBase64Data(customizeService, items, humans, data.Span);
                break;
            case 3:
            {
                version = CompressionFunctions.Decompress(data, CompressionVersionMode.Uncompressed, out Memory<byte> decompressed);
                var jObj2 = JsonElement.Parse(decompressed.Span);
                Debug.Assert(version is 3);
                ret = jObj2.TryReadProperty("Identifier"u8, out Guid? _)
                    ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                    : DesignBase.LoadDesignBase(customizeService, items, jObj2);
                break;
            }
            case 5:
            {
                data    = data[DesignBase64Migration.Base64SizeV4..];
                version = CompressionFunctions.Decompress(data, CompressionVersionMode.Uncompressed, out Memory<byte> decompressed);
                var jObj2 = JsonElement.Parse(decompressed.Span);
                Debug.Assert(version is 5);
                ret = jObj2.TryReadProperty("Identifier"u8, out Guid? _)
                    ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                    : DesignBase.LoadDesignBase(customizeService, items, jObj2);
                break;
            }
            case 6:
            {
                version = CompressionFunctions.Decompress(data, CompressionVersionMode.Uncompressed, out Memory<byte> decompressed);
                var jObj2 = JsonElement.Parse(decompressed.Span);
                Debug.Assert(version is 6);
                ret = jObj2.TryReadProperty("Identifier"u8, out Guid? _)
                    ? Design.LoadDesign(saveService, customizeService, items, linkLoader, jObj2)
                    : DesignBase.LoadDesignBase(customizeService, items, jObj2);
                break;
            }

            default: throw new Exception($"Unknown Version {data.Span[0]}.");
        }

        if (!customize)
            ret.Application.RemoveCustomize();

        if (!equip)
            ret.Application.RemoveEquip();

        return ret;
    }

    public static byte[] ToBase64(ReadOnlySpan<byte> utf8Json)
        => CompressionFunctions.ToCompressedBase64(utf8Json, Version, CompressionVersionMode.Uncompressed);

    public IEnumerable<(EquipSlot Slot, EquipItem Item, StainIds Stains)> FromDrawData(IReadOnlyList<CharacterArmor> armors,
        CharacterWeapon mainhand, CharacterWeapon offhand, bool skipWarnings)
    {
        if (armors.Count is not 10)
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
