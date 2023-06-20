using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Structs;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using System.Linq;
using CustomizeIndex = Glamourer.Customization.CustomizeIndex;

namespace Glamourer.State;

public class ActorState
{
    public enum MetaFlag
    {
        Wetness = EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices,
        HatState,
        VisorState,
        WeaponState,
    }

    public ActorIdentifier Identifier { get; internal init; }
    public DesignData      ActorData;
    public DesignData      ModelData;

    private readonly StateChanged.Source[] _sources = Enumerable
        .Repeat(StateChanged.Source.Game, EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices + 4).ToArray();

    internal ActorState(ActorIdentifier identifier)
        => Identifier = identifier;

    public ref StateChanged.Source this[EquipSlot slot, bool stain]
        => ref _sources[slot.ToIndex() + (stain ? EquipFlagExtensions.NumEquipFlags / 2 : 0)];

    public ref StateChanged.Source this[CustomizeIndex type]
        => ref _sources[EquipFlagExtensions.NumEquipFlags + (int)type];

    public ref StateChanged.Source this[MetaFlag flag]
        => ref _sources[(int)flag];
}
