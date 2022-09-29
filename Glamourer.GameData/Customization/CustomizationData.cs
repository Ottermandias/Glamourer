using System;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Customization;

public unsafe struct Customize
{
    public readonly CustomizeData* Data;

    public Customize(CustomizeData* data)
        => Data = data;

    public Race Race
    {
        get => (Race)Data->Data[0];
        set => Data->Data[0] = (byte)value;
    }

    // Skip Unknown Gender
    public Gender Gender
    {
        get => (Gender)(Data->Data[1] + 1);
        set => Data->Data[1] = (byte)(value - 1);
    }

    public byte BodyType
    {
        get => Data->Data[2];
        set => Data->Data[2] = value;
    }

    public byte Height
    {
        get => Data->Data[3];
        set => Data->Data[3] = value;
    }

    public SubRace Clan
    {
        get => (SubRace)Data->Data[4];
        set => Data->Data[4] = (byte)value;
    }

    public byte Face
    {
        get => Data->Data[5];
        set => Data->Data[5] = value;
    }

    public byte Hairstyle
    {
        get => Data->Data[6];
        set => Data->Data[6] = value;
    }

    public bool HighlightsOn
    {
        get => Data->Data[7] >> 7 == 1;
        set => Data->Data[7] = (byte)(value ? Data->Data[7] | 0x80 : Data->Data[7] & 0x7F);
    }

    public byte SkinColor
    {
        get => Data->Data[8];
        set => Data->Data[8] = value;
    }

    public byte EyeColorRight
    {
        get => Data->Data[9];
        set => Data->Data[9] = value;
    }

    public byte HairColor
    {
        get => Data->Data[10];
        set => Data->Data[10] = value;
    }

    public byte HighlightsColor
    {
        get => Data->Data[11];
        set => Data->Data[11] = value;
    }

    public readonly ref struct FacialFeatureStruct
    {
        private readonly byte* _bitfield;

        public FacialFeatureStruct(byte* data)
            => _bitfield = data;

        public bool this[int idx]
        {
            get => (*_bitfield & (1 << idx)) != 0;
            set => Set(idx, value);
        }

        public void Clear()
            => *_bitfield = 0;

        public void All()
            => *_bitfield = 0xFF;

        public void Set(int idx, bool value)
            => *_bitfield = (byte)(value ? *_bitfield | (1 << idx) : *_bitfield & ~(1 << idx));
    }

    public FacialFeatureStruct FacialFeatures
        => new(Data->Data + 12);

    public byte TattooColor
    {
        get => Data->Data[13];
        set => Data->Data[13] = value;
    }

    public byte Eyebrows
    {
        get => Data->Data[14];
        set => Data->Data[14] = value;
    }

    public byte EyeColorLeft
    {
        get => Data->Data[15];
        set => Data->Data[15] = value;
    }

    public byte EyeShape
    {
        get => (byte)(Data->Data[16] & 0x7F);
        set => Data->Data[16] = (byte)((value & 0x7F) | (Data->Data[16] & 0x80));
    }

    public bool SmallIris
    {
        get => Data->Data[16] >> 7 == 1;
        set => Data->Data[16] = (byte)(value ? Data->Data[16] | 0x80 : Data->Data[16] & 0x7F);
    }

    public byte Nose
    {
        get => Data->Data[17];
        set => Data->Data[17] = value;
    }

    public byte Jaw
    {
        get => Data->Data[18];
        set => Data->Data[18] = value;
    }

    public byte Mouth
    {
        get => (byte)(Data->Data[19] & 0x7F);
        set => Data->Data[19] = (byte)((value & 0x7F) | (Data->Data[19] & 0x80));
    }

    public bool Lipstick
    {
        get => Data->Data[19] >> 7 == 1;
        set => Data->Data[19] = (byte)(value ? Data->Data[19] | 0x80 : Data->Data[19] & 0x7F);
    }

    public ref byte LipColor
        => ref Data->Data[20];

    public ref byte MuscleMass
        => ref Data->Data[21];

    public ref byte TailShape
        => ref Data->Data[22];

    public ref byte BustSize
        => ref Data->Data[23];

    public byte FacePaint
    {
        get => (byte)(Data->Data[24] & 0x7F);
        set => Data->Data[24] = (byte)((value & 0x7F) | (Data->Data[24] & 0x80));
    }

