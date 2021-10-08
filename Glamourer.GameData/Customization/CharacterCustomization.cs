using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization
{
    public unsafe struct LazyCustomization
    {
        public CharacterCustomization* Address;

        public LazyCustomization(IntPtr characterPtr)
            => Address = (CharacterCustomization*) (characterPtr + CharacterCustomization.CustomizationOffset);

        public ref CharacterCustomization Value
            => ref *Address;

        public LazyCustomization(CharacterCustomization data)
            => Address = &data;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CharacterCustomization
    {
        public const int CustomizationOffset = 0x1898;
        public const int CustomizationBytes  = 26;

        public static CharacterCustomization Default = new()
        {
            Race            = Race.Hyur,
            Gender          = Gender.Male,
            BodyType        = 1,
            Height          = 50,
            Clan            = SubRace.Midlander,
            Face            = 1,
            Hairstyle       = 1,
            HighlightsOn    = false,
            SkinColor       = 1,
            EyeColorRight   = 1,
            HighlightsColor = 1,
            FacialFeatures  = 0,
            TattooColor     = 1,
            Eyebrow         = 1,
            EyeColorLeft    = 1,
            EyeShape        = 1,
            Nose            = 1,
            Jaw             = 1,
            Mouth           = 1,
            LipColor        = 1,
            MuscleMass      = 50,
            TailShape       = 1,
            BustSize        = 50,
            FacePaint       = 1,
            FacePaintColor  = 1,
        };

        public  Race    Race;
        private byte    _gender;
        public  byte    BodyType;
        public  byte    Height;
        public  SubRace Clan;
        public  byte    Face;
        public  byte    Hairstyle;
        private byte    _highlightsOn;
        public  byte    SkinColor;
        public  byte    EyeColorRight;
        public  byte    HairColor;
        public  byte    HighlightsColor;
        public  byte    FacialFeatures;
        public  byte    TattooColor;
        public  byte    Eyebrow;
        public  byte    EyeColorLeft;
        private byte    _eyeShape;
        public  byte    Nose;
        public  byte    Jaw;
        private byte    _mouth;
        public  byte    LipColor;
        public  byte    MuscleMass;
        public  byte    TailShape;
        public  byte    BustSize;
        private byte    _facePaint;
        public  byte    FacePaintColor;

        public Gender Gender
        {
            get => (Gender) (_gender + 1);
            set => _gender = (byte) (value - 1);
        }

        public bool HighlightsOn
        {
            get => (_highlightsOn & 128) == 128;
            set => _highlightsOn = (byte) (value ? _highlightsOn | 128 : _highlightsOn & 127);
        }

        public bool FacialFeature(int idx)
            => (FacialFeatures & (1 << idx)) != 0;

        public void FacialFeature(int idx, bool set)
        {
            if (set)
                FacialFeatures |= (byte) (1 << idx);
            else
                FacialFeatures &= (byte) ~(1 << idx);
        }

        public byte EyeShape
        {
            get => (byte) (_eyeShape & 127);
            set => _eyeShape = (byte) ((value & 127) | (_eyeShape & 128));
        }

        public bool SmallIris
        {
            get => (_eyeShape & 128) == 128;
            set => _eyeShape = (byte) (value ? _eyeShape | 128 : _eyeShape & 127);
        }


        public byte Mouth
        {
            get => (byte) (_mouth & 127);
            set => _mouth = (byte) ((value & 127) | (_mouth & 128));
        }

        public bool Lipstick
        {
            get => (_mouth & 128) == 128;
            set => _mouth = (byte) (value ? _mouth | 128 : _mouth & 127);
        }

        public byte FacePaint
        {
            get => (byte) (_facePaint & 127);
            set => _facePaint = (byte) ((value & 127) | (_facePaint & 128));
        }

        public bool FacePaintReversed
        {
            get => (_facePaint & 128) == 128;
            set => _facePaint = (byte) (value ? _facePaint | 128 : _facePaint & 127);
        }

        public unsafe void Read(IntPtr customizeAddress)
        {
            fixed (Race* ptr = &Race)
            {
                Buffer.MemoryCopy(customizeAddress.ToPointer(), ptr, CustomizationBytes, CustomizationBytes);
            }
        }

        public void Read(Character character)
            => Read(character.Address + CustomizationOffset);

        public CharacterCustomization(Character character)
            : this()
        {
            Read(character.Address + CustomizationOffset);
        }

        public byte this[CustomizationId id]
        {
            get => id switch
            {
                CustomizationId.Race                      => (byte) Race,
                CustomizationId.Gender                    => (byte) Gender,
                CustomizationId.BodyType                  => BodyType,
                CustomizationId.Height                    => Height,
                CustomizationId.Clan                      => (byte) Clan,
                CustomizationId.Face                      => Face,
                CustomizationId.Hairstyle                 => Hairstyle,
                CustomizationId.HighlightsOnFlag          => _highlightsOn,
                CustomizationId.SkinColor                 => SkinColor,
                CustomizationId.EyeColorR                 => EyeColorRight,
                CustomizationId.HairColor                 => HairColor,
                CustomizationId.HighlightColor            => HighlightsColor,
                CustomizationId.FacialFeaturesTattoos     => FacialFeatures,
                CustomizationId.TattooColor               => TattooColor,
                CustomizationId.Eyebrows                  => Eyebrow,
                CustomizationId.EyeColorL                 => EyeColorLeft,
                CustomizationId.EyeShape                  => EyeShape,
                CustomizationId.Nose                      => Nose,
                CustomizationId.Jaw                       => Jaw,
                CustomizationId.Mouth                     => Mouth,
                CustomizationId.LipColor                  => LipColor,
                CustomizationId.MuscleToneOrTailEarLength => MuscleMass,
                CustomizationId.TailEarShape              => TailShape,
                CustomizationId.BustSize                  => BustSize,
                CustomizationId.FacePaint                 => FacePaint,
                CustomizationId.FacePaintColor            => FacePaintColor,
                _                                         => throw new ArgumentOutOfRangeException(nameof(id), id, null),
            };
            set
            {
                switch (id)
                {
                    case CustomizationId.Race:
                        Race = (Race) value;
                        break;
                    case CustomizationId.Gender:
                        Gender = (Gender) value;
                        break;
                    case CustomizationId.BodyType:
                        BodyType = value;
                        break;
                    case CustomizationId.Height:
                        Height = value;
                        break;
                    case CustomizationId.Clan:
                        Clan = (SubRace) value;
                        break;
                    case CustomizationId.Face:
                        Face = value;
                        break;
                    case CustomizationId.Hairstyle:
                        Hairstyle = value;
                        break;
                    case CustomizationId.HighlightsOnFlag:
                        HighlightsOn = (value & 128) == 128;
                        break;
                    case CustomizationId.SkinColor:
                        SkinColor = value;
                        break;
                    case CustomizationId.EyeColorR:
                        EyeColorRight = value;
                        break;
                    case CustomizationId.HairColor:
                        HairColor = value;
                        break;
                    case CustomizationId.HighlightColor:
                        HighlightsColor = value;
                        break;
                    case CustomizationId.FacialFeaturesTattoos:
                        FacialFeatures = value;
                        break;
                    case CustomizationId.TattooColor:
                        TattooColor = value;
                        break;
                    case CustomizationId.Eyebrows:
                        Eyebrow = value;
                        break;
                    case CustomizationId.EyeColorL:
                        EyeColorLeft = value;
                        break;
                    case CustomizationId.EyeShape:
                        EyeShape = value;
                        break;
                    case CustomizationId.Nose:
                        Nose = value;
                        break;
                    case CustomizationId.Jaw:
                        Jaw = value;
                        break;
                    case CustomizationId.Mouth:
                        Mouth = value;
                        break;
                    case CustomizationId.LipColor:
                        LipColor = value;
                        break;
                    case CustomizationId.MuscleToneOrTailEarLength:
                        MuscleMass = value;
                        break;
                    case CustomizationId.TailEarShape:
                        TailShape = value;
                        break;
                    case CustomizationId.BustSize:
                        BustSize = value;
                        break;
                    case CustomizationId.FacePaint:
                        FacePaint = value;
                        break;
                    case CustomizationId.FacePaintColor:
                        FacePaintColor = value;
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
                }
            }
        }

        public unsafe void Write(IntPtr characterAddress)
        {
            fixed (Race* ptr = &Race)
            {
                Buffer.MemoryCopy(ptr, (byte*) characterAddress + CustomizationOffset, CustomizationBytes, CustomizationBytes);
            }
        }

        public unsafe void WriteBytes(byte[] array, int offset = 0)
        {
            fixed (Race* ptr = &Race)
            {
                Marshal.Copy(new IntPtr(ptr), array, offset, CustomizationBytes);
            }
        }

        public byte[] ToBytes()
        {
            var ret = new byte[CustomizationBytes];
            WriteBytes(ret);
            return ret;
        }
    }
}
