using System;
using System.Diagnostics;
using System.Text;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using Glamourer.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public class DesignConverter
{
    public const byte Version = 5;

    private readonly ItemManager          _items;
    private readonly DesignManager        _designs;
    private readonly CustomizationService _customize;
    private readonly HumanModelList       _humans;

    public DesignConverter(ItemManager items, DesignManager designs, CustomizationService customize, HumanModelList humans)
    {
        _items     = items;
        _designs   = designs;
        _customize = customize;
        _humans    = humans;
    }

    public JObject ShareJObject(DesignBase design)
        => design.JsonSerialize();

    public JObject ShareJObject(Design design)
        => design.JsonSerialize();

    public JObject ShareJObject(ActorState state, EquipFlag equipFlags, CustomizeFlag customizeFlags)
    {
        var design = Convert(state, equipFlags, customizeFlags);
        return ShareJObject(design);
    }

    public string ShareBase64(Design design)
        => ShareBackwardCompatible(ShareJObject(design), design);

    public string ShareBase64(DesignBase design)
        => ShareBackwardCompatible(ShareJObject(design), design);

    public string ShareBase64(ActorState state)
    {
        var design = Convert(state, EquipFlagExtensions.All, CustomizeFlagExtensions.All);
        return ShareBackwardCompatible(ShareJObject(design), design);
    }

    public DesignBase Convert(ActorState state, EquipFlag equipFlags, CustomizeFlag customizeFlags)
    {
        var design = _designs.CreateTemporary();
        design.ApplyEquip     = equipFlags & EquipFlagExtensions.All;
        design.ApplyCustomize = customizeFlags & CustomizeFlagExtensions.All;
        design.SetApplyHatVisible(design.DoApplyEquip(EquipSlot.Head));
        design.SetApplyVisorToggle(design.DoApplyEquip(EquipSlot.Head));
        design.SetApplyWeaponVisible(design.DoApplyEquip(EquipSlot.MainHand) || design.DoApplyEquip(EquipSlot.OffHand));
        design.SetApplyWetness(design.DesignData.IsWet());
        design.DesignData = state.ModelData;
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
                    ret.MigrateBase64(_items, _humans, base64);
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
                case Version:
                {
                    bytes   = bytes[DesignBase64Migration.Base64SizeV4..];
                    version = bytes.DecompressToString(out var decompressed);
                    var jObj2 = JObject.Parse(decompressed);
                    Debug.Assert(version == Version);
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

        if (!customize)
        {
            ret.ApplyCustomize = 0;
            ret.SetApplyWetness(false);
        }

        if (!equip)
        {
            ret.ApplyEquip = 0;
            ret.SetApplyHatVisible(false);
            ret.SetApplyWeaponVisible(false);
            ret.SetApplyVisorToggle(false);
        }

        return ret;
    }

    private static string ShareBase64(JObject jObj)
    {
        var json       = jObj.ToString(Formatting.None);
        var compressed = json.Compress(Version);
        return System.Convert.ToBase64String(compressed);
    }

    private static string ShareBackwardCompatible(JObject jObject, DesignBase design)
    {
        var oldBase64 = DesignBase64Migration.CreateOldBase64(design.DesignData, design.ApplyEquip, design.ApplyCustomize,
            design.DoApplyHatVisible(), design.DoApplyVisorToggle(), design.DoApplyWeaponVisible(), design.WriteProtected(), 1f);
        var oldBytes   = System.Convert.FromBase64String(oldBase64);
        var json       = jObject.ToString(Formatting.None);
        var compressed = json.Compress(Version);
        var bytes      = new byte[oldBytes.Length + compressed.Length];
        oldBytes.CopyTo(bytes, 0);
        compressed.CopyTo(bytes, oldBytes.Length);
        return System.Convert.ToBase64String(bytes);
    }
}
