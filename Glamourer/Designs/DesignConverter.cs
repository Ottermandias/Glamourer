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
using Penumbra.GameData.Enums;

namespace Glamourer.Designs;

public class DesignConverter
{
    public const byte Version = 3;

    private readonly ItemManager          _items;
    private readonly DesignManager        _designs;
    private readonly CustomizationService _customize;

    public DesignConverter(ItemManager items, DesignManager designs, CustomizationService customize)
    {
        _items     = items;
        _designs   = designs;
        _customize = customize;
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
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(DesignBase design)
        => ShareBase64(ShareJObject(design));

    public string ShareBase64(ActorState state)
        => ShareBase64(ShareJObject(state, EquipFlagExtensions.All, CustomizeFlagExtensions.All));

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

    public DesignBase? FromBase64(string base64, bool customize, bool equip)
    {
        var bytes = System.Convert.FromBase64String(base64);

        DesignBase ret;
        try
        {
            switch (bytes[0])
            {
                case (byte)'{':
                    var jObj1 = JObject.Parse(Encoding.UTF8.GetString(bytes));
                    ret = jObj1["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj1)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj1);
                    break;
                case 1:
                case 2:
                    ret = _designs.CreateTemporary();
                    ret.MigrateBase64(_items, base64);
                    break;
                case Version:
                    var version = bytes.DecompressToString(out var decompressed);
                    var jObj2   = JObject.Parse(decompressed);
                    Debug.Assert(version == Version);
                    ret = jObj2["Identifier"] != null
                        ? Design.LoadDesign(_customize, _items, jObj2)
                        : DesignBase.LoadDesignBase(_customize, _items, jObj2);
                    break;
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
}
