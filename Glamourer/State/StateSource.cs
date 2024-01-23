using Penumbra.GameData.Enums;

namespace Glamourer.State;

public enum StateSource : byte
{
    Game,
    Manual,
    Fixed,
    Ipc,

    // Only used for CustomizeParameters.
    Pending,
}

public unsafe struct StateSources
{
    public const  int  Size = (StateIndex.Size + 1) / 2;
    private fixed byte _data[Size];


    public StateSources()
    { }

    public StateSource this[StateIndex index]
    {
        get
        {
            var val = _data[index.Value / 2];
            return (StateSource)((index.Value & 1) == 1 ? val >> 4 : val & 0x0F);
        }
        set
        {
            var val = _data[index.Value / 2];
            if ((index.Value & 1) == 1)
                val = (byte)((val & 0x0F) | ((byte)value << 4));
            else
                val = (byte)((val & 0xF0) | (byte)value);
            _data[index.Value / 2] = val;
        }
    }

    public StateSource this[EquipSlot slot, bool stain]
    {
        get => this[slot.ToState(stain)];
        set => this[slot.ToState(stain)] = value;
    }

    public void RemoveFixedDesignSources()
    {
        for (var i = 0; i < Size; ++i)
        {
            var value = _data[i];
            switch (value)
            {
                case (byte)StateSource.Fixed | ((byte)StateSource.Fixed << 4):
                    _data[i] = (byte)StateSource.Manual | ((byte)StateSource.Manual << 4);
                    break;

                case (byte)StateSource.Game | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.Manual | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.Ipc | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.Pending | ((byte)StateSource.Fixed << 4):
                    _data[i] = (byte)((value & 0x0F) | ((byte)StateSource.Manual << 4));
                    break;
                case (byte)StateSource.Fixed:
                case ((byte)StateSource.Manual << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.Ipc << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.Pending << 4) | (byte)StateSource.Fixed:
                    _data[i] = (byte)((value & 0xF0) | (byte)StateSource.Manual);
                    break;
            }
        }
    }
}
