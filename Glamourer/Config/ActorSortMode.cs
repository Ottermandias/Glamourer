using Luna.Generators;

namespace Glamourer.Config;

[NamedEnum(Utf16: false)]
[TooltipEnum]
public enum ActorSortMode : byte
{
    [Name("Default Order")]
    [Tooltip("Use the order given by the game's list of objects.")]
    Default,

    [Name("Alphabetical")]
    [Tooltip("Order available actors alphabetically by their name.")]
    Lexicographical,

    [Name("Alphabetical (Player First)")]
    [Tooltip("Order available actors alphabetically by their name, but keep your own character at the top.")]
    LexicographicalPlayerFirst,
}
