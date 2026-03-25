using Glamourer.Config;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcFilter : TokenizedFilter<NpcFilter.TokenType, NpcCacheItem, NpcFilter.NpcFilterToken>, IUiService
{
    public enum TokenType : byte
    {
        Name,
        Id,
        Color,
    }

    public NpcFilter(Configuration config)
    {
        if (config.RememberNpcFilter)
            Set(config.Filters.NpcFilter);
        FilterChanged += () => config.Filters.NpcFilter = Text;
    }

    protected override void DrawTooltip()
    {
        if (!Im.Item.Hovered())
            return;

        using var style = Im.Style.PushDefault();
        using var tt    = Im.Tooltip.Begin();
        Im.Text("Filter NPC appearances for those where their names contain the given substring."u8);
        ImEx.TextMultiColored("Enter "u8).Then("i:[number]"u8, ColorId.TriStateCheck.Value()).Then(" to filter for NPCs of certain IDs."u8)
            .End();
        ImEx.TextMultiColored("Enter "u8).Then("c:[string]"u8, ColorId.TriStateCheck.Value())
            .Then(" to filter for NPC appearances set to specific colors."u8).End();
    }

    public readonly struct NpcFilterToken() : IFilterToken<TokenType, NpcFilterToken>
    {
        public string    Needle       { get; init; }         = string.Empty;
        public uint      ParsedNeedle { get; private init; } = 0;
        public TokenType Type         { get; init; }

        public bool Contains(NpcFilterToken other)
        {
            if (Type != other.Type)
                return false;
            if (Type is TokenType.Id)
                return ParsedNeedle == other.ParsedNeedle;

            return Needle.Contains(other.Needle);
        }

        public static bool ConvertToken(char tokenCharacter, out TokenType type)
        {
            type = tokenCharacter switch
            {
                'i' or 'I' => TokenType.Id,
                'c' or 'C' => TokenType.Color,
                _          => TokenType.Name,
            };
            return type is not TokenType.Name;
        }

        public static bool AllowsNone(TokenType type)
            => false;

        public static bool ProcessList(List<NpcFilterToken> list, TokenModifier modifier)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var entry = list[i];
                if (entry.Type is not TokenType.Id)
                    continue;

                if (!uint.TryParse(entry.Needle, out var value))
                {
                    list.RemoveAt(i--);
                    if (modifier is TokenModifier.Forced)
                        return true;
                }
                else
                {
                    list[i] = new NpcFilterToken
                    {
                        ParsedNeedle = value,
                        Type         = TokenType.Id,
                    };
                }
            }

            return false;
        }
    }

    protected override bool Matches(in NpcFilterToken token, in NpcCacheItem npcCacheItem)
    {
        return token.Type switch
        {
            TokenType.Name  => npcCacheItem.Name.Utf16.Contains(token.Needle, StringComparison.InvariantCultureIgnoreCase),
            TokenType.Id    => npcCacheItem.Npc.Id == token.ParsedNeedle,
            TokenType.Color => npcCacheItem.ColorText.Contains(token.Needle, StringComparison.InvariantCultureIgnoreCase),
            _               => false,
        };
    }

    protected override bool MatchesNone(TokenType type, bool negated, in NpcCacheItem npcCacheItem)
        => false;
}
