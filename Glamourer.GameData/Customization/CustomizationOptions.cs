using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Customization;

// Generate everything about customization per tribe and gender.
public partial class CustomizationOptions
{
    // All races except for Unknown
    internal static readonly Race[] Races = ((Race[])Enum.GetValues(typeof(Race))).Skip(1).ToArray();

    // All tribes except for Unknown
    internal static readonly SubRace[] Clans = ((SubRace[])Enum.GetValues(typeof(SubRace))).Skip(1).ToArray();

    // Two genders.
    internal static readonly Gender[] Genders =
    {
        Gender.Male,
        Gender.Female,
    };

    // Every tribe and gender has a separate set of available customizations.
    internal CustomizationSet GetList(SubRace race, Gender gender)
        => _customizationSets[ToIndex(race, gender)];

    // Get specific icons.
    internal ImGuiScene.TextureWrap GetIcon(uint id)
        => _icons.LoadIcon(id);

    private readonly IconStorage _icons;

    private static readonly int                ListSize           = Clans.Length * Genders.Length;
    private readonly        CustomizationSet[] _customizationSets = new CustomizationSet[ListSize];


    // Get the index for the given pair of tribe and gender.
    private static int ToIndex(SubRace race, Gender gender)
    {
        var idx = ((int)race - 1) * Genders.Length + (gender == Gender.Female ? 1 : 0);
        if (idx < 0 || idx >= ListSize)
            ThrowException(race, gender);
        return idx;
    }

    private static void ThrowException(SubRace race, Gender gender)
        => throw new Exception($"Invalid customization requested for {race} {gender}.");
}

public partial class CustomizationOptions
{
    internal readonly bool Valid;

    public string GetName(CustomName name)
        => _names[(int)name];

    internal CustomizationOptions(DalamudPluginInterface pi, DataManager gameData, ClientLanguage language)
    {
        var tmp = new TemporaryData(gameData, this, language);
        _icons = new IconStorage(pi, gameData, _customizationSets.Length * 50);
        Valid  = tmp.Valid;
        SetNames(gameData, tmp);
        foreach (var race in Clans)
        {
            foreach (var gender in Genders)
                _customizationSets[ToIndex(race, gender)] = tmp.GetSet(race, gender);
        }
    }


    // Obtain localized names of customization options and race names from the game data.
    private readonly string[] _names = new string[Enum.GetValues<CustomName>().Length];

