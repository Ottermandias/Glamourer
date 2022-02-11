using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud;
using Dalamud.Data;
using Dalamud.Plugin;
using Glamourer.Util;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Penumbra.GameData.Enums;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Customization
{
    public partial class CustomizationOptions
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
            var row      = _hairSheet.GetRow(((uint) race - 1) * 2 - 1 + (uint) gender)!;
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
            if (race > SubRace.Veena || race == SubRace.Unknown)
                throw new ArgumentOutOfRangeException(nameof(race), race, null);

            var gv  = gender == Gender.Male ? 0 : 1;
            var idx = ((int) race * 2 + gv) * 5 + 3;

            return (CreateColorPicker(CustomizationId.SkinColor, idx << 8,       192),
                CreateColorPicker(CustomizationId.HairColor,     (idx + 1) << 8, 192));
        }

        private Customization FromValueAndIndex(CustomizationId id, uint value, int index)
        {
            var row = _customizeSheet.GetRow(value);
            return row == null
                ? new Customization(id, (byte) (index + 1), value,    0)
                : new Customization(id, row.FeatureID,      row.Icon, (ushort) row.RowId);
        }

        private static int GetListSize(CharaMakeParams row, CustomizationId id)
        {
            var menu = row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == id);
            return menu?.Size ?? 0;
        }

        private Customization[] GetFacePaints(SubRace race, Gender gender)
        {
            var row      = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            var paintList = new List<Customization>(row.Unknown37);
            for (var i = 0; i < row.Unknown37; ++i)
            {
                var name = $"Unknown{73 + i * 9}";
                var customizeIdx =
                    (uint?)row.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row)
                 ?? uint.MaxValue;
                if (customizeIdx == uint.MaxValue)
                    continue;

                var paintRow = _customizeSheet.GetRow(customizeIdx);
                paintList.Add(paintRow != null
                    ? new Customization(CustomizationId.FacePaint, paintRow.FeatureID, paintRow.Icon, (ushort)paintRow.RowId)
                    : new Customization(CustomizationId.FacePaint, (byte)i,           customizeIdx, 0));
            }

            return paintList.ToArray();
        }

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
            var row = _listSheet.GetRow(((uint) race - 1) * 2 - 1 + (uint) gender)!;
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
                FacePaints           = GetFacePaints(race, gender),
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

            var nameArray = ((CustomizationId[]) Enum.GetValues(typeof(CustomizationId))).Select(c =>
            {
                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customization == c);
                if (menu == null)
                {
                    if (c == CustomizationId.HighlightsOnFlag)
                        return _lobby.GetRow(237)?.Text.ToString() ?? "Highlights";

                    return c.ToDefaultName();
                }

                if (c == CustomizationId.FacialFeaturesTattoos)
                    return
                        $"{_lobby.GetRow(1741)?.Text.ToString() ?? "Facial Features"} & {_lobby.GetRow(1742)?.Text.ToString() ?? "Tattoos"}";

                var textRow = _lobby.GetRow(menu.Value.Id);
                return textRow?.Text.ToString() ?? c.ToDefaultName();
            }).ToArray();
            nameArray[(int) CustomizationId.EyeColorL] = nameArray[(int) CustomizationId.EyeColorR];
            nameArray[(int) CustomizationId.EyeColorR] = GetName(CustomName.OddEyes);
            set.OptionName                             = nameArray;

            set.Types = ((CustomizationId[]) Enum.GetValues(typeof(CustomizationId))).Select(c =>
            {
                switch (c)
                {
                    case CustomizationId.HighlightColor:
                    case CustomizationId.EyeColorL:
                    case CustomizationId.EyeColorR:
                        return CharaMakeParams.MenuType.ColorPicker;
                }

                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customization == c);
                return menu?.Type ?? CharaMakeParams.MenuType.ListSelector;
            }).ToArray();

            return set;
        }

        private readonly ExcelSheet<CharaMakeCustomize> _customizeSheet;
        private readonly ExcelSheet<CharaMakeParams>    _listSheet;
        private readonly ExcelSheet<HairMakeType>       _hairSheet;
        private readonly ExcelSheet<Lobby>              _lobby;
        private readonly CmpFile                        _cmpFile;
        private readonly Customization[]                _highlightPicker;
        private readonly Customization[]                _eyeColorPicker;
        private readonly Customization[]                _facePaintColorPickerDark;
        private readonly Customization[]                _facePaintColorPickerLight;
        private readonly Customization[]                _lipColorPickerDark;
        private readonly Customization[]                _lipColorPickerLight;
        private readonly Customization[]                _tattooColorPicker;
        private readonly string[]                       _names = new string[(int) CustomName.Num];

        public string GetName(CustomName name)
            => _names[(int) name];

        private static Language FromClientLanguage(ClientLanguage language)
            => language switch
            {
                ClientLanguage.English  => Language.English,
                ClientLanguage.French   => Language.French,
                ClientLanguage.German   => Language.German,
                ClientLanguage.Japanese => Language.Japanese,
                _                       => Language.English,
            };

        internal CustomizationOptions(DalamudPluginInterface pi, DataManager gameData, ClientLanguage language)
        {
            try
            {
                _cmpFile = new CmpFile(gameData);
            }
            catch (Exception e)
            {
                throw new Exception("READ THIS\n======== Could not obtain the human.cmp file which is necessary for color sets.\n"
                  + "======== This usually indicates an error with your index files caused by TexTools modifications.\n"
                  + "======== If you have used TexTools before, you will probably need to start over in it to use Glamourer.", e);
            }

            _customizeSheet = gameData.GetExcelSheet<CharaMakeCustomize>()!;
                _lobby          = gameData.GetExcelSheet<Lobby>()!;
            var tmp = gameData.Excel.GetType()!.GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(CharaMakeParams))!.Invoke(gameData.Excel, new object?[]
            {
                "charamaketype",
                FromClientLanguage(language),
                null,
            }) as ExcelSheet<CharaMakeParams>;
            _listSheet = tmp!;
            _hairSheet = gameData.GetExcelSheet<HairMakeType>()!;
            SetNames(gameData);

            _highlightPicker           = CreateColorPicker(CustomizationId.HighlightColor, 256,  192);
            _lipColorPickerDark        = CreateColorPicker(CustomizationId.LipColor,       512,  96);
            _lipColorPickerLight       = CreateColorPicker(CustomizationId.LipColor,       1024, 96, true);
            _eyeColorPicker            = CreateColorPicker(CustomizationId.EyeColorL,      0,    192);
            _facePaintColorPickerDark  = CreateColorPicker(CustomizationId.FacePaintColor, 640,  96);
            _facePaintColorPickerLight = CreateColorPicker(CustomizationId.FacePaintColor, 1152, 96, true);
            _tattooColorPicker         = CreateColorPicker(CustomizationId.TattooColor,    0,    192);

            _icons = new IconStorage(pi, gameData, _list.Length * 50);
            foreach (var race in Clans)
            {
                foreach (var gender in Genders)
                    _list[ToIndex(race, gender)] = GetSet(race, gender);
            }
        }

        private void SetNames(DataManager gameData)
        {
            var subRace = gameData.GetExcelSheet<Tribe>()!;
            _names[(int) CustomName.Clan]       = _lobby.GetRow(102)?.Text ?? "Clan";
            _names[(int) CustomName.Gender]     = _lobby.GetRow(103)?.Text ?? "Gender";
            _names[(int) CustomName.Reverse]    = _lobby.GetRow(2135)?.Text ?? "Reverse";
            _names[(int) CustomName.OddEyes]    = _lobby.GetRow(2125)?.Text ?? "Odd Eyes";
            _names[(int) CustomName.IrisSmall]  = _lobby.GetRow(1076)?.Text ?? "Small";
            _names[(int) CustomName.IrisLarge]  = _lobby.GetRow(1075)?.Text ?? "Large";
            _names[(int) CustomName.IrisSize]   = _lobby.GetRow(244)?.Text ?? "Iris Size";
            _names[(int) CustomName.MidlanderM] = subRace.GetRow((int) SubRace.Midlander)?.Masculine.ToString() ?? SubRace.Midlander.ToName();
            _names[(int) CustomName.MidlanderF] = subRace.GetRow((int) SubRace.Midlander)?.Feminine.ToString() ?? SubRace.Midlander.ToName();
            _names[(int) CustomName.HighlanderM] =
                subRace.GetRow((int) SubRace.Highlander)?.Masculine.ToString() ?? SubRace.Highlander.ToName();
            _names[(int) CustomName.HighlanderF] = subRace.GetRow((int) SubRace.Highlander)?.Feminine.ToString() ?? SubRace.Highlander.ToName();
            _names[(int) CustomName.WildwoodM]   = subRace.GetRow((int) SubRace.Wildwood)?.Masculine.ToString() ?? SubRace.Wildwood.ToName();
            _names[(int) CustomName.WildwoodF]   = subRace.GetRow((int) SubRace.Wildwood)?.Feminine.ToString() ?? SubRace.Wildwood.ToName();
            _names[(int) CustomName.DuskwightM]  = subRace.GetRow((int) SubRace.Duskwight)?.Masculine.ToString() ?? SubRace.Duskwight.ToName();
            _names[(int) CustomName.DuskwightF]  = subRace.GetRow((int) SubRace.Duskwight)?.Feminine.ToString() ?? SubRace.Duskwight.ToName();
            _names[(int) CustomName.PlainsfolkM] =
                subRace.GetRow((int) SubRace.Plainsfolk)?.Masculine.ToString() ?? SubRace.Plainsfolk.ToName();
            _names[(int) CustomName.PlainsfolkF] = subRace.GetRow((int) SubRace.Plainsfolk)?.Feminine.ToString() ?? SubRace.Plainsfolk.ToName();
            _names[(int) CustomName.DunesfolkM]  = subRace.GetRow((int) SubRace.Dunesfolk)?.Masculine.ToString() ?? SubRace.Dunesfolk.ToName();
            _names[(int) CustomName.DunesfolkF]  = subRace.GetRow((int) SubRace.Dunesfolk)?.Feminine.ToString() ?? SubRace.Dunesfolk.ToName();
            _names[(int) CustomName.SeekerOfTheSunM] =
                subRace.GetRow((int) SubRace.SeekerOfTheSun)?.Masculine.ToString() ?? SubRace.SeekerOfTheSun.ToName();
            _names[(int) CustomName.SeekerOfTheSunF] =
                subRace.GetRow((int) SubRace.SeekerOfTheSun)?.Feminine.ToString() ?? SubRace.SeekerOfTheSun.ToName();
            _names[(int) CustomName.KeeperOfTheMoonM] =
                subRace.GetRow((int) SubRace.KeeperOfTheMoon)?.Masculine.ToString() ?? SubRace.KeeperOfTheMoon.ToName();
            _names[(int) CustomName.KeeperOfTheMoonF] =
                subRace.GetRow((int) SubRace.KeeperOfTheMoon)?.Feminine.ToString() ?? SubRace.KeeperOfTheMoon.ToName();
            _names[(int) CustomName.SeawolfM] = subRace.GetRow((int) SubRace.Seawolf)?.Masculine.ToString() ?? SubRace.Seawolf.ToName();
            _names[(int) CustomName.SeawolfF] = subRace.GetRow((int) SubRace.Seawolf)?.Feminine.ToString() ?? SubRace.Seawolf.ToName();
            _names[(int) CustomName.HellsguardM] =
                subRace.GetRow((int) SubRace.Hellsguard)?.Masculine.ToString() ?? SubRace.Hellsguard.ToName();
            _names[(int) CustomName.HellsguardF] = subRace.GetRow((int) SubRace.Hellsguard)?.Feminine.ToString() ?? SubRace.Hellsguard.ToName();
            _names[(int) CustomName.RaenM]       = subRace.GetRow((int) SubRace.Raen)?.Masculine.ToString() ?? SubRace.Raen.ToName();
            _names[(int) CustomName.RaenF]       = subRace.GetRow((int) SubRace.Raen)?.Feminine.ToString() ?? SubRace.Raen.ToName();
            _names[(int) CustomName.XaelaM]      = subRace.GetRow((int) SubRace.Xaela)?.Masculine.ToString() ?? SubRace.Xaela.ToName();
            _names[(int) CustomName.XaelaF]      = subRace.GetRow((int) SubRace.Xaela)?.Feminine.ToString() ?? SubRace.Xaela.ToName();
            _names[(int) CustomName.HelionM]     = subRace.GetRow((int) SubRace.Helion)?.Masculine.ToString() ?? SubRace.Helion.ToName();
            _names[(int) CustomName.HelionF]     = subRace.GetRow((int) SubRace.Helion)?.Feminine.ToString() ?? SubRace.Helion.ToName();
            _names[(int) CustomName.LostM]       = subRace.GetRow((int) SubRace.Lost)?.Masculine.ToString() ?? SubRace.Lost.ToName();
            _names[(int) CustomName.LostF]       = subRace.GetRow((int) SubRace.Lost)?.Feminine.ToString() ?? SubRace.Lost.ToName();
            _names[(int) CustomName.RavaM]       = subRace.GetRow((int) SubRace.Rava)?.Masculine.ToString() ?? SubRace.Rava.ToName();
            _names[(int) CustomName.RavaF]       = subRace.GetRow((int) SubRace.Rava)?.Feminine.ToString() ?? SubRace.Rava.ToName();
            _names[(int) CustomName.VeenaM]      = subRace.GetRow((int) SubRace.Veena)?.Masculine.ToString() ?? SubRace.Veena.ToName();
            _names[(int) CustomName.VeenaF]      = subRace.GetRow((int) SubRace.Veena)?.Feminine.ToString() ?? SubRace.Veena.ToName();
        }
    }
}
