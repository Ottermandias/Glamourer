using Glamourer.Designs;
using Glamourer.Services;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Interop.CharaFile;

public sealed class CharaFile
{
    public string        Name = string.Empty;
    public DesignData    Data = new();
    public CustomizeFlag ApplyCustomize;
    public EquipFlag     ApplyEquip;
    public BonusItemFlag ApplyBonus;

    public static CharaFile ParseData(ItemManager items, string data, string? name = null)
    {
        var jObj = JObject.Parse(data);
        SanityCheck(jObj);
        var ret = new CharaFile();
        ret.Data.SetDefaultEquipment(items);
        ret.Data.ModelId   = ParseModelId(jObj);
        ret.Name           = jObj["Nickname"]?.ToObject<string>() ?? name ?? "New Design";
        ret.ApplyCustomize = ParseCustomize(jObj, ref ret.Data.Customize);
        ret.ApplyEquip     = ParseEquipment(items, jObj, ref ret.Data);
        ret.ApplyBonus     = ParseBonusItems(items, jObj, ref ret.Data);
        return ret;
    }

    private static EquipFlag ParseEquipment(ItemManager items, JObject jObj, ref DesignData data)
    {
        EquipFlag ret = 0;
        ParseWeapon(items, jObj, "MainHand", EquipSlot.MainHand, ref data, ref ret);
        ParseWeapon(items, jObj, "OffHand",  EquipSlot.OffHand,  ref data, ref ret);
        ParseGear(items, jObj, "HeadGear",  EquipSlot.Head,    ref data, ref ret);
        ParseGear(items, jObj, "Body",      EquipSlot.Body,    ref data, ref ret);
        ParseGear(items, jObj, "Hands",     EquipSlot.Hands,   ref data, ref ret);
        ParseGear(items, jObj, "Legs",      EquipSlot.Legs,    ref data, ref ret);
        ParseGear(items, jObj, "Feet",      EquipSlot.Feet,    ref data, ref ret);
        ParseGear(items, jObj, "Ears",      EquipSlot.Ears,    ref data, ref ret);
        ParseGear(items, jObj, "Neck",      EquipSlot.Neck,    ref data, ref ret);
        ParseGear(items, jObj, "Wrists",    EquipSlot.Wrists,  ref data, ref ret);
        ParseGear(items, jObj, "LeftRing",  EquipSlot.LFinger, ref data, ref ret);
        ParseGear(items, jObj, "RightRing", EquipSlot.RFinger, ref data, ref ret);
        return ret;
    }

    private static BonusItemFlag ParseBonusItems(ItemManager items, JObject jObj, ref DesignData data)
    {
        BonusItemFlag ret = 0;
        ParseBonus(items, jObj, "Glasses", "GlassesId", BonusItemFlag.Glasses, ref data, ref ret);
        return ret;
    }

    private static void ParseWeapon(ItemManager items, JObject jObj, string property, EquipSlot slot, ref DesignData data, ref EquipFlag flags)
    {
        var jTok = jObj[property];
        if (jTok == null)
            return;

        var set     = jTok["ModelSet"]?.ToObject<ushort>() ?? 0;
        var type    = jTok["ModelBase"]?.ToObject<ushort>() ?? 0;
        var variant = jTok["ModelVariant"]?.ToObject<byte>() ?? 0;
        var dye     = jTok["DyeId"]?.ToObject<byte>() ?? 0;
        var item    = items.Identify(slot, set, type, variant, slot is EquipSlot.OffHand ? data.MainhandType : FullEquipType.Unknown);
        if (!item.Valid)
            return;

        data.SetItem(slot, item);
        data.SetStain(slot, new StainIds(dye));
        if (slot is EquipSlot.MainHand)
            data.SetItem(EquipSlot.OffHand, items.GetDefaultOffhand(item));
        flags |= slot.ToFlag();
        flags |= slot.ToStainFlag();
    }

    private static void ParseGear(ItemManager items, JObject jObj, string property, EquipSlot slot, ref DesignData data, ref EquipFlag flags)
    {
        var jTok = jObj[property];
        if (jTok == null)
            return;

        var set     = jTok["ModelBase"]?.ToObject<ushort>() ?? 0;
        var variant = jTok["ModelVariant"]?.ToObject<byte>() ?? 0;
        var dye     = jTok["DyeId"]?.ToObject<byte>() ?? 0;
        var item    = items.Identify(slot, set, variant);
        if (!item.Valid)
            return;

        data.SetItem(slot, item);
        data.SetStain(slot, new StainIds(dye));
        flags |= slot.ToFlag();
        flags |= slot.ToStainFlag();
    }

