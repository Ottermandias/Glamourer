using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CustomizationData
{
    public const int CustomizationOffset = 0x830;
    public const int CustomizationBytes  = 26;

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

    // Skip Unknown Gender
    public Gender Gender
    {
        get => (Gender)(_gender + 1);
        set => _gender = (byte)(value - 1);
    }

    // Single bit flag.
    public bool HighlightsOn
    {
        get => (_highlightsOn & 128) == 128;
        set => _highlightsOn = (byte)(value ? _highlightsOn | 128 : _highlightsOn & 127);
    }

    // Get status of specific facial feature 0-7.
    public bool FacialFeature(int idx)
        => (FacialFeatures & (1 << idx)) != 0;

    // Set value of specific facial feature 0-7.
    public void FacialFeature(int idx, bool set)
    {
        if (set)
            FacialFeatures |= (byte)(1 << idx);
        else
            FacialFeatures &= (byte)~(1 << idx);
    }

    // Lower 7 bits
    public byte EyeShape
    {
        get => (byte)(_eyeShape & 127);
        set => _eyeShape = (byte)((value & 127) | (_eyeShape & 128));
    }

    // Uppermost bit flag.
    public bool SmallIris
    {
        get => (_eyeShape & 128) == 128;
        set => _eyeShape = (byte)(value ? _eyeShape | 128 : _eyeShape & 127);
    }

    // Lower 7 bits.
    public byte Mouth
    {
        get => (byte)(_mouth & 127);
        set => _mouth = (byte)((value & 127) | (_mouth & 128));
    }

    // Uppermost bit flag.
    public bool Lipstick
    {
        get => (_mouth & 128) == 128;
        set => _mouth = (byte)(value ? _mouth | 128 : _mouth & 127);
    }

    // Lower 7 bits.
    public byte FacePaint
    {
        get => (byte)(_facePaint & 127);
        set => _facePaint = (byte)((value & 127) | (_facePaint & 128));
    }

    // Uppermost bit flag.
    public bool FacePaintReversed
    {
        get => (_facePaint & 128) == 128;
        set => _facePaint = (byte)(value ? _facePaint | 128 : _facePaint & 127);
    }

    public static CustomizationData Default = new()
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

    public unsafe void Read(CustomizationData* customize)
    {
        fixed (CustomizationData* ptr = &this)
        {
            *ptr = *customize;
        }
    }

    public unsafe void Read(IntPtr customizeAddress)
        => Read((CustomizationData*)customizeAddress);

    public void Read(Character character)
        => Read(character.Address + CustomizationOffset);

    public unsafe void Read(Human* human)
        => Read((CustomizationData*)human->CustomizeData);

    public CustomizationData(Character character)
        : this()
    {
        Read(character.Address + CustomizationOffset);
    }

    public unsafe CustomizationData(Human* human)
        : this()
    {
        Read(human);
    }

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

    public void Set(CustomizationId id, byte value)
    {
        switch (id)
        {
            // @formatter:off
            case CustomizationId.Race:                      Race            = (Race)value;          break;
            case CustomizationId.Gender:                    Gender          = (Gender)value;        break;
            case CustomizationId.BodyType:                  BodyType        = value;                break;
            case CustomizationId.Height:                    Height          = value;                break;
            case CustomizationId.Clan:                      Clan            = (SubRace)value;       break;
            case CustomizationId.Face:                      Face            = value;                break;
            case CustomizationId.Hairstyle:                 Hairstyle       = value;                break;
            case CustomizationId.HighlightsOnFlag:          HighlightsOn    = (value & 128) == 128; break;
            case CustomizationId.SkinColor:                 SkinColor       = value;                break;
            case CustomizationId.EyeColorR:                 EyeColorRight   = value;                break;
            case CustomizationId.HairColor:                 HairColor       = value;                break;
            case CustomizationId.HighlightColor:            HighlightsColor = value;                break;
            case CustomizationId.FacialFeaturesTattoos:     FacialFeatures  = value;                break;
            case CustomizationId.TattooColor:               TattooColor     = value;                break;
            case CustomizationId.Eyebrows:                  Eyebrow         = value;                break;
            case CustomizationId.EyeColorL:                 EyeColorLeft    = value;                break;
            case CustomizationId.EyeShape:                  EyeShape        = value;                break;
            case CustomizationId.Nose:                      Nose            = value;                break;
            case CustomizationId.Jaw:                       Jaw             = value;                break;
            case CustomizationId.Mouth:                     Mouth           = value;                break;
            case CustomizationId.LipColor:                  LipColor        = value;                break;
            case CustomizationId.MuscleToneOrTailEarLength: MuscleMass      = value;                break;
            case CustomizationId.TailEarShape:              TailShape       = value;                break;
            case CustomizationId.BustSize:                  BustSize        = value;                break;
            case CustomizationId.FacePaint:                 FacePaint       = value;                break;
            case CustomizationId.FacePaintColor:            FacePaintColor  = value;                break;
            default:                                        throw new ArgumentOutOfRangeException(nameof(id), id, null);
            // @formatter:on
        }
    }

    public byte this[CustomizationId id]
    {
        get => Get(id);
        set => Set(id, value);
    }

    public unsafe void Write(FFXIVClientStructs.FFXIV.Client.Game.Character.Character* character)
    {
        fixed (CustomizationData* ptr = &this)
        {
            Buffer.MemoryCopy(ptr, character->CustomizeData, CustomizationBytes, CustomizationBytes);
        }
    }

    public unsafe void Write(IntPtr characterAddress)
        => Write((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)characterAddress);

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

    public string HumanReadable()
    {
        // TODO
        var sb = new StringBuilder();
        sb.Append($"Race: {Race.ToName()} - {Clan.ToName()}\n");
        sb.Append($"Gender: {Gender.ToName()}\n");
        sb.Append($"Height: {Height}%\n");
        sb.Append($"Face: #{Face}\n");
        sb.Append($"Hairstyle: #{Hairstyle}\n");
        sb.Append($"Haircolor: #{HairColor}");
        if (HighlightsOn)
            sb.Append($" with Highlights #{HighlightsColor}\n");
        else
            sb.Append('\n');
        return sb.ToString();
    }
}
