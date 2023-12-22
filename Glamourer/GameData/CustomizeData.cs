using System;
using System.Runtime.InteropServices;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.GameData;

// Any customization value can be represented in 8 bytes by its ID,
// a byte value, an optional value-id and an optional icon or color.
[StructLayout(LayoutKind.Explicit)]
public readonly struct CustomizeData : IEquatable<CustomizeData>
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
        Index       = index;
        Value       = value;
        IconId      = data;
        Color       = data;
        CustomizeId = customizeId;
    }

    public bool Equals(CustomizeData other)
        => Index == other.Index
         && Value.Value == other.Value.Value
         && CustomizeId == other.CustomizeId;

    public override bool Equals(object? obj)
        => obj is CustomizeData other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine((int)Index, Value.Value, CustomizeId);
}
