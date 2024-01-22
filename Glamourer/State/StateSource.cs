using Glamourer.GameData;
using Penumbra.GameData.Enums;
using static Glamourer.Events.StateChanged;

namespace Glamourer.State;

public readonly struct StateSource
{
    public static readonly int Size = EquipFlagExtensions.NumEquipFlags
      + CustomizationExtensions.NumIndices
      + 5
      + CrestExtensions.AllRelevantSet.Count
      + CustomizeParameterExtensions.AllFlags.Count;


    private readonly Source[] _data = Enumerable.Repeat(Source.Game, Size).ToArray();

    public StateSource()
    { }

    public ref Source this[EquipSlot slot, bool stain]
        => ref _data[slot.ToIndex() + (stain ? EquipFlagExtensions.NumEquipFlags / 2 : 0)];

    public ref Source this[CrestFlag slot]
        => ref _data[EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices + 5 + slot.ToInternalIndex()];

    public ref Source this[CustomizeIndex type]
        => ref _data[EquipFlagExtensions.NumEquipFlags + (int)type];

    public ref Source this[MetaIndex index]
        => ref _data[(int)index];

    public ref Source this[CustomizeParameterFlag flag]
        => ref _data[
            EquipFlagExtensions.NumEquipFlags
          + CustomizationExtensions.NumIndices
          + 5
          + CrestExtensions.AllRelevantSet.Count
          + flag.ToInternalIndex()];

    public void RemoveFixedDesignSources()
    {
        for (var i = 0; i < _data.Length; ++i)
        {
            if (_data[i] is Source.Fixed)
                _data[i] = Source.Manual;
        }
    }
}
