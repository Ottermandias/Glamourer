using System;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.GameData;

namespace Glamourer.Customization;

public static class CustomizationNpcOptions
{
    public unsafe struct NpcData
    {
        public        string          Name;
        public        Customize       Customize;
        private fixed byte            _equip[40];
        public        CharacterWeapon Mainhand;
        public        CharacterWeapon Offhand;
        public        uint            Id;
        public        bool            VisorToggled;
        public        ObjectKind      Kind;

        public ReadOnlySpan<CharacterArmor> Equip
        {
            get
            {
                fixed (byte* ptr = _equip)
                {
                    return new ReadOnlySpan<CharacterArmor>((CharacterArmor*)ptr, 10);
                }
            }
        }

        public string WriteGear()
        {
            var sb   = new StringBuilder(128);
            var span = Equip;
            for (var i = 0; i < 10; ++i)
            {
                sb.Append(span[i].Set.Id.ToString("D4"));
                sb.Append('-');
                sb.Append(span[i].Variant.Id.ToString("D3"));
                sb.Append('-');
                sb.Append(span[i].Stain.Id.ToString("D3"));
                sb.Append(",  ");
            }

            sb.Append(Mainhand.Set.Id.ToString("D4"));
            sb.Append('-');
            sb.Append(Mainhand.Type.Id.ToString("D4"));
            sb.Append('-');
            sb.Append(Mainhand.Variant.Id.ToString("D3"));
            sb.Append('-');
            sb.Append(Mainhand.Stain.Id.ToString("D4"));
            sb.Append(",  ");
            sb.Append(Offhand.Set.Id.ToString("D4"));
            sb.Append('-');
            sb.Append(Offhand.Type.Id.ToString("D4"));
            sb.Append('-');
            sb.Append(Offhand.Variant.Id.ToString("D3"));
            sb.Append('-');
            sb.Append(Offhand.Stain.Id.ToString("D3"));
            return sb.ToString();
        }

        internal void Set(int idx, uint value)
        {
            fixed (byte* ptr = _equip)
            {
                ((uint*)ptr)[idx] = value;
            }
        }

        public bool DataEquals(in NpcData other)
        {
            if (VisorToggled != other.VisorToggled)
                return false;

            if (!Customize.Equals(other.Customize))
                return false;

            if (!Mainhand.Equals(other.Mainhand))
                return false;

            if (!Offhand.Equals(other.Offhand))
                return false;

            fixed (byte* ptr1 = _equip, ptr2 = other._equip)
            {
                return new ReadOnlySpan<byte>(ptr1, 40).SequenceEqual(new ReadOnlySpan<byte>(ptr2, 40));
            }
        }
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

    public static unsafe IReadOnlyList<NpcData> CreateNpcData(IReadOnlyDictionary<uint, string> eNpcs,
        IReadOnlyDictionary<uint, string> bnpcNames, IObjectIdentifier identifier, IDataManager data)
    {
        var enpcSheet = data.GetExcelSheet<ENpcBase>()!;
        var bnpcSheet = data.GetExcelSheet<BNpcBase>()!;
        var list      = new List<NpcData>(eNpcs.Count + (int)bnpcSheet.RowCount);
        foreach (var (id, name) in eNpcs)
        {
            var row = enpcSheet.GetRow(id);
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

        foreach (var baseRow in bnpcSheet)
        {
            if (baseRow.ModelChara.Value!.Type != 1)
                continue;

            var bnpcNameIds = identifier.GetBnpcNames(baseRow.RowId);
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
                if (bnpcNames.TryGetValue(bnpcNameId.Id, out var name) && !name.IsNullOrWhitespace())
                    list.Add(ret with { Name = name });
            }
        }

        var groups = list.GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.ToList());
        list.Clear();
        foreach (var (name, duplicates) in groups.OrderBy(kvp => kvp.Key))
        {
            for (var i = 0; i < duplicates.Count; ++i)
            {
                var current = duplicates[i];
                var add     = true;
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
                list.Add(duplicates[0]);
            else
                list.AddRange(duplicates
                    .Select(duplicate => duplicate with { Name = $"{name} ({(duplicate.Kind is ObjectKind.BattleNpc ? 'B' : 'E')}{duplicate.Id})" }));
        }

        var lastWeird = list.FindIndex(d => char.IsAsciiLetterOrDigit(d.Name[0]));
        if (lastWeird != -1)
        {
            list.AddRange(list.Take(lastWeird));
            list.RemoveRange(0, lastWeird);
        }

        return list;
    }


    public static Dictionary<(SubRace, Gender), IReadOnlyList<(CustomizeIndex, CustomizeValue)>> CreateNpcData(CustomizationSet[] sets,
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

        return dict.ToDictionary(kvp => kvp.Key,
            kvp => (IReadOnlyList<(CustomizeIndex, CustomizeValue)>)kvp.Value.OrderBy(p => p.Item1).ThenBy(p => p.Item2.Value).ToArray());
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
}
