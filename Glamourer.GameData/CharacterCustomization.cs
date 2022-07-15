using System;
using Glamourer.Customization;
using Penumbra.GameData.Enums;

namespace Glamourer;

public readonly unsafe struct CharacterCustomization
{
    public static readonly CharacterCustomization Null = new(null);

    private readonly CustomizationData* _data;

    public IntPtr Address
        => (IntPtr)_data;

    public CharacterCustomization(CustomizationData* data)
        => _data = data;

    public ref Race Race
        => ref _data->Race;

    public ref SubRace Clan
        => ref _data->Clan;

    public Gender Gender
    {
        get => _data->Gender;
        set => _data->Gender = value;
    }

    public ref byte BodyType
        => ref _data->BodyType;

    public ref byte Height
        => ref _data->Height;

    public ref byte Face
        => ref _data->Face;

    public ref byte Hairstyle
        => ref _data->Hairstyle;

    public bool HighlightsOn
    {
        get => _data->HighlightsOn;
        set => _data->HighlightsOn = value;
    }

    public ref byte SkinColor
        => ref _data->SkinColor;

    public ref byte EyeColorRight
        => ref _data->EyeColorRight;

    public ref byte HairColor
        => ref _data->HairColor;

    public ref byte HighlightsColor
        => ref _data->HighlightsColor;

    public ref byte FacialFeatures
        => ref _data->FacialFeatures;

    public ref byte TattooColor
        => ref _data->TattooColor;

    public ref byte Eyebrow
        => ref _data->Eyebrow;

    public ref byte EyeColorLeft
        => ref _data->EyeColorLeft;

    public byte EyeShape
    {
        get => _data->EyeShape;
        set => _data->EyeShape = value;
    }

    public byte FacePaint
    {
        get => _data->FacePaint;
        set => _data->FacePaint = value;
    }

    public bool FacePaintReversed
    {
        get => _data->FacePaintReversed;
        set => _data->FacePaintReversed = value;
    }

    public byte Mouth
    {
        get => _data->Mouth;
        set => _data->Mouth = value;
    }

    public bool SmallIris
    {
        get => _data->SmallIris;
        set => _data->SmallIris = value;
    }

    public bool Lipstick
    {
        get => _data->Lipstick;
        set => _data->Lipstick = value;
    }

    public ref byte Nose
        => ref _data->Nose;

    public ref byte Jaw
        => ref _data->Jaw;

    public ref byte LipColor
        => ref _data->LipColor;

    public ref byte MuscleMass
        => ref _data->MuscleMass;

    public ref byte TailShape
        => ref _data->TailShape;

    public ref byte BustSize
        => ref _data->BustSize;

    public ref byte FacePaintColor
        => ref _data->FacePaintColor;

    public bool FacialFeature(int idx)
        => _data->FacialFeature(idx);

    public void FacialFeature(int idx, bool set)
        => _data->FacialFeature(idx, set);

    public byte this[CustomizationId id]
    {
        get => _data->Get(id);
        set => _data->Set(id, value);
    }

    public static implicit operator CharacterCustomization(CustomizationData* val)
        => new(val);

    public static implicit operator CharacterCustomization(IntPtr val)
        => new((CustomizationData*)val);

    public static implicit operator bool(CharacterCustomization customize)
        => customize._data != null;

    public static bool operator true(CharacterCustomization customize)
        => customize._data != null;

    public static bool operator false(CharacterCustomization customize)
        => customize._data == null;

    public static bool operator !(CharacterCustomization customize)
        => customize._data == null;
}
