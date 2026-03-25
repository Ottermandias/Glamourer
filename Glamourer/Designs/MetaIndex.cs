using Glamourer.Api.Enums;
using Glamourer.State;
using Luna.Generators;

namespace Glamourer.Designs;

[NamedEnum]
[TooltipEnum]
public enum MetaIndex
{
    [Name("Force Wetness")]
    [Tooltip("Force the character to be wet or not.")]
    Wetness = StateIndex.MetaWetness,

    [Name("Hat Visible")]
    [Tooltip("Hide or show the characters head gear.")]
    HatState = StateIndex.MetaHatState,

    [Name("Visor Toggled")]
    [Tooltip("Toggle the visor state of the characters head gear.")]
    VisorState = StateIndex.MetaVisorState,

    [Name("Weapon Visible")]
    [Tooltip("Hide or show the characters weapons when not drawn.")]
    WeaponState = StateIndex.MetaWeaponState,
    ModelId = StateIndex.MetaModelId,

    [Name("Ears Visible")]
    [Tooltip("Hide or show the characters ears through the head gear. (Viera only)")]
    EarState = StateIndex.MetaEarState,
}

public static partial class MetaExtensions
{
    public static readonly IReadOnlyList<MetaIndex> AllRelevant =
        [MetaIndex.Wetness, MetaIndex.HatState, MetaIndex.VisorState, MetaIndex.WeaponState, MetaIndex.EarState];

    public const MetaFlag All = MetaFlag.Wetness | MetaFlag.HatState | MetaFlag.VisorState | MetaFlag.WeaponState | MetaFlag.EarState;

    public static MetaFlag ToFlag(this MetaIndex index)
        => index switch
        {
            MetaIndex.Wetness     => MetaFlag.Wetness,
            MetaIndex.HatState    => MetaFlag.HatState,
            MetaIndex.VisorState  => MetaFlag.VisorState,
            MetaIndex.WeaponState => MetaFlag.WeaponState,
            MetaIndex.EarState    => MetaFlag.EarState,
            _                     => (MetaFlag)byte.MaxValue,
        };

    public static MetaIndex ToIndex(this MetaFlag index)
        => index switch
        {
            MetaFlag.Wetness     => MetaIndex.Wetness,
            MetaFlag.HatState    => MetaIndex.HatState,
            MetaFlag.VisorState  => MetaIndex.VisorState,
            MetaFlag.WeaponState => MetaIndex.WeaponState,
            MetaFlag.EarState    => MetaIndex.EarState,
            _                    => (MetaIndex)byte.MaxValue,
        };

    public static IEnumerable<MetaIndex> ToIndices(this MetaFlag index)
    {
        if (index.HasFlag(MetaFlag.Wetness))
            yield return MetaIndex.Wetness;
        if (index.HasFlag(MetaFlag.HatState))
            yield return MetaIndex.HatState;
        if (index.HasFlag(MetaFlag.VisorState))
            yield return MetaIndex.VisorState;
        if (index.HasFlag(MetaFlag.WeaponState))
            yield return MetaIndex.WeaponState;
        if (index.HasFlag(MetaFlag.EarState))
            yield return MetaIndex.EarState;
    }
}
