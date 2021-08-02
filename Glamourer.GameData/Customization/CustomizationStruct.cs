using System;
using System.Runtime.InteropServices;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CustomizationStruct
    {
        public CustomizationStruct(IntPtr ptr)
            => _ptr = (byte*) ptr;

        private readonly byte* _ptr;

        public byte this[CustomizationId id]
        {
            get => id switch
            {
                // Needs to handle the Highlander Race in enum.
                CustomizationId.Race => (byte) (_ptr[(int) CustomizationId.Race] > 1 ? _ptr[(int) CustomizationId.Race] + 1 : 1),
                // Needs to handle Gender.Unknown = 0.
                CustomizationId.Gender => (byte) (_ptr[(int) id] + 1),
                // Just a flag.
                CustomizationId.HighlightsOnFlag => (byte) ((_ptr[(int) id] & 128) == 128 ? 1 : 0),
                // Eye also includes iris flag at bit 128.
                CustomizationId.EyeShape => (byte) (_ptr[(int) CustomizationId.EyeShape] & 127),
                // Mouth also includes Lipstick flag at bit 128.
                CustomizationId.Mouth => (byte) (_ptr[(int) CustomizationId.Mouth] & 127),
                // FacePaint also includes Reverse bit at 128.
                CustomizationId.FacePaint => (byte) (_ptr[(int) CustomizationId.FacePaint] & 127),
                _                         => _ptr[(int) id],
            };
            set
            {
                _ptr[(int) id] = id switch
                {
                    CustomizationId.Race             => (byte) (value > (byte) Race.Midlander ? value - 1 : value),
                    CustomizationId.Gender           => (byte) (value - 1),
                    CustomizationId.HighlightsOnFlag => (byte) (value != 0 ? 128 : 0),
                    CustomizationId.EyeShape         => (byte) ((_ptr[(int) CustomizationId.EyeShape] & 128) | (value & 127)),
                    CustomizationId.Mouth            => (byte) ((_ptr[(int) CustomizationId.Mouth] & 128) | (value & 127)),
                    CustomizationId.FacePaint        => (byte) ((_ptr[(int) CustomizationId.FacePaint] & 128) | (value & 127)),
                    _                                => value,
                };
                
                if (Race != Race.Hrothgar)
                    return;

                // Handle Hrothgar Face being dependent on hairstyle, 2 faces per hairstyle.
                switch (id)
                {
                    case CustomizationId.Hairstyle:
                        _ptr[(int) CustomizationId.Face] = (byte) ((value + 1) / 2);
                        break;
                    case CustomizationId.Face:
                        _ptr[(int) CustomizationId.Hairstyle] = (byte) (value * 2 - 1);
                        break;
                }
            }
        }

        public Race Race
        {
            get => (Race) this[CustomizationId.Race];
            set => this[CustomizationId.Race] = (byte) value;
        }

        public Gender Gender
        {
            get => (Gender) this[CustomizationId.Gender];
            set => this[CustomizationId.Gender] = (byte) value;
        }

        public byte BodyType
        {
            get => this[CustomizationId.BodyType];
            set => this[CustomizationId.BodyType] = value;
        }

        public byte Height
        {
            get => _ptr[(int) CustomizationId.Height];
            set => _ptr[(int) CustomizationId.Height] = value;
        }

        public SubRace Clan
        {
            get => (SubRace) this[CustomizationId.Clan];
            set => this[CustomizationId.Clan] = (byte) value;
        }

        public byte Face
        {
            get => this[CustomizationId.Face];
            set => this[CustomizationId.Face] = value;
        }

        public byte Hairstyle
        {
            get => this[CustomizationId.Hairstyle];
            set => this[CustomizationId.Hairstyle] = value;
        }

        public bool HighlightsOn
        {
            get => this[CustomizationId.HighlightsOnFlag] == 1;
            set => this[CustomizationId.HighlightsOnFlag] = (byte) (value ? 1 : 0);
        }

        public byte SkinColor
        {
            get => this[CustomizationId.SkinColor];
            set => this[CustomizationId.SkinColor] = value;
        }

        public byte EyeColorRight
        {
            get => this[CustomizationId.EyeColorR];
            set => this[CustomizationId.EyeColorR] = value;
        }

        public byte HairColor
        {
            get => this[CustomizationId.HairColor];
            set => this[CustomizationId.HairColor] = value;
        }

        public byte HighlightsColor
        {
            get => this[CustomizationId.HighlightColor];
            set => this[CustomizationId.HighlightColor] = value;
        }

        public bool FacialFeature(int idx)
            => (this[CustomizationId.FacialFeaturesTattoos] & (1 << idx)) != 0;

        public void FacialFeature(int idx, bool set)
        {
            if (set)
                this[CustomizationId.FacialFeaturesTattoos] |= (byte) (1 << idx);
            else
                this[CustomizationId.FacialFeaturesTattoos] &= (byte) ~(1 << idx);
        }

        public byte FacialFeatures
        {
            get => this[CustomizationId.FacialFeaturesTattoos];
            set => this[CustomizationId.FacialFeaturesTattoos] = value;
        }

        public byte TattooColor
        {
            get => this[CustomizationId.TattooColor];
            set => this[CustomizationId.TattooColor] = value;
        }

        public byte Eyebrow
        {
            get => this[CustomizationId.Eyebrows];
            set => this[CustomizationId.Eyebrows] = value;
        }

        public byte EyeColorLeft
        {
            get => this[CustomizationId.EyeColorL];
            set => this[CustomizationId.EyeColorL] = value;
        }

        public byte Eye
        {
            get => this[CustomizationId.EyeShape];
            set => this[CustomizationId.EyeShape] = value;
        }

        public bool SmallIris
        {
            get => (_ptr[(int) CustomizationId.EyeShape] & 128) == 128;
            set => _ptr[(int) CustomizationId.EyeShape] = (byte) (this[CustomizationId.EyeShape] | (value ? 128u : 0u));
        }

        public byte Nose
        {
            get => _ptr[(int) CustomizationId.Nose];
            set => _ptr[(int) CustomizationId.Nose] = value;
        }

        public byte Jaw
        {
            get => _ptr[(int) CustomizationId.Jaw];
            set => _ptr[(int) CustomizationId.Jaw] = value;
        }

        public byte Mouth
        {
            get => this[CustomizationId.Mouth];
            set => this[CustomizationId.Mouth] = value;
        }

        public bool LipstickSet
        {
            get => (_ptr[(int) CustomizationId.Mouth] & 128) == 128;
            set => _ptr[(int) CustomizationId.Mouth] = (byte) (this[CustomizationId.Mouth] | (value ? 128u : 0u));
        }

        public byte LipColor
        {
            get => _ptr[(int) CustomizationId.LipColor];
            set => _ptr[(int) CustomizationId.LipColor] = value;
        }

        public byte MuscleMass
        {
            get => _ptr[(int) CustomizationId.MuscleToneOrTailEarLength];
            set => _ptr[(int) CustomizationId.MuscleToneOrTailEarLength] = value;
        }

        public byte TailShape
        {
            get => _ptr[(int) CustomizationId.TailEarShape];
            set => _ptr[(int) CustomizationId.TailEarShape] = value;
        }

        public byte BustSize
        {
            get => _ptr[(int) CustomizationId.BustSize];
            set => _ptr[(int) CustomizationId.BustSize] = value;
        }

        public byte FacePaint
        {
            get => this[CustomizationId.FacePaint];
            set => this[CustomizationId.FacePaint] = value;
        }

        public bool FacePaintReversed
        {
            get => (_ptr[(int) CustomizationId.FacePaint] & 128) == 128;
            set => _ptr[(int) CustomizationId.FacePaint] = (byte) (this[CustomizationId.FacePaint] | (value ? 128u : 0u));
        }

        public byte FacePaintColor
        {
            get => _ptr[(int) CustomizationId.FacePaintColor];
            set => _ptr[(int) CustomizationId.FacePaintColor] = value;
        }
    }
}