    private static void ParseBonus(ItemManager items, JObject jObj, string property, string subProperty, BonusItemFlag slot,
        ref DesignData data, ref BonusItemFlag flags)
    {
        var id = jObj[property]?[subProperty]?.ToObject<int>();
        if (id is null)
            return;

        if (id is 0)
        {
            data.SetBonusItem(slot, BonusItem.Empty(slot));
            flags |= slot;
        }

        if (!items.DictBonusItems.TryGetValue((BonusItemId)id.Value, out var item) || item.Slot != slot)
            return;

        data.SetBonusItem(slot, item);
        flags |= slot;
    }

    private static CustomizeFlag ParseCustomize(JObject jObj, ref CustomizeArray customize)
    {
        CustomizeFlag ret = 0;
        customize.Race   = ParseRace(jObj, ref ret);
        customize.Gender = ParseGender(jObj, ref ret);
        customize.Clan   = ParseTribe(jObj, ref ret);
        ParseByte(jObj, "Height", CustomizeIndex.Height,    ref customize, ref ret);
        ParseByte(jObj, "Head",   CustomizeIndex.Face,      ref customize, ref ret);
        ParseByte(jObj, "Hair",   CustomizeIndex.Hairstyle, ref customize, ref ret);
        ParseHighlights(jObj, ref customize, ref ret);
        ParseByte(jObj, "Skintone",   CustomizeIndex.SkinColor,       ref customize, ref ret);
        ParseByte(jObj, "REyeColor",  CustomizeIndex.EyeColorRight,   ref customize, ref ret);
        ParseByte(jObj, "HairTone",   CustomizeIndex.HairColor,       ref customize, ref ret);
        ParseByte(jObj, "Highlights", CustomizeIndex.HighlightsColor, ref customize, ref ret);
        ParseFacial(jObj, ref customize, ref ret);
        ParseByte(jObj, "LimbalEyes",         CustomizeIndex.TattooColor,    ref customize, ref ret);
        ParseByte(jObj, "Eyebrows",           CustomizeIndex.Eyebrows,       ref customize, ref ret);
        ParseByte(jObj, "LEyeColor",          CustomizeIndex.EyeColorLeft,   ref customize, ref ret);
        ParseByte(jObj, "Eyes",               CustomizeIndex.EyeShape,       ref customize, ref ret);
        ParseByte(jObj, "Nose",               CustomizeIndex.Nose,           ref customize, ref ret);
        ParseByte(jObj, "Jaw",                CustomizeIndex.Jaw,            ref customize, ref ret);
        ParseByte(jObj, "Mouth",              CustomizeIndex.Mouth,          ref customize, ref ret);
        ParseByte(jObj, "LipsToneFurPattern", CustomizeIndex.LipColor,       ref customize, ref ret);
        ParseByte(jObj, "EarMuscleTailSize",  CustomizeIndex.MuscleMass,     ref customize, ref ret);
        ParseByte(jObj, "TailEarsType",       CustomizeIndex.TailShape,      ref customize, ref ret);
        ParseByte(jObj, "Bust",               CustomizeIndex.BustSize,       ref customize, ref ret);
        ParseByte(jObj, "FacePaint",          CustomizeIndex.FacePaint,      ref customize, ref ret);
        ParseByte(jObj, "FacePaintColor",     CustomizeIndex.FacePaintColor, ref customize, ref ret);
        ParseAge(jObj);

        if (ret.HasFlag(CustomizeFlag.EyeShape))
            ret |= CustomizeFlag.SmallIris;

        if (ret.HasFlag(CustomizeFlag.Mouth))
            ret |= CustomizeFlag.Lipstick;

        if (ret.HasFlag(CustomizeFlag.FacePaint))
            ret |= CustomizeFlag.FacePaintReversed;

        return ret;
    }

    private static uint ParseModelId(JObject jObj)
    {
        var jTok = jObj["ModelType"];
        if (jTok == null)
            throw new Exception("No Model ID given.");

        var id = jTok.ToObject<uint>();
        if (id != 0)
            throw new Exception($"Model ID {id} != 0 not supported.");

        return id;
    }

    private static void ParseFacial(JObject jObj, ref CustomizeArray customize, ref CustomizeFlag application)
    {
        var jTok = jObj["FacialFeatures"];
        if (jTok == null)
            return;

        application |= CustomizeFlag.FacialFeature1
          | CustomizeFlag.FacialFeature2
          | CustomizeFlag.FacialFeature3
          | CustomizeFlag.FacialFeature4
          | CustomizeFlag.FacialFeature5
          | CustomizeFlag.FacialFeature6
          | CustomizeFlag.FacialFeature7
          | CustomizeFlag.LegacyTattoo;

        var value = jTok.ToObject<string>()!;
        if (value is "None")
            return;

        if (value.Contains("First"))
            customize[CustomizeIndex.FacialFeature1] = CustomizeValue.Max;
        if (value.Contains("Second"))
            customize[CustomizeIndex.FacialFeature2] = CustomizeValue.Max;
        if (value.Contains("Third"))
            customize[CustomizeIndex.FacialFeature3] = CustomizeValue.Max;
        if (value.Contains("Fourth"))
            customize[CustomizeIndex.FacialFeature4] = CustomizeValue.Max;
        if (value.Contains("Fifth"))
            customize[CustomizeIndex.FacialFeature5] = CustomizeValue.Max;
        if (value.Contains("Sixth"))
            customize[CustomizeIndex.FacialFeature6] = CustomizeValue.Max;
        if (value.Contains("Seventh"))
            customize[CustomizeIndex.FacialFeature7] = CustomizeValue.Max;
        if (value.Contains("LegacyTattoo"))
            customize[CustomizeIndex.LegacyTattoo] = CustomizeValue.Max;
    }

