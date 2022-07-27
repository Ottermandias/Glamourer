using System;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Customization;

public unsafe ref struct Customize
{
    private readonly CustomizeData* _data;

    private Customize(CustomizeData* data)
        => _data = data;

    public Race Race
    {
        get => (Race)_data->Data[0];
        set => _data->Data[0] = (byte)value;
    }

    // Skip Unknown Gender
    public Gender Gender
    {
        get => (Gender)(_data->Data[1] + 1);
        set => _data->Data[1] = (byte)(value - 1);
    }

    public ref byte BodyType
        => ref _data->Data[2];

    public ref byte Height
        => ref _data->Data[3];

    public SubRace Clan
    {
        get => (SubRace)_data->Data[4];
        set => _data->Data[4] = (byte)value;
    }

    public ref byte Face
        => ref _data->Data[5];

    public ref byte Hairstyle
        => ref _data->Data[6];

    public bool HighlightsOn
    {
        get => _data->Data[7] >> 7 == 1;
        set => _data->Data[7] = (byte)(value ? _data->Data[7] | 0x80 : _data->Data[7] & 0x7F);
    }

    public ref byte SkinColor
        => ref _data->Data[8];

    public ref byte EyeColorRight
        => ref _data->Data[9];

    public ref byte HairColor
        => ref _data->Data[10];

    public ref byte HighlightsColor
        => ref _data->Data[11];

    public readonly ref struct FacialFeatureStruct
    {
        private readonly byte* _bitfield;

        public FacialFeatureStruct(byte* data)
            => _bitfield = data;

        public bool this[int idx]
        {
            get => (*_bitfield & (1 << idx)) != 0;
            set => *_bitfield = (byte)(value ? *_bitfield | (1 << idx) : *_bitfield & ~(1 << idx));
        }

        public void Clear()
            => *_bitfield = 0;

        public void All()
            => *_bitfield = 0xFF;
    }

    public FacialFeatureStruct FacialFeatures
        => new(_data->Data + 12);

    public ref byte TattooColor
        => ref _data->Data[13];

    public ref byte Eyebrows
        => ref _data->Data[14];

    public ref byte EyeColorLeft
        => ref _data->Data[15];

    public byte EyeShape
    {
        get => (byte)(_data->Data[16] & 0x7F);
        set => _data->Data[16] = (byte)((value & 0x7F) | (_data->Data[16] & 0x80));
    }

    public bool SmallIris
    {
        get => _data->Data[16] >> 7 == 1;
        set => _data->Data[16] = (byte)(value ? _data->Data[16] | 0x80 : _data->Data[16] & 0x7F);
    }

    public ref byte Nose
        => ref _data->Data[17];

    public ref byte Jaw
        => ref _data->Data[18];

    public byte Mouth
    {
        get => (byte)(_data->Data[19] & 0x7F);
        set => _data->Data[19] = (byte)((value & 0x7F) | (_data->Data[19] & 0x80));
    }

    public bool Lipstick
    {
        get => _data->Data[19] >> 7 == 1;
        set => _data->Data[19] = (byte)(value ? _data->Data[19] | 0x80 : _data->Data[19] & 0x7F);
    }

    public ref byte LipColor
        => ref _data->Data[20];

    public ref byte MuscleMass
        => ref _data->Data[21];

    public ref byte TailShape
        => ref _data->Data[22];

    public ref byte BustSize
        => ref _data->Data[23];

    public byte FacePaint
    {
        get => (byte)(_data->Data[24] & 0x7F);
        set => _data->Data[24] = (byte)((value & 0x7F) | (_data->Data[24] & 0x80));
    }

    public bool FacePaintReversed
    {
        get => _data->Data[24] >> 7 == 1;
        set => _data->Data[24] = (byte)(value ? _data->Data[24] | 0x80 : _data->Data[24] & 0x7F);
    }

    public ref byte FacePaintColor
        => ref _data->Data[25];

    internal static readonly CustomizeData Default = GenerateDefault();
    internal static readonly CustomizeData Empty   = new();

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
            CustomizationId.HighlightsOnFlag          => _data->Data[7],
            CustomizationId.SkinColor                 => SkinColor,
            CustomizationId.EyeColorR                 => EyeColorRight,
            CustomizationId.HairColor                 => HairColor,
            CustomizationId.HighlightColor            => HighlightsColor,
            CustomizationId.FacialFeaturesTattoos     => _data->Data[12],
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
            case CustomizationId.FacialFeaturesTattoos:     _data->Data[12] = value;                break;
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
        => throw new NotImplementedException();

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
}