    private void SetNames(DataManager gameData, TemporaryData tmp)
    {
        var subRace = gameData.GetExcelSheet<Tribe>()!;

        void Set(CustomName id, Lumina.Text.SeString? s, string def)
            => _names[(int)id] = s?.ToDalamudString().TextValue ?? def;

        Set(CustomName.Clan,             tmp.Lobby.GetRow(102)?.Text,                             "Clan");
        Set(CustomName.Gender,           tmp.Lobby.GetRow(103)?.Text,                             "Gender");
        Set(CustomName.Reverse,          tmp.Lobby.GetRow(2135)?.Text,                            "Reverse");
        Set(CustomName.OddEyes,          tmp.Lobby.GetRow(2125)?.Text,                            "Odd Eyes");
        Set(CustomName.IrisSmall,        tmp.Lobby.GetRow(1076)?.Text,                            "Small");
        Set(CustomName.IrisLarge,        tmp.Lobby.GetRow(1075)?.Text,                            "Large");
        Set(CustomName.IrisSize,         tmp.Lobby.GetRow(244)?.Text,                             "Iris Size");
        Set(CustomName.MidlanderM,       subRace.GetRow((int)SubRace.Midlander)?.Masculine,       SubRace.Midlander.ToName());
        Set(CustomName.MidlanderF,       subRace.GetRow((int)SubRace.Midlander)?.Feminine,        SubRace.Midlander.ToName());
        Set(CustomName.HighlanderM,      subRace.GetRow((int)SubRace.Highlander)?.Masculine,      SubRace.Highlander.ToName());
        Set(CustomName.HighlanderF,      subRace.GetRow((int)SubRace.Highlander)?.Feminine,       SubRace.Highlander.ToName());
        Set(CustomName.WildwoodM,        subRace.GetRow((int)SubRace.Wildwood)?.Masculine,        SubRace.Wildwood.ToName());
        Set(CustomName.WildwoodF,        subRace.GetRow((int)SubRace.Wildwood)?.Feminine,         SubRace.Wildwood.ToName());
        Set(CustomName.DuskwightM,       subRace.GetRow((int)SubRace.Duskwight)?.Masculine,       SubRace.Duskwight.ToName());
        Set(CustomName.DuskwightF,       subRace.GetRow((int)SubRace.Duskwight)?.Feminine,        SubRace.Duskwight.ToName());
        Set(CustomName.PlainsfolkM,      subRace.GetRow((int)SubRace.Plainsfolk)?.Masculine,      SubRace.Plainsfolk.ToName());
        Set(CustomName.PlainsfolkF,      subRace.GetRow((int)SubRace.Plainsfolk)?.Feminine,       SubRace.Plainsfolk.ToName());
        Set(CustomName.DunesfolkM,       subRace.GetRow((int)SubRace.Dunesfolk)?.Masculine,       SubRace.Dunesfolk.ToName());
        Set(CustomName.DunesfolkF,       subRace.GetRow((int)SubRace.Dunesfolk)?.Feminine,        SubRace.Dunesfolk.ToName());
        Set(CustomName.SeekerOfTheSunM,  subRace.GetRow((int)SubRace.SeekerOfTheSun)?.Masculine,  SubRace.SeekerOfTheSun.ToName());
        Set(CustomName.SeekerOfTheSunF,  subRace.GetRow((int)SubRace.SeekerOfTheSun)?.Feminine,   SubRace.SeekerOfTheSun.ToName());
        Set(CustomName.KeeperOfTheMoonM, subRace.GetRow((int)SubRace.KeeperOfTheMoon)?.Masculine, SubRace.KeeperOfTheMoon.ToName());
        Set(CustomName.KeeperOfTheMoonF, subRace.GetRow((int)SubRace.KeeperOfTheMoon)?.Feminine,  SubRace.KeeperOfTheMoon.ToName());
        Set(CustomName.SeawolfM,         subRace.GetRow((int)SubRace.Seawolf)?.Masculine,         SubRace.Seawolf.ToName());
        Set(CustomName.SeawolfF,         subRace.GetRow((int)SubRace.Seawolf)?.Feminine,          SubRace.Seawolf.ToName());
        Set(CustomName.HellsguardM,      subRace.GetRow((int)SubRace.Hellsguard)?.Masculine,      SubRace.Hellsguard.ToName());
        Set(CustomName.HellsguardF,      subRace.GetRow((int)SubRace.Hellsguard)?.Feminine,       SubRace.Hellsguard.ToName());
        Set(CustomName.RaenM,            subRace.GetRow((int)SubRace.Raen)?.Masculine,            SubRace.Raen.ToName());
        Set(CustomName.RaenF,            subRace.GetRow((int)SubRace.Raen)?.Feminine,             SubRace.Raen.ToName());
        Set(CustomName.XaelaM,           subRace.GetRow((int)SubRace.Xaela)?.Masculine,           SubRace.Xaela.ToName());
        Set(CustomName.XaelaF,           subRace.GetRow((int)SubRace.Xaela)?.Feminine,            SubRace.Xaela.ToName());
        Set(CustomName.HelionM,          subRace.GetRow((int)SubRace.Helion)?.Masculine,          SubRace.Helion.ToName());
        Set(CustomName.HelionF,          subRace.GetRow((int)SubRace.Helion)?.Feminine,           SubRace.Helion.ToName());
        Set(CustomName.LostM,            subRace.GetRow((int)SubRace.Lost)?.Masculine,            SubRace.Lost.ToName());
        Set(CustomName.LostF,            subRace.GetRow((int)SubRace.Lost)?.Feminine,             SubRace.Lost.ToName());
        Set(CustomName.RavaM,            subRace.GetRow((int)SubRace.Rava)?.Masculine,            SubRace.Rava.ToName());
        Set(CustomName.RavaF,            subRace.GetRow((int)SubRace.Rava)?.Feminine,             SubRace.Rava.ToName());
        Set(CustomName.VeenaM,           subRace.GetRow((int)SubRace.Veena)?.Masculine,           SubRace.Veena.ToName());
        Set(CustomName.VeenaF,           subRace.GetRow((int)SubRace.Veena)?.Feminine,            SubRace.Veena.ToName());
    }

    private class TemporaryData
    {
        public bool Valid
            => _cmpFile.Valid;

