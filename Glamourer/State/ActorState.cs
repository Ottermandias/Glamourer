using Glamourer.Designs;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Glamourer.GameData;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class ActorState
{
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
    public StateSources Sources = new();

    internal ActorState(ActorIdentifier identifier)
        => Identifier = identifier.CreatePermanent();

    public CustomizeParameterFlag OnlyChangedParameters()
        => CustomizeParameterExtensions.AllFlags.Where(f => Sources[f] is not StateSource.Game)
            .Aggregate((CustomizeParameterFlag)0, (a, b) => a | b);

    public bool UpdateTerritory(ushort territory)
    {
        if (territory == LastTerritory)
            return false;

        LastTerritory = territory;
        return true;
    }
}