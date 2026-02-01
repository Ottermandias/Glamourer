using ImSharp;
using Luna.Generators;

namespace Glamourer.GameData;

[Flags]
[NamedEnum(Utf16: false)]
public enum CustomizeParameterFlag : ushort
{
    [Name("Skin Color")]
    SkinDiffuse = 0x0001,

    [Name("Muscle Tone")]
    MuscleTone = 0x0002,

    [Name("Skin Shine")]
    SkinSpecular = 0x0004,

    [Name("Lip Color")]
    LipDiffuse = 0x0008,

    [Name("Hair Color")]
    HairDiffuse = 0x0010,

    [Name("Hair Shine")]
    HairSpecular = 0x0020,

    [Name("Hair Highlights")]
    HairHighlight = 0x0040,

    [Name("Left Eye Color")]
    LeftEye = 0x0080,

    [Name("Right Eye Color")]
    RightEye = 0x0100,

    [Name("Feature Color")]
    FeatureColor = 0x0200,

    [Name("Multiplier for Face Paint")]
    FacePaintUvMultiplier = 0x0400,

    [Name("Offset of Face Paint")]
    FacePaintUvOffset = 0x0800,

    [Name("Face Paint Color")]
    DecalColor = 0x1000,

    [Name("Left Limbal Ring Intensity")]
    LeftLimbalIntensity = 0x2000,

    [Name("Right Limbal Ring Intensity")]
    RightLimbalIntensity = 0x4000,
}

public static partial class CustomizeParameterExtensions
{
    // Speculars are not available anymore.
    public const CustomizeParameterFlag All = (CustomizeParameterFlag)0x7FDB;

    public const CustomizeParameterFlag RgbTriples = All
      & ~(RgbaQuadruples | Percentages | Values);

    public const CustomizeParameterFlag RgbaQuadruples = CustomizeParameterFlag.DecalColor | CustomizeParameterFlag.LipDiffuse;

    public const CustomizeParameterFlag Percentages = CustomizeParameterFlag.MuscleTone
      | CustomizeParameterFlag.LeftLimbalIntensity
      | CustomizeParameterFlag.RightLimbalIntensity;

    public const CustomizeParameterFlag Values = CustomizeParameterFlag.FacePaintUvOffset | CustomizeParameterFlag.FacePaintUvMultiplier;

    public static readonly IReadOnlyList<CustomizeParameterFlag> AllFlags =
        [.. CustomizeParameterFlag.Values.Where(f => All.HasFlag(f))];

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
}
