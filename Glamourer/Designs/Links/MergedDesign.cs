using Glamourer.Events;
using Glamourer.GameData;
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
        Design.SetApplyWetness(false);
        Design.SetApplyVisorToggle(false);
        Design.SetApplyWeaponVisible(false);
        Design.SetApplyHatVisible(false);
    }

    public readonly DesignBase                                                  Design;
    public readonly Dictionary<FullEquipType, (EquipItem, StateChanged.Source)> Weapons = new(4);
    public readonly StateSource                                                 Source  = new();

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
