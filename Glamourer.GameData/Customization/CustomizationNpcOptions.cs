using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public static class CustomizationNpcOptions
{
    public static Dictionary<(SubRace, Gender), HashSet<(CustomizeIndex, CustomizeValue)>> CreateNpcData(CustomizationSet[] sets,
        ExcelSheet<BNpcCustomize> bNpc, ExcelSheet<ENpcBase> eNpc)
    {
        var customizes = bNpc.SelectWhere(FromBnpcCustomize)
            .Concat(eNpc.SelectWhere(FromEnpcBase)).ToList();

        var dict = new Dictionary<(SubRace, Gender), HashSet<(CustomizeIndex, CustomizeValue)>>();
        var customizeIndices = new[]
        {
            CustomizeIndex.Face,
            CustomizeIndex.Hairstyle,
            CustomizeIndex.LipColor,
            CustomizeIndex.SkinColor,
            CustomizeIndex.FacePaintColor,
            CustomizeIndex.HighlightsColor,
            CustomizeIndex.HairColor,
            CustomizeIndex.FacePaint,
            CustomizeIndex.TattooColor,
            CustomizeIndex.EyeColorLeft,
            CustomizeIndex.EyeColorRight,
        };

        foreach (var customize in customizes)
        {
            var set = sets[CustomizationOptions.ToIndex(customize.Clan, customize.Gender)];
            foreach (var customizeIndex in customizeIndices)
            {
                var value = customize[customizeIndex];
                if (value == CustomizeValue.Zero)
                    continue;

                if (set.DataByValue(customizeIndex, value, out _, customize.Face) >= 0)
                    continue;

                if (!dict.TryGetValue((set.Clan, set.Gender), out var npcSet))
                {
                    npcSet = new HashSet<(CustomizeIndex, CustomizeValue)> { (customizeIndex, value) };
                    dict.Add((set.Clan, set.Gender), npcSet);
                }
                else
                {
                    npcSet.Add((customizeIndex, value));
                }
            }
        }

        return dict;
    }

    private static (bool, Customize) FromBnpcCustomize(BNpcCustomize bnpcCustomize)
    {
        var customize = new Customize();
        customize.Data.Set(0,  (byte)bnpcCustomize.Race.Row);
        customize.Data.Set(1,  bnpcCustomize.Gender);
        customize.Data.Set(2,  bnpcCustomize.BodyType);
        customize.Data.Set(3,  bnpcCustomize.Height);
        customize.Data.Set(4,  (byte)bnpcCustomize.Tribe.Row);
        customize.Data.Set(5,  bnpcCustomize.Face);
        customize.Data.Set(6,  bnpcCustomize.HairStyle);
        customize.Data.Set(7,  bnpcCustomize.HairHighlight);
        customize.Data.Set(8,  bnpcCustomize.SkinColor);
        customize.Data.Set(9,  bnpcCustomize.EyeHeterochromia);
        customize.Data.Set(10, bnpcCustomize.HairColor);
        customize.Data.Set(11, bnpcCustomize.HairHighlightColor);
        customize.Data.Set(12, bnpcCustomize.FacialFeature);
        customize.Data.Set(13, bnpcCustomize.FacialFeatureColor);
        customize.Data.Set(14, bnpcCustomize.Eyebrows);
        customize.Data.Set(15, bnpcCustomize.EyeColor);
        customize.Data.Set(16, bnpcCustomize.EyeShape);
        customize.Data.Set(17, bnpcCustomize.Nose);
        customize.Data.Set(18, bnpcCustomize.Jaw);
        customize.Data.Set(19, bnpcCustomize.Mouth);
        customize.Data.Set(20, bnpcCustomize.LipColor);
        customize.Data.Set(21, bnpcCustomize.BustOrTone1);
        customize.Data.Set(22, bnpcCustomize.ExtraFeature1);
        customize.Data.Set(23, bnpcCustomize.ExtraFeature2OrBust);
        customize.Data.Set(24, bnpcCustomize.FacePaint);
        customize.Data.Set(25, bnpcCustomize.FacePaintColor);

        if (customize.BodyType.Value != 1
         || !CustomizationOptions.Races.Contains(customize.Race)
         || !CustomizationOptions.Clans.Contains(customize.Clan)
         || !CustomizationOptions.Genders.Contains(customize.Gender))
            return (false, Customize.Default);

        return (true, customize);
    }

    private static (bool, Customize) FromEnpcBase(ENpcBase enpcBase)
    {
        if (enpcBase.ModelChara.Row != 0)
            return (false, Customize.Default);

        var customize = new Customize();
        customize.Data.Set(0,  (byte)enpcBase.Race.Row);
        customize.Data.Set(1,  enpcBase.Gender);
        customize.Data.Set(2,  enpcBase.BodyType);
        customize.Data.Set(3,  enpcBase.Height);
        customize.Data.Set(4,  (byte)enpcBase.Tribe.Row);
        customize.Data.Set(5,  enpcBase.Face);
        customize.Data.Set(6,  enpcBase.HairStyle);
        customize.Data.Set(7,  enpcBase.HairHighlight);
        customize.Data.Set(8,  enpcBase.SkinColor);
        customize.Data.Set(9,  enpcBase.EyeHeterochromia);
        customize.Data.Set(10, enpcBase.HairColor);
        customize.Data.Set(11, enpcBase.HairHighlightColor);
        customize.Data.Set(12, enpcBase.FacialFeature);
        customize.Data.Set(13, enpcBase.FacialFeatureColor);
        customize.Data.Set(14, enpcBase.Eyebrows);
        customize.Data.Set(15, enpcBase.EyeColor);
        customize.Data.Set(16, enpcBase.EyeShape);
        customize.Data.Set(17, enpcBase.Nose);
        customize.Data.Set(18, enpcBase.Jaw);
        customize.Data.Set(19, enpcBase.Mouth);
        customize.Data.Set(20, enpcBase.LipColor);
        customize.Data.Set(21, enpcBase.BustOrTone1);
        customize.Data.Set(22, enpcBase.ExtraFeature1);
        customize.Data.Set(23, enpcBase.ExtraFeature2OrBust);
        customize.Data.Set(24, enpcBase.FacePaint);
        customize.Data.Set(25, enpcBase.FacePaintColor);

        if (customize.BodyType.Value != 1
         || !CustomizationOptions.Races.Contains(customize.Race)
         || !CustomizationOptions.Clans.Contains(customize.Clan)
         || !CustomizationOptions.Genders.Contains(customize.Gender))
            return (false, Customize.Default);

        return (true, customize);
    }
}
