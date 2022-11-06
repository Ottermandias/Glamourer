using System;

namespace Glamourer.Saves;

[Flags]
public enum DesignFlagsV1 : byte
{
    VisorState       = 0x01,
    VisorApply       = 0x02,
    WeaponStateShown = 0x04,
    WeaponStateApply = 0x08,
    WetnessState     = 0x10,
    WetnessApply     = 0x20,
    ReadOnly         = 0x40,
}
