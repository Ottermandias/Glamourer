using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Designs.Links;

public sealed class MergedDesign
{
    public MergedDesign(DesignManager designManager)
    {
        Design                 = designManager.CreateTemporary();
        Design.ApplyEquip      = 0;
        Design.ApplyCustomize  = 0;
        Design.ApplyCrest      = 0;
        Design.ApplyParameters = 0;
        Design.ApplyMeta       = 0;
    }

    public MergedDesign(DesignBase design)
    {
        Design = design;
        if (design.DoApplyEquip(EquipSlot.MainHand))
        {
            var weapon = design.DesignData.Item(EquipSlot.MainHand);
            if (weapon.Valid)
                Weapons.TryAdd(weapon.Type, (weapon, StateChanged.Source.Manual));
        }

        if (design.DoApplyEquip(EquipSlot.OffHand))
        {
            var weapon = design.DesignData.Item(EquipSlot.OffHand);
            if (weapon.Valid)
                Weapons.TryAdd(weapon.Type, (weapon, StateChanged.Source.Manual));
        }
    }

    public MergedDesign(Design design)
        : this((DesignBase)design)
    {
        foreach (var (mod, settings) in design.AssociatedMods)
            AssociatedMods[mod] = settings;
    }

    public readonly DesignBase                                                  Design;
    public readonly Dictionary<FullEquipType, (EquipItem, StateChanged.Source)> Weapons        = new(4);
    public readonly StateSource                                                 Source         = new();
    public readonly SortedList<Mod, ModSettings>                                AssociatedMods = [];

    public StateChanged.Source GetSource(EquipSlot slot, bool stain, StateChanged.Source actualSource)
        => GetSource(Source[slot, stain], actualSource);

    public StateChanged.Source GetSource(CrestFlag slot, StateChanged.Source actualSource)
        => GetSource(Source[slot], actualSource);

    public StateChanged.Source GetSource(CustomizeIndex type, StateChanged.Source actualSource)
        => GetSource(Source[type], actualSource);

    public StateChanged.Source GetSource(MetaIndex index, StateChanged.Source actualSource)
        => GetSource(Source[index], actualSource);

    public StateChanged.Source GetSource(CustomizeParameterFlag flag, StateChanged.Source actualSource)
        => GetSource(Source[flag], actualSource);

    public static StateChanged.Source GetSource(StateChanged.Source given, StateChanged.Source actualSource)
        => given is StateChanged.Source.Game ? StateChanged.Source.Game : actualSource;
}
