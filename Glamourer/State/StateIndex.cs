using Glamourer.Designs;
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
    public const int EquipBody     = 1;
    public const int EquipHands    = 2;
    public const int EquipLegs     = 3;
    public const int EquipFeet     = 4;
    public const int EquipEars     = 5;
    public const int EquipNeck     = 6;
    public const int EquipWrist    = 7;
    public const int EquipRFinger  = 8;
    public const int EquipLFinger  = 9;
    public const int EquipMainhand = 10;
    public const int EquipOffhand  = 11;

    public const int StainHead     = 12;
    public const int StainBody     = 13;
    public const int StainHands    = 14;
    public const int StainLegs     = 15;
    public const int StainFeet     = 16;
    public const int StainEars     = 17;
    public const int StainNeck     = 18;
    public const int StainWrist    = 19;
    public const int StainRFinger  = 20;
    public const int StainLFinger  = 21;
    public const int StainMainhand = 22;
    public const int StainOffhand  = 23;

    public const int CustomizeRace              = 24;
    public const int CustomizeGender            = 25;
    public const int CustomizeBodyType          = 26;
    public const int CustomizeHeight            = 27;
    public const int CustomizeClan              = 28;
    public const int CustomizeFace              = 29;
    public const int CustomizeHairstyle         = 30;
    public const int CustomizeHighlights        = 31;
    public const int CustomizeSkinColor         = 32;
    public const int CustomizeEyeColorRight     = 33;
    public const int CustomizeHairColor         = 34;
    public const int CustomizeHighlightsColor   = 35;
    public const int CustomizeFacialFeature1    = 36;
    public const int CustomizeFacialFeature2    = 37;
    public const int CustomizeFacialFeature3    = 38;
    public const int CustomizeFacialFeature4    = 39;
    public const int CustomizeFacialFeature5    = 40;
    public const int CustomizeFacialFeature6    = 41;
    public const int CustomizeFacialFeature7    = 42;
    public const int CustomizeLegacyTattoo      = 43;
    public const int CustomizeTattooColor       = 44;
    public const int CustomizeEyebrows          = 45;
    public const int CustomizeEyeColorLeft      = 46;
    public const int CustomizeEyeShape          = 47;
    public const int CustomizeSmallIris         = 48;
    public const int CustomizeNose              = 49;
    public const int CustomizeJaw               = 50;
    public const int CustomizeMouth             = 51;
    public const int CustomizeLipstick          = 52;
    public const int CustomizeLipColor          = 53;
    public const int CustomizeMuscleMass        = 54;
    public const int CustomizeTailShape         = 55;
    public const int CustomizeBustSize          = 56;
    public const int CustomizeFacePaint         = 57;
    public const int CustomizeFacePaintReversed = 58;
    public const int CustomizeFacePaintColor    = 59;

    public const int MetaWetness     = 60;
    public const int MetaHatState    = 61;
    public const int MetaVisorState  = 62;
    public const int MetaWeaponState = 63;
    public const int MetaModelId     = 64;

    public const int CrestHead    = 65;
    public const int CrestBody    = 66;
    public const int CrestOffhand = 67;

    public const int ParamSkinDiffuse           = 68;
    public const int ParamMuscleTone            = 69;
    public const int ParamSkinSpecular          = 70;
    public const int ParamLipDiffuse            = 71;
    public const int ParamHairDiffuse           = 72;
    public const int ParamHairSpecular          = 73;
    public const int ParamHairHighlight         = 74;
    public const int ParamLeftEye               = 75;
    public const int ParamRightEye              = 76;
    public const int ParamFeatureColor          = 77;
    public const int ParamFacePaintUvMultiplier = 78;
    public const int ParamFacePaintUvOffset     = 79;
    public const int ParamDecalColor            = 80;

    public const int Size = 81;
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
