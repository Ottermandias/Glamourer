using Glamourer.Designs;
using Glamourer.Services;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop.CharaFile;

public sealed class CmaFile
{
    public string     Name = string.Empty;
    public DesignData Data = new();

    public static CmaFile? ParseData(ItemManager items, string data, string? name = null)
    {
        try
        {
            var jObj = JObject.Parse(data);
            var ret  = new CmaFile();
            ret.Data.SetDefaultEquipment(items);
            ParseMainHand(items, jObj, ref ret.Data);
            ParseOffHand(items, jObj, ref ret.Data);
            ret.Name = jObj["Description"]?.ToObject<string>() ?? name ?? "New Design";
            ParseEquipment(items, jObj, ref ret.Data);
            ParseCustomization(jObj, ref ret.Data);
            return ret;
        }
        catch
        {
            return null;
        }
    }

    private static unsafe void ParseCustomization(JObject jObj, ref DesignData data)
    {
        var bytes = jObj["CharacterBytes"]?.ToObject<string>() ?? string.Empty;
        if (bytes.Length is not 26 * 3 - 1)
            return;

        bytes = bytes.Replace(" ", string.Empty);
        var byteData = Convert.FromHexString(bytes);
        fixed (byte* ptr = byteData)
        {
            data.Customize.Read(ptr);
        }
    }

    private static unsafe void ParseEquipment(ItemManager items, JObject jObj, ref DesignData data)
    {
        var bytes = jObj["EquipmentBytes"]?.ToObject<string>() ?? string.Empty;
        bytes = bytes.Replace(" ", string.Empty);
        var byteData = Convert.FromHexString(bytes);
        fixed (byte* ptr = byteData)
        {
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var idx = slot.ToIndex();
                if (idx * 4 + 3 >= byteData.Length)
                    continue;

                var armor = ((CharacterArmor*)ptr)[idx];
                var item  = items.Identify(slot, armor.Set, armor.Variant);
                data.SetItem(slot, item);
                data.SetStain(slot, armor.Stain);
            }

            data.Customize.Read(ptr);
        }
    }

    private static void ParseMainHand(ItemManager items, JObject jObj, ref DesignData data)
    {
        var mainhand = jObj["MainHand"];
        if (mainhand == null)
        {
            data.SetItem(EquipSlot.MainHand, items.DefaultSword);
            data.SetStain(EquipSlot.MainHand, 0);
            return;
        }

        var set     = mainhand["Item1"]?.ToObject<ushort>() ?? items.DefaultSword.PrimaryId;
        var type    = mainhand["Item2"]?.ToObject<ushort>() ?? items.DefaultSword.SecondaryId;
        var variant = mainhand["Item3"]?.ToObject<byte>() ?? items.DefaultSword.Variant;
        var stain   = mainhand["Item4"]?.ToObject<byte>() ?? 0;
        var item    = items.Identify(EquipSlot.MainHand, set, type, variant);

        data.SetItem(EquipSlot.MainHand, item.Valid ? item : items.DefaultSword);
        data.SetStain(EquipSlot.MainHand, stain);
    }

    private static void ParseOffHand(ItemManager items, JObject jObj, ref DesignData data)
    {
        var offhand        = jObj["OffHand"];
        var defaultOffhand = items.GetDefaultOffhand(data.Item(EquipSlot.MainHand));
        if (offhand == null)
        {
            data.SetItem(EquipSlot.MainHand, defaultOffhand);
            data.SetStain(EquipSlot.MainHand, defaultOffhand.PrimaryId.Id == 0 ? 0 : data.Stain(EquipSlot.MainHand));
            return;
        }

        var set     = offhand["Item1"]?.ToObject<ushort>() ?? items.DefaultSword.PrimaryId;
        var type    = offhand["Item2"]?.ToObject<ushort>() ?? items.DefaultSword.SecondaryId;
        var variant = offhand["Item3"]?.ToObject<byte>() ?? items.DefaultSword.Variant;
        var stain   = offhand["Item4"]?.ToObject<byte>() ?? 0;
        var item    = items.Identify(EquipSlot.OffHand, set, type, variant, data.MainhandType);

        data.SetItem(EquipSlot.OffHand, item.Valid ? item : defaultOffhand);
        data.SetStain(EquipSlot.OffHand, defaultOffhand.PrimaryId.Id == 0 ? 0 : (StainId)stain);
    }
}
