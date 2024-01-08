using Glamourer.Designs;
using Glamourer.Events;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Glamourer.GameData;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class ActorState
{
    public enum MetaIndex
    {
        Wetness = EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices,
        HatState,
        VisorState,
        WeaponState,
        ModelId,
    }

    public readonly ActorIdentifier Identifier;

    public bool AllowsRedraw(ICondition condition)
        => Identifier.Type is not IdentifierType.Special && !condition[ConditionFlag.OccupiedInCutSceneEvent];

    /// <summary> This should always represent the unmodified state of the draw object. </summary>
    public DesignData BaseData;

    /// <summary> This should be the desired state of the draw object. </summary>
    public DesignData ModelData;

    /// <summary> The last seen job. </summary>
    public JobId LastJob;

    /// <summary> The Lock-Key locking this state. </summary>
    public uint Combination;

    /// <summary> The territory the draw object was created last. </summary>
    public ushort LastTerritory;

    /// <summary> Whether the State is locked at all. </summary>
    public bool IsLocked
        => Combination != 0;

    /// <summary> Whether the given key can open the lock. </summary>
    public bool CanUnlock(uint key)
        => !IsLocked || Combination == key;

    /// <summary> Lock the current state for further manipulations. </summary>
    public bool Lock(uint combination)
    {
        if (combination == 0)
            return false;
        if (Combination != 0)
            return Combination == combination;

        Combination = combination;
        return true;
    }

    /// <summary> Unlock the current state. </summary>
    public bool Unlock(uint key)
    {
        if (key == Combination)
            Combination = 0;
        return !IsLocked;
    }

    /// <summary> Lock for temporary changes until after redrawing. </summary>
    public bool TempLock()
        => Lock(1337);

    /// <summary> Unlock temp locks. </summary>
    public bool TempUnlock()
        => Unlock(1337);

    /// <summary> This contains whether a change to the base data was made by the game, the user via manual input or through automatic application. </summary>
    private readonly StateChanged.Source[] _sources = Enumerable
        .Repeat(StateChanged.Source.Game,
            EquipFlagExtensions.NumEquipFlags
          + CustomizationExtensions.NumIndices
          + 5
          + CrestExtensions.AllRelevantSet.Count
          + CustomizeParameterExtensions.AllFlags.Count).ToArray();

    internal ActorState(ActorIdentifier identifier)
        => Identifier = identifier.CreatePermanent();

    public ref StateChanged.Source this[EquipSlot slot, bool stain]
        => ref _sources[slot.ToIndex() + (stain ? EquipFlagExtensions.NumEquipFlags / 2 : 0)];

    public ref StateChanged.Source this[CrestFlag slot]
        => ref _sources[EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices + 5 + slot.ToInternalIndex()];

    public ref StateChanged.Source this[CustomizeIndex type]
        => ref _sources[EquipFlagExtensions.NumEquipFlags + (int)type];

    public ref StateChanged.Source this[MetaIndex index]
        => ref _sources[(int)index];

    public ref StateChanged.Source this[CustomizeParameterFlag flag]
        => ref _sources[
            EquipFlagExtensions.NumEquipFlags
          + CustomizationExtensions.NumIndices + 5
          + CrestExtensions.AllRelevantSet.Count
          + flag.ToInternalIndex()];

    public void RemoveFixedDesignSources()
    {
        for (var i = 0; i < _sources.Length; ++i)
        {
            if (_sources[i] is StateChanged.Source.Fixed)
                _sources[i] = StateChanged.Source.Manual;
        }
    }

    public CustomizeParameterFlag OnlyChangedParameters()
        => CustomizeParameterExtensions.AllFlags.Where(f => this[f] is not StateChanged.Source.Game).Aggregate((CustomizeParameterFlag) 0, (a, b) => a | b);

    public bool UpdateTerritory(ushort territory)
    {
        if (territory == LastTerritory)
            return false;

        LastTerritory = territory;
        return true;
    }
}