        public CustomizationSet GetSet(SubRace race, Gender gender)
        {
            var (skin, hair) = GetColors(race, gender);
            var row      = _listSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            var hrothgar = race.ToRace() == Race.Hrothgar;
            // Create the initial set with all the easily accessible parameters available for anyone.
            var set = new CustomizationSet(race, gender)
            {
                HairStyles           = GetHairStyles(race, gender),
                HairColors           = hair,
                SkinColors           = skin,
                EyeColors            = _eyeColorPicker,
                HighlightColors      = _highlightPicker,
                TattooColors         = _tattooColorPicker,
                LipColorsDark        = hrothgar ? HrothgarFurPattern(row) : _lipColorPickerDark,
                LipColorsLight       = hrothgar ? Array.Empty<CustomizationData>() : _lipColorPickerLight,
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

            SetAvailability(set, row);
            SetFacialFeatures(set, row);
            SetHairByFace(set);
            SetMenuTypes(set, row);
            SetNames(set, row);

            return set;
        }

        public TemporaryData(DataManager gameData, CustomizationOptions options, ClientLanguage language)
        {
            _options        = options;
            _cmpFile        = new CmpFile(gameData);
            _customizeSheet = gameData.GetExcelSheet<CharaMakeCustomize>()!;
            Lobby           = gameData.GetExcelSheet<Lobby>()!;
            var tmp = gameData.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)?
                .MakeGenericMethod(typeof(CharaMakeParams)).Invoke(gameData.Excel, new object?[]
                {
                    "charamaketype",
                    language.ToLumina(),
                    null,
                }) as ExcelSheet<CharaMakeParams>;
            _listSheet                 = tmp!;
            _hairSheet                 = gameData.GetExcelSheet<HairMakeType>()!;
            _highlightPicker           = CreateColorPicker(CustomizationId.HighlightColor, 256,  192);
            _lipColorPickerDark        = CreateColorPicker(CustomizationId.LipColor,       512,  96);
            _lipColorPickerLight       = CreateColorPicker(CustomizationId.LipColor,       1024, 96, true);
            _eyeColorPicker            = CreateColorPicker(CustomizationId.EyeColorL,      0,    192);
            _facePaintColorPickerDark  = CreateColorPicker(CustomizationId.FacePaintColor, 640,  96);
            _facePaintColorPickerLight = CreateColorPicker(CustomizationId.FacePaintColor, 1152, 96, true);
            _tattooColorPicker         = CreateColorPicker(CustomizationId.TattooColor,    0,    192);
        }

        // Required sheets.
        private readonly ExcelSheet<CharaMakeCustomize> _customizeSheet;
        private readonly ExcelSheet<CharaMakeParams>    _listSheet;
        private readonly ExcelSheet<HairMakeType>       _hairSheet;
        public readonly  ExcelSheet<Lobby>              Lobby;
        private readonly CmpFile                        _cmpFile;

        // Those values are shared between all races.
        private readonly CustomizationData[] _highlightPicker;
        private readonly CustomizationData[] _eyeColorPicker;
        private readonly CustomizationData[] _facePaintColorPickerDark;
        private readonly CustomizationData[] _facePaintColorPickerLight;
        private readonly CustomizationData[] _lipColorPickerDark;
        private readonly CustomizationData[] _lipColorPickerLight;
        private readonly CustomizationData[] _tattooColorPicker;

        private readonly CustomizationOptions _options;

        private CustomizationData[] CreateColorPicker(CustomizationId id, int offset, int num, bool light = false)
            => _cmpFile.GetSlice(offset, num)
                .Select((c, i) => new CustomizationData(id, (CustomizationByteValue)(light ? 128 + i : 0 + i), c, (ushort)(offset + i)))
                .ToArray();


        private void SetHairByFace(CustomizationSet set)
        {
            if (set.Race != Race.Hrothgar)
            {
                set.HairByFace = Enumerable.Repeat(set.HairStyles, set.Faces.Count + 1).ToArray();
                return;
            }

            var tmp = new IReadOnlyList<CustomizationData>[set.Faces.Count + 1];
            tmp[0] = set.HairStyles;

            for (var i = 1; i <= set.Faces.Count; ++i)
            {
                bool Valid(CustomizationData c)
                {
                    var data = _customizeSheet.GetRow(c.CustomizeId)?.Unknown6 ?? 0;
                    return data == 0 || data == i + set.Faces.Count;
                }

                tmp[i] = set.HairStyles.Where(Valid).ToArray();
            }

            set.HairByFace = tmp;
        }

