using System.Runtime.InteropServices;

namespace Glamourer.Customization;

// Any customization value can be represented in 8 bytes by its ID,
// a byte value, an optional value-id and an optional icon or color.
[StructLayout(LayoutKind.Explicit)]
public readonly struct CustomizeData
{
    [FieldOffset(0)]
    public readonly CustomizeIndex Index;

    [FieldOffset(1)]
    public readonly CustomizeValue Value;

    [FieldOffset(2)]
    public readonly ushort CustomizeId;

    [FieldOffset(4)]
    public readonly uint IconId;

    [FieldOffset(4)]
    public readonly uint Color;

    public CustomizeData(CustomizeIndex index, CustomizeValue value, uint data = 0, ushort customizeId = 0)
    {
        Index          = index;
        Value       = value;
        IconId      = data;
        Color       = data;
        CustomizeId = customizeId;
    }
}
