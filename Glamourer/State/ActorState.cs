using System;
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
        ModelId,
    }

    public readonly ActorIdentifier Identifier;

    /// <summary> This should always represent the unmodified state of the draw object. </summary>
    public DesignData BaseData;

    /// <summary> This should be the desired state of the draw object. </summary>
    public DesignData ModelData;

    /// <summary> The last seen job. </summary>
    public byte LastJob;

    /// <summary> This contains whether a change to the base data was made by the game, the user via manual input or through automatic application. </summary>
    private readonly StateChanged.Source[] _sources = Enumerable
        .Repeat(StateChanged.Source.Game, EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices + 5).ToArray();

    internal ActorState(ActorIdentifier identifier)
        => Identifier = identifier.CreatePermanent();

    public ref StateChanged.Source this[EquipSlot slot, bool stain]
        => ref _sources[slot.ToIndex() + (stain ? EquipFlagExtensions.NumEquipFlags / 2 : 0)];

    public ref StateChanged.Source this[CustomizeIndex type]
        => ref _sources[EquipFlagExtensions.NumEquipFlags + (int)type];

    public ref StateChanged.Source this[MetaFlag flag]
        => ref _sources[(int)flag];
}
