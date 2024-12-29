using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.GameData;

/// <summary> Contains a set of all human NPC appearances with their names. </summary>
public class NpcCustomizeSet : IAsyncDataContainer, IReadOnlyList<NpcData>
{
    /// <inheritdoc/>
    public string Name
        => nameof(NpcCustomizeSet);

    /// <inheritdoc/>
    public long Time { get; private set; }

    /// <inheritdoc/>
    public long Memory { get; private set; }

    /// <inheritdoc/>
    public int TotalCount
        => _data.Count;

    /// <inheritdoc/>
    public Task Awaiter { get; }

    /// <inheritdoc/>
    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

    /// <summary> The list of data. </summary>
    private readonly List<NpcData> _data = [];

    private readonly BitArray _hairColors      = new(256);
    private readonly BitArray _eyeColors       = new(256);
    private readonly BitArray _facepaintColors = new(256);
    private readonly BitArray _tattooColors    = new(256);

    public bool CheckColor(CustomizeIndex type, CustomizeValue value)
        => type switch
        {
            CustomizeIndex.HairColor       => _hairColors[value.Value],
            CustomizeIndex.HighlightsColor => _hairColors[value.Value],
            CustomizeIndex.EyeColorLeft    => _eyeColors[value.Value],
            CustomizeIndex.EyeColorRight   => _eyeColors[value.Value],
            CustomizeIndex.FacePaintColor  => _facepaintColors[value.Value],
            CustomizeIndex.TattooColor     => _tattooColors[value.Value],
            _                              => false,
        };

    /// <summary> Create the data when ready. </summary>
    public NpcCustomizeSet(IDataManager data, DictENpc eNpcs, DictBNpc bNpcs, DictBNpcNames bNpcNames)
    {
        var waitTask = Task.WhenAll(eNpcs.Awaiter, bNpcs.Awaiter, bNpcNames.Awaiter);
        Awaiter = waitTask.ContinueWith(_ =>
        {
            var watch    = Stopwatch.StartNew();
            var eNpcTask = Task.Run(() => CreateEnpcData(data, eNpcs));
            var bNpcTask = Task.Run(() => CreateBnpcData(data, bNpcs, bNpcNames));
            FilterAndOrderNpcData(eNpcTask.Result, bNpcTask.Result);
            Time = watch.ElapsedMilliseconds;
        });
    }

    /// <summary> Create data from event NPCs. </summary>
    private static List<NpcData> CreateEnpcData(IDataManager data, DictENpc eNpcs)
    {
        var enpcSheet = data.GetExcelSheet<ENpcBase>();
        var list      = new List<NpcData>(eNpcs.Count);

        // Go through all event NPCs already collected into a dictionary.
        foreach (var (id, name) in eNpcs)
        {
            // We only accept NPCs with valid names.
            if (!enpcSheet.TryGetRow(id.Id, out var row) || name.IsNullOrWhitespace())
                continue;

            // Check if the customization is a valid human.
            var (valid, customize) = FromEnpcBase(row);
            if (!valid)
                continue;

            var ret = new NpcData
            {
                Name      = name,
                Customize = customize,
                ModelId   = row.ModelChara.RowId,
                Id        = id,
                Kind      = ObjectKind.EventNpc,
            };

            // Event NPCs have a reference to NpcEquip but also contain the appearance in their own row.
            // Prefer the NpcEquip reference if it is set and the own does not appear to be set, otherwise use the own.
            if (row.NpcEquip.RowId != 0 && row.NpcEquip.Value is { } equip && row is { ModelBody: 0, ModelLegs: 0 })
                ApplyNpcEquip(ref ret, equip);
            else
                ApplyNpcEquip(ref ret, row);

            list.Add(ret);
        }

        return list;
    }

    /// <summary> Create data from battle NPCs. </summary>
    private static List<NpcData> CreateBnpcData(IDataManager data, DictBNpc bNpcs, DictBNpcNames bNpcNames)
    {
        var bnpcSheet = data.GetExcelSheet<BNpcBase>();
        var list      = new List<NpcData>(bnpcSheet.Count);

        // We go through all battle NPCs in the sheet because the dictionary refers to names.
        foreach (var baseRow in bnpcSheet)
        {
            // Only accept humans.
            if (baseRow.ModelChara.Value.Type != 1)
                continue;

            var bnpcNameIds = bNpcNames[baseRow.RowId];
            // Only accept battle NPCs with known associated names.
            if (bnpcNameIds.Count == 0)
                continue;

            // Check if the customization is a valid human.
            var (valid, customize) = FromBnpcCustomize(baseRow.BNpcCustomize.Value);
            if (!valid)
                continue;

            var equip = baseRow.NpcEquip.Value;
            var ret = new NpcData
            {
                Customize = customize,
                ModelId   = baseRow.ModelChara.RowId,
                Id        = baseRow.RowId,
                Kind      = ObjectKind.BattleNpc,
            };
            ApplyNpcEquip(ref ret, equip);
            // Add the appearance for each associated name.
            foreach (var bnpcNameId in bnpcNameIds)
            {
                if (bNpcs.TryGetValue(bnpcNameId.Id, out var name) && !name.IsNullOrWhitespace())
                    list.Add(ret with { Name = name });
            }
        }

        return list;
    }

    /// <summary> Given the battle NPC and event NPC lists, order and deduplicate entries. </summary>
    private void FilterAndOrderNpcData(IReadOnlyCollection<NpcData> eNpcEquip, IReadOnlyCollection<NpcData> bNpcEquip)
    {
        _data.Clear();
        // This is a maximum since we deduplicate.
        _data.EnsureCapacity(eNpcEquip.Count + bNpcEquip.Count);
        // Convert the NPCs to a dictionary of lists grouped by name.
        var groups = eNpcEquip.Concat(bNpcEquip).GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.ToList());
        // Iterate through the sorted list.
        foreach (var (_, duplicates) in groups.OrderBy(kvp => kvp.Key))
        {
            // Remove any duplicate entries for a name with identical data.
            for (var i = 0; i < duplicates.Count; ++i)
            {
                var current = duplicates[i];
                _hairColors[current.Customize[CustomizeIndex.HairColor].Value]           = true;
                _hairColors[current.Customize[CustomizeIndex.HighlightsColor].Value]     = true;
                _eyeColors[current.Customize[CustomizeIndex.EyeColorLeft].Value]         = true;
                _eyeColors[current.Customize[CustomizeIndex.EyeColorRight].Value]        = true;
                _facepaintColors[current.Customize[CustomizeIndex.FacePaintColor].Value] = true;
                _tattooColors[current.Customize[CustomizeIndex.TattooColor].Value]       = true;
                for (var j = 0; j < i; ++j)
                {
                    if (current.DataEquals(duplicates[j]))
                    {
                        duplicates.RemoveAt(i--);
                        break;
                    }
                }
            }

            // If there is only a single entry, add that. 
            if (duplicates.Count == 1)
            {
                _data.Add(duplicates[0]);
                Memory += 96;
            }
            else
            {
                _data.AddRange(duplicates);
                Memory += 96 * duplicates.Count;
            }
        }

        // Sort non-alphanumeric entries at the end instead of the beginning.
        var lastWeird = _data.FindIndex(d => char.IsAsciiLetterOrDigit(d.Name[0]));
        if (lastWeird != -1)
        {
            _data.AddRange(_data.Take(lastWeird));
            _data.RemoveRange(0, lastWeird);
        }

        // Reduce memory footprint.
        _data.TrimExcess();
    }

    /// <summary> Apply equipment from a NpcEquip row. </summary>
    private static void ApplyNpcEquip(ref NpcData data, NpcEquip row)
    {
        data.Set(0, row.ModelHead | (row.DyeHead.RowId << 24) | ((ulong)row.Dye2Head.RowId << 32));
        data.Set(1, row.ModelBody | (row.DyeBody.RowId << 24) | ((ulong)row.Dye2Body.RowId << 32));
        data.Set(2, row.ModelHands | (row.DyeHands.RowId << 24) | ((ulong)row.Dye2Hands.RowId << 32));
        data.Set(3, row.ModelLegs | (row.DyeLegs.RowId << 24) | ((ulong)row.Dye2Legs.RowId << 32));
        data.Set(4, row.ModelFeet | (row.DyeFeet.RowId << 24) | ((ulong)row.Dye2Feet.RowId << 32));
        data.Set(5, row.ModelEars | (row.DyeEars.RowId << 24) | ((ulong)row.Dye2Ears.RowId << 32));
        data.Set(6, row.ModelNeck | (row.DyeNeck.RowId << 24) | ((ulong)row.Dye2Neck.RowId << 32));
        data.Set(7, row.ModelWrists | (row.DyeWrists.RowId << 24) | ((ulong)row.Dye2Wrists.RowId << 32));
        data.Set(8, row.ModelRightRing | (row.DyeRightRing.RowId << 24) | ((ulong)row.Dye2RightRing.RowId << 32));
        data.Set(9, row.ModelLeftRing | (row.DyeLeftRing.RowId << 24) | ((ulong)row.Dye2LeftRing.RowId << 32));
        data.Mainhand = new CharacterWeapon(row.ModelMainHand | ((ulong)row.DyeMainHand.RowId << 48) | ((ulong)row.Dye2MainHand.RowId << 56));
        data.Offhand = new CharacterWeapon(row.ModelOffHand | ((ulong)row.DyeOffHand.RowId << 48) | ((ulong)row.Dye2OffHand.RowId << 56));
        data.VisorToggled = row.Visor;
    }

    /// <summary> Apply equipment from a ENpcBase Row row. </summary>
    private static void ApplyNpcEquip(ref NpcData data, ENpcBase row)
    {
        data.Set(0, row.ModelHead | (row.DyeHead.RowId << 24) | ((ulong)row.Dye2Head.RowId << 32));
        data.Set(1, row.ModelBody | (row.DyeBody.RowId << 24) | ((ulong)row.Dye2Body.RowId << 32));
        data.Set(2, row.ModelHands | (row.DyeHands.RowId << 24) | ((ulong)row.Dye2Hands.RowId << 32));
        data.Set(3, row.ModelLegs | (row.DyeLegs.RowId << 24) | ((ulong)row.Dye2Legs.RowId << 32));
        data.Set(4, row.ModelFeet | (row.DyeFeet.RowId << 24) | ((ulong)row.Dye2Feet.RowId << 32));
        data.Set(5, row.ModelEars | (row.DyeEars.RowId << 24) | ((ulong)row.Dye2Ears.RowId << 32));
        data.Set(6, row.ModelNeck | (row.DyeNeck.RowId << 24) | ((ulong)row.Dye2Neck.RowId << 32));
        data.Set(7, row.ModelWrists | (row.DyeWrists.RowId << 24) | ((ulong)row.Dye2Wrists.RowId << 32));
        data.Set(8, row.ModelRightRing | (row.DyeRightRing.RowId << 24) | ((ulong)row.Dye2RightRing.RowId << 32));
        data.Set(9, row.ModelLeftRing | (row.DyeLeftRing.RowId << 24) | ((ulong)row.Dye2LeftRing.RowId << 32));
        data.Mainhand = new CharacterWeapon(row.ModelMainHand | ((ulong)row.DyeMainHand.RowId << 48) | ((ulong)row.Dye2MainHand.RowId << 56));
        data.Offhand = new CharacterWeapon(row.ModelOffHand | ((ulong)row.DyeOffHand.RowId << 48) | ((ulong)row.Dye2OffHand.RowId << 56));
        data.VisorToggled = row.Visor;
    }

    /// <summary> Obtain customizations from a BNpcCustomize row and check if the human is valid. </summary>
    private static (bool, CustomizeArray) FromBnpcCustomize(BNpcCustomize bnpcCustomize)
    {
        var customize = new CustomizeArray();
        customize.SetByIndex(0,  (CustomizeValue)(byte)bnpcCustomize.Race.RowId);
        customize.SetByIndex(1,  (CustomizeValue)bnpcCustomize.Gender);
        customize.SetByIndex(2,  (CustomizeValue)bnpcCustomize.BodyType);
        customize.SetByIndex(3,  (CustomizeValue)bnpcCustomize.Height);
        customize.SetByIndex(4,  (CustomizeValue)(byte)bnpcCustomize.Tribe.RowId);
        customize.SetByIndex(5,  (CustomizeValue)bnpcCustomize.Face);
        customize.SetByIndex(6,  (CustomizeValue)bnpcCustomize.HairStyle);
        customize.SetByIndex(7,  (CustomizeValue)bnpcCustomize.HairHighlight);
        customize.SetByIndex(8,  (CustomizeValue)bnpcCustomize.SkinColor);
        customize.SetByIndex(9,  (CustomizeValue)bnpcCustomize.EyeHeterochromia);
        customize.SetByIndex(10, (CustomizeValue)bnpcCustomize.HairColor);
        customize.SetByIndex(11, (CustomizeValue)bnpcCustomize.HairHighlightColor);
        customize.SetByIndex(12, (CustomizeValue)bnpcCustomize.FacialFeature);
        customize.SetByIndex(13, (CustomizeValue)bnpcCustomize.FacialFeatureColor);
        customize.SetByIndex(14, (CustomizeValue)bnpcCustomize.Eyebrows);
        customize.SetByIndex(15, (CustomizeValue)bnpcCustomize.EyeColor);
        customize.SetByIndex(16, (CustomizeValue)bnpcCustomize.EyeShape);
        customize.SetByIndex(17, (CustomizeValue)bnpcCustomize.Nose);
        customize.SetByIndex(18, (CustomizeValue)bnpcCustomize.Jaw);
        customize.SetByIndex(19, (CustomizeValue)bnpcCustomize.Mouth);
        customize.SetByIndex(20, (CustomizeValue)bnpcCustomize.LipColor);
        customize.SetByIndex(21, (CustomizeValue)bnpcCustomize.BustOrTone1);
        customize.SetByIndex(22, (CustomizeValue)bnpcCustomize.ExtraFeature1);
        customize.SetByIndex(23, (CustomizeValue)bnpcCustomize.ExtraFeature2OrBust);
        customize.SetByIndex(24, (CustomizeValue)bnpcCustomize.FacePaint);
        customize.SetByIndex(25, (CustomizeValue)bnpcCustomize.FacePaintColor);

        if (!CustomizeManager.Races.Contains(customize.Race)
         || !CustomizeManager.Clans.Contains(customize.Clan)
         || !CustomizeManager.Genders.Contains(customize.Gender))
            return (false, CustomizeArray.Default);

        return (true, customize);
    }

    /// <summary> Obtain customizations from a ENpcBase row and check if the human is valid. </summary>
    private static (bool, CustomizeArray) FromEnpcBase(ENpcBase enpcBase)
    {
        if (enpcBase.ModelChara.ValueNullable?.Type != 1)
            return (false, CustomizeArray.Default);

        var customize = new CustomizeArray();
        customize.SetByIndex(0,  (CustomizeValue)(byte)enpcBase.Race.RowId);
        customize.SetByIndex(1,  (CustomizeValue)enpcBase.Gender);
        customize.SetByIndex(2,  (CustomizeValue)enpcBase.BodyType);
        customize.SetByIndex(3,  (CustomizeValue)enpcBase.Height);
        customize.SetByIndex(4,  (CustomizeValue)(byte)enpcBase.Tribe.RowId);
        customize.SetByIndex(5,  (CustomizeValue)enpcBase.Face);
        customize.SetByIndex(6,  (CustomizeValue)enpcBase.HairStyle);
        customize.SetByIndex(7,  (CustomizeValue)enpcBase.HairHighlight);
        customize.SetByIndex(8,  (CustomizeValue)enpcBase.SkinColor);
        customize.SetByIndex(9,  (CustomizeValue)enpcBase.EyeHeterochromia);
        customize.SetByIndex(10, (CustomizeValue)enpcBase.HairColor);
        customize.SetByIndex(11, (CustomizeValue)enpcBase.HairHighlightColor);
        customize.SetByIndex(12, (CustomizeValue)enpcBase.FacialFeature);
        customize.SetByIndex(13, (CustomizeValue)enpcBase.FacialFeatureColor);
        customize.SetByIndex(14, (CustomizeValue)enpcBase.Eyebrows);
        customize.SetByIndex(15, (CustomizeValue)enpcBase.EyeColor);
        customize.SetByIndex(16, (CustomizeValue)enpcBase.EyeShape);
        customize.SetByIndex(17, (CustomizeValue)enpcBase.Nose);
        customize.SetByIndex(18, (CustomizeValue)enpcBase.Jaw);
        customize.SetByIndex(19, (CustomizeValue)enpcBase.Mouth);
        customize.SetByIndex(20, (CustomizeValue)enpcBase.LipColor);
        customize.SetByIndex(21, (CustomizeValue)enpcBase.BustOrTone1);
        customize.SetByIndex(22, (CustomizeValue)enpcBase.ExtraFeature1);
        customize.SetByIndex(23, (CustomizeValue)enpcBase.ExtraFeature2OrBust);
        customize.SetByIndex(24, (CustomizeValue)enpcBase.FacePaint);
        customize.SetByIndex(25, (CustomizeValue)enpcBase.FacePaintColor);

        if (!CustomizeManager.Races.Contains(customize.Race)
         || !CustomizeManager.Clans.Contains(customize.Clan)
         || !CustomizeManager.Genders.Contains(customize.Gender))
            return (false, CustomizeArray.Default);

        return (true, customize);
    }

    /// <inheritdoc/>
    public IEnumerator<NpcData> GetEnumerator()
        => _data.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _data.Count;

    /// <inheritdoc/>
    public NpcData this[int index]
        => _data[index];
}
