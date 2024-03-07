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
                Weapons.TryAdd(weapon.Type, (weapon, StateSource.Manual));
        }

        if (design.DoApplyEquip(EquipSlot.OffHand))
        {
            var weapon = design.DesignData.Item(EquipSlot.OffHand);
            if (weapon.Valid)
                Weapons.TryAdd(weapon.Type, (weapon, StateSource.Manual));
        }
    }

    public MergedDesign(Design design)
        : this((DesignBase)design)
    {
        foreach (var (mod, settings) in design.AssociatedMods)
            AssociatedMods[mod] = settings;
    }

    public readonly DesignBase                                          Design;
    public readonly Dictionary<FullEquipType, (EquipItem, StateSource)> Weapons        = new(4);
    public readonly SortedList<Mod, ModSettings>                        AssociatedMods = [];
    public          StateSources                                        Sources        = new();
}
