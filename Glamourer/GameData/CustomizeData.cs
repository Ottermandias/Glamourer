using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.GameData;

/// <summary>
/// Any customization value can be represented in 8 bytes by its ID,
/// a byte value, an optional value-id and an optional icon or color.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct CustomizeData : IEquatable<CustomizeData>
{
    /// <summary> The index of the option this value is for. </summary>
    [FieldOffset(0)]
    public readonly CustomizeIndex Index;

    /// <summary> The value for the option. </summary>
    [FieldOffset(1)]
    public readonly CustomizeValue Value;

    /// <summary> The internal ID for sheets. </summary>
    [FieldOffset(2)]
    public readonly ushort CustomizeId;

    /// <summary> An ID for an associated icon. </summary>
    [FieldOffset(4)]
    public readonly uint IconId;

    /// <summary> An ID for an associated color. </summary>
    [FieldOffset(4)]
    public readonly Rgba32 Color;

    /// <summary> Construct a CustomizeData from single data values. </summary>
    public CustomizeData(CustomizeIndex index, CustomizeValue value, uint data = 0, ushort customizeId = 0)
    {
        Index       = index;
        Value       = value;
        IconId      = data;
        Color       = data;
        CustomizeId = customizeId;
    }

    /// <inheritdoc/>
    public bool Equals(CustomizeData other)
        => Index == other.Index
         && Value.Value == other.Value.Value
         && CustomizeId == other.CustomizeId;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is CustomizeData other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine((int)Index, Value.Value, CustomizeId);

    public static bool operator ==(CustomizeData left, CustomizeData right)
        => left.Equals(right);

    public static bool operator !=(CustomizeData left, CustomizeData right)
        => !(left == right);
}
