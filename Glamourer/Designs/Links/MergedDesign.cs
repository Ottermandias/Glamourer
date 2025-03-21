﻿using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Links;

public readonly struct WeaponList
{
    private readonly Dictionary<FullEquipType, List<(EquipItem, StateSource, JobFlag)>> _list = new(4);

    public IEnumerable<(EquipItem, StateSource, JobFlag)> Values
        => _list.Values.SelectMany(t => t);

    public void Clear()
        => _list.Clear();

    public bool TryAdd(FullEquipType type, EquipItem item, StateSource source, JobFlag flags)
    {
        if (!_list.TryGetValue(type, out var list))
        {
            list = new List<(EquipItem, StateSource, JobFlag)>(2);
            _list.Add(type, list);
        }

        var existingFlags  = list.Count == 0 ? 0 : list.Select(t => t.Item3).Aggregate((t, existing) => t | existing);
        var remainingFlags = flags & ~existingFlags;

        if (remainingFlags == 0)
            return false;

        list.Add((item, source, remainingFlags));
        return true;
    }

    public bool TryGet(FullEquipType type, JobId id, bool gameStateAllowed, out (EquipItem, StateSource) ret)
    {
        if (!_list.TryGetValue(type, out var list))
        {
            ret = default;
            return false;
        }

        var flag = (JobFlag)(1ul << id.Id);

        foreach (var (item, source, flags) in list)
        {
            if (flags.HasFlag(flag) && (gameStateAllowed || source is not StateSource.Game))
            {
                ret = (item, source);
                return true;
            }
        }

        ret = default;
        return false;
    }

    public WeaponList()
    { }
}

public sealed class MergedDesign
{
    public MergedDesign(DesignManager designManager)
    {
        Design             = designManager.CreateTemporary();
        Design.Application = ApplicationCollection.None;
    }

    public MergedDesign(DesignBase design)
    {
        Design = design;
        if (design.DoApplyEquip(EquipSlot.MainHand))
        {
            var weapon = design.DesignData.Item(EquipSlot.MainHand);
            if (weapon.Valid)
                Weapons.TryAdd(weapon.Type, weapon, StateSource.Manual, JobFlag.All);
        }

        if (design.DoApplyEquip(EquipSlot.OffHand))
        {
            var weapon = design.DesignData.Item(EquipSlot.OffHand);
            if (weapon.Valid)
                Weapons.TryAdd(weapon.Type, weapon, StateSource.Manual, JobFlag.All);
        }

        ForcedRedraw = design is IDesignStandIn { ForcedRedraw: true };
    }

    public MergedDesign(Design design)
        : this((DesignBase)design)
    {
        foreach (var (mod, settings) in design.AssociatedMods)
            AssociatedMods[mod] = settings;
    }

    public readonly DesignBase                   Design;
    public readonly WeaponList                   Weapons        = new();
    public readonly SortedList<Mod, ModSettings> AssociatedMods = [];
    public          StateSources                 Sources        = new();
    public          bool                         ForcedRedraw;
    public          bool                         ResetAdvancedDyes;
    public          bool                         ResetTemporarySettings;
}
