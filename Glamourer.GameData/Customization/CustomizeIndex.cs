using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Glamourer.Customization;

public enum CustomizeIndex : byte
{
    Race,
    Gender,
    BodyType,
    Height,
    Clan,
    Face,
    Hairstyle,
    Highlights,
    SkinColor,
    EyeColorRight,
    HairColor,
    HighlightsColor,
    FacialFeature1,
    FacialFeature2,
    FacialFeature3,
    FacialFeature4,
    FacialFeature5,
    FacialFeature6,
    FacialFeature7,
    LegacyTattoo,
    TattooColor,
    Eyebrows,
    EyeColorLeft,
    EyeShape,
    SmallIris,
    Nose,
    Jaw,
    Mouth,
    Lipstick,
    LipColor,
    MuscleMass,
    TailShape,
    BustSize,
    FacePaint,
    FacePaintReversed,
    FacePaintColor,
}

public static class CustomizationExtensions
{
    public const int NumIndices = (int)CustomizeIndex.FacePaintColor + 1;

    public static readonly CustomizeIndex[] All = Enum.GetValues<CustomizeIndex>()
        .Where(v => v is not CustomizeIndex.Race and not CustomizeIndex.BodyType).ToArray();

    public static readonly CustomizeIndex[] AllBasic = All
        .Where(v => v is not CustomizeIndex.Gender and not CustomizeIndex.Clan).ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (int ByteIdx, byte Mask) ToByteAndMask(this CustomizeIndex index)
        => index switch
        {
            CustomizeIndex.Race              => (0, 0xFF),
            CustomizeIndex.Gender            => (1, 0xFF),
            CustomizeIndex.BodyType          => (2, 0xFF),
            CustomizeIndex.Height            => (3, 0xFF),
            CustomizeIndex.Clan              => (4, 0xFF),
            CustomizeIndex.Face              => (5, 0xFF),
            CustomizeIndex.Hairstyle         => (6, 0xFF),
            CustomizeIndex.Highlights        => (7, 0x80),
            CustomizeIndex.SkinColor         => (8, 0xFF),
            CustomizeIndex.EyeColorRight     => (9, 0xFF),
            CustomizeIndex.HairColor         => (10, 0xFF),
            CustomizeIndex.HighlightsColor   => (11, 0xFF),
            CustomizeIndex.FacialFeature1    => (12, 0x01),
            CustomizeIndex.FacialFeature2    => (12, 0x02),
            CustomizeIndex.FacialFeature3    => (12, 0x04),
            CustomizeIndex.FacialFeature4    => (12, 0x08),
            CustomizeIndex.FacialFeature5    => (12, 0x10),
            CustomizeIndex.FacialFeature6    => (12, 0x20),
            CustomizeIndex.FacialFeature7    => (12, 0x40),
            CustomizeIndex.LegacyTattoo      => (12, 0x80),
            CustomizeIndex.TattooColor       => (13, 0xFF),
            CustomizeIndex.Eyebrows          => (14, 0xFF),
            CustomizeIndex.EyeColorLeft      => (15, 0xFF),
            CustomizeIndex.EyeShape          => (16, 0x7F),
            CustomizeIndex.SmallIris         => (16, 0x80),
            CustomizeIndex.Nose              => (17, 0xFF),
            CustomizeIndex.Jaw               => (18, 0xFF),
            CustomizeIndex.Mouth             => (19, 0x7F),
            CustomizeIndex.Lipstick          => (19, 0x80),
            CustomizeIndex.LipColor          => (20, 0xFF),
            CustomizeIndex.MuscleMass        => (21, 0xFF),
            CustomizeIndex.TailShape         => (22, 0xFF),
            CustomizeIndex.BustSize          => (23, 0xFF),
            CustomizeIndex.FacePaint         => (24, 0x7F),
            CustomizeIndex.FacePaintReversed => (24, 0x80),
            CustomizeIndex.FacePaintColor    => (25, 0xFF),
            _                                => (0, 0x00),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static CustomizeFlag ToFlag(this CustomizeIndex index)
        => (CustomizeFlag)(1ul << (int)index);

    public static string ToDefaultName(this CustomizeIndex customizeIndex)
        => customizeIndex switch
        {
            CustomizeIndex.Race              => "Race",
            CustomizeIndex.Gender            => "Gender",
            CustomizeIndex.BodyType          => "Body Type",
            CustomizeIndex.Height            => "Height",
            CustomizeIndex.Clan              => "Clan",
            CustomizeIndex.Face              => "Head Style",
            CustomizeIndex.Hairstyle         => "Hair Style",
            CustomizeIndex.Highlights        => "Highlights",
            CustomizeIndex.SkinColor         => "Skin Color",
            CustomizeIndex.EyeColorRight     => "Right Eye Color",
            CustomizeIndex.HairColor         => "Hair Color",
            CustomizeIndex.HighlightsColor   => "Highlights Color",
            CustomizeIndex.TattooColor       => "Tattoo Color",
            CustomizeIndex.Eyebrows          => "Eyebrow Style",
            CustomizeIndex.EyeColorLeft      => "Left Eye Color",
            CustomizeIndex.EyeShape          => "Small Pupils",
            CustomizeIndex.Nose              => "Nose Style",
            CustomizeIndex.Jaw               => "Jaw Style",
            CustomizeIndex.Mouth             => "Mouth Style",
            CustomizeIndex.MuscleMass        => "Muscle Tone",
            CustomizeIndex.TailShape         => "Tail Shape",
            CustomizeIndex.BustSize          => "Bust Size",
            CustomizeIndex.FacePaint         => "Face Paint",
            CustomizeIndex.FacePaintColor    => "Face Paint Color",
            CustomizeIndex.LipColor          => "Lip Color",
            CustomizeIndex.FacialFeature1    => "Facial Feature 1",
            CustomizeIndex.FacialFeature2    => "Facial Feature 2",
            CustomizeIndex.FacialFeature3    => "Facial Feature 3",
            CustomizeIndex.FacialFeature4    => "Facial Feature 4",
            CustomizeIndex.FacialFeature5    => "Facial Feature 5",
            CustomizeIndex.FacialFeature6    => "Facial Feature 6",
            CustomizeIndex.FacialFeature7    => "Facial Feature 7",
            CustomizeIndex.LegacyTattoo      => "Legacy Tattoo",
            CustomizeIndex.SmallIris         => "Small Iris",
            CustomizeIndex.Lipstick          => "Enable Lipstick",
            CustomizeIndex.FacePaintReversed => "Reverse Face Paint",
            _                                => string.Empty,
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe CustomizeValue Get(this in Penumbra.GameData.Structs.CustomizeData data, CustomizeIndex index)
    {
        var (offset, mask) = index.ToByteAndMask();
        return (CustomizeValue)(data.Data[offset] & mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool Set(this ref Penumbra.GameData.Structs.CustomizeData data, CustomizeIndex index, CustomizeValue value)
    {
        var (offset, mask) = index.ToByteAndMask();
        return mask != 0xFF
            ? SetIfDifferentMasked(ref data.Data[offset], value, mask)
            : SetIfDifferent(ref data.Data[offset], value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferentMasked(ref byte oldValue, CustomizeValue newValue, byte mask)
    {
        var tmp = (byte)(newValue.Value & mask);
        tmp = (byte)(tmp | (oldValue & ~mask));
        if (oldValue == tmp)
            return false;

        oldValue = tmp;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferent(ref byte oldValue, CustomizeValue newValue)
    {
        if (oldValue == newValue.Value)
            return false;

        oldValue = newValue.Value;
        return true;
    }
}
