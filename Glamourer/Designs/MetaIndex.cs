﻿using Glamourer.Api.Enums;
using Glamourer.State;

namespace Glamourer.Designs;

public enum MetaIndex
{
    Wetness     = StateIndex.MetaWetness,
    HatState    = StateIndex.MetaHatState,
    VisorState  = StateIndex.MetaVisorState,
    WeaponState = StateIndex.MetaWeaponState,
    ModelId     = StateIndex.MetaModelId,
}

[Flags]
public enum MetaFlag : byte
{
    Wetness     = 0x01,
    HatState    = 0x02,
    VisorState  = 0x04,
    WeaponState = 0x08,
}

public static class MetaExtensions
{
    public static readonly IReadOnlyList<MetaIndex> AllRelevant =
        [MetaIndex.Wetness, MetaIndex.HatState, MetaIndex.VisorState, MetaIndex.WeaponState];

    public const MetaFlag All = MetaFlag.Wetness | MetaFlag.HatState | MetaFlag.VisorState | MetaFlag.WeaponState;

    public static MetaFlag ToFlag(this MetaIndex index)
        => index switch
        {
            MetaIndex.Wetness     => MetaFlag.Wetness,
            MetaIndex.HatState    => MetaFlag.HatState,
            MetaIndex.VisorState  => MetaFlag.VisorState,
            MetaIndex.WeaponState => MetaFlag.WeaponState,
            _                     => (MetaFlag)byte.MaxValue,
        };

    public static MetaIndex ToIndex(this SetMetaFlag index)
        => index switch
        {
            SetMetaFlag.Wetness     => MetaIndex.Wetness,
            SetMetaFlag.HatState    => MetaIndex.HatState,
            SetMetaFlag.VisorState  => MetaIndex.VisorState,
            SetMetaFlag.WeaponState => MetaIndex.WeaponState,
            _                       => (MetaIndex)byte.MaxValue,
        };

    public static IEnumerable<MetaIndex> ToIndices(this SetMetaFlag index)
    {
        if (index.HasFlag(SetMetaFlag.Wetness))
            yield return MetaIndex.Wetness;
        if (index.HasFlag(SetMetaFlag.HatState))
            yield return MetaIndex.HatState;
        if (index.HasFlag(SetMetaFlag.VisorState))
            yield return MetaIndex.VisorState;
        if (index.HasFlag(SetMetaFlag.WeaponState))
            yield return MetaIndex.WeaponState;
    }


    public static MetaIndex ToIndex(this MetaFlag index)
        => index switch
        {
            MetaFlag.Wetness     => MetaIndex.Wetness,
            MetaFlag.HatState    => MetaIndex.HatState,
            MetaFlag.VisorState  => MetaIndex.VisorState,
            MetaFlag.WeaponState => MetaIndex.WeaponState,
            _                    => (MetaIndex)byte.MaxValue,
        };

    public static string ToName(this MetaIndex index)
        => index switch
        {
            MetaIndex.HatState    => "Hat Visible",
            MetaIndex.VisorState  => "Visor Toggled",
            MetaIndex.WeaponState => "Weapon Visible",
            MetaIndex.Wetness     => "Force Wetness",
            _                     => "Unknown Meta",
        };

    public static string ToTooltip(this MetaIndex index)
        => index switch
        {
            MetaIndex.HatState    => "Hide or show the characters head gear.",
            MetaIndex.VisorState  => "Toggle the visor state of the characters head gear.",
            MetaIndex.WeaponState => "Hide or show the characters weapons when not drawn.",
            MetaIndex.Wetness     => "Force the character to be wet or not.",
            _                     => string.Empty,
        };
}
