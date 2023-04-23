using System;
using System.Runtime.CompilerServices;

namespace Glamourer.Customization;

[Flags]
public enum CustomizeFlag : ulong
{
    Invalid           = 0,
    Race              = 1ul << CustomizeIndex.Race,
    Gender            = 1ul << CustomizeIndex.Gender,
    BodyType          = 1ul << CustomizeIndex.BodyType,
    Height            = 1ul << CustomizeIndex.Height,
    Clan              = 1ul << CustomizeIndex.Clan,
    Face              = 1ul << CustomizeIndex.Face,
    Hairstyle         = 1ul << CustomizeIndex.Hairstyle,
    Highlights        = 1ul << CustomizeIndex.Highlights,
    SkinColor         = 1ul << CustomizeIndex.SkinColor,
    EyeColorRight     = 1ul << CustomizeIndex.EyeColorRight,
    HairColor         = 1ul << CustomizeIndex.HairColor,
    HighlightsColor   = 1ul << CustomizeIndex.HighlightsColor,
    FacialFeature1    = 1ul << CustomizeIndex.FacialFeature1,
    FacialFeature2    = 1ul << CustomizeIndex.FacialFeature2,
    FacialFeature3    = 1ul << CustomizeIndex.FacialFeature3,
    FacialFeature4    = 1ul << CustomizeIndex.FacialFeature4,
    FacialFeature5    = 1ul << CustomizeIndex.FacialFeature5,
    FacialFeature6    = 1ul << CustomizeIndex.FacialFeature6,
    FacialFeature7    = 1ul << CustomizeIndex.FacialFeature7,
    LegacyTattoo      = 1ul << CustomizeIndex.LegacyTattoo,
    TattooColor       = 1ul << CustomizeIndex.TattooColor,
    Eyebrows          = 1ul << CustomizeIndex.Eyebrows,
    EyeColorLeft      = 1ul << CustomizeIndex.EyeColorLeft,
    EyeShape          = 1ul << CustomizeIndex.EyeShape,
    SmallIris         = 1ul << CustomizeIndex.SmallIris,
    Nose              = 1ul << CustomizeIndex.Nose,
    Jaw               = 1ul << CustomizeIndex.Jaw,
    Mouth             = 1ul << CustomizeIndex.Mouth,
    Lipstick          = 1ul << CustomizeIndex.Lipstick,
    LipColor          = 1ul << CustomizeIndex.LipColor,
    MuscleMass        = 1ul << CustomizeIndex.MuscleMass,
    TailShape         = 1ul << CustomizeIndex.TailShape,
    BustSize          = 1ul << CustomizeIndex.BustSize,
    FacePaint         = 1ul << CustomizeIndex.FacePaint,
    FacePaintReversed = 1ul << CustomizeIndex.FacePaintReversed,
    FacePaintColor    = 1ul << CustomizeIndex.FacePaintColor,
}

public static class CustomizeFlagExtensions
{
    public const CustomizeFlag All            = (CustomizeFlag)(((ulong)CustomizeFlag.FacePaintColor << 1) - 1ul);
    public const CustomizeFlag RedrawRequired = CustomizeFlag.Race | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.Face;

    public static bool RequiresRedraw(this CustomizeFlag flags)
        => (flags & RedrawRequired) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static CustomizeIndex ToIndex(this CustomizeFlag flag)
        => flag switch
        {
            CustomizeFlag.Race              => CustomizeIndex.Race,
            CustomizeFlag.Gender            => CustomizeIndex.Gender,
            CustomizeFlag.BodyType          => CustomizeIndex.BodyType,
            CustomizeFlag.Height            => CustomizeIndex.Height,
            CustomizeFlag.Clan              => CustomizeIndex.Clan,
            CustomizeFlag.Face              => CustomizeIndex.Face,
            CustomizeFlag.Hairstyle         => CustomizeIndex.Hairstyle,
            CustomizeFlag.Highlights        => CustomizeIndex.Highlights,
            CustomizeFlag.SkinColor         => CustomizeIndex.SkinColor,
            CustomizeFlag.EyeColorRight     => CustomizeIndex.EyeColorRight,
            CustomizeFlag.HairColor         => CustomizeIndex.HairColor,
            CustomizeFlag.HighlightsColor   => CustomizeIndex.HighlightsColor,
            CustomizeFlag.FacialFeature1    => CustomizeIndex.FacialFeature1,
            CustomizeFlag.FacialFeature2    => CustomizeIndex.FacialFeature2,
            CustomizeFlag.FacialFeature3    => CustomizeIndex.FacialFeature3,
            CustomizeFlag.FacialFeature4    => CustomizeIndex.FacialFeature4,
            CustomizeFlag.FacialFeature5    => CustomizeIndex.FacialFeature5,
            CustomizeFlag.FacialFeature6    => CustomizeIndex.FacialFeature6,
            CustomizeFlag.FacialFeature7    => CustomizeIndex.FacialFeature7,
            CustomizeFlag.LegacyTattoo      => CustomizeIndex.LegacyTattoo,
            CustomizeFlag.TattooColor       => CustomizeIndex.TattooColor,
            CustomizeFlag.Eyebrows          => CustomizeIndex.Eyebrows,
            CustomizeFlag.EyeColorLeft      => CustomizeIndex.EyeColorLeft,
            CustomizeFlag.EyeShape          => CustomizeIndex.EyeShape,
            CustomizeFlag.SmallIris         => CustomizeIndex.SmallIris,
            CustomizeFlag.Nose              => CustomizeIndex.Nose,
            CustomizeFlag.Jaw               => CustomizeIndex.Jaw,
            CustomizeFlag.Mouth             => CustomizeIndex.Mouth,
            CustomizeFlag.Lipstick          => CustomizeIndex.Lipstick,
            CustomizeFlag.LipColor          => CustomizeIndex.LipColor,
            CustomizeFlag.MuscleMass        => CustomizeIndex.MuscleMass,
            CustomizeFlag.TailShape         => CustomizeIndex.TailShape,
            CustomizeFlag.BustSize          => CustomizeIndex.BustSize,
            CustomizeFlag.FacePaint         => CustomizeIndex.FacePaint,
            CustomizeFlag.FacePaintReversed => CustomizeIndex.FacePaintReversed,
            CustomizeFlag.FacePaintColor    => CustomizeIndex.FacePaintColor,
            _                               => (CustomizeIndex)byte.MaxValue,
        };

    public static bool SetIfDifferent(ref this CustomizeFlag flags, CustomizeFlag flag, bool value)
    {
        var newValue = value ? flags | flag : flags & ~flag;
        if (newValue == flags)
            return false;

        flags = newValue;
        return true;
    }
}
