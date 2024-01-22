using Penumbra.GameData.Enums;

namespace Glamourer.State;

public enum MetaIndex
{
    Wetness = EquipFlagExtensions.NumEquipFlags + CustomizationExtensions.NumIndices,
    HatState,
    VisorState,
    WeaponState,
    ModelId,
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
            _                     => (MetaFlag) byte.MaxValue,
        };
}
