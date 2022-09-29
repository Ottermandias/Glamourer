using System;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Customization;

public record struct CustomizationByteValue(byte Value)
{
    public static readonly CustomizationByteValue Zero = new(0);

    public static explicit operator CustomizationByteValue(byte value)
        => new(value);

    public static CustomizationByteValue operator ++(CustomizationByteValue v)
        => new(++v.Value);

    public static CustomizationByteValue operator --(CustomizationByteValue v)
        => new(--v.Value);

    public static bool operator <(CustomizationByteValue v, int count)
        => v.Value < count;

    public static bool operator >(CustomizationByteValue v, int count)
        => v.Value > count;

    public static CustomizationByteValue operator +(CustomizationByteValue v, int rhs)
        => new((byte)(v.Value + rhs));

    public static CustomizationByteValue operator -(CustomizationByteValue v, int rhs)
        => new((byte)(v.Value - rhs));

    public override string ToString()
        => Value.ToString();
}

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

    public CustomizationByteValue BodyType
    {
        get => (CustomizationByteValue)Data->Data[2];
        set => Data->Data[2] = value.Value;
    }

    public CustomizationByteValue Height
    {
        get => (CustomizationByteValue)Data->Data[3];
        set => Data->Data[3] = value.Value;
    }

    public SubRace Clan
    {
        get => (SubRace)Data->Data[4];
        set => Data->Data[4] = (byte)value;
    }

    public CustomizationByteValue Face
    {
        get => (CustomizationByteValue)Data->Data[5];
        set => Data->Data[5] = value.Value;
    }

    public CustomizationByteValue Hairstyle
    {
        get => (CustomizationByteValue)Data->Data[6];
        set => Data->Data[6] = value.Value;
    }

    public bool HighlightsOn
    {
        get => Data->Data[7] >> 7 == 1;
        set => Data->Data[7] = (byte)(value ? Data->Data[7] | 0x80 : Data->Data[7] & 0x7F);
    }

    public CustomizationByteValue SkinColor
    {
        get => (CustomizationByteValue)Data->Data[8];
        set => Data->Data[8] = value.Value;
    }

    public CustomizationByteValue EyeColorRight
    {
        get => (CustomizationByteValue)Data->Data[9];
        set => Data->Data[9] = value.Value;
    }

    public CustomizationByteValue HairColor
    {
        get => (CustomizationByteValue)Data->Data[10];
        set => Data->Data[10] = value.Value;
    }

    public CustomizationByteValue HighlightsColor
    {
        get => (CustomizationByteValue)Data->Data[11];
        set => Data->Data[11] = value.Value;
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

    public CustomizationByteValue TattooColor
    {
        get => (CustomizationByteValue)Data->Data[13];
        set => Data->Data[13] = value.Value;
    }

    public CustomizationByteValue Eyebrows
    {
        get => (CustomizationByteValue)Data->Data[14];
        set => Data->Data[14] = value.Value;
    }

    public CustomizationByteValue EyeColorLeft
    {
        get => (CustomizationByteValue)Data->Data[15];
        set => Data->Data[15] = value.Value;
    }

    public CustomizationByteValue EyeShape
    {
        get => (CustomizationByteValue)(Data->Data[16] & 0x7F);
        set => Data->Data[16] = (byte)((value.Value & 0x7F) | (Data->Data[16] & 0x80));
    }

    public bool SmallIris
    {
        get => Data->Data[16] >> 7 == 1;
        set => Data->Data[16] = (byte)(value ? Data->Data[16] | 0x80 : Data->Data[16] & 0x7F);
    }

    public CustomizationByteValue Nose
    {
        get => (CustomizationByteValue)Data->Data[17];
        set => Data->Data[17] = value.Value;
    }

    public CustomizationByteValue Jaw
    {
        get => (CustomizationByteValue)Data->Data[18];
        set => Data->Data[18] = value.Value;
    }

    public CustomizationByteValue Mouth
    {
        get => (CustomizationByteValue)(Data->Data[19] & 0x7F);
        set => Data->Data[19] = (byte)((value.Value & 0x7F) | (Data->Data[19] & 0x80));
    }

    public bool Lipstick
    {
        get => Data->Data[19] >> 7 == 1;
        set => Data->Data[19] = (byte)(value ? Data->Data[19] | 0x80 : Data->Data[19] & 0x7F);
    }

    public CustomizationByteValue LipColor
    {
        get => (CustomizationByteValue)Data->Data[20];
        set => Data->Data[20] = value.Value;
    }

    public CustomizationByteValue MuscleMass
    {
        get => (CustomizationByteValue)Data->Data[21];
        set => Data->Data[21] = value.Value;
    }

    public CustomizationByteValue TailShape
    {
        get => (CustomizationByteValue)Data->Data[22];
        set => Data->Data[22] = value.Value;
    }

    public CustomizationByteValue BustSize
    {
        get => (CustomizationByteValue)Data->Data[23];
        set => Data->Data[23] = value.Value;
    }

    public CustomizationByteValue FacePaint
    {
        get => (CustomizationByteValue)(Data->Data[24] & 0x7F);
        set => Data->Data[24] = (byte)((value.Value & 0x7F) | (Data->Data[24] & 0x80));
    }

    public bool FacePaintReversed
    {
        get => Data->Data[24] >> 7 == 1;
        set => Data->Data[24] = (byte)(value ? Data->Data[24] | 0x80 : Data->Data[24] & 0x7F);
    }

    public CustomizationByteValue FacePaintColor
    {
        get => (CustomizationByteValue)Data->Data[25];
        set => Data->Data[25] = value.Value;
    }

    public static readonly CustomizeData Default = GenerateDefault();
    public static readonly CustomizeData Empty   = new();

    public CustomizationByteValue Get(CustomizationId id)
        => id switch
        {
            CustomizationId.Race                      => (CustomizationByteValue)(byte)Race,
            CustomizationId.Gender                    => (CustomizationByteValue)(byte)Gender,
            CustomizationId.BodyType                  => BodyType,
            CustomizationId.Height                    => Height,
            CustomizationId.Clan                      => (CustomizationByteValue)(byte)Clan,
            CustomizationId.Face                      => Face,
            CustomizationId.Hairstyle                 => Hairstyle,
            CustomizationId.HighlightsOnFlag          => (CustomizationByteValue)Data->Data[7],
            CustomizationId.SkinColor                 => SkinColor,
            CustomizationId.EyeColorR                 => EyeColorRight,
            CustomizationId.HairColor                 => HairColor,
            CustomizationId.HighlightColor            => HighlightsColor,
            CustomizationId.FacialFeaturesTattoos     => (CustomizationByteValue)Data->Data[12],
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

    public void Set(CustomizationId id, CustomizationByteValue value)
    {
        switch (id)
        {
            // @formatter:off
            case CustomizationId.Race:                      Race             = (Race)value.Value;          break;
            case CustomizationId.Gender:                    Gender           = (Gender)value.Value;        break;
            case CustomizationId.BodyType:                  BodyType         = value;                      break;
            case CustomizationId.Height:                    Height           = value;                      break;
            case CustomizationId.Clan:                      Clan             = (SubRace)value.Value;       break;
            case CustomizationId.Face:                      Face             = value;                      break;
            case CustomizationId.Hairstyle:                 Hairstyle        = value;                      break;
            case CustomizationId.HighlightsOnFlag:          HighlightsOn     = (value.Value & 128) == 128; break;
            case CustomizationId.SkinColor:                 SkinColor        = value;                      break;
            case CustomizationId.EyeColorR:                 EyeColorRight    = value;                      break;
            case CustomizationId.HairColor:                 HairColor        = value;                      break;
            case CustomizationId.HighlightColor:            HighlightsColor  = value;                      break;
            case CustomizationId.FacialFeaturesTattoos:     Data->Data[12]   = value.Value;                break;
            case CustomizationId.TattooColor:               TattooColor      = value;                      break;
            case CustomizationId.Eyebrows:                  Eyebrows         = value;                      break;
            case CustomizationId.EyeColorL:                 EyeColorLeft     = value;                      break;
            case CustomizationId.EyeShape:                  EyeShape         = value;                      break;
            case CustomizationId.Nose:                      Nose             = value;                      break;
            case CustomizationId.Jaw:                       Jaw              = value;                      break;
            case CustomizationId.Mouth:                     Mouth            = value;                      break;
            case CustomizationId.LipColor:                  LipColor         = value;                      break;
            case CustomizationId.MuscleToneOrTailEarLength: MuscleMass       = value;                      break;
            case CustomizationId.TailEarShape:              TailShape        = value;                      break;
            case CustomizationId.BustSize:                  BustSize         = value;                      break;
            case CustomizationId.FacePaint:                 FacePaint        = value;                      break;
            case CustomizationId.FacePaintColor:            FacePaintColor   = value;                      break;
            default:                                        throw new ArgumentOutOfRangeException(nameof(id), id, null);
            // @formatter:on
        }
    }

    public bool Equals(Customize other)
        => CustomizeData.Equals(Data, other.Data);

    public CustomizationByteValue this[CustomizationId id]
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
            BodyType        = (CustomizationByteValue)1,
            Height          = (CustomizationByteValue)50,
            Clan            = SubRace.Midlander,
            Face            = (CustomizationByteValue)1,
            Hairstyle       = (CustomizationByteValue)1,
            HighlightsOn    = false,
            SkinColor       = (CustomizationByteValue)1,
            EyeColorRight   = (CustomizationByteValue)1,
            HighlightsColor = (CustomizationByteValue)1,
            TattooColor     = (CustomizationByteValue)1,
            Eyebrows        = (CustomizationByteValue)1,
            EyeColorLeft    = (CustomizationByteValue)1,
            EyeShape        = (CustomizationByteValue)1,
            Nose            = (CustomizationByteValue)1,
            Jaw             = (CustomizationByteValue)1,
            Mouth           = (CustomizationByteValue)1,
            LipColor        = (CustomizationByteValue)1,
            MuscleMass      = (CustomizationByteValue)50,
            TailShape       = (CustomizationByteValue)1,
            BustSize        = (CustomizationByteValue)50,
            FacePaint       = (CustomizationByteValue)1,
            FacePaintColor  = (CustomizationByteValue)1,
        };
        customize.FacialFeatures.Clear();

        return ret;
    }

    public void Load(Customize other)
        => Data->Read(other.Data);

    public void Write(IntPtr target)
        => Data->Write((void*)target);
}
