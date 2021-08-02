using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud;
using Dalamud.Plugin;
using Glamourer.Util;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Penumbra.GameData.Enums;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Customization
{
    public class CustomizationOptions
    {
        internal static readonly Race[]    Races = ((Race[]) Enum.GetValues(typeof(Race))).Skip(1).ToArray();
        internal static readonly SubRace[] Clans = ((SubRace[]) Enum.GetValues(typeof(SubRace))).Skip(1).ToArray();

        internal static readonly Gender[] Genders =
        {
            Gender.Male,
            Gender.Female,
        };

        internal CustomizationSet GetList(SubRace race, Gender gender)
            => _list[ToIndex(race, gender)];

        internal ImGuiScene.TextureWrap GetIcon(uint id)
            => _icons.LoadIcon(id);

        private static readonly int ListSize = Clans.Length * Genders.Length;

        private readonly CustomizationSet[] _list = new CustomizationSet[ListSize];
        private readonly IconStorage        _icons;

        private static void ThrowException(SubRace race, Gender gender)
            => throw new Exception($"Invalid customization requested for {race} {gender}.");

        private static int ToIndex(SubRace race, Gender gender)
        {
            if (race == SubRace.Unknown || gender != Gender.Female && gender != Gender.Male)
                ThrowException(race, gender);

            var ret = (int) race - 1;
            ret = ret * Genders.Length + (gender == Gender.Female ? 1 : 0);
            return ret;
        }

        private Customization[] GetHairStyles(SubRace race, Gender gender)
        {
            var row      = _hairSheet.GetRow(((uint) race - 1) * 2 - 1 + (uint) gender);
            var hairList = new List<Customization>(row.Unknown30);
            for (var i = 0; i < row.Unknown30; ++i)
            {
                var name = $"Unknown{66 + i * 9}";
                var customizeIdx =
                    (uint?) row.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row)
                 ?? uint.MaxValue;
                if (customizeIdx == uint.MaxValue)
                    continue;

                var hairRow = _customizeSheet.GetRow(customizeIdx);
                hairList.Add(hairRow != null
                    ? new Customization(CustomizationId.Hairstyle, hairRow.FeatureID, hairRow.Icon, (ushort) hairRow.RowId)
                    : new Customization(CustomizationId.Hairstyle, (byte) i,          customizeIdx, 0));
            }

            return hairList.ToArray();
        }

        private Customization[] CreateColorPicker(CustomizationId id, int offset, int num, bool light = false)
            => _cmpFile.RgbaColors.Skip(offset).Take(num)
                .Select((c, i) => new Customization(id, (byte) (light ? 128 + i : 0 + i), c, (ushort) (offset + i)))
                .ToArray();

        private (Customization[], Customization[]) GetColors(SubRace race, Gender gender)
        {
            var (skinOffset, hairOffset) = race switch
            {
                SubRace.Midlander       => gender == Gender.Male ? (0x1200, 0x1300) : (0x0D00, 0x0E00),
                SubRace.Highlander      => gender == Gender.Male ? (0x1C00, 0x1D00) : (0x1700, 0x1800),
                SubRace.Wildwood        => gender == Gender.Male ? (0x2600, 0x2700) : (0x2100, 0x2200),
                SubRace.Duskwright      => gender == Gender.Male ? (0x3000, 0x3100) : (0x2B00, 0x2C00),
                SubRace.Plainsfolk      => gender == Gender.Male ? (0x3A00, 0x3B00) : (0x3500, 0x3600),
                SubRace.Dunesfolk       => gender == Gender.Male ? (0x4400, 0x4500) : (0x3F00, 0x4000),
                SubRace.SeekerOfTheSun  => gender == Gender.Male ? (0x4E00, 0x4F00) : (0x4900, 0x4A00),
                SubRace.KeeperOfTheMoon => gender == Gender.Male ? (0x5800, 0x5900) : (0x5300, 0x5400),
                SubRace.Seawolf         => gender == Gender.Male ? (0x6200, 0x6300) : (0x5D00, 0x5E00),
                SubRace.Hellsguard      => gender == Gender.Male ? (0x6C00, 0x6D00) : (0x6700, 0x6800),
                SubRace.Raen            => gender == Gender.Male ? (0x7100, 0x7700) : (0x7600, 0x7200),
                SubRace.Xaela           => gender == Gender.Male ? (0x7B00, 0x8100) : (0x8000, 0x7C00),
                SubRace.Hellion         => gender == Gender.Male ? (0x8500, 0x8600) : (0x0000, 0x0000),
                SubRace.Lost            => gender == Gender.Male ? (0x8C00, 0x8F00) : (0x0000, 0x0000),
                SubRace.Rava            => gender == Gender.Male ? (0x0000, 0x0000) : (0x9E00, 0x9F00),
                SubRace.Veena           => gender == Gender.Male ? (0x0000, 0x0000) : (0xA800, 0xA900),
                _                       => throw new ArgumentOutOfRangeException(nameof(race), race, null),
            };

            return (CreateColorPicker(CustomizationId.SkinColor, skinOffset, 192),
                CreateColorPicker(CustomizationId.HairColor,     hairOffset, 192));
        }

        private Customization FromValueAndIndex(CustomizationId id, uint value, int index)
        {
            var row = _customizeSheet.GetRow(value);
            return row == null
                ? new Customization(id, (byte) (index + 1),  value,    0)
                : new Customization(id, row.FeatureID, row.Icon, (ushort) row.RowId);
        }

        private int GetListSize(CharaMakeParams row, CustomizationId id)
        {
            var menu = row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == id);
            return menu?.Size ?? 0;
        }

        private Customization[] GetFacePaints(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.FacePaint)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.FacePaint, v, i)).ToArray()
             ?? Array.Empty<Customization>();

        private Customization[] GetTailEarShapes(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.TailEarShape)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.TailEarShape, v, i)).ToArray()
             ?? Array.Empty<Customization>();

        private Customization[] GetFaces(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.Face)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.Face, v, i)).ToArray()
             ?? Array.Empty<Customization>();

        private Customization[] HrothgarFurPattern(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.LipColor)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.LipColor, v, i)).ToArray()
             ?? Array.Empty<Customization>();

        private Customization[] HrothgarFaces(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.Hairstyle)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.Hairstyle, v, i)).ToArray()
             ?? Array.Empty<Customization>();

        private CustomizationSet GetSet(SubRace race, Gender gender)
        {
            var (skin, hair) = GetColors(race, gender);
            var row = _listSheet.GetRow(((uint) race - 1) * 2 - 1 + (uint) gender);
            var set = new CustomizationSet(race, gender)
            {
                HairStyles           = race.ToRace() == Race.Hrothgar ? HrothgarFaces(row) : GetHairStyles(race, gender),
                HairColors           = hair,
                SkinColors           = skin,
                EyeColors            = _eyeColorPicker,
                HighlightColors      = _highlightPicker,
                TattooColors         = _tattooColorPicker,
                LipColorsDark        = race.ToRace() == Race.Hrothgar ? HrothgarFurPattern(row) : _lipColorPickerDark,
                LipColorsLight       = race.ToRace() == Race.Hrothgar ? Array.Empty<Customization>() : _lipColorPickerLight,
                FacePaintColorsDark  = _facePaintColorPickerDark,
                FacePaintColorsLight = _facePaintColorPickerLight,
                Faces                = GetFaces(row),
                NumEyebrows          = GetListSize(row, CustomizationId.Eyebrows),
                NumEyeShapes         = GetListSize(row, CustomizationId.EyeShape),
                NumNoseShapes        = GetListSize(row, CustomizationId.Nose),
                NumJawShapes         = GetListSize(row, CustomizationId.Jaw),
                NumMouthShapes       = GetListSize(row, CustomizationId.Mouth),
                FacePaints           = GetFacePaints(row),
                TailEarShapes        = GetTailEarShapes(row),
            };

            if (GetListSize(row, CustomizationId.BustSize) > 0)
                set.SetAvailable(CustomizationId.BustSize);
            if (GetListSize(row, CustomizationId.MuscleToneOrTailEarLength) > 0)
                set.SetAvailable(CustomizationId.MuscleToneOrTailEarLength);

            if (set.NumEyebrows > 0)
                set.SetAvailable(CustomizationId.Eyebrows);
            if (set.NumEyeShapes > 0)
                set.SetAvailable(CustomizationId.EyeShape);
            if (set.NumNoseShapes > 0)
                set.SetAvailable(CustomizationId.Nose);
            if (set.NumJawShapes > 0)
                set.SetAvailable(CustomizationId.Jaw);
            if (set.NumMouthShapes > 0)
                set.SetAvailable(CustomizationId.Mouth);
            if (set.FacePaints.Count > 0)
            {
                set.SetAvailable(CustomizationId.FacePaint);
                set.SetAvailable(CustomizationId.FacePaintColor);
            }
            if (set.TailEarShapes.Count > 0)
                set.SetAvailable(CustomizationId.TailEarShape);
            if (set.Faces.Count > 0)
                set.SetAvailable(CustomizationId.Face);

            var count       = race.ToRace() == Race.Hrothgar ? set.HairStyles.Count : set.Faces.Count;
            var featureDict = new List<IReadOnlyList<Customization>>(count);
            for (var i = 0; i < count; ++i)
            {
                featureDict.Add(row.FacialFeatureByFace[i].Icons.Select((val, idx)
                        => new Customization(CustomizationId.FacialFeaturesTattoos, (byte) (1 << idx), val, (ushort) (i * 8 + idx)))
                    .Append(new Customization(CustomizationId.FacialFeaturesTattoos, 1 << 7, 137905, (ushort) ((i + 1) * 8)))
                    .ToArray());
            }

            set.FeaturesTattoos = featureDict;

            return set;
        }

        private readonly ExcelSheet<CharaMakeCustomize> _customizeSheet;
        private readonly ExcelSheet<CharaMakeParams>    _listSheet;
        private readonly ExcelSheet<HairMakeType>       _hairSheet;
        private readonly CmpFile                        _cmpFile;
        private readonly Customization[]                _highlightPicker;
        private readonly Customization[]                _eyeColorPicker;
        private readonly Customization[]                _facePaintColorPickerDark;
        private readonly Customization[]                _facePaintColorPickerLight;
        private readonly Customization[]                _lipColorPickerDark;
        private readonly Customization[]                _lipColorPickerLight;
        private readonly Customization[]                _tattooColorPicker;

        private static Language FromClientLanguage(ClientLanguage language)
            => language switch
            {
                ClientLanguage.English  => Language.English,
                ClientLanguage.French   => Language.French,
                ClientLanguage.German   => Language.German,
                ClientLanguage.Japanese => Language.Japanese,
                _                       => Language.English,
            };

        internal CustomizationOptions(DalamudPluginInterface pi)
        {
            _cmpFile        = new CmpFile(pi);
            _customizeSheet = pi.Data.GetExcelSheet<CharaMakeCustomize>();
            var tmp = pi.Data.Excel.GetType()!.GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(CharaMakeParams))!.Invoke(pi.Data.Excel, new object?[]
            {
                "charamaketype",
                FromClientLanguage(pi.ClientState.ClientLanguage),
                null,
            }) as ExcelSheet<CharaMakeParams>;
            _listSheet = tmp!;
            _hairSheet = pi.Data.GetExcelSheet<HairMakeType>();

            _highlightPicker           = CreateColorPicker(CustomizationId.HighlightColor, 256,  192);
            _lipColorPickerDark        = CreateColorPicker(CustomizationId.LipColor,       512,  96);
            _lipColorPickerLight       = CreateColorPicker(CustomizationId.LipColor,       1024, 96, true);
            _eyeColorPicker            = CreateColorPicker(CustomizationId.EyeColorL,      0,    192);
            _facePaintColorPickerDark  = CreateColorPicker(CustomizationId.FacePaintColor, 640,  96);
            _facePaintColorPickerLight = CreateColorPicker(CustomizationId.FacePaintColor, 1152, 96, true);
            _tattooColorPicker         = CreateColorPicker(CustomizationId.TattooColor,    0,    192);

            _icons = new IconStorage(pi, _list.Length * 50);
            foreach (var race in Clans)
            {
                foreach (var gender in Genders)
                    _list[ToIndex(race, gender)] = GetSet(race, gender);
            }
        }
    }
}
