using Penumbra.GameData.Enums;

namespace Glamourer.State;

public enum StateSource : byte
{
    Game,
    Manual,
    Fixed,
    IpcFixed,
    IpcManual,

    // Only used for CustomizeParameters and advanced dyes.
    Pending,
    IpcPending,
}

public static class StateSourceExtensions
{
    public static StateSource Base(this StateSource source)
        => source switch
        {
            StateSource.Manual or StateSource.Pending       => StateSource.Manual,
            StateSource.IpcManual or StateSource.IpcPending => StateSource.Manual,
            StateSource.Fixed or StateSource.IpcFixed       => StateSource.Fixed,
            _                                               => StateSource.Game,
        };

    public static bool IsGame(this StateSource source)
        => source.Base() is StateSource.Game;

    public static bool IsManual(this StateSource source)
        => source.Base() is StateSource.Manual;

    public static bool IsFixed(this StateSource source)
        => source.Base() is StateSource.Fixed;

    public static StateSource SetPending(this StateSource source)
        => source switch
        {
            StateSource.Manual    => StateSource.Pending,
            StateSource.IpcManual => StateSource.IpcPending,
            _                     => source,
        };

    public static bool RequiresChange(this StateSource source)
        => source switch
        {
            StateSource.Manual    => true,
            StateSource.IpcFixed  => true,
            StateSource.IpcManual => true,
            _                     => false,
        };

    public static bool IsIpc(this StateSource source)
        => source is StateSource.IpcManual or StateSource.IpcFixed or StateSource.IpcPending;
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
                case (byte)StateSource.IpcFixed | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.Pending | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.IpcPending | ((byte)StateSource.Fixed << 4):
                case (byte)StateSource.IpcManual | ((byte)StateSource.Fixed << 4):
                    _data[i] = (byte)((value & 0x0F) | ((byte)StateSource.Manual << 4));
                    break;
                case (byte)StateSource.Fixed:
                case ((byte)StateSource.Manual << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.IpcFixed << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.Pending << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.IpcPending << 4) | (byte)StateSource.Fixed:
                case ((byte)StateSource.IpcManual << 4) | (byte)StateSource.Fixed:
                    _data[i] = (byte)((value & 0xF0) | (byte)StateSource.Manual);
                    break;
            }
        }
    }
}
