using Glamourer.Interop;
using Glamourer.Structs;

namespace Glamourer.Fixed;

public struct FixedCondition
{
    private const ulong _territoryFlag = 1ul << 32;
    private const ulong _jobFlag       = 1ul << 33;
    private       ulong _data;

    public static FixedCondition TerritoryCondition(ushort territoryType)
        => new() { _data = territoryType | _territoryFlag };

    public static FixedCondition JobCondition(JobGroup group)
        => new() { _data = group.Id | _jobFlag };

    public bool Check(Actor actor)
    {
        //if ((_data & (_territoryFlag | _jobFlag)) == 0)
        //    return true;
        //
        //if ((_data & _territoryFlag) != 0)
        //    return Dalamud.ClientState.TerritoryType == (ushort)_data;
        //
        //if (actor && GameData.JobGroups(Dalamud.GameData).TryGetValue((ushort)_data, out var group) && group.Fits(actor.Job))
        //    return true;
        //
        return true;
    }

    public override string ToString()
        => _data.ToString();
}
