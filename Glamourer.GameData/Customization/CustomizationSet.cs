using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OtterGui;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

// Each Subrace and Gender combo has a customization set.
// This describes the available customizations, their types and their names.
public class CustomizationSet
{
    internal CustomizationSet(SubRace clan, Gender gender)
    {
        Gender = gender;
        Clan   = clan;
        Race   = clan.ToRace();
        _settingAvailable = Race == Race.Hrothgar && gender == Gender.Female
            ? 0u
            : DefaultAvailable;
    }

    public Gender  Gender { get; }
    public SubRace Clan   { get; }
    public Race    Race   { get; }

    private uint _settingAvailable;

    internal void SetAvailable(CustomizationId id)
        => _settingAvailable |= 1u << (int)id;

    public bool IsAvailable(CustomizationId id)
        => (_settingAvailable & (1u << (int)id)) != 0;

    private const uint DefaultAvailable =
        (1u << (int)CustomizationId.Height)
      | (1u << (int)CustomizationId.Hairstyle)
      | (1u << (int)CustomizationId.SkinColor)
      | (1u << (int)CustomizationId.EyeColorR)
      | (1u << (int)CustomizationId.EyeColorL)
      | (1u << (int)CustomizationId.HairColor)
      | (1u << (int)CustomizationId.HighlightColor)
      | (1u << (int)CustomizationId.FacialFeaturesTattoos)
      | (1u << (int)CustomizationId.TattooColor)
      | (1u << (int)CustomizationId.LipColor)
      | (1u << (int)CustomizationId.Height);

    public string ToHumanReadable(Customize customizationData)
    {
        var sb = new StringBuilder();
        foreach (var id in Enum.GetValues<CustomizationId>().Where(IsAvailable))
            sb.AppendFormat("{0,-20}", Option(id)).Append(customizationData[id]);

        return sb.ToString();
    }

    // Meta
    public IReadOnlyList<string> OptionName { get; internal set; } = null!;

    public string Option(CustomizationId id)
        => OptionName[(int)id];

    public IReadOnlyList<CharaMakeParams.MenuType>                          Types { get; internal set; } = null!;
    public IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizationId[]> Order { get; internal set; } = null!;


    // Always list selector.
    public int NumEyebrows    { get; internal init; }
    public int NumEyeShapes   { get; internal init; }
    public int NumNoseShapes  { get; internal init; }
    public int NumJawShapes   { get; internal init; }
    public int NumMouthShapes { get; internal init; }