        private static void SetMenuTypes(CustomizationSet set, CharaMakeParams row)
        {
            // Set up the menu types for all customizations.
            set.Types = ((CustomizationId[])Enum.GetValues(typeof(CustomizationId))).Select(c =>
            {
                // Those types are not correctly given in the menu, so special case them to color pickers.
                switch (c)
                {
                    case CustomizationId.HighlightColor:
                    case CustomizationId.EyeColorL:
                    case CustomizationId.EyeColorR:
                        return CharaMakeParams.MenuType.ColorPicker;
                }

                // Otherwise find the first menu corresponding to the id.
                // If there is none, assume a list.
                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customization == c);
                return menu?.Type ?? CharaMakeParams.MenuType.ListSelector;
            }).ToArray();
            set.Order = CustomizationSet.ComputeOrder(set);
        }

        // Set customizations available if they have any options.
        private static void SetAvailability(CustomizationSet set, CharaMakeParams row)
        {
            void Set(bool available, CustomizationId flag)
            {
                if (available)
                    set.SetAvailable(flag);
            }

            // Both are percentages that are either unavailable or 0-100.
            Set(GetListSize(row, CustomizationId.BustSize) > 0,                  CustomizationId.BustSize);
            Set(GetListSize(row, CustomizationId.MuscleToneOrTailEarLength) > 0, CustomizationId.MuscleToneOrTailEarLength);
            Set(set.NumEyebrows > 0,                                             CustomizationId.Eyebrows);
            Set(set.NumEyeShapes > 0,                                            CustomizationId.EyeShape);
            Set(set.NumNoseShapes > 0,                                           CustomizationId.Nose);
            Set(set.NumJawShapes > 0,                                            CustomizationId.Jaw);
            Set(set.NumMouthShapes > 0,                                          CustomizationId.Mouth);
            Set(set.TailEarShapes.Count > 0,                                     CustomizationId.TailEarShape);
            Set(set.Faces.Count > 0,                                             CustomizationId.Face);
            Set(set.FacePaints.Count > 0,                                        CustomizationId.FacePaint);
            Set(set.FacePaints.Count > 0,                                        CustomizationId.FacePaintColor);
        }

        // Create a list of lists of facial features and the legacy tattoo.
        private static void SetFacialFeatures(CustomizationSet set, CharaMakeParams row)
        {
            var count       = set.Faces.Count;
            var featureDict = new List<IReadOnlyList<CustomizationData>>(count);

            for (var i = 0; i < count; ++i)
            {
                var legacyTattoo = new CustomizationData(CustomizationId.FacialFeaturesTattoos, (CustomizationByteValue)(1 << 7), 137905,
                    (ushort)((i + 1) * 8));
                featureDict.Add(row.FacialFeatureByFace[i].Icons.Select((val, idx)
                        => new CustomizationData(CustomizationId.FacialFeaturesTattoos, (CustomizationByteValue)(1 << idx), val,
                            (ushort)(i * 8 + idx)))
                    .Append(legacyTattoo)
                    .ToArray());
            }

            set.FeaturesTattoos = featureDict.ToArray();
        }

        // Set the names for the given set of parameters.
        private void SetNames(CustomizationSet set, CharaMakeParams row)
        {
            var nameArray = ((CustomizationId[])Enum.GetValues(typeof(CustomizationId))).Select(c =>
            {
                // Find the first menu that corresponds to the Id.
                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customization == c);
                if (menu == null)
                {
                    // If none exists and the id corresponds to highlights, set the Highlights name.
                    if (c == CustomizationId.HighlightsOnFlag)
                        return Lobby.GetRow(237)?.Text.ToDalamudString().ToString() ?? "Highlights";

                    // Otherwise there is an error and we use the default name.
                    return c.ToDefaultName();
                }

                // Facial Features and Tattoos is created by combining two strings.
                if (c == CustomizationId.FacialFeaturesTattoos)
                    return
                        $"{Lobby.GetRow(1741)?.Text.ToDalamudString().ToString() ?? "Facial Features"} & {Lobby.GetRow(1742)?.Text.ToDalamudString().ToString() ?? "Tattoos"}";

                // Otherwise all is normal, get the menu name or if it does not work the default name.
                var textRow = Lobby.GetRow(menu.Value.Id);
                return textRow?.Text.ToDalamudString().ToString() ?? c.ToDefaultName();
            }).ToArray();

