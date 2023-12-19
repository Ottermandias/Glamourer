using System;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

internal class FunEquipSet
{
    public static FunEquipSet? GetSet(FunModule.FestivalType type)
    {
        return type switch
        {
            FunModule.FestivalType.Halloween  => Halloween,
            FunModule.FestivalType.Christmas  => Christmas,
            FunModule.FestivalType.AprilFirst => AprilFirst,
            _                                 => null,
        };
    }

    public readonly record struct Group(CharacterArmor Head, CharacterArmor Body, CharacterArmor Hands, CharacterArmor Legs,
        CharacterArmor Feet, StainId[]? Stains = null)
    {
        public Group(ushort headS, byte headV, ushort bodyS, byte bodyV, ushort handsS, byte handsV, ushort legsS, byte legsV, ushort feetS,
            byte feetV, StainId[]? stains = null)
            : this(new CharacterArmor(headS, headV, 0), new CharacterArmor(bodyS, bodyV, 0), new CharacterArmor(handsS, handsV, 0),
                new CharacterArmor(legsS,    legsV, 0), new CharacterArmor(feetS, feetV, 0), stains)
        { }

        public static Group FullSetWithoutHat(ushort modelSet, byte variant, StainId[]? stains = null)
            => new(0279, 1, modelSet, variant, modelSet, variant, modelSet, variant, modelSet, variant, stains);

        public static Group FullSet(ushort modelSet, byte variant, StainId[]? stains = null)
            => new(modelSet, variant, modelSet, variant, modelSet, variant, modelSet, variant, modelSet, variant, stains);
    }

    private readonly Group[] _groups;

    public void Apply(StainId[] allStains, Random rng, Span<CharacterArmor> armor)
    {
        var groupIdx = rng.Next(0, _groups.Length);
        var group    = _groups[groupIdx];
        var stains   = group.Stains ?? allStains;
        var stainIdx = rng.Next(0, stains.Length);
        var stain    = stains[stainIdx];
        Glamourer.Log.Verbose($"Chose group {groupIdx} and stain {stainIdx} for festival costume.");
        if (group.Head.Set != 0)
            armor[0] = group.Head.With(stain);
        if (group.Body.Set != 0)
            armor[1] = group.Body.With(stain);
        if (group.Hands.Set != 0)
            armor[2] = group.Hands.With(stain);
        if (group.Legs.Set != 0)
            armor[3] = group.Legs.With(stain);
        if (group.Feet.Set != 0)
            armor[4] = group.Feet.With(stain);
    }

    public static readonly FunEquipSet Christmas = new
    (
        new Group(6170, 1, 6170, 1, 6170, 1, 6170, 1, 6170, 1), // Unorthodox Saint
        new Group(6006, 1, 6170, 1, 6170, 1, 6170, 1, 6170, 1), // Unorthodox Saint
        new Group(6007, 1, 6170, 1, 6170, 1, 6170, 1, 6170, 1), // Unorthodox Saint
        new Group(0058, 1, 0058, 1, 6005, 1, 0000, 0, 6005, 1), // Reindeer
        new Group(6005, 1, 0058, 1, 6005, 1, 0000, 0, 6005, 1), // Reindeer
        new Group(0231, 1, 0231, 1, 0279, 1, 0231, 1, 0231, 1), // Starlight
        new Group(0231, 1, 6030, 1, 0279, 1, 0231, 1, 0231, 1), // Starlight
        new Group(0053, 1, 0053, 1, 0279, 1, 0049, 6, 0053, 1), // Sweet Dream
        new Group(0136, 1, 0136, 1, 0136, 1, 0000, 0, 0000, 0)  // Snowman
    );

    public static readonly FunEquipSet Halloween = new
    (
        new Group(0316, 1, 0316, 1, 0316, 1, 0049, 6, 0316, 1), // Witch
        new Group(6047, 1, 6047, 1, 6047, 1, 6047, 1, 6047, 1), // Werewolf
        new Group(6148, 1, 6148, 1, 6148, 1, 6148, 1, 6148, 1), // Wake Doctor
        new Group(6117, 1, 6117, 1, 6117, 1, 6117, 1, 6117, 1), // Clown
        new Group(0000, 0, 0137, 1, 0000, 0, 0000, 0, 0000, 0), // Howling Spirit
        new Group(0000, 0, 0137, 2, 0000, 0, 0000, 0, 0000, 0), // Wailing Spirit
        new Group(0232, 1, 0232, 1, 0279, 1, 0232, 1, 0232, 1), // Eerie Attire
        new Group(0232, 1, 6036, 1, 0279, 1, 0232, 1, 0232, 1), // Vampire
        new Group(0505, 6, 0505, 6, 0505, 6, 0505, 6, 0505, 6)  // Manusya Casting
    );

    public static readonly FunEquipSet AprilFirst = new
    (
        new Group(6133, 1, 6133, 1, 0000, 0, 0000, 0, 0000, 0), // Gaja
        new Group(6121, 1, 6121, 1, 0000, 0, 0000, 0, 0000, 0), // Chicken
        new Group(6103, 1, 6103, 1, 0000, 0, 0000, 0, 0000, 0), // Rabbit
        new Group(6103, 1, 6103, 2, 0000, 0, 0000, 0, 0000, 0), // Rabbit
        new Group(6089, 1, 6089, 1, 0000, 0, 0000, 0, 0000, 0), // Toad
        new Group(0241, 1, 0046, 1, 0000, 0, 0279, 1, 6169, 3), // Chocobo
        new Group(0046, 1, 0046, 1, 0000, 0, 0279, 1, 6169, 3), // Chocobo
        new Group(0058, 1, 0058, 1, 0000, 0, 0000, 0, 0000, 0), // Reindeer
        new Group(0136, 1, 0136, 1, 0136, 1, 0000, 0, 0000, 0), // Snowman
        new Group(0159, 1, 0000, 0, 0000, 0, 0000, 0, 0000, 0), // Slime Crown
        new Group(6117, 1, 6117, 1, 6117, 1, 6117, 1, 6117, 1), // Clown
        new Group(6169, 3, 6169, 3, 0279, 1, 6169, 3, 6169, 3), // Chocobo Pajama
        new Group(6169, 2, 6169, 2, 0279, 2, 6169, 2, 6169, 2)  // Cactuar Pajama
    );

    private FunEquipSet(params Group[] groups)
        => _groups = groups;
}