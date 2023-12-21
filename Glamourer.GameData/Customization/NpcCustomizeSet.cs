using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Glamourer.Customization;

public class NpcCustomizeSet : IAsyncDataContainer, IReadOnlyList<NpcData>
{
    public string Name
        => nameof(NpcCustomizeSet);

    private readonly List<NpcData> _data = [];

    public long Time   { get; private set; }
    public long Memory { get; private set; }
    public int TotalCount
        => _data.Count;

    public Task Awaiter { get; }

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

    private static List<NpcData> CreateEnpcData(IDataManager data, DictENpc eNpcs)
    {
        var enpcSheet = data.GetExcelSheet<ENpcBase>()!;
        var list      = new List<NpcData>(eNpcs.Count);

        foreach (var (id, name) in eNpcs)
        {
            var row = enpcSheet.GetRow(id.Id);
            if (row == null || name.IsNullOrWhitespace())
                continue;

            var (valid, customize) = FromEnpcBase(row);
            if (!valid)
                continue;

            var ret = new NpcData
            {
                Name      = name,
                Customize = customize,
                Id        = id,
                Kind      = ObjectKind.EventNpc,
            };

            if (row.NpcEquip.Row != 0 && row.NpcEquip.Value is { } equip)
            {
                ApplyNpcEquip(ref ret, equip);
            }
            else
            {
                ret.Set(0, row.ModelHead | (row.DyeHead.Row << 24));
                ret.Set(1, row.ModelBody | (row.DyeBody.Row << 24));
                ret.Set(2, row.ModelHands | (row.DyeHands.Row << 24));
                ret.Set(3, row.ModelLegs | (row.DyeLegs.Row << 24));
                ret.Set(4, row.ModelFeet | (row.DyeFeet.Row << 24));
                ret.Set(5, row.ModelEars | (row.DyeEars.Row << 24));
                ret.Set(6, row.ModelNeck | (row.DyeNeck.Row << 24));
                ret.Set(7, row.ModelWrists | (row.DyeWrists.Row << 24));
                ret.Set(8, row.ModelRightRing | (row.DyeRightRing.Row << 24));
                ret.Set(9, row.ModelLeftRing | (row.DyeLeftRing.Row << 24));
                ret.Mainhand     = new CharacterWeapon(row.ModelMainHand | ((ulong)row.DyeMainHand.Row << 48));
                ret.Offhand      = new CharacterWeapon(row.ModelOffHand | ((ulong)row.DyeOffHand.Row << 48));
                ret.VisorToggled = row.Visor;
            }

            list.Add(ret);
        }

        return list;
    }

    private static List<NpcData> CreateBnpcData(IDataManager data, DictBNpc bNpcs, DictBNpcNames bNpcNames)
    {
        var bnpcSheet = data.GetExcelSheet<BNpcBase>()!;
        var list      = new List<NpcData>((int)bnpcSheet.RowCount);
        foreach (var baseRow in bnpcSheet)
        {
            if (baseRow.ModelChara.Value!.Type != 1)
                continue;

            var bnpcNameIds = bNpcNames[baseRow.RowId];
            if (bnpcNameIds.Count == 0)
                continue;

            var (valid, customize) = FromBnpcCustomize(baseRow.BNpcCustomize.Value!);
            if (!valid)
                continue;

            var equip = baseRow.NpcEquip.Value!;
            var ret = new NpcData
            {
                Customize = customize,
                Id        = baseRow.RowId,
                Kind      = ObjectKind.BattleNpc,
            };
            ApplyNpcEquip(ref ret, equip);
            foreach (var bnpcNameId in bnpcNameIds)
            {
                if (bNpcs.TryGetValue(bnpcNameId.Id, out var name) && !name.IsNullOrWhitespace())
                    list.Add(ret with { Name = name });
            }
        }

        return list;
    }

    private void FilterAndOrderNpcData(List<NpcData> eNpcEquip, List<NpcData> bNpcEquip)
    {
        _data.Clear();
        _data.EnsureCapacity(eNpcEquip.Count + bNpcEquip.Count);
        var groups = eNpcEquip.Concat(bNpcEquip).GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var (name, duplicates) in groups.OrderBy(kvp => kvp.Key))
        {
            for (var i = 0; i < duplicates.Count; ++i)
            {
                var current = duplicates[i];
                for (var j = 0; j < i; ++j)
                {
                    if (current.DataEquals(duplicates[j]))
                    {
                        duplicates.RemoveAt(i--);
                        break;
                    }
                }
            }

            if (duplicates.Count == 1)
            {
                _data.Add(duplicates[0]);
                Memory += 96;
            }
            else
            {
                _data.AddRange(duplicates
                    .Select(duplicate => duplicate with
                    {
                        Name = $"{name} ({(duplicate.Kind is ObjectKind.BattleNpc ? 'B' : 'E')}{duplicate.Id})"
                    }));
                Memory += 96 * duplicates.Count + duplicates.Sum(d => d.Name.Length * 2);
            }
        }

        var lastWeird = _data.FindIndex(d => char.IsAsciiLetterOrDigit(d.Name[0]));
        if (lastWeird != -1)
        {
            _data.AddRange(_data.Take(lastWeird));
            _data.RemoveRange(0, lastWeird);
        }
        _data.TrimExcess();
    }

    private static void ApplyNpcEquip(ref NpcData data, NpcEquip row)
    {
        data.Set(0, row.ModelHead | (row.DyeHead.Row << 24));
        data.Set(1, row.ModelBody | (row.DyeBody.Row << 24));
        data.Set(2, row.ModelHands | (row.DyeHands.Row << 24));
        data.Set(3, row.ModelLegs | (row.DyeLegs.Row << 24));
        data.Set(4, row.ModelFeet | (row.DyeFeet.Row << 24));
        data.Set(5, row.ModelEars | (row.DyeEars.Row << 24));
        data.Set(6, row.ModelNeck | (row.DyeNeck.Row << 24));
        data.Set(7, row.ModelWrists | (row.DyeWrists.Row << 24));
        data.Set(8, row.ModelRightRing | (row.DyeRightRing.Row << 24));
        data.Set(9, row.ModelLeftRing | (row.DyeLeftRing.Row << 24));
        data.Mainhand     = new CharacterWeapon(row.ModelMainHand | ((ulong)row.DyeMainHand.Row << 48));
        data.Offhand      = new CharacterWeapon(row.ModelOffHand | ((ulong)row.DyeOffHand.Row << 48));
        data.VisorToggled = row.Visor;
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
        if (enpcBase.ModelChara.Value?.Type != 1)
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

    public IEnumerator<NpcData> GetEnumerator()
        => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _data.Count;

    public NpcData this[int index]
        => _data[index];
}