            // Add names for both eye colors.
            nameArray[(int)CustomizationId.EyeColorL] = nameArray[(int)CustomizationId.EyeColorR];
            nameArray[(int)CustomizationId.EyeColorR] = _options.GetName(CustomName.OddEyes);

            set.OptionName = nameArray;
        }

        // Obtain available skin and hair colors for the given subrace and gender.
        private (CustomizationData[], CustomizationData[]) GetColors(SubRace race, Gender gender)
        {
            if (race is > SubRace.Veena or SubRace.Unknown)
                throw new ArgumentOutOfRangeException(nameof(race), race, null);

            var gv  = gender == Gender.Male ? 0 : 1;
            var idx = ((int)race * 2 + gv) * 5 + 3;

            return (CreateColorPicker(CustomizationId.SkinColor, idx << 8,       192),
                CreateColorPicker(CustomizationId.HairColor,     (idx + 1) << 8, 192));
        }

        // Obtain available hairstyles via reflection from the Hair sheet for the given subrace and gender.
        private CustomizationData[] GetHairStyles(SubRace race, Gender gender)
        {
            var row = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            // Unknown30 is the number of available hairstyles.
            var hairList = new List<CustomizationData>(row.Unknown30);
            // Hairstyles can be found starting at Unknown66.
            for (var i = 0; i < row.Unknown30; ++i)
            {
                var name = $"Unknown{66 + i * 9}";
                var customizeIdx = (uint?)row.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row)
                 ?? uint.MaxValue;
                if (customizeIdx == uint.MaxValue)
                    continue;

                // Hair Row from CustomizeSheet might not be set in case of unlockable hair.
                var hairRow = _customizeSheet.GetRow(customizeIdx);
                hairList.Add(hairRow != null
                    ? new CustomizationData(CustomizationId.Hairstyle, (CustomizationByteValue)hairRow.FeatureID, hairRow.Icon,
                        (ushort)hairRow.RowId)
                    : new CustomizationData(CustomizationId.Hairstyle, (CustomizationByteValue)i, customizeIdx));
            }

            return hairList.ToArray();
        }

        // Get Features.
        private CustomizationData FromValueAndIndex(CustomizationId id, uint value, int index)
        {
            var row = _customizeSheet.GetRow(value);
            return row == null
                ? new CustomizationData(id, (CustomizationByteValue)(index + 1),   value)
                : new CustomizationData(id, (CustomizationByteValue)row.FeatureID, row.Icon, (ushort)row.RowId);
        }

        // Get List sizes.
        private static int GetListSize(CharaMakeParams row, CustomizationId id)
        {
            var menu = row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == id);
            return menu?.Size ?? 0;
        }

        // Get face paints from the hair sheet via reflection.
        private CustomizationData[] GetFacePaints(SubRace race, Gender gender)
        {
            var row       = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            var paintList = new List<CustomizationData>(row.Unknown37);

            // Number of available face paints is at Unknown37.
            for (var i = 0; i < row.Unknown37; ++i)
            {
                // Face paints start at Unknown73.
                var name = $"Unknown{73 + i * 9}";
                var customizeIdx =
                    (uint?)row.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row)
                 ?? uint.MaxValue;
                if (customizeIdx == uint.MaxValue)
                    continue;

                var paintRow = _customizeSheet.GetRow(customizeIdx);
                // Facepaint Row from CustomizeSheet might not be set in case of unlockable facepaints.
                paintList.Add(paintRow != null
                    ? new CustomizationData(CustomizationId.FacePaint, (CustomizationByteValue)paintRow.FeatureID, paintRow.Icon,
                        (ushort)paintRow.RowId)
                    : new CustomizationData(CustomizationId.FacePaint, (CustomizationByteValue)i, customizeIdx));
            }

            return paintList.ToArray();
        }

        // Specific icons for tails or ears.
        private CustomizationData[] GetTailEarShapes(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.TailEarShape)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.TailEarShape, v, i)).ToArray()
             ?? Array.Empty<CustomizationData>();

        // Specific icons for faces.
        private CustomizationData[] GetFaces(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.Face)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.Face, v, i)).ToArray()
             ?? Array.Empty<CustomizationData>();

        // Specific icons for Hrothgar patterns.
        private CustomizationData[] HrothgarFurPattern(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customization == CustomizationId.LipColor)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizationId.LipColor, v, i)).ToArray()
             ?? Array.Empty<CustomizationData>();
    }
}
