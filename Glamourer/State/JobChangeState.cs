using Glamourer.Designs.Links;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public sealed class JobChangeState : IService
{
    private readonly WeaponList _weaponList = new();

    public ActorState? State { get; private set; }

    public void Reset()
    {
        State = null;
        _weaponList.Clear();
    }

    public bool HasState
        => State != null;

    public ActorIdentifier Identifier
        => State?.Identifier ?? ActorIdentifier.Invalid;

    public bool TryGetValue(FullEquipType slot, JobId jobId, out (EquipItem, StateSource) data)
        => _weaponList.TryGet(slot, jobId, out data);

    public void Set(ActorState state, IEnumerable<(EquipItem, StateSource, JobFlag)> items)
    {
        foreach (var (item, source, flags) in items.Where(p => p.Item1.Valid))
            _weaponList.TryAdd(item.Type, item, source, flags);
        State = state;
    }
}