    public bool FacePaintReversed
    {
        get => Data->Data[24] >> 7 == 1;
        set => Data->Data[24] = (byte)(value ? Data->Data[24] | 0x80 : Data->Data[24] & 0x7F);
    }

    public ref byte FacePaintColor
        => ref Data->Data[25];

    public static readonly CustomizeData Default = GenerateDefault();
    public static readonly CustomizeData Empty   = new();

    public byte Get(CustomizationId id)
        => id switch
        {
            CustomizationId.Race                      => (byte)Race,
            CustomizationId.Gender                    => (byte)Gender,
            CustomizationId.BodyType                  => BodyType,
            CustomizationId.Height                    => Height,
            CustomizationId.Clan                      => (byte)Clan,
            CustomizationId.Face                      => Face,
            CustomizationId.Hairstyle                 => Hairstyle,
            CustomizationId.HighlightsOnFlag          => Data->Data[7],
            CustomizationId.SkinColor                 => SkinColor,
            CustomizationId.EyeColorR                 => EyeColorRight,
            CustomizationId.HairColor                 => HairColor,
            CustomizationId.HighlightColor            => HighlightsColor,
            CustomizationId.FacialFeaturesTattoos     => Data->Data[12],
            CustomizationId.TattooColor               => TattooColor,
            CustomizationId.Eyebrows                  => Eyebrows,
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

    public void Set(CustomizationId id, byte value)
    {
        switch (id)
        {
            // @formatter:off
            case CustomizationId.Race:                      Race             = (Race)value;          break;
            case CustomizationId.Gender:                    Gender           = (Gender)value;        break;
            case CustomizationId.BodyType:                  BodyType         = value;                break;
            case CustomizationId.Height:                    Height           = value;                break;
            case CustomizationId.Clan:                      Clan             = (SubRace)value;       break;
            case CustomizationId.Face:                      Face             = value;                break;
            case CustomizationId.Hairstyle:                 Hairstyle        = value;                break;
            case CustomizationId.HighlightsOnFlag:          HighlightsOn     = (value & 128) == 128; break;
            case CustomizationId.SkinColor:                 SkinColor        = value;                break;
            case CustomizationId.EyeColorR:                 EyeColorRight    = value;                break;
            case CustomizationId.HairColor:                 HairColor        = value;                break;
            case CustomizationId.HighlightColor:            HighlightsColor  = value;                break;
            case CustomizationId.FacialFeaturesTattoos:     Data->Data[12] = value;                break;
            case CustomizationId.TattooColor:               TattooColor      = value;                break;
            case CustomizationId.Eyebrows:                  Eyebrows         = value;                break;
            case CustomizationId.EyeColorL:                 EyeColorLeft     = value;                break;
            case CustomizationId.EyeShape:                  EyeShape         = value;                break;
            case CustomizationId.Nose:                      Nose             = value;                break;
            case CustomizationId.Jaw:                       Jaw              = value;                break;
            case CustomizationId.Mouth:                     Mouth            = value;                break;
            case CustomizationId.LipColor:                  LipColor         = value;                break;
            case CustomizationId.MuscleToneOrTailEarLength: MuscleMass       = value;                break;
            case CustomizationId.TailEarShape:              TailShape        = value;                break;
            case CustomizationId.BustSize:                  BustSize         = value;                break;
            case CustomizationId.FacePaint:                 FacePaint        = value;                break;
            case CustomizationId.FacePaintColor:            FacePaintColor   = value;                break;
            default:                                        throw new ArgumentOutOfRangeException(nameof(id), id, null);
            // @formatter:on
        }
    }

    public bool Equals(Customize other)
        => CustomizeData.Equals(Data, other.Data);

    public byte this[CustomizationId id]
    {
        get => Get(id);
        set => Set(id, value);
    }

    private static CustomizeData GenerateDefault()
    {
        var ret = new CustomizeData();
        var customize = new Customize(&ret)
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
            TattooColor     = 1,
            Eyebrows        = 1,
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
        customize.FacialFeatures.Clear();

        return ret;
    }

    public void Load(Customize other)
        => Data->Read(other.Data);

    public void Write(IntPtr target)
        => Data->Write((void*)target);
}
