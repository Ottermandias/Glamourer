using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud;
using Dalamud.Plugin.Services;
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
        => _icons.LoadIcon(id)!;

    private readonly IconStorage _icons;

    private static readonly int                ListSize           = Clans.Length * Genders.Length;
    private readonly        CustomizationSet[] _customizationSets = new CustomizationSet[ListSize];


    // Get the index for the given pair of tribe and gender.
    internal static int ToIndex(SubRace race, Gender gender)
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
    public string GetName(CustomName name)
        => _names[(int)name];

    internal CustomizationOptions(ITextureProvider textures, IDataManager gameData)
    {
        var tmp = new TemporaryData(gameData, this);
        _icons = new IconStorage(textures, gameData);
        SetNames(gameData, tmp);
        foreach (var race in Clans)
        {
            foreach (var gender in Genders)
                _customizationSets[ToIndex(race, gender)] = tmp.GetSet(race, gender);
        }
        tmp.SetNpcData(_customizationSets);
    }

    // Obtain localized names of customization options and race names from the game data.
    private readonly string[] _names = new string[Enum.GetValues<CustomName>().Length];

    private void SetNames(IDataManager gameData, TemporaryData tmp)
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
                Voices               = row.Voices,
                HairStyles           = GetHairStyles(race, gender),
                HairColors           = hair,
                SkinColors           = skin,
                EyeColors            = _eyeColorPicker,
                HighlightColors      = _highlightPicker,
                TattooColors         = _tattooColorPicker,
                LipColorsDark        = hrothgar ? HrothgarFurPattern(row) : _lipColorPickerDark,
                LipColorsLight       = hrothgar ? Array.Empty<CustomizeData>() : _lipColorPickerLight,
                FacePaintColorsDark  = _facePaintColorPickerDark,
                FacePaintColorsLight = _facePaintColorPickerLight,
                Faces                = GetFaces(row),
                NumEyebrows          = GetListSize(row, CustomizeIndex.Eyebrows),
                NumEyeShapes         = GetListSize(row, CustomizeIndex.EyeShape),
                NumNoseShapes        = GetListSize(row, CustomizeIndex.Nose),
                NumJawShapes         = GetListSize(row, CustomizeIndex.Jaw),
                NumMouthShapes       = GetListSize(row, CustomizeIndex.Mouth),
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

        public void SetNpcData(CustomizationSet[] sets)
        {
            var data = CustomizationNpcOptions.CreateNpcData(sets, _bnpcCustomize, _enpcBase);
            foreach (var set in sets)
            {
                if (data.TryGetValue((set.Clan, set.Gender), out var npcData))
                    set.NpcOptions = npcData.ToArray();
            }
        }


        public TemporaryData(IDataManager gameData, CustomizationOptions options)
        {
            _options        = options;
            _cmpFile        = new CmpFile(gameData);
            _customizeSheet = gameData.GetExcelSheet<CharaMakeCustomize>()!;
            _bnpcCustomize  = gameData.GetExcelSheet<BNpcCustomize>()!;
            _enpcBase       = gameData.GetExcelSheet<ENpcBase>()!;
            Lobby           = gameData.GetExcelSheet<Lobby>()!;
            var tmp = gameData.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.NonPublic)?
                .MakeGenericMethod(typeof(CharaMakeParams)).Invoke(gameData.Excel, new object?[]
                {
                    "charamaketype",
                    gameData.Language.ToLumina(),
                    null,
                }) as ExcelSheet<CharaMakeParams>;
            _listSheet                 = tmp!;
            _hairSheet                 = gameData.GetExcelSheet<HairMakeType>()!;
            _highlightPicker           = CreateColorPicker(CustomizeIndex.HighlightsColor, 256,  192);
            _lipColorPickerDark        = CreateColorPicker(CustomizeIndex.LipColor,        512,  96);
            _lipColorPickerLight       = CreateColorPicker(CustomizeIndex.LipColor,        1024, 96, true);
            _eyeColorPicker            = CreateColorPicker(CustomizeIndex.EyeColorLeft,    0,    192);
            _facePaintColorPickerDark  = CreateColorPicker(CustomizeIndex.FacePaintColor,  640,  96);
            _facePaintColorPickerLight = CreateColorPicker(CustomizeIndex.FacePaintColor,  1152, 96, true);
            _tattooColorPicker         = CreateColorPicker(CustomizeIndex.TattooColor,     0,    192);
        }

        // Required sheets.
        private readonly ExcelSheet<CharaMakeCustomize> _customizeSheet;
        private readonly ExcelSheet<CharaMakeParams>    _listSheet;
        private readonly ExcelSheet<HairMakeType>       _hairSheet;
        private readonly ExcelSheet<BNpcCustomize>      _bnpcCustomize;
        private readonly ExcelSheet<ENpcBase>           _enpcBase;
        public readonly  ExcelSheet<Lobby>              Lobby;
        private readonly CmpFile                        _cmpFile;

        // Those values are shared between all races.
        private readonly CustomizeData[] _highlightPicker;
        private readonly CustomizeData[] _eyeColorPicker;
        private readonly CustomizeData[] _facePaintColorPickerDark;
        private readonly CustomizeData[] _facePaintColorPickerLight;
        private readonly CustomizeData[] _lipColorPickerDark;
        private readonly CustomizeData[] _lipColorPickerLight;
        private readonly CustomizeData[] _tattooColorPicker;

        private readonly CustomizationOptions _options;


        private CustomizeData[] CreateColorPicker(CustomizeIndex index, int offset, int num, bool light = false)
            => _cmpFile.GetSlice(offset, num)
                .Select((c, i) => new CustomizeData(index, (CustomizeValue)(light ? 128 + i : 0 + i), c, (ushort)(offset + i)))
                .ToArray();


        private void SetHairByFace(CustomizationSet set)
        {
            if (set.Race != Race.Hrothgar)
            {
                set.HairByFace = Enumerable.Repeat(set.HairStyles, set.Faces.Count + 1).ToArray();
                return;
            }

            var tmp = new IReadOnlyList<CustomizeData>[set.Faces.Count + 1];
            tmp[0] = set.HairStyles;

            for (var i = 1; i <= set.Faces.Count; ++i)
            {
                bool Valid(CustomizeData c)
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
            set.Types = Enum.GetValues<CustomizeIndex>().Select(c =>
            {
                // Those types are not correctly given in the menu, so special case them to color pickers.
                switch (c)
                {
                    case CustomizeIndex.HighlightsColor:
                    case CustomizeIndex.EyeColorLeft:
                    case CustomizeIndex.EyeColorRight:
                    case CustomizeIndex.FacePaintColor:
                        return CharaMakeParams.MenuType.ColorPicker;
                    case CustomizeIndex.BodyType: return CharaMakeParams.MenuType.Nothing;
                    case CustomizeIndex.FacePaintReversed:
                    case CustomizeIndex.Highlights:
                    case CustomizeIndex.SmallIris:
                    case CustomizeIndex.Lipstick:
                        return CharaMakeParams.MenuType.Checkmark;
                    case CustomizeIndex.FacialFeature1:
                    case CustomizeIndex.FacialFeature2:
                    case CustomizeIndex.FacialFeature3:
                    case CustomizeIndex.FacialFeature4:
                    case CustomizeIndex.FacialFeature5:
                    case CustomizeIndex.FacialFeature6:
                    case CustomizeIndex.FacialFeature7:
                    case CustomizeIndex.LegacyTattoo:
                        return CharaMakeParams.MenuType.IconCheckmark;
                }

                var gameId = c.ToByteAndMask().ByteIdx;
                // Otherwise find the first menu corresponding to the id.
                // If there is none, assume a list.
                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customize == gameId);
                return menu?.Type ?? CharaMakeParams.MenuType.ListSelector;
            }).ToArray();
            set.Order = CustomizationSet.ComputeOrder(set);
        }

        // Set customizations available if they have any options.
        private static void SetAvailability(CustomizationSet set, CharaMakeParams row)
        {
            if (set.Race == Race.Hrothgar && set.Gender == Gender.Female)
                return;

            void Set(bool available, CustomizeIndex flag)
            {
                if (available)
                    set.SetAvailable(flag);
            }

            Set(true,                                            CustomizeIndex.Height);
            Set(set.Faces.Count > 0,                             CustomizeIndex.Face);
            Set(true,                                            CustomizeIndex.Hairstyle);
            Set(true,                                            CustomizeIndex.Highlights);
            Set(true,                                            CustomizeIndex.SkinColor);
            Set(true,                                            CustomizeIndex.EyeColorRight);
            Set(true,                                            CustomizeIndex.HairColor);
            Set(true,                                            CustomizeIndex.HighlightsColor);
            Set(true,                                            CustomizeIndex.TattooColor);
            Set(set.NumEyebrows > 0,                             CustomizeIndex.Eyebrows);
            Set(true,                                            CustomizeIndex.EyeColorLeft);
            Set(set.NumEyeShapes > 0,                            CustomizeIndex.EyeShape);
            Set(set.NumNoseShapes > 0,                           CustomizeIndex.Nose);
            Set(set.NumJawShapes > 0,                            CustomizeIndex.Jaw);
            Set(set.NumMouthShapes > 0,                          CustomizeIndex.Mouth);
            Set(set.LipColorsDark.Count > 0,                     CustomizeIndex.LipColor);
            Set(GetListSize(row, CustomizeIndex.MuscleMass) > 0, CustomizeIndex.MuscleMass);
            Set(set.TailEarShapes.Count > 0,                     CustomizeIndex.TailShape);
            Set(GetListSize(row, CustomizeIndex.BustSize) > 0,   CustomizeIndex.BustSize);
            Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaint);
            Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaintColor);
            Set(true,                                            CustomizeIndex.FacialFeature1);
            Set(true,                                            CustomizeIndex.FacialFeature2);
            Set(true,                                            CustomizeIndex.FacialFeature3);
            Set(true,                                            CustomizeIndex.FacialFeature4);
            Set(true,                                            CustomizeIndex.FacialFeature5);
            Set(true,                                            CustomizeIndex.FacialFeature6);
            Set(true,                                            CustomizeIndex.FacialFeature7);
            Set(true,                                            CustomizeIndex.LegacyTattoo);
            Set(true,                                            CustomizeIndex.SmallIris);
            Set(set.Race != Race.Hrothgar,                       CustomizeIndex.Lipstick);
            Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaintReversed);
        }

        // Create a list of lists of facial features and the legacy tattoo.
        private static void SetFacialFeatures(CustomizationSet set, CharaMakeParams row)
        {
            var count = set.Faces.Count;
            set.FacialFeature1 = new List<(CustomizeData, CustomizeData)>(count);

            static (CustomizeData, CustomizeData) Create(CustomizeIndex i, uint data)
                => (new CustomizeData(i, CustomizeValue.Zero, data, 0), new CustomizeData(i, CustomizeValue.Max, data, 1));

            set.LegacyTattoo = Create(CustomizeIndex.LegacyTattoo, 137905);

            var tmp = Enumerable.Repeat(0, 7).Select(_ => new (CustomizeData, CustomizeData)[count + 1]).ToArray();
            for (var i = 0; i < count; ++i)
            {
                var data = row.FacialFeatureByFace[i].Icons;
                tmp[0][i + 1] = Create(CustomizeIndex.FacialFeature1, data[0]);
                tmp[1][i + 1] = Create(CustomizeIndex.FacialFeature2, data[1]);
                tmp[2][i + 1] = Create(CustomizeIndex.FacialFeature3, data[2]);
                tmp[3][i + 1] = Create(CustomizeIndex.FacialFeature4, data[3]);
                tmp[4][i + 1] = Create(CustomizeIndex.FacialFeature5, data[4]);
                tmp[5][i + 1] = Create(CustomizeIndex.FacialFeature6, data[5]);
                tmp[6][i + 1] = Create(CustomizeIndex.FacialFeature7, data[6]);
            }

            set.FacialFeature1 = tmp[0];
            set.FacialFeature2 = tmp[1];
            set.FacialFeature3 = tmp[2];
            set.FacialFeature4 = tmp[3];
            set.FacialFeature5 = tmp[4];
            set.FacialFeature6 = tmp[5];
            set.FacialFeature7 = tmp[6];
        }

        // Set the names for the given set of parameters.
        private void SetNames(CustomizationSet set, CharaMakeParams row)
        {
            var nameArray = Enum.GetValues<CustomizeIndex>().Select(c =>
            {
                // Find the first menu that corresponds to the Id.
                var byteId = c.ToByteAndMask().ByteIdx;
                var menu = row.Menus
                    .Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customize == byteId);
                if (menu == null)
                {
                    // If none exists and the id corresponds to highlights, set the Highlights name.
                    if (c == CustomizeIndex.Highlights)
                        return Lobby.GetRow(237)?.Text.ToDalamudString().ToString() ?? "Highlights";

                    // Otherwise there is an error and we use the default name.
                    return c.ToDefaultName();
                }

                // Facial Features and Tattoos is created by combining two strings.
                if (c is >= CustomizeIndex.FacialFeature1 and <= CustomizeIndex.LegacyTattoo)
                    return
                        $"{Lobby.GetRow(1741)?.Text.ToDalamudString().ToString() ?? "Facial Features"} & {Lobby.GetRow(1742)?.Text.ToDalamudString().ToString() ?? "Tattoos"}";

                // Otherwise all is normal, get the menu name or if it does not work the default name.
                var textRow = Lobby.GetRow(menu.Value.Id);
                return textRow?.Text.ToDalamudString().ToString() ?? c.ToDefaultName();
            }).ToArray();

            // Add names for both eye colors.
            nameArray[(int)CustomizeIndex.EyeColorLeft]  = nameArray[(int)CustomizeIndex.EyeColorRight];
            nameArray[(int)CustomizeIndex.EyeColorRight] = _options.GetName(CustomName.OddEyes);

            set.OptionName = nameArray;
        }

        // Obtain available skin and hair colors for the given subrace and gender.
        private (CustomizeData[], CustomizeData[]) GetColors(SubRace race, Gender gender)
        {
            if (race is > SubRace.Veena or SubRace.Unknown)
                throw new ArgumentOutOfRangeException(nameof(race), race, null);

            var gv  = gender == Gender.Male ? 0 : 1;
            var idx = ((int)race * 2 + gv) * 5 + 3;

            return (CreateColorPicker(CustomizeIndex.SkinColor, idx << 8,       192),
                CreateColorPicker(CustomizeIndex.HairColor,     (idx + 1) << 8, 192));
        }

        // Obtain available hairstyles via reflection from the Hair sheet for the given subrace and gender.
        private CustomizeData[] GetHairStyles(SubRace race, Gender gender)
        {
            var row = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            // Unknown30 is the number of available hairstyles.
            var hairList = new List<CustomizeData>(row.Unknown30);
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
                if (hairRow == null)
                    hairList.Add(new CustomizeData(CustomizeIndex.Hairstyle, (CustomizeValue)i, customizeIdx));
                else if (_options._icons.IconExists(hairRow.Icon))
                    hairList.Add(new CustomizeData(CustomizeIndex.Hairstyle, (CustomizeValue)hairRow.FeatureID, hairRow.Icon,
                        (ushort)hairRow.RowId));
            }

            return hairList.OrderBy(h => h.Value.Value).ToArray();
        }

        // Get Features.
        private CustomizeData FromValueAndIndex(CustomizeIndex id, uint value, int index)
        {
            var row = _customizeSheet.GetRow(value);
            return row == null
                ? new CustomizeData(id, (CustomizeValue)(index + 1),   value)
                : new CustomizeData(id, (CustomizeValue)row.FeatureID, row.Icon, (ushort)row.RowId);
        }

        // Get List sizes.
        private static int GetListSize(CharaMakeParams row, CustomizeIndex index)
        {
            var gameId = index.ToByteAndMask().ByteIdx;
            var menu   = row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customize == gameId);
            return menu?.Size ?? 0;
        }

        // Get face paints from the hair sheet via reflection.
        private CustomizeData[] GetFacePaints(SubRace race, Gender gender)
        {
            var row       = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
            var paintList = new List<CustomizeData>(row.Unknown37);
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
                if (paintRow != null)
                    paintList.Add(new CustomizeData(CustomizeIndex.FacePaint, (CustomizeValue)paintRow.FeatureID, paintRow.Icon,
                        (ushort)paintRow.RowId));
                else
                    paintList.Add(new CustomizeData(CustomizeIndex.FacePaint, (CustomizeValue)i, customizeIdx));
            }

            return paintList.OrderBy(p => p.Value.Value).ToArray();
        }

        // Specific icons for tails or ears.
        private CustomizeData[] GetTailEarShapes(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customize == CustomizeIndex.TailShape.ToByteAndMask().ByteIdx)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizeIndex.TailShape, v, i)).ToArray()
             ?? Array.Empty<CustomizeData>();

        // Specific icons for faces.
        private CustomizeData[] GetFaces(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>().FirstOrDefault(m => m!.Value.Customize == CustomizeIndex.Face.ToByteAndMask().ByteIdx)
                    ?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizeIndex.Face, v, i)).ToArray()
             ?? Array.Empty<CustomizeData>();

        // Specific icons for Hrothgar patterns.
        private CustomizeData[] HrothgarFurPattern(CharaMakeParams row)
            => row.Menus.Cast<CharaMakeParams.Menu?>()
                    .FirstOrDefault(m => m!.Value.Customize == CustomizeIndex.LipColor.ToByteAndMask().ByteIdx)?.Values
                    .Select((v, i) => FromValueAndIndex(CustomizeIndex.LipColor, v, i)).ToArray()
             ?? Array.Empty<CustomizeData>();
    }
}
