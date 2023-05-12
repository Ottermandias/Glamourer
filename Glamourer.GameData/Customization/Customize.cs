using System;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public unsafe struct Customize
{
    public Penumbra.GameData.Structs.CustomizeData Data;

    public Customize(in Penumbra.GameData.Structs.CustomizeData data)
        => Data = data;

    public Race Race
    {
        get => (Race)Data.Get(CustomizeIndex.Race).Value;
        set => Data.Set(CustomizeIndex.Race, (CustomizeValue)(byte)value);
    }

    public Gender Gender
    {
        get => (Gender)Data.Get(CustomizeIndex.Gender).Value + 1;
        set => Data.Set(CustomizeIndex.Gender, (CustomizeValue)(byte)value - 1);
    }

    public CustomizeValue BodyType
    {
        get => Data.Get(CustomizeIndex.BodyType);
        set => Data.Set(CustomizeIndex.BodyType, value);
    }

    public SubRace Clan
    {
        get => (SubRace)Data.Get(CustomizeIndex.Clan).Value;
        set => Data.Set(CustomizeIndex.Clan, (CustomizeValue)(byte)value);
    }

    public CustomizeValue Face
    {
        get => Data.Get(CustomizeIndex.Face);
        set => Data.Set(CustomizeIndex.Face, value);
    }


    public static readonly Customize Default = GenerateDefault();
    public static readonly Customize Empty   = new();

    public CustomizeValue Get(CustomizeIndex index)
        => Data.Get(index);

    public bool Set(CustomizeIndex flag, CustomizeValue index)
        => Data.Set(flag, index);

    public bool Equals(Customize other)
        => Equals(Data, other.Data);

    public CustomizeValue this[CustomizeIndex index]
    {
        get => Get(index);
        set => Set(index, value);
    }

    private static Customize GenerateDefault()
    {
        var ret = new Customize();
        ret.Set(CustomizeIndex.BodyType,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.Height,          (CustomizeValue)50);
        ret.Set(CustomizeIndex.Face,            (CustomizeValue)1);
        ret.Set(CustomizeIndex.Hairstyle,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.SkinColor,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeColorRight,   (CustomizeValue)1);
        ret.Set(CustomizeIndex.HighlightsColor, (CustomizeValue)1);
        ret.Set(CustomizeIndex.TattooColor,     (CustomizeValue)1);
        ret.Set(CustomizeIndex.Eyebrows,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeColorLeft,    (CustomizeValue)1);
        ret.Set(CustomizeIndex.EyeShape,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.Nose,            (CustomizeValue)1);
        ret.Set(CustomizeIndex.Jaw,             (CustomizeValue)1);
        ret.Set(CustomizeIndex.Mouth,           (CustomizeValue)1);
        ret.Set(CustomizeIndex.LipColor,        (CustomizeValue)1);
        ret.Set(CustomizeIndex.MuscleMass,      (CustomizeValue)50);
        ret.Set(CustomizeIndex.TailShape,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.BustSize,        (CustomizeValue)50);
        ret.Set(CustomizeIndex.FacePaint,       (CustomizeValue)1);
        ret.Set(CustomizeIndex.FacePaintColor,  (CustomizeValue)1);
        return ret;
    }

    public void Load(Customize other)
        => Data.Read(&other.Data);

    public void Write(nint target)
        => Data.Write((void*)target);

    public bool LoadBase64(string data)
        => Data.LoadBase64(data);

    public string WriteBase64()
        => Data.WriteBase64();

    public static CustomizeFlag Compare(Customize lhs, Customize rhs)
    {
        CustomizeFlag ret = 0;
        foreach (var idx in Enum.GetValues<CustomizeIndex>())
        {
            var l = lhs[idx];
            var r = rhs[idx];
            if (l.Value != r.Value)
                ret |= idx.ToFlag();
        }

        return ret;
    }
}
