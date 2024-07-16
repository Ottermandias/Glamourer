namespace Glamourer.GameData;

[Flags]
public enum CustomizeParameterFlag : ushort
{
    SkinDiffuse           = 0x0001,
    MuscleTone            = 0x0002,
    SkinSpecular          = 0x0004,
    LipDiffuse            = 0x0008,
    HairDiffuse           = 0x0010,
    HairSpecular          = 0x0020,
    HairHighlight         = 0x0040,
    LeftEye               = 0x0080,
    RightEye              = 0x0100,
    FeatureColor          = 0x0200,
    FacePaintUvMultiplier = 0x0400,
    FacePaintUvOffset     = 0x0800,
    DecalColor            = 0x1000,
    LeftScleraIntensity   = 0x2000,
    RightScleraIntensity  = 0x4000,
}

public static class CustomizeParameterExtensions
{
    // Speculars are not available anymore.
    public const CustomizeParameterFlag All = (CustomizeParameterFlag)0x1FDB;

    public const CustomizeParameterFlag RgbTriples = All
      & ~(RgbaQuadruples | Percentages | Values);

    public const CustomizeParameterFlag RgbaQuadruples = CustomizeParameterFlag.DecalColor | CustomizeParameterFlag.LipDiffuse;

    public const CustomizeParameterFlag Percentages = CustomizeParameterFlag.MuscleTone
      | CustomizeParameterFlag.LeftScleraIntensity
      | CustomizeParameterFlag.RightScleraIntensity;

    public const CustomizeParameterFlag Values = CustomizeParameterFlag.FacePaintUvOffset | CustomizeParameterFlag.FacePaintUvMultiplier;

    public static readonly IReadOnlyList<CustomizeParameterFlag> AllFlags        = [.. Enum.GetValues<CustomizeParameterFlag>()];
    public static readonly IReadOnlyList<CustomizeParameterFlag> RgbaFlags       = AllFlags.Where(f => RgbaQuadruples.HasFlag(f)).ToArray();
    public static readonly IReadOnlyList<CustomizeParameterFlag> RgbFlags        = AllFlags.Where(f => RgbTriples.HasFlag(f)).ToArray();
    public static readonly IReadOnlyList<CustomizeParameterFlag> PercentageFlags = AllFlags.Where(f => Percentages.HasFlag(f)).ToArray();
    public static readonly IReadOnlyList<CustomizeParameterFlag> ValueFlags      = AllFlags.Where(f => Values.HasFlag(f)).ToArray();

    public static int Count(this CustomizeParameterFlag flag)
        => RgbaQuadruples.HasFlag(flag) ? 4 : RgbTriples.HasFlag(flag) ? 3 : 1;

    public static IEnumerable<CustomizeParameterFlag> Iterate(this CustomizeParameterFlag flags)
        => AllFlags.Where(f => flags.HasFlag(f));

    public static int ToInternalIndex(this CustomizeParameterFlag flag)
        => BitOperations.TrailingZeroCount((uint)flag);

    public static string ToName(this CustomizeParameterFlag flag)
        => flag switch
        {
            CustomizeParameterFlag.SkinDiffuse           => "Skin Color",
            CustomizeParameterFlag.MuscleTone            => "Muscle Tone",
            CustomizeParameterFlag.SkinSpecular          => "Skin Shine",
            CustomizeParameterFlag.LipDiffuse            => "Lip Color",
            CustomizeParameterFlag.HairDiffuse           => "Hair Color",
            CustomizeParameterFlag.HairSpecular          => "Hair Shine",
            CustomizeParameterFlag.HairHighlight         => "Hair Highlights",
            CustomizeParameterFlag.LeftEye               => "Left Eye Color",
            CustomizeParameterFlag.RightEye              => "Right Eye Color",
            CustomizeParameterFlag.FeatureColor          => "Tattoo Color",
            CustomizeParameterFlag.FacePaintUvMultiplier => "Multiplier for Face Paint",
            CustomizeParameterFlag.FacePaintUvOffset     => "Offset of Face Paint",
            CustomizeParameterFlag.DecalColor            => "Face Paint Color",
            CustomizeParameterFlag.LeftScleraIntensity   => "Left Sclera Intensity",
            CustomizeParameterFlag.RightScleraIntensity  => "Right Sclera Intensity",
            _                                            => string.Empty,
        };
}
