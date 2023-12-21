using Penumbra.GameData.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Glamourer.Customization;

public static class CustomizationNpcOptions
{
    public static Dictionary<(SubRace, Gender), IReadOnlyList<(CustomizeIndex, CustomizeValue)>> CreateNpcData(CustomizationSet[] sets, NpcCustomizeSet npcCustomizeSet)
    {
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

        foreach (var customize in npcCustomizeSet.Select(s => s.Customize))
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
                    npcSet = [(customizeIndex, value)];
                    dict.Add((set.Clan, set.Gender), npcSet);
                }
                else
                {
                    npcSet.Add((customizeIndex, value));
                }
            }
        }

        return dict.ToDictionary(kvp => kvp.Key,
            kvp => (IReadOnlyList<(CustomizeIndex, CustomizeValue)>)kvp.Value.OrderBy(p => p.Item1).ThenBy(p => p.Item2.Value).ToArray());
    }
}
