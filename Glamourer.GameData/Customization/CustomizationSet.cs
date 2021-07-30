using System;
using System.Collections.Generic;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization
{
    public class CustomizationSet
    {
        public const int DefaultAvailable =
            (1 << (int) CustomizationId.Height)
          | (1 << (int) CustomizationId.Hairstyle)
          | (1 << (int) CustomizationId.HighlightsOnFlag)
          | (1 << (int) CustomizationId.SkinColor)
          | (1 << (int) CustomizationId.EyeColorR)
          | (1 << (int) CustomizationId.EyeColorL)
          | (1 << (int) CustomizationId.HairColor)
          | (1 << (int) CustomizationId.HighlightColor)
          | (1 << (int) CustomizationId.FacialFeaturesTattoos)
          | (1 << (int) CustomizationId.TattooColor)
          | (1 << (int) CustomizationId.LipColor)
          | (1 << (int) CustomizationId.Height);

        internal CustomizationSet(SubRace clan, Gender gender)
        {
            Gender = gender;
            Clan   = clan;
            _settingAvailable =
                clan.ToRace() == Race.Viera && gender == Gender.Male
             || clan.ToRace() == Race.Hrothgar && gender == Gender.Female
                    ? 0
                    : DefaultAvailable;
        }

        public Gender  Gender { get; }
        public SubRace Clan   { get; }

        public Race Race
            => Clan.ToRace();

        private int _settingAvailable = DefaultAvailable;

        internal void SetAvailable(CustomizationId id)
            => _settingAvailable |= 1 << (int) id;

        public bool IsAvailable(CustomizationId id)
            => (_settingAvailable & (1 << (int) id)) != 0;

        public int NumEyebrows    { get; internal set; }
        public int NumEyeShapes   { get; internal set; }
        public int NumNoseShapes  { get; internal set; }
        public int NumJawShapes   { get; internal set; }
        public int NumMouthShapes { get; internal set; }


        public IReadOnlyList<Customization>                Faces           { get; internal set; } = null!;
        public IReadOnlyList<Customization>                HairStyles      { get; internal set; } = null!;
        public IReadOnlyList<Customization>                TailEarShapes   { get; internal set; } = null!;
        public IReadOnlyList<IReadOnlyList<Customization>> FeaturesTattoos { get; internal set; } = null!;
        public IReadOnlyList<Customization>                FacePaints      { get; internal set; } = null!;

        public IReadOnlyList<Customization> SkinColors           { get; internal set; } = null!;
        public IReadOnlyList<Customization> HairColors           { get; internal set; } = null!;
        public IReadOnlyList<Customization> HighlightColors      { get; internal set; } = null!;
        public IReadOnlyList<Customization> EyeColors            { get; internal set; } = null!;
        public IReadOnlyList<Customization> TattooColors         { get; internal set; } = null!;
        public IReadOnlyList<Customization> FacePaintColorsLight { get; internal set; } = null!;
        public IReadOnlyList<Customization> FacePaintColorsDark  { get; internal set; } = null!;
        public IReadOnlyList<Customization> LipColorsLight       { get; internal set; } = null!;
        public IReadOnlyList<Customization> LipColorsDark        { get; internal set; } = null!;

        public IReadOnlyDictionary<CustomizationId, string> OptionName { get; internal set; } = null!;

        public Customization FacialFeature(int faceIdx, int idx)
            => FeaturesTattoos[faceIdx][idx];

        public Customization Data(CustomizationId id, int idx)
        {
            if (idx > Count(id))
                throw new IndexOutOfRangeException();

            switch (id.ToType())
            {
                case CharaMakeParams.MenuType.Percentage:   return new Customization(id, (byte) idx, 0, (ushort) idx);
                case CharaMakeParams.MenuType.ListSelector: return new Customization(id, (byte) idx, 0, (ushort) idx);
            }

            return id switch
            {
                CustomizationId.Face                  => Faces[idx],
                CustomizationId.Hairstyle             => HairStyles[idx],
                CustomizationId.TailEarShape          => TailEarShapes[idx],
                CustomizationId.FacePaint             => FacePaints[idx],
                CustomizationId.FacialFeaturesTattoos => FeaturesTattoos[0][idx],

                CustomizationId.SkinColor      => SkinColors[idx],
                CustomizationId.EyeColorL      => EyeColors[idx],
                CustomizationId.EyeColorR      => EyeColors[idx],
                CustomizationId.HairColor      => HairColors[idx],
                CustomizationId.HighlightColor => HighlightColors[idx],
                CustomizationId.TattooColor    => TattooColors[idx],
                CustomizationId.LipColor       => idx < 96 ? LipColorsDark[idx] : LipColorsLight[idx - 96],
                CustomizationId.FacePaintColor => idx < 96 ? FacePaintColorsDark[idx] : FacePaintColorsLight[idx - 96],
                _                              => new Customization(0, 0),
            };
        }

        public int Count(CustomizationId id)
        {
            if (!IsAvailable(id))
                return 0;

            if (id.ToType() == CharaMakeParams.MenuType.Percentage)
                return 101;

            return id switch
            {
                CustomizationId.Face                  => Faces.Count,
                CustomizationId.Hairstyle             => HairStyles.Count,
                CustomizationId.HighlightsOnFlag      => 2,
                CustomizationId.SkinColor             => SkinColors.Count,
                CustomizationId.EyeColorR             => EyeColors.Count,
                CustomizationId.HairColor             => HairColors.Count,
                CustomizationId.HighlightColor        => HighlightColors.Count,
                CustomizationId.FacialFeaturesTattoos => 8,
                CustomizationId.TattooColor           => TattooColors.Count,
                CustomizationId.Eyebrows              => NumEyebrows,
                CustomizationId.EyeColorL             => EyeColors.Count,
                CustomizationId.EyeShape              => NumEyeShapes,
                CustomizationId.Nose                  => NumNoseShapes,
                CustomizationId.Jaw                   => NumJawShapes,
                CustomizationId.Mouth                 => NumMouthShapes,
                CustomizationId.LipColor              => LipColorsLight.Count + LipColorsDark.Count,
                CustomizationId.TailEarShape          => TailEarShapes.Count,
                CustomizationId.FacePaint             => FacePaints.Count,
                CustomizationId.FacePaintColor        => FacePaintColorsLight.Count + FacePaintColorsDark.Count,
                _                                     => throw new ArgumentOutOfRangeException(nameof(id), id, null),
            };
        }
    }
}
