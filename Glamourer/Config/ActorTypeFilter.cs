using Luna.Generators;

namespace Glamourer.Config;

[NamedEnum(Utf16: false)]
[TooltipEnum]
[Flags]
public enum ActorTypeFilter : uint
{
    None = 0,

    [Name("Players")]
    [Tooltip("Show or hide all player characters.")]
    Player = 1 << 0,

    [Name("Battle Non-Player Characters")]
    [Tooltip("Show or hide all NPCs that can be involved in combat and that are not owned by a player.")]
    BattleNpc = 1 << 1,

    [Name("Event Non-Player Characters")]
    [Tooltip("Show or hide all NPCs that can not be involved in combat and that are not owned by a player.")]
    EventNpc = 1 << 2,

    [Name("Minions")]
    [Tooltip("Show or hide all Minions.")]
    Minion = 1 << 3,

    [Name("Mounts")]
    [Tooltip("Show or hide all Mounts.")]
    Mount = 1 << 4,

    [Name("Accessories")]
    [Tooltip("Show or hide all Accessories (Wings, Backpacks, Glasses).")]
    Accessory = 1 << 5,

    [Name("Retainer")]
    [Tooltip("Show or hide all Retainers.")]
    Retainer = 1 << 6,

    [Name("Interface Actors")]
    [Tooltip("Show or hide all special interface actors like the character screen actor.")]
    Special = 1 << 7,

    [Name("Owned Non-Player Characters")]
    [Tooltip("Show or hide all NPCs that are owned by a player.")]
    Owned = 1 << 8,

    [Name("Foreign Homeworld")]
    [Tooltip("Show or hide all player characters that are not from your character's homeworld.")]
    Homeworld = 1 << 9,
}

public static partial class ActorTypeFilterExtensions
{
    public const ActorTypeFilter AllFiltered = (ActorTypeFilter)((1 << 10) - 1);

    extension(ActorTypeFilter)
    {
        public static ActorTypeFilter All
            => AllFiltered;
    }
}