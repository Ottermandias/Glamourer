using Glamourer.Api.Enums;
using Glamourer.State;

namespace Glamourer.Designs;

public enum MetaIndex
{
    Wetness     = StateIndex.MetaWetness,
    HatState    = StateIndex.MetaHatState,
    VisorState  = StateIndex.MetaVisorState,
    WeaponState = StateIndex.MetaWeaponState,
    ModelId     = StateIndex.MetaModelId,
    EarState    = StateIndex.MetaEarState,
}

public static class MetaExtensions
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

    public static string ToName(this MetaIndex index)
        => index switch
        {
            MetaIndex.HatState    => "Hat Visible",
            MetaIndex.VisorState  => "Visor Toggled",
            MetaIndex.WeaponState => "Weapon Visible",
            MetaIndex.Wetness     => "Force Wetness",
            MetaIndex.EarState    => "Ears Visible",
            _                     => "Unknown Meta",
        };

    public static string ToTooltip(this MetaIndex index)
        => index switch
        {
            MetaIndex.HatState    => "Hide or show the characters head gear.",
            MetaIndex.VisorState  => "Toggle the visor state of the characters head gear.",
            MetaIndex.WeaponState => "Hide or show the characters weapons when not drawn.",
            MetaIndex.Wetness     => "Force the character to be wet or not.",
            MetaIndex.EarState    => "Hide or show the characters ears through the head gear. (Viera only)",
            _                     => string.Empty,
        };
}
