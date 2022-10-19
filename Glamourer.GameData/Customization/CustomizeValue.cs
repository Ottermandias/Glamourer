namespace Glamourer.Customization;

public record struct CustomizeValue(byte Value)
{
    public static readonly CustomizeValue Zero = new(0);
    public static readonly CustomizeValue Max  = new(0xFF);

    public static CustomizeValue Bool(bool b)
        => b ? Max : Zero;

    public static explicit operator CustomizeValue(byte value)
        => new(value);

    public static CustomizeValue operator ++(CustomizeValue v)
        => new(++v.Value);

    public static CustomizeValue operator --(CustomizeValue v)
        => new(--v.Value);

    public static bool operator <(CustomizeValue v, int count)
        => v.Value < count;

    public static bool operator >(CustomizeValue v, int count)
        => v.Value > count;

    public static CustomizeValue operator +(CustomizeValue v, int rhs)
        => new((byte)(v.Value + rhs));

    public static CustomizeValue operator -(CustomizeValue v, int rhs)
        => new((byte)(v.Value - rhs));

    public override string ToString()
        => Value.ToString();
}
