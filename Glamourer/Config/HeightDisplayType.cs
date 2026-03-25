using Luna.Generators;

namespace Glamourer.Config;

[TooltipEnum]
public enum HeightDisplayType
{
    [Tooltip("Do Not Display")]
    None,

    [Tooltip("Centimetres (000.0 cm)")]
    Centimetre,

    [Tooltip("Metres (0.00 m)")]
    Metre,

    [Tooltip("Inches (00.0 in)")]
    Wrong,

    [Tooltip("Feet (0'00'')")]
    WrongFoot,

    [Tooltip("Corgis (0.0 Corgis)")]
    Corgi,

    [Tooltip("Olympic-size swimming Pools (0.000 Pools)")]
    OlympicPool,
}
