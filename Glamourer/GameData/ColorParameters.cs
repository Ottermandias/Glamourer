using Dalamud.Plugin.Services;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;

namespace Glamourer.GameData;

/// <summary> Parse the Human.cmp file as a list of 4-byte integer values to obtain colors. </summary>
public class ColorParameters
{
    public readonly CmpData RgbaColors;

    public unsafe CustomizeData[] GetColors(CustomizeIndex index, SubRace race = SubRace.Unknown, Gender gender = Gender.Unknown)
    {
        // Use Interface for everything but double parameters, since those would be identical for interface.
        // Outside of those, Interface seems to match actual colors after shaders / diffuses better than applied color.
        return index switch
        {
            CustomizeIndex.HighlightsColor => SetSingle(index, RgbaColors.Interface.HairHighlights,
                nameof(CmpData.ColorParameters.HairHighlights)),
            CustomizeIndex.EyeColorLeft  => SetSingle(index, RgbaColors.Interface.Eyes,     nameof(CmpData.ColorParameters.Eyes)),
            CustomizeIndex.EyeColorRight => SetSingle(index, RgbaColors.Interface.Eyes,     nameof(CmpData.ColorParameters.Eyes)),
            CustomizeIndex.TattooColor   => SetSingle(index, RgbaColors.Interface.Features, nameof(CmpData.ColorParameters.Features)),
            CustomizeIndex.LipColor => SetDouble(index,   RgbaColors.Parameters.LipsDark, RgbaColors.Parameters.LipsLight,
                nameof(CmpData.ColorParameters.LipsDark), nameof(CmpData.ColorParameters.LipsLight)),
            CustomizeIndex.FacePaintColor => SetDouble(index,  RgbaColors.Parameters.FacePaintDark, RgbaColors.Parameters.FacePaintLight,
                nameof(CmpData.ColorParameters.FacePaintDark), nameof(CmpData.ColorParameters.FacePaintLight)),
            CustomizeIndex.SkinColor => SetRace(index, RgbaColors.Races[Index(race, gender, out var idx)].SkinInterface, idx,
                nameof(CmpData.GenderClanColorParameters.SkinInterface)),
            CustomizeIndex.HairColor => SetRace(index, RgbaColors.Races[Index(race, gender, out var idx)].HairInterface, idx,
                nameof(CmpData.GenderClanColorParameters.HairInterface)),
            _ => [],
        };

        static CustomizeData[] SetSingle(CustomizeIndex index, in CmpData.FullColors colors, string name)
        {
            var ret        = new CustomizeData[192];
            var parameters = Marshal.OffsetOf<CmpData>(nameof(CmpData.Interface));
            var dataOffset = Marshal.OffsetOf<CmpData.ColorParameters>(name);
            var offset     = (parameters + dataOffset) / sizeof(Rgba32);
            for (var i = 0; i < 192; ++i)
                ret[i] = new CustomizeData(index, (CustomizeValue)i, colors[i].Color, (ushort)(offset + i));

            return ret;
        }

        static CustomizeData[] SetDouble(CustomizeIndex index, in CmpData.TonedColors colorsDark, in CmpData.TonedColors colorsLight,
            string nameDark, string nameLight)
        {
            var ret             = new CustomizeData[192];
            var parameters      = Marshal.OffsetOf<CmpData>(nameof(CmpData.Parameters));
            var dataOffsetDark  = Marshal.OffsetOf<CmpData.ColorParameters>(nameDark);
            var dataOffsetLight = Marshal.OffsetOf<CmpData.ColorParameters>(nameLight);
            var offsetDark      = (parameters + dataOffsetDark) / sizeof(Rgba32);
            var offsetLight     = (parameters + dataOffsetLight) / sizeof(Rgba32);
            for (var i = 0; i < 96; ++i)
                ret[i] = new CustomizeData(index, (CustomizeValue)i, colorsDark[i].Color, (ushort)(offsetDark + i));

            for (var i = 0; i < 96; ++i)
                ret[i + 96] = new CustomizeData(index, (CustomizeValue)(128 + i), colorsLight[i].Color, (ushort)(offsetLight + i));

            return ret;
        }

        static int Index(SubRace race, Gender gender, out int index)
            => index = CmpData.Index(race, gender);

        static CustomizeData[] SetRace(CustomizeIndex index, in CmpData.FullColors data, int genderRace, string name)
        {
            var ret        = new CustomizeData[192];
            var dataOffset = Marshal.OffsetOf<CmpData>(nameof(CmpData.Races));
            var raceOffset = genderRace * sizeof(CmpData.GenderClanColorParameters);
            var typeOffset = Marshal.OffsetOf<CmpData.GenderClanColorParameters>(name);
            var offset     = (dataOffset + raceOffset + typeOffset) / 4;
            for (var i = 0; i < 192; ++i)
                ret[i] = new CustomizeData(index, (CustomizeValue)i, data[i].Color, (ushort)(offset + i));
            return ret;
        }
    }

    public unsafe ColorParameters(IDataManager gameData, IPluginLog log)
    {
        try
        {
            var file = gameData.GetFile("chara/xls/charamake/human.cmp")!;
            if (file.Data.Length != sizeof(CmpData))
                throw new Exception($"human.cmp changed and has unexpected size {sizeof(CmpData)}");

            // Just copy all the data into an uint array.
            fixed (byte* ptr1 = file.Data)
            {
                fixed (CmpData* ptr2 = &RgbaColors)
                {
                    MemoryUtility.MemCpyUnchecked(ptr2, ptr1, file.Data.Length);
                }
            }
        }
        catch (Exception e)
        {
            log.Error("READ THIS\n======== Could not obtain the human.cmp file which is necessary for color sets.\n"
              + "======== This usually indicates an error with your index files caused by TexTools modifications.\n"
              + "======== If you have used TexTools before, you will probably need to start over in it to use Glamourer.", e);
            RgbaColors = default;
        }
    }
}
