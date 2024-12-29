using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.GameData;

/// <summary>
/// Each SubRace and Gender combo has a customization set.
/// This describes the available customizations, their types and their names.
/// </summary>
public class CustomizeSet
{
    private NpcCustomizeSet _npcCustomizations;

    internal CustomizeSet(NpcCustomizeSet npcCustomizations, SubRace clan, Gender gender)
    {
        _npcCustomizations = npcCustomizations;
        Gender             = gender;
        Clan               = clan;
        Race               = clan.ToRace();
        SettingAvailable   = 0;
    }

    public Gender  Gender { get; }
    public SubRace Clan   { get; }
    public Race    Race   { get; }

    public string Name { get; internal init; } = string.Empty;

    public CustomizeFlag SettingAvailable { get; internal set; }

    internal void SetAvailable(CustomizeIndex index)
        => SettingAvailable |= index.ToFlag();

    public bool IsAvailable(CustomizeIndex index)
        => SettingAvailable.HasFlag(index.ToFlag());

    // Meta
    public IReadOnlyList<string> OptionName { get; internal init; } = null!;

    public string Option(CustomizeIndex index)
        => OptionName[(int)index];

    public IReadOnlyList<byte>                             Voices { get; internal init; } = null!;
    public IReadOnlyList<MenuType>                         Types  { get; internal set; }  = null!;
    public IReadOnlyDictionary<MenuType, CustomizeIndex[]> Order  { get; internal set; }  = null!;


    // Always list selector.
    public int NumEyebrows    { get; internal init; }
    public int NumEyeShapes   { get; internal init; }
    public int NumNoseShapes  { get; internal init; }
    public int NumJawShapes   { get; internal init; }
    public int NumMouthShapes { get; internal init; }