    private static void ParseHighlights(JObject jObj, ref CustomizeArray customize, ref CustomizeFlag application)
    {
        var jTok = jObj["EnableHighlights"];
        if (jTok == null)
            return;

        var value = jTok.ToObject<bool>();
        application                          |= CustomizeFlag.Highlights;
        customize[CustomizeIndex.Highlights] =  value ? CustomizeValue.Max : CustomizeValue.Zero;
    }

    private static Race ParseRace(JObject jObj, ref CustomizeFlag application)
    {
        var race = jObj["Race"]?.ToObject<string>() switch
        {
            null       => Race.Unknown,
            "Hyur"     => Race.Hyur,
            "Elezen"   => Race.Elezen,
            "Lalafel"  => Race.Lalafell,
            "Miqote"   => Race.Miqote,
            "Roegadyn" => Race.Roegadyn,
            "AuRa"     => Race.AuRa,
            "Hrothgar" => Race.Hrothgar,
            "Viera"    => Race.Viera,
            _          => throw new Exception($"Invalid Race value {jObj["Race"]?.ToObject<string>()}."),
        };
        if (race == Race.Unknown)
            return Race.Hyur;

        application |= CustomizeFlag.Race;
        return race;
    }

    private static Gender ParseGender(JObject jObj, ref CustomizeFlag application)
    {
        var gender = jObj["Gender"]?.ToObject<string>() switch
        {
            null        => Gender.Unknown,
            "Masculine" => Gender.Male,
            "Feminine"  => Gender.Female,
            _           => throw new Exception($"Invalid Gender value {jObj["Gender"]?.ToObject<string>()}."),
        };
        if (gender == Gender.Unknown)
            return Gender.Male;

        application |= CustomizeFlag.Gender;
        return gender;
    }

    private static void ParseAge(JObject jObj)
    {
        var age = jObj["Age"]?.ToObject<string>();
        if (age is not null and not "Normal")
            throw new Exception($"Age {age} != Normal is not supported.");
    }

    private static unsafe void ParseByte(JObject jObj, string property, CustomizeIndex idx, ref CustomizeArray customize,
        ref CustomizeFlag application)
    {
        var jTok = jObj[property];
        if (jTok == null)
            return;

        customize.Data[idx.ToByteAndMask().ByteIdx] =  jTok.ToObject<byte>();
        application                                 |= idx.ToFlag();
    }

    private static SubRace ParseTribe(JObject jObj, ref CustomizeFlag application)
    {
        var tribe = jObj["Tribe"]?.ToObject<string>() switch
        {
            null              => SubRace.Unknown,
            "Midlander"       => SubRace.Midlander,
            "Highlander"      => SubRace.Highlander,
            "Wildwood"        => SubRace.Wildwood,
            "Duskwight"       => SubRace.Duskwight,
            "Plainsfolk"      => SubRace.Plainsfolk,
            "Dunesfolk"       => SubRace.Dunesfolk,
            "SeekerOfTheSun"  => SubRace.SeekerOfTheSun,
            "KeeperOfTheMoon" => SubRace.KeeperOfTheMoon,
            "SeaWolf"         => SubRace.Seawolf,
            "Hellsguard"      => SubRace.Hellsguard,
            "Raen"            => SubRace.Raen,
            "Xaela"           => SubRace.Xaela,
            "Helions"         => SubRace.Helion,
            "TheLost"         => SubRace.Lost,
            "Rava"            => SubRace.Rava,
            "Veena"           => SubRace.Veena,
            _                 => throw new Exception($"Invalid Tribe value {jObj["Tribe"]?.ToObject<string>()}."),
        };
        if (tribe == SubRace.Unknown)
            return SubRace.Midlander;

        application |= CustomizeFlag.Clan;
        return tribe;
    }

    private static void SanityCheck(JObject jObj)
    {
        var type = jObj["ObjectKind"]?.ToObject<string>();
        if (type is not "Player")
            throw new Exception($"ObjectKind {type} != Player is not supported.");
    }
}
