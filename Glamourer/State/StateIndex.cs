﻿using Glamourer.Designs;
using Glamourer.GameData;
using Penumbra.GameData.Enums;

namespace Glamourer.State;

public readonly record struct StateIndex(int Value) : IEqualityOperators<StateIndex, StateIndex, bool>
{
    public static readonly StateIndex Invalid = new(-1);

    public static implicit operator StateIndex(EquipFlag flag)
        => flag switch
        {
            EquipFlag.Head          => new StateIndex(EquipHead),
            EquipFlag.Body          => new StateIndex(EquipBody),
            EquipFlag.Hands         => new StateIndex(EquipHands),
            EquipFlag.Legs          => new StateIndex(EquipLegs),
            EquipFlag.Feet          => new StateIndex(EquipFeet),
            EquipFlag.Ears          => new StateIndex(EquipEars),
            EquipFlag.Neck          => new StateIndex(EquipNeck),
            EquipFlag.Wrist         => new StateIndex(EquipWrist),
            EquipFlag.RFinger       => new StateIndex(EquipRFinger),
            EquipFlag.LFinger       => new StateIndex(EquipLFinger),
            EquipFlag.Mainhand      => new StateIndex(EquipMainhand),
            EquipFlag.Offhand       => new StateIndex(EquipOffhand),
            EquipFlag.HeadStain     => new StateIndex(StainHead),
            EquipFlag.BodyStain     => new StateIndex(StainBody),
            EquipFlag.HandsStain    => new StateIndex(StainHands),
            EquipFlag.LegsStain     => new StateIndex(StainLegs),
            EquipFlag.FeetStain     => new StateIndex(StainFeet),
            EquipFlag.EarsStain     => new StateIndex(StainEars),
            EquipFlag.NeckStain     => new StateIndex(StainNeck),
            EquipFlag.WristStain    => new StateIndex(StainWrist),
            EquipFlag.RFingerStain  => new StateIndex(StainRFinger),
            EquipFlag.LFingerStain  => new StateIndex(StainLFinger),
            EquipFlag.MainhandStain => new StateIndex(StainMainhand),
            EquipFlag.OffhandStain  => new StateIndex(StainOffhand),
            _                       => Invalid,
        };

    public static implicit operator StateIndex(CustomizeIndex index)
        => index switch
        {
            CustomizeIndex.Race              => new StateIndex(CustomizeRace),
            CustomizeIndex.Gender            => new StateIndex(CustomizeGender),
            CustomizeIndex.BodyType          => new StateIndex(CustomizeBodyType),
            CustomizeIndex.Height            => new StateIndex(CustomizeHeight),
            CustomizeIndex.Clan              => new StateIndex(CustomizeClan),
            CustomizeIndex.Face              => new StateIndex(CustomizeFace),
            CustomizeIndex.Hairstyle         => new StateIndex(CustomizeHairstyle),
            CustomizeIndex.Highlights        => new StateIndex(CustomizeHighlights),
            CustomizeIndex.SkinColor         => new StateIndex(CustomizeSkinColor),
            CustomizeIndex.EyeColorRight     => new StateIndex(CustomizeEyeColorRight),
            CustomizeIndex.HairColor         => new StateIndex(CustomizeHairColor),
            CustomizeIndex.HighlightsColor   => new StateIndex(CustomizeHighlightsColor),
            CustomizeIndex.FacialFeature1    => new StateIndex(CustomizeFacialFeature1),
            CustomizeIndex.FacialFeature2    => new StateIndex(CustomizeFacialFeature2),
            CustomizeIndex.FacialFeature3    => new StateIndex(CustomizeFacialFeature3),
            CustomizeIndex.FacialFeature4    => new StateIndex(CustomizeFacialFeature4),
            CustomizeIndex.FacialFeature5    => new StateIndex(CustomizeFacialFeature5),
            CustomizeIndex.FacialFeature6    => new StateIndex(CustomizeFacialFeature6),
            CustomizeIndex.FacialFeature7    => new StateIndex(CustomizeFacialFeature7),
            CustomizeIndex.LegacyTattoo      => new StateIndex(CustomizeLegacyTattoo),
            CustomizeIndex.TattooColor       => new StateIndex(CustomizeTattooColor),
            CustomizeIndex.Eyebrows          => new StateIndex(CustomizeEyebrows),
            CustomizeIndex.EyeColorLeft      => new StateIndex(CustomizeEyeColorLeft),
            CustomizeIndex.EyeShape          => new StateIndex(CustomizeEyeShape),
            CustomizeIndex.SmallIris         => new StateIndex(CustomizeSmallIris),
            CustomizeIndex.Nose              => new StateIndex(CustomizeNose),
            CustomizeIndex.Jaw               => new StateIndex(CustomizeJaw),
            CustomizeIndex.Mouth             => new StateIndex(CustomizeMouth),
            CustomizeIndex.Lipstick          => new StateIndex(CustomizeLipstick),
            CustomizeIndex.LipColor          => new StateIndex(CustomizeLipColor),
            CustomizeIndex.MuscleMass        => new StateIndex(CustomizeMuscleMass),
            CustomizeIndex.TailShape         => new StateIndex(CustomizeTailShape),
            CustomizeIndex.BustSize          => new StateIndex(CustomizeBustSize),
            CustomizeIndex.FacePaint         => new StateIndex(CustomizeFacePaint),
            CustomizeIndex.FacePaintReversed => new StateIndex(CustomizeFacePaintReversed),
            CustomizeIndex.FacePaintColor    => new StateIndex(CustomizeFacePaintColor),
            _                                => Invalid,
        };

    public static implicit operator StateIndex(MetaIndex meta)
        => new((int)meta);

    public static implicit operator StateIndex(CrestFlag crest)
        => crest switch
        {
            CrestFlag.OffHand => new StateIndex(CrestOffhand),
            CrestFlag.Head    => new StateIndex(CrestHead),
            CrestFlag.Body    => new StateIndex(CrestBody),
            _                 => Invalid,
        };

    public static implicit operator StateIndex(CustomizeParameterFlag param)
        => param switch
        {
            CustomizeParameterFlag.SkinDiffuse           => new StateIndex(ParamSkinDiffuse),
            CustomizeParameterFlag.MuscleTone            => new StateIndex(ParamMuscleTone),
            CustomizeParameterFlag.SkinSpecular          => new StateIndex(ParamSkinSpecular),
            CustomizeParameterFlag.LipDiffuse            => new StateIndex(ParamLipDiffuse),
            CustomizeParameterFlag.HairDiffuse           => new StateIndex(ParamHairDiffuse),
            CustomizeParameterFlag.HairSpecular          => new StateIndex(ParamHairSpecular),
            CustomizeParameterFlag.HairHighlight         => new StateIndex(ParamHairHighlight),
            CustomizeParameterFlag.LeftEye               => new StateIndex(ParamLeftEye),
            CustomizeParameterFlag.RightEye              => new StateIndex(ParamRightEye),
            CustomizeParameterFlag.FeatureColor          => new StateIndex(ParamFeatureColor),
            CustomizeParameterFlag.FacePaintUvMultiplier => new StateIndex(ParamFacePaintUvMultiplier),
            CustomizeParameterFlag.FacePaintUvOffset     => new StateIndex(ParamFacePaintUvOffset),
            CustomizeParameterFlag.DecalColor            => new StateIndex(ParamDecalColor),
            _                                            => Invalid,
        };

    public const int EquipHead     = 0;
    public const int EquipBody     = EquipHead + 1;
    public const int EquipHands    = EquipBody + 1;
    public const int EquipLegs     = EquipHands + 1;
    public const int EquipFeet     = EquipLegs + 1;
    public const int EquipEars     = EquipFeet + 1;
    public const int EquipNeck     = EquipEars + 1;
    public const int EquipWrist    = EquipNeck + 1;
    public const int EquipRFinger  = EquipWrist + 1;
    public const int EquipLFinger  = EquipRFinger + 1;
    public const int EquipMainhand = EquipLFinger + 1;
    public const int EquipOffhand  = EquipMainhand + 1;

    public const int StainHead     = EquipOffhand + 1;
    public const int StainBody     = StainHead + 1;
    public const int StainHands    = StainBody + 1;
    public const int StainLegs     = StainHands + 1;
    public const int StainFeet     = StainLegs + 1;
    public const int StainEars     = StainFeet + 1;
    public const int StainNeck     = StainEars + 1;
    public const int StainWrist    = StainNeck + 1;
    public const int StainRFinger  = StainWrist + 1;
    public const int StainLFinger  = StainRFinger + 1;
    public const int StainMainhand = StainLFinger + 1;
    public const int StainOffhand  = StainMainhand + 1;

    public const int CustomizeRace              = StainOffhand + 1;
    public const int CustomizeGender            = CustomizeRace + 1;
    public const int CustomizeBodyType          = CustomizeGender + 1;
    public const int CustomizeHeight            = CustomizeBodyType + 1;
    public const int CustomizeClan              = CustomizeHeight + 1;
    public const int CustomizeFace              = CustomizeClan + 1;
    public const int CustomizeHairstyle         = CustomizeFace + 1;
    public const int CustomizeHighlights        = CustomizeHairstyle + 1;
    public const int CustomizeSkinColor         = CustomizeHighlights + 1;
    public const int CustomizeEyeColorRight     = CustomizeSkinColor + 1;
    public const int CustomizeHairColor         = CustomizeEyeColorRight + 1;
    public const int CustomizeHighlightsColor   = CustomizeHairColor + 1;
    public const int CustomizeFacialFeature1    = CustomizeHighlightsColor + 1;
    public const int CustomizeFacialFeature2    = CustomizeFacialFeature1 + 1;
    public const int CustomizeFacialFeature3    = CustomizeFacialFeature2 + 1;
    public const int CustomizeFacialFeature4    = CustomizeFacialFeature3 + 1;
    public const int CustomizeFacialFeature5    = CustomizeFacialFeature4 + 1;
    public const int CustomizeFacialFeature6    = CustomizeFacialFeature5 + 1;
    public const int CustomizeFacialFeature7    = CustomizeFacialFeature6 + 1;
    public const int CustomizeLegacyTattoo      = CustomizeFacialFeature7 + 1;
    public const int CustomizeTattooColor       = CustomizeLegacyTattoo + 1;
    public const int CustomizeEyebrows          = CustomizeTattooColor + 1;
    public const int CustomizeEyeColorLeft      = CustomizeEyebrows + 1;
    public const int CustomizeEyeShape          = CustomizeEyeColorLeft + 1;
    public const int CustomizeSmallIris         = CustomizeEyeShape + 1;
    public const int CustomizeNose              = CustomizeSmallIris + 1;
    public const int CustomizeJaw               = CustomizeNose + 1;
    public const int CustomizeMouth             = CustomizeJaw + 1;
    public const int CustomizeLipstick          = CustomizeMouth + 1;
    public const int CustomizeLipColor          = CustomizeLipstick + 1;
    public const int CustomizeMuscleMass        = CustomizeLipColor + 1;
    public const int CustomizeTailShape         = CustomizeMuscleMass + 1;
    public const int CustomizeBustSize          = CustomizeTailShape + 1;
    public const int CustomizeFacePaint         = CustomizeBustSize + 1;
    public const int CustomizeFacePaintReversed = CustomizeFacePaint + 1;
    public const int CustomizeFacePaintColor    = CustomizeFacePaintReversed + 1;

    public const int MetaWetness     = CustomizeFacePaintColor + 1;
    public const int MetaHatState    = MetaWetness + 1;
    public const int MetaVisorState  = MetaHatState + 1;
    public const int MetaWeaponState = MetaVisorState + 1;
    public const int MetaModelId     = MetaWeaponState + 1;

    public const int CrestHead    = MetaModelId + 1;
    public const int CrestBody    = CrestHead + 1;
    public const int CrestOffhand = CrestBody + 1;

    public const int ParamSkinDiffuse           = CrestOffhand + 1;
    public const int ParamMuscleTone            = ParamSkinDiffuse + 1;
    public const int ParamSkinSpecular          = ParamMuscleTone + 1;
    public const int ParamLipDiffuse            = ParamSkinSpecular + 1;
    public const int ParamHairDiffuse           = ParamLipDiffuse + 1;
    public const int ParamHairSpecular          = ParamHairDiffuse + 1;
    public const int ParamHairHighlight         = ParamHairSpecular + 1;
    public const int ParamLeftEye               = ParamHairHighlight + 1;
    public const int ParamRightEye              = ParamLeftEye + 1;
    public const int ParamFeatureColor          = ParamRightEye + 1;
    public const int ParamFacePaintUvMultiplier = ParamFeatureColor + 1;
    public const int ParamFacePaintUvOffset     = ParamFacePaintUvMultiplier + 1;
    public const int ParamDecalColor            = ParamFacePaintUvOffset + 1;

    public const int Size = ParamDecalColor + 1;

    public static IEnumerable<StateIndex> All
        => Enumerable.Range(0, Size - 1).Select(i => new StateIndex(i));

    public bool GetApply(DesignBase data)
        => GetFlag() switch
        {
            EquipFlag e              => data.ApplyEquip.HasFlag(e),
            CustomizeFlag c          => data.ApplyCustomize.HasFlag(c),
            MetaFlag m               => data.ApplyMeta.HasFlag(m),
            CrestFlag c              => data.ApplyCrest.HasFlag(c),
            CustomizeParameterFlag c => data.ApplyParameters.HasFlag(c),
            bool v                   => v,
            _                        => false,
        };

    public string ToName()
        => GetFlag() switch
        {
            EquipFlag e              => GetName(e),
            CustomizeFlag c          => c.ToIndex().ToDefaultName(),
            MetaFlag m               => m.ToIndex().ToName(),
            CrestFlag c              => c.ToLabel(),
            CustomizeParameterFlag c => c.ToName(),
            bool v                   => "Model ID",
            _                        => "Unknown",
        };

    public object GetFlag()
        => Value switch
        {
            EquipHead     => EquipFlag.Head,
            EquipBody     => EquipFlag.Body,
            EquipHands    => EquipFlag.Hands,
            EquipLegs     => EquipFlag.Legs,
            EquipFeet     => EquipFlag.Feet,
            EquipEars     => EquipFlag.Ears,
            EquipNeck     => EquipFlag.Neck,
            EquipWrist    => EquipFlag.Wrist,
            EquipRFinger  => EquipFlag.RFinger,
            EquipLFinger  => EquipFlag.LFinger,
            EquipMainhand => EquipFlag.Mainhand,
            EquipOffhand  => EquipFlag.Offhand,

            StainHead     => EquipFlag.HeadStain,
            StainBody     => EquipFlag.BodyStain,
            StainHands    => EquipFlag.HandsStain,
            StainLegs     => EquipFlag.LegsStain,
            StainFeet     => EquipFlag.FeetStain,
            StainEars     => EquipFlag.EarsStain,
            StainNeck     => EquipFlag.NeckStain,
            StainWrist    => EquipFlag.WristStain,
            StainRFinger  => EquipFlag.RFingerStain,
            StainLFinger  => EquipFlag.LFingerStain,
            StainMainhand => EquipFlag.MainhandStain,
            StainOffhand  => EquipFlag.OffhandStain,

            CustomizeRace              => CustomizeFlag.Race,
            CustomizeGender            => CustomizeFlag.Gender,
            CustomizeBodyType          => CustomizeFlag.BodyType,
            CustomizeHeight            => CustomizeFlag.Height,
            CustomizeClan              => CustomizeFlag.Clan,
            CustomizeFace              => CustomizeFlag.Face,
            CustomizeHairstyle         => CustomizeFlag.Hairstyle,
            CustomizeHighlights        => CustomizeFlag.Highlights,
            CustomizeSkinColor         => CustomizeFlag.SkinColor,
            CustomizeEyeColorRight     => CustomizeFlag.EyeColorRight,
            CustomizeHairColor         => CustomizeFlag.HairColor,
            CustomizeHighlightsColor   => CustomizeFlag.HighlightsColor,
            CustomizeFacialFeature1    => CustomizeFlag.FacialFeature1,
            CustomizeFacialFeature2    => CustomizeFlag.FacialFeature2,
            CustomizeFacialFeature3    => CustomizeFlag.FacialFeature3,
            CustomizeFacialFeature4    => CustomizeFlag.FacialFeature4,
            CustomizeFacialFeature5    => CustomizeFlag.FacialFeature5,
            CustomizeFacialFeature6    => CustomizeFlag.FacialFeature6,
            CustomizeFacialFeature7    => CustomizeFlag.FacialFeature7,
            CustomizeLegacyTattoo      => CustomizeFlag.LegacyTattoo,
            CustomizeTattooColor       => CustomizeFlag.TattooColor,
            CustomizeEyebrows          => CustomizeFlag.Eyebrows,
            CustomizeEyeColorLeft      => CustomizeFlag.EyeColorLeft,
            CustomizeEyeShape          => CustomizeFlag.EyeShape,
            CustomizeSmallIris         => CustomizeFlag.SmallIris,
            CustomizeNose              => CustomizeFlag.Nose,
            CustomizeJaw               => CustomizeFlag.Jaw,
            CustomizeMouth             => CustomizeFlag.Mouth,
            CustomizeLipstick          => CustomizeFlag.Lipstick,
            CustomizeLipColor          => CustomizeFlag.LipColor,
            CustomizeMuscleMass        => CustomizeFlag.MuscleMass,
            CustomizeTailShape         => CustomizeFlag.TailShape,
            CustomizeBustSize          => CustomizeFlag.BustSize,
            CustomizeFacePaint         => CustomizeFlag.FacePaint,
            CustomizeFacePaintReversed => CustomizeFlag.FacePaintReversed,
            CustomizeFacePaintColor    => CustomizeFlag.FacePaintColor,

            MetaWetness     => MetaFlag.Wetness,
            MetaHatState    => MetaFlag.HatState,
            MetaVisorState  => MetaFlag.VisorState,
            MetaWeaponState => MetaFlag.WeaponState,
            MetaModelId     => true,

            CrestHead    => CrestFlag.Head,
            CrestBody    => CrestFlag.Body,
            CrestOffhand => CrestFlag.OffHand,

            ParamSkinDiffuse           => CustomizeParameterFlag.SkinDiffuse,
            ParamMuscleTone            => CustomizeParameterFlag.MuscleTone,
            ParamSkinSpecular          => CustomizeParameterFlag.SkinSpecular,
            ParamLipDiffuse            => CustomizeParameterFlag.LipDiffuse,
            ParamHairDiffuse           => CustomizeParameterFlag.HairDiffuse,
            ParamHairSpecular          => CustomizeParameterFlag.HairSpecular,
            ParamHairHighlight         => CustomizeParameterFlag.HairHighlight,
            ParamLeftEye               => CustomizeParameterFlag.LeftEye,
            ParamRightEye              => CustomizeParameterFlag.RightEye,
            ParamFeatureColor          => CustomizeParameterFlag.FeatureColor,
            ParamFacePaintUvMultiplier => CustomizeParameterFlag.FacePaintUvMultiplier,
            ParamFacePaintUvOffset     => CustomizeParameterFlag.FacePaintUvOffset,
            ParamDecalColor            => CustomizeParameterFlag.DecalColor,

            _ => -1,
        };

    public object? GetValue(in DesignData data)
    {
        return Value switch
        {
            EquipHead     => data.Item(EquipSlot.Head),
            EquipBody     => data.Item(EquipSlot.Body),
            EquipHands    => data.Item(EquipSlot.Hands),
            EquipLegs     => data.Item(EquipSlot.Legs),
            EquipFeet     => data.Item(EquipSlot.Feet),
            EquipEars     => data.Item(EquipSlot.Ears),
            EquipNeck     => data.Item(EquipSlot.Neck),
            EquipWrist    => data.Item(EquipSlot.Wrists),
            EquipRFinger  => data.Item(EquipSlot.RFinger),
            EquipLFinger  => data.Item(EquipSlot.LFinger),
            EquipMainhand => data.Item(EquipSlot.MainHand),
            EquipOffhand  => data.Item(EquipSlot.OffHand),

            StainHead     => data.Stain(EquipSlot.Head),
            StainBody     => data.Stain(EquipSlot.Body),
            StainHands    => data.Stain(EquipSlot.Hands),
            StainLegs     => data.Stain(EquipSlot.Legs),
            StainFeet     => data.Stain(EquipSlot.Feet),
            StainEars     => data.Stain(EquipSlot.Ears),
            StainNeck     => data.Stain(EquipSlot.Neck),
            StainWrist    => data.Stain(EquipSlot.Wrists),
            StainRFinger  => data.Stain(EquipSlot.RFinger),
            StainLFinger  => data.Stain(EquipSlot.LFinger),
            StainMainhand => data.Stain(EquipSlot.MainHand),
            StainOffhand  => data.Stain(EquipSlot.OffHand),

            CustomizeRace              => data.Customize[CustomizeIndex.Race],
            CustomizeGender            => data.Customize[CustomizeIndex.Gender],
            CustomizeBodyType          => data.Customize[CustomizeIndex.BodyType],
            CustomizeHeight            => data.Customize[CustomizeIndex.Height],
            CustomizeClan              => data.Customize[CustomizeIndex.Clan],
            CustomizeFace              => data.Customize[CustomizeIndex.Face],
            CustomizeHairstyle         => data.Customize[CustomizeIndex.Hairstyle],
            CustomizeHighlights        => data.Customize[CustomizeIndex.Highlights],
            CustomizeSkinColor         => data.Customize[CustomizeIndex.SkinColor],
            CustomizeEyeColorRight     => data.Customize[CustomizeIndex.EyeColorRight],
            CustomizeHairColor         => data.Customize[CustomizeIndex.HairColor],
            CustomizeHighlightsColor   => data.Customize[CustomizeIndex.HighlightsColor],
            CustomizeFacialFeature1    => data.Customize[CustomizeIndex.FacialFeature1],
            CustomizeFacialFeature2    => data.Customize[CustomizeIndex.FacialFeature2],
            CustomizeFacialFeature3    => data.Customize[CustomizeIndex.FacialFeature3],
            CustomizeFacialFeature4    => data.Customize[CustomizeIndex.FacialFeature4],
            CustomizeFacialFeature5    => data.Customize[CustomizeIndex.FacialFeature5],
            CustomizeFacialFeature6    => data.Customize[CustomizeIndex.FacialFeature6],
            CustomizeFacialFeature7    => data.Customize[CustomizeIndex.FacialFeature7],
            CustomizeLegacyTattoo      => data.Customize[CustomizeIndex.LegacyTattoo],
            CustomizeTattooColor       => data.Customize[CustomizeIndex.TattooColor],
            CustomizeEyebrows          => data.Customize[CustomizeIndex.Eyebrows],
            CustomizeEyeColorLeft      => data.Customize[CustomizeIndex.EyeColorLeft],
            CustomizeEyeShape          => data.Customize[CustomizeIndex.EyeShape],
            CustomizeSmallIris         => data.Customize[CustomizeIndex.SmallIris],
            CustomizeNose              => data.Customize[CustomizeIndex.Nose],
            CustomizeJaw               => data.Customize[CustomizeIndex.Jaw],
            CustomizeMouth             => data.Customize[CustomizeIndex.Mouth],
            CustomizeLipstick          => data.Customize[CustomizeIndex.Lipstick],
            CustomizeLipColor          => data.Customize[CustomizeIndex.LipColor],
            CustomizeMuscleMass        => data.Customize[CustomizeIndex.MuscleMass],
            CustomizeTailShape         => data.Customize[CustomizeIndex.TailShape],
            CustomizeBustSize          => data.Customize[CustomizeIndex.BustSize],
            CustomizeFacePaint         => data.Customize[CustomizeIndex.FacePaint],
            CustomizeFacePaintReversed => data.Customize[CustomizeIndex.FacePaintReversed],
            CustomizeFacePaintColor    => data.Customize[CustomizeIndex.FacePaintColor],

            MetaWetness     => data.GetMeta(MetaIndex.Wetness),
            MetaHatState    => data.GetMeta(MetaIndex.HatState),
            MetaVisorState  => data.GetMeta(MetaIndex.VisorState),
            MetaWeaponState => data.GetMeta(MetaIndex.WeaponState),
            MetaModelId     => data.ModelId,

            CrestHead    => data.Crest(CrestFlag.Head),
            CrestBody    => data.Crest(CrestFlag.Body),
            CrestOffhand => data.Crest(CrestFlag.OffHand),

            ParamSkinDiffuse           => data.Parameters[CustomizeParameterFlag.SkinDiffuse],
            ParamMuscleTone            => data.Parameters[CustomizeParameterFlag.MuscleTone],
            ParamSkinSpecular          => data.Parameters[CustomizeParameterFlag.SkinSpecular],
            ParamLipDiffuse            => data.Parameters[CustomizeParameterFlag.LipDiffuse],
            ParamHairDiffuse           => data.Parameters[CustomizeParameterFlag.HairDiffuse],
            ParamHairSpecular          => data.Parameters[CustomizeParameterFlag.HairSpecular],
            ParamHairHighlight         => data.Parameters[CustomizeParameterFlag.HairHighlight],
            ParamLeftEye               => data.Parameters[CustomizeParameterFlag.LeftEye],
            ParamRightEye              => data.Parameters[CustomizeParameterFlag.RightEye],
            ParamFeatureColor          => data.Parameters[CustomizeParameterFlag.FeatureColor],
            ParamFacePaintUvMultiplier => data.Parameters[CustomizeParameterFlag.FacePaintUvMultiplier],
            ParamFacePaintUvOffset     => data.Parameters[CustomizeParameterFlag.FacePaintUvOffset],
            ParamDecalColor            => data.Parameters[CustomizeParameterFlag.DecalColor],

            _ => null,
        };
    }

    private static string GetName(EquipFlag flag)
    {
        var slot = flag.ToSlot(out var stain);
        return stain ? $"{slot.ToName()} Stain" : slot.ToName();
    }
}