    // Always Icon Selector
    public IReadOnlyList<CustomizeData>                  Faces          { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData>                  HairStyles     { get; internal init; } = null!;
    public IReadOnlyList<IReadOnlyList<CustomizeData>>   HairByFace     { get; internal set; }  = null!;
    public IReadOnlyList<CustomizeData>                  TailEarShapes  { get; internal init; } = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature1 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature2 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature3 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature4 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature5 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature6 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature7 { get; internal set; }  = null!;
    public (CustomizeData, CustomizeData)                LegacyTattoo   { get; internal set; }
    public IReadOnlyList<CustomizeData>                  FacePaints     { get; internal init; } = null!;

    public IReadOnlyList<(CustomizeIndex Type, CustomizeValue Value)> NpcOptions { get; internal set; } =
        Array.Empty<(CustomizeIndex Type, CustomizeValue Value)>();

    // Always Color Selector
    public IReadOnlyList<CustomizeData> SkinColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> HairColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> HighlightColors      { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> EyeColors            { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> TattooColors         { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> FacePaintColorsLight { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> FacePaintColorsDark  { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> LipColorsLight       { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> LipColorsDark        { get; internal init; } = null!;

    public bool Validate(CustomizeIndex index, CustomizeValue value, out CustomizeData? custom, CustomizeValue face)
    {
        if (IsAvailable(index))
            return DataByValue(index, value, out custom, face) >= 0
             || _npcCustomizations.CheckColor(index, value)
             || NpcOptions.Any(t => t.Type == index && t.Value == value);

        custom = null;
        return value == CustomizeValue.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int DataByValue(CustomizeIndex index, CustomizeValue value, out CustomizeData? custom, CustomizeValue face)
    {
        var type = Types[(int)index];

        return type switch
        {
            MenuType.ListSelector  => GetInteger0(out custom),
            MenuType.List1Selector => GetInteger1(out custom),
            MenuType.IconSelector => index switch
            {
                CustomizeIndex.Face => Get(Faces, HrothgarFaceHack(value), out custom),
                CustomizeIndex.Hairstyle => Get((face = HrothgarFaceHack(face)).Value < HairByFace.Count ? HairByFace[face.Value] : HairStyles,
                    value, out custom),
                CustomizeIndex.TailShape => Get(TailEarShapes, value, out custom),
                CustomizeIndex.FacePaint => Get(FacePaints,    value, out custom),
                CustomizeIndex.LipColor  => Get(LipColorsDark, value, out custom),
                _                        => Invalid(out custom),
            },
            MenuType.ColorPicker => index switch
            {
                CustomizeIndex.SkinColor       => Get(SkinColors,                                       value, out custom),
                CustomizeIndex.EyeColorLeft    => Get(EyeColors,                                        value, out custom),
                CustomizeIndex.EyeColorRight   => Get(EyeColors,                                        value, out custom),
                CustomizeIndex.HairColor       => Get(HairColors,                                       value, out custom),
                CustomizeIndex.HighlightsColor => Get(HighlightColors,                                  value, out custom),
                CustomizeIndex.TattooColor     => Get(TattooColors,                                     value, out custom),
                CustomizeIndex.LipColor        => Get(LipColorsDark.Concat(LipColorsLight),             value, out custom),
                CustomizeIndex.FacePaintColor  => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), value, out custom),
                _                              => Invalid(out custom),
            },
            MenuType.DoubleColorPicker => index switch
            {
                CustomizeIndex.LipColor       => Get(LipColorsDark.Concat(LipColorsLight),             value, out custom),
                CustomizeIndex.FacePaintColor => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), value, out custom),
                _                             => Invalid(out custom),
            },
            MenuType.IconCheckmark => GetBool(index, value, out custom),
            MenuType.Percentage    => GetInteger0(out custom),
            MenuType.Checkmark     => GetBool(index, value, out custom),
            _                      => Invalid(out custom),
        };

        int Get(IEnumerable<CustomizeData> list, CustomizeValue v, out CustomizeData? output)
        {
            var (val, idx) = list.Cast<CustomizeData?>().WithIndex().FirstOrDefault(p => p.Value!.Value.Value == v);
            if (val == null)
            {
                output = null;
                return -1;
            }

            output = val;
            return idx;
        }

        static int Invalid(out CustomizeData? custom)
        {
            custom = null;
            return -1;
        }

        static int GetBool(CustomizeIndex index, CustomizeValue value, out CustomizeData? custom)
        {
            if (value == CustomizeValue.Zero)
            {
                custom = new CustomizeData(index, CustomizeValue.Zero);
                return 0;
            }

            var (_, mask) = index.ToByteAndMask();
            if (value.Value == mask)
            {
                custom = new CustomizeData(index, new CustomizeValue(mask), 0, 1);
                return 1;
            }

            custom = null;
            return -1;
        }

        int GetInteger1(out CustomizeData? custom)
        {
            if (value > 0 && value < Count(index) + 1)
            {
                custom = new CustomizeData(index, value, 0, (ushort)(value.Value - 1));
                return value.Value;
            }

            custom = null;
            return -1;
        }

        int GetInteger0(out CustomizeData? custom)
        {
            if (value < Count(index))
            {
                custom = new CustomizeData(index, value, 0, value.Value);
                return value.Value;
            }

            custom = null;
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CustomizeData Data(CustomizeIndex index, int idx)
        => Data(index, idx, CustomizeValue.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CustomizeData Data(CustomizeIndex index, int idx, CustomizeValue face)
    {
        if (idx >= Count(index, face = HrothgarFaceHack(face)))
            throw new IndexOutOfRangeException();

        switch (Types[(int)index])
        {
            case MenuType.Percentage:    return new CustomizeData(index, (CustomizeValue)idx,           0, (ushort)idx);
            case MenuType.ListSelector:  return new CustomizeData(index, (CustomizeValue)idx,           0, (ushort)idx);
            case MenuType.List1Selector: return new CustomizeData(index, (CustomizeValue)(idx + 1),     0, (ushort)idx);
            case MenuType.Checkmark:     return new CustomizeData(index, CustomizeValue.Bool(idx != 0), 0, (ushort)idx);
        }

        return index switch
        {
            CustomizeIndex.Face            => Faces[idx],
            CustomizeIndex.Hairstyle       => face < HairByFace.Count ? HairByFace[face.Value][idx] : HairStyles[idx],
            CustomizeIndex.TailShape       => TailEarShapes[idx],
            CustomizeIndex.FacePaint       => FacePaints[idx],
            CustomizeIndex.FacialFeature1  => idx == 0 ? FacialFeature1[face.Value].Item1 : FacialFeature1[face.Value].Item2,
            CustomizeIndex.FacialFeature2  => idx == 0 ? FacialFeature2[face.Value].Item1 : FacialFeature2[face.Value].Item2,
            CustomizeIndex.FacialFeature3  => idx == 0 ? FacialFeature3[face.Value].Item1 : FacialFeature3[face.Value].Item2,
            CustomizeIndex.FacialFeature4  => idx == 0 ? FacialFeature4[face.Value].Item1 : FacialFeature4[face.Value].Item2,
            CustomizeIndex.FacialFeature5  => idx == 0 ? FacialFeature5[face.Value].Item1 : FacialFeature5[face.Value].Item2,
            CustomizeIndex.FacialFeature6  => idx == 0 ? FacialFeature6[face.Value].Item1 : FacialFeature6[face.Value].Item2,
            CustomizeIndex.FacialFeature7  => idx == 0 ? FacialFeature7[face.Value].Item1 : FacialFeature7[face.Value].Item2,
            CustomizeIndex.LegacyTattoo    => idx == 0 ? LegacyTattoo.Item1 : LegacyTattoo.Item2,
            CustomizeIndex.SkinColor       => SkinColors[idx],
            CustomizeIndex.EyeColorLeft    => EyeColors[idx],
            CustomizeIndex.EyeColorRight   => EyeColors[idx],
            CustomizeIndex.HairColor       => HairColors[idx],
            CustomizeIndex.HighlightsColor => HighlightColors[idx],
            CustomizeIndex.TattooColor     => TattooColors[idx],
            CustomizeIndex.LipColor        => idx < 96 ? LipColorsDark[idx] : LipColorsLight[idx - 96],
            CustomizeIndex.FacePaintColor  => idx < 96 ? FacePaintColorsDark[idx] : FacePaintColorsLight[idx - 96],
            _                              => new CustomizeData(0, CustomizeValue.Zero),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MenuType Type(CustomizeIndex index)
        => Types[(int)index];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Count(CustomizeIndex index)
        => Count(index, CustomizeValue.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Count(CustomizeIndex index, CustomizeValue face)
    {
        if (!IsAvailable(index))
            return 0;

        return Type(index) switch
        {
            MenuType.Percentage    => 101,
            MenuType.IconCheckmark => 2,
            MenuType.Checkmark     => 2,
            _ => index switch
            {
                CustomizeIndex.Face => Faces.Count,
                CustomizeIndex.Hairstyle => (face = HrothgarFaceHack(face)) < HairByFace.Count
                    ? HairByFace[face.Value].Count
                    : HairStyles.Count,
                CustomizeIndex.SkinColor       => SkinColors.Count,
                CustomizeIndex.EyeColorRight   => EyeColors.Count,
                CustomizeIndex.HairColor       => HairColors.Count,
                CustomizeIndex.HighlightsColor => HighlightColors.Count,
                CustomizeIndex.TattooColor     => TattooColors.Count,
                CustomizeIndex.Eyebrows        => NumEyebrows,
                CustomizeIndex.EyeColorLeft    => EyeColors.Count,
                CustomizeIndex.EyeShape        => NumEyeShapes,
                CustomizeIndex.Nose            => NumNoseShapes,
                CustomizeIndex.Jaw             => NumJawShapes,
                CustomizeIndex.Mouth           => NumMouthShapes,
                CustomizeIndex.LipColor        => LipColorsLight.Count + LipColorsDark.Count,
                CustomizeIndex.TailShape       => TailEarShapes.Count,
                CustomizeIndex.FacePaint       => FacePaints.Count,
                CustomizeIndex.FacePaintColor  => FacePaintColorsLight.Count + FacePaintColorsDark.Count,
                _                              => throw new ArgumentOutOfRangeException(nameof(index), index, null),
            },
        };
    }

    private CustomizeValue HrothgarFaceHack(CustomizeValue value)
        => Race == Race.Hrothgar && value.Value is > 4 and < 9 ? value - 4 : value;
}

public static class CustomizationSetExtensions
{
    /// <summary> Return only the available customizations in this set and Clan or Gender. </summary>
    public static CustomizeFlag FixApplication(this CustomizeFlag flag, CustomizeSet set)
        => flag & (set.SettingAvailable | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.BodyType);
}
