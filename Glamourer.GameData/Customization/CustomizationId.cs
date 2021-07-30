using System;

namespace Glamourer.Customization
{
    public enum CustomizationId : byte
    {
        Race                      = 0,
        Gender                    = 1,
        BodyType                  = 2,
        Height                    = 3,
        Clan                      = 4,
        Face                      = 5,
        Hairstyle                 = 6,
        HighlightsOnFlag          = 7,
        SkinColor                 = 8,
        EyeColorR                 = 9,
        HairColor                 = 10,
        HighlightColor            = 11,
        FacialFeaturesTattoos     = 12, // Bitmask, 1-7 per face, 8 is 1.0 tattoo
        TattooColor               = 13,
        Eyebrows                  = 14,
        EyeColorL                 = 15,
        EyeShape                  = 16, // Flag 128 for Small
        Nose                      = 17,
        Jaw                       = 18,
        Mouth                     = 19, // Flag 128 for Lip Color set
        LipColor                  = 20, // Flag 128 for Light instead of Dark
        MuscleToneOrTailEarLength = 21,
        TailEarShape              = 22,
        BustSize                  = 23,
        FacePaint                 = 24,
        FacePaintColor            = 25, // Flag 128 for Light instead of Dark.
    }

    public static class CustomizationExtensions
    {
        public static CharaMakeParams.MenuType ToType(this CustomizationId customizationId, bool isHrothgar = false)
            => customizationId switch
            {
                CustomizationId.Race                      => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.Gender                    => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.BodyType                  => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.Height                    => CharaMakeParams.MenuType.Percentage,
                CustomizationId.Clan                      => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.Face                      => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.Hairstyle                 => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.HighlightsOnFlag          => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.SkinColor                 => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.EyeColorR                 => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.HairColor                 => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.HighlightColor            => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.FacialFeaturesTattoos     => CharaMakeParams.MenuType.MultiIconSelector,
                CustomizationId.TattooColor               => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.Eyebrows                  => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.EyeColorL                 => CharaMakeParams.MenuType.ColorPicker,
                CustomizationId.EyeShape                  => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.Nose                      => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.Jaw                       => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.Mouth                     => CharaMakeParams.MenuType.ListSelector,
                CustomizationId.MuscleToneOrTailEarLength => CharaMakeParams.MenuType.Percentage,
                CustomizationId.TailEarShape              => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.BustSize                  => CharaMakeParams.MenuType.Percentage,
                CustomizationId.FacePaint                 => CharaMakeParams.MenuType.IconSelector,
                CustomizationId.FacePaintColor            => CharaMakeParams.MenuType.ColorPicker,

                CustomizationId.LipColor => isHrothgar ? CharaMakeParams.MenuType.IconSelector : CharaMakeParams.MenuType.ColorPicker,
                _                        => throw new ArgumentOutOfRangeException(nameof(customizationId), customizationId, null),
            };
    }
}