public static class StateExtensions
{
    public static StateIndex ToState(this EquipSlot slot, bool stain = false)
        => (slot, stain) switch
        {
            (EquipSlot.Head, true)      => new StateIndex(StateIndex.EquipHead),
            (EquipSlot.Body, true)      => new StateIndex(StateIndex.EquipBody),
            (EquipSlot.Hands, true)     => new StateIndex(StateIndex.EquipHands),
            (EquipSlot.Legs, true)      => new StateIndex(StateIndex.EquipLegs),
            (EquipSlot.Feet, true)      => new StateIndex(StateIndex.EquipFeet),
            (EquipSlot.Ears, true)      => new StateIndex(StateIndex.EquipEars),
            (EquipSlot.Neck, true)      => new StateIndex(StateIndex.EquipNeck),
            (EquipSlot.Wrists, true)    => new StateIndex(StateIndex.EquipWrist),
            (EquipSlot.RFinger, true)   => new StateIndex(StateIndex.EquipRFinger),
            (EquipSlot.LFinger, true)   => new StateIndex(StateIndex.EquipLFinger),
            (EquipSlot.MainHand, true)  => new StateIndex(StateIndex.EquipMainhand),
            (EquipSlot.OffHand, true)   => new StateIndex(StateIndex.EquipOffhand),
            (EquipSlot.Head, false)     => new StateIndex(StateIndex.StainHead),
            (EquipSlot.Body, false)     => new StateIndex(StateIndex.StainBody),
            (EquipSlot.Hands, false)    => new StateIndex(StateIndex.StainHands),
            (EquipSlot.Legs, false)     => new StateIndex(StateIndex.StainLegs),
            (EquipSlot.Feet, false)     => new StateIndex(StateIndex.StainFeet),
            (EquipSlot.Ears, false)     => new StateIndex(StateIndex.StainEars),
            (EquipSlot.Neck, false)     => new StateIndex(StateIndex.StainNeck),
            (EquipSlot.Wrists, false)   => new StateIndex(StateIndex.StainWrist),
            (EquipSlot.RFinger, false)  => new StateIndex(StateIndex.StainRFinger),
            (EquipSlot.LFinger, false)  => new StateIndex(StateIndex.StainLFinger),
            (EquipSlot.MainHand, false) => new StateIndex(StateIndex.StainMainhand),
            (EquipSlot.OffHand, false)  => new StateIndex(StateIndex.StainOffhand),
            _                           => StateIndex.Invalid,
        };
}