    // Always Icon Selector
    public IReadOnlyList<CustomizationData>                Faces           { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData>                HairStyles      { get; internal init; } = null!;
    public IReadOnlyList<IReadOnlyList<CustomizationData>> HairByFace      { get; internal set; }  = null!;
    public IReadOnlyList<CustomizationData>                TailEarShapes   { get; internal init; } = null!;
    public IReadOnlyList<IReadOnlyList<CustomizationData>> FeaturesTattoos { get; internal set; }  = null!;
    public IReadOnlyList<CustomizationData>                FacePaints      { get; internal init; } = null!;

    public CustomizationData FacialFeature(CustomizationByteValue face, int idx)
    {
        face = HrothgarFaceHack(face);
        var faceIdx = Faces.IndexOf(p => p.Value == face);
        return FeaturesTattoos[faceIdx != -1 ? faceIdx : 0][idx];
    }


    // Always Color Selector
    public IReadOnlyList<CustomizationData> SkinColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> HairColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> HighlightColors      { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> EyeColors            { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> TattooColors         { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> FacePaintColorsLight { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> FacePaintColorsDark  { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> LipColorsLight       { get; internal init; } = null!;
    public IReadOnlyList<CustomizationData> LipColorsDark        { get; internal init; } = null!;


    public int DataByValue(CustomizationId id, CustomizationByteValue value, out CustomizationData? custom)
    {
        var type = id.ToType();
        custom = null;
        if (type is CharaMakeParams.MenuType.Percentage or CharaMakeParams.MenuType.ListSelector)
        {
            if (value < Count(id))
            {
                custom = new CustomizationData(id, value, 0, value.Value);
                return value.Value;
            }

            return -1;
        }

        int Get(IEnumerable<CustomizationData> list, CustomizationByteValue v, ref CustomizationData? output)
        {
            var (val, idx) = list.Cast<CustomizationData?>().WithIndex().FirstOrDefault(p => p.Item1!.Value.Value == v);
            if (val == null)
                return -1;

            output = val;
            return idx;
        }

        return id switch
        {
            CustomizationId.SkinColor      => Get(SkinColors,                                       value, ref custom),
            CustomizationId.EyeColorL      => Get(EyeColors,                                        value, ref custom),
            CustomizationId.EyeColorR      => Get(EyeColors,                                        value, ref custom),
            CustomizationId.HairColor      => Get(HairColors,                                       value, ref custom),
            CustomizationId.HighlightColor => Get(HighlightColors,                                  value, ref custom),
            CustomizationId.TattooColor    => Get(TattooColors,                                     value, ref custom),
            CustomizationId.LipColor       => Get(LipColorsDark.Concat(LipColorsLight),             value, ref custom),
            CustomizationId.FacePaintColor => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), value, ref custom),

            CustomizationId.Face                  => Get(Faces,              HrothgarFaceHack(value), ref custom),
            CustomizationId.Hairstyle             => Get(HairStyles,         value,                   ref custom),
            CustomizationId.TailEarShape          => Get(TailEarShapes,      value,                   ref custom),
            CustomizationId.FacePaint             => Get(FacePaints,         value,                   ref custom),
            CustomizationId.FacialFeaturesTattoos => Get(FeaturesTattoos[0], value,                   ref custom),
            _                                     => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }

    public CustomizationData Data(CustomizationId id, int idx)
        => Data(id, idx, CustomizationByteValue.Zero);

    public CustomizationData Data(CustomizationId id, int idx, CustomizationByteValue face)
    {
        if (idx >= Count(id, face = HrothgarFaceHack(face)))
            throw new IndexOutOfRangeException();

        switch (id.ToType())
        {
            case CharaMakeParams.MenuType.Percentage:   return new CustomizationData(id, (CustomizationByteValue)idx, 0, (ushort)idx);
            case CharaMakeParams.MenuType.ListSelector: return new CustomizationData(id, (CustomizationByteValue)idx, 0, (ushort)idx);
        }

        return id switch
        {
            CustomizationId.Face                  => Faces[idx],
            CustomizationId.Hairstyle             => face < HairByFace.Count ? HairByFace[face.Value][idx] : HairStyles[idx],
            CustomizationId.TailEarShape          => TailEarShapes[idx],
            CustomizationId.FacePaint             => FacePaints[idx],
            CustomizationId.FacialFeaturesTattoos => FeaturesTattoos[0][idx],

            CustomizationId.SkinColor      => SkinColors[idx],
            CustomizationId.EyeColorL      => EyeColors[idx],
            CustomizationId.EyeColorR      => EyeColors[idx],
            CustomizationId.HairColor      => HairColors[idx],
            CustomizationId.HighlightColor => HighlightColors[idx],
            CustomizationId.TattooColor    => TattooColors[idx],
            CustomizationId.LipColor       => idx < 96 ? LipColorsDark[idx] : LipColorsLight[idx - 96],
            CustomizationId.FacePaintColor => idx < 96 ? FacePaintColorsDark[idx] : FacePaintColorsLight[idx - 96],
            _                              => new CustomizationData(0, CustomizationByteValue.Zero),
        };
    }

    public CharaMakeParams.MenuType Type(CustomizationId id)
        => Types[(int)id];

    internal static IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizationId[]> ComputeOrder(CustomizationSet set)
    {
        var ret = (CustomizationId[])Enum.GetValues(typeof(CustomizationId));
        ret[(int)CustomizationId.TattooColor] = CustomizationId.EyeColorL;
        ret[(int)CustomizationId.EyeColorL]   = CustomizationId.EyeColorR;
        ret[(int)CustomizationId.EyeColorR]   = CustomizationId.TattooColor;

        var dict = ret.Skip(2).Where(set.IsAvailable).GroupBy(set.Type).ToDictionary(k => k.Key, k => k.ToArray());
        foreach (var type in Enum.GetValues<CharaMakeParams.MenuType>())
            dict.TryAdd(type, Array.Empty<CustomizationId>());
        return dict;
    }

    public int Count(CustomizationId id)
        => Count(id, CustomizationByteValue.Zero);

    public int Count(CustomizationId id, CustomizationByteValue face)
    {
        if (!IsAvailable(id))
            return 0;

        if (id.ToType() == CharaMakeParams.MenuType.Percentage)
            return 101;

        return id switch
        {
            CustomizationId.Face                  => Faces.Count,
            CustomizationId.Hairstyle             => (face = HrothgarFaceHack(face)) < HairByFace.Count ? HairByFace[face.Value].Count : 0,
            CustomizationId.HighlightsOnFlag      => 2,
            CustomizationId.SkinColor             => SkinColors.Count,
            CustomizationId.EyeColorR             => EyeColors.Count,
            CustomizationId.HairColor             => HairColors.Count,
            CustomizationId.HighlightColor        => HighlightColors.Count,
            CustomizationId.FacialFeaturesTattoos => 8,
            CustomizationId.TattooColor           => TattooColors.Count,
            CustomizationId.Eyebrows              => NumEyebrows,
            CustomizationId.EyeColorL             => EyeColors.Count,
            CustomizationId.EyeShape              => NumEyeShapes,
            CustomizationId.Nose                  => NumNoseShapes,
            CustomizationId.Jaw                   => NumJawShapes,
            CustomizationId.Mouth                 => NumMouthShapes,
            CustomizationId.LipColor              => LipColorsLight.Count + LipColorsDark.Count,
            CustomizationId.TailEarShape          => TailEarShapes.Count,
            CustomizationId.FacePaint             => FacePaints.Count,
            CustomizationId.FacePaintColor        => FacePaintColorsLight.Count + FacePaintColorsDark.Count,
            _                                     => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
    }

    private CustomizationByteValue HrothgarFaceHack(CustomizationByteValue value)
        => Race == Race.Hrothgar && value.Value is > 4 and < 9 ? value - 4 : value;
}
