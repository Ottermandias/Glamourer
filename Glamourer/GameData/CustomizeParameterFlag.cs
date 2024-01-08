namespace Glamourer.GameData;

[Flags]
public enum CustomizeParameterFlag : ushort
{
    SkinDiffuse           = 0x0001,
    MuscleTone            = 0x0002,
    SkinSpecular          = 0x0004,
    LipDiffuse            = 0x0008,
    LipOpacity            = 0x0010,
    HairDiffuse           = 0x0020,
    HairSpecular          = 0x0040,
    HairHighlight         = 0x0080,
    LeftEye               = 0x0100,
    RightEye              = 0x0200,
    FeatureColor          = 0x0400,
    FacePaintUvMultiplier = 0x0800,
    FacePaintUvOffset     = 0x1000,
}

public static class CustomizeParameterExtensions
{
    public const CustomizeParameterFlag All = (CustomizeParameterFlag)0x1FFF;

    public const CustomizeParameterFlag Triples = All
      & ~(CustomizeParameterFlag.MuscleTone
          | CustomizeParameterFlag.LipOpacity
          | CustomizeParameterFlag.FacePaintUvOffset
          | CustomizeParameterFlag.FacePaintUvMultiplier);

    public const CustomizeParameterFlag Percentages = CustomizeParameterFlag.MuscleTone | CustomizeParameterFlag.LipOpacity;
    public const CustomizeParameterFlag Values      = CustomizeParameterFlag.FacePaintUvOffset | CustomizeParameterFlag.FacePaintUvMultiplier;

    public static readonly IReadOnlyList<CustomizeParameterFlag> AllFlags        = [.. Enum.GetValues<CustomizeParameterFlag>()];
    public static readonly IReadOnlyList<CustomizeParameterFlag> TripleFlags     = AllFlags.Where(f => Triples.HasFlag(f)).ToArray();
    public static readonly IReadOnlyList<CustomizeParameterFlag> PercentageFlags = AllFlags.Where(f => Percentages.HasFlag(f)).ToArray();
    public static readonly IReadOnlyList<CustomizeParameterFlag> ValueFlags      = AllFlags.Where(f => Values.HasFlag(f)).ToArray();

    public static int Count(this CustomizeParameterFlag flag)
        => Triples.HasFlag(flag) ? 3 : 1;

    public static IEnumerable<CustomizeParameterFlag> Iterate(this CustomizeParameterFlag flags)
        => AllFlags.Where(f => flags.HasFlag(f));

    public static int ToInternalIndex(this CustomizeParameterFlag flag)
        => BitOperations.TrailingZeroCount((uint)flag);
}
