using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public sealed class JobChangeState : Dictionary<FullEquipType, (EquipItem, StateSource)>, IService
{
    public ActorState? State { get; private set; }

    public void Reset()
    {
        State = null;
        Clear();
    }

    public bool HasState
        => State != null;

    public ActorIdentifier Identifier
        => State?.Identifier ?? ActorIdentifier.Invalid;

    public void Set(ActorState state, IEnumerable<(EquipItem, StateSource)> items)
    {
        foreach (var (item, source) in items.Where(p => p.Item1.Valid))
            TryAdd(item.Type, (item, source));
        State = state;
    }
}
