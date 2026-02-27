using ImSharp;

namespace Glamourer.Gui.Tabs.DesignTab;

public enum DesignFilterTokenType
{
    Default,
    Mod,
    Tag,
    Color,
    Item,
    Name,
    FullContext,
}

public readonly struct DesignFilterToken() : IFilterToken<DesignFilterTokenType, DesignFilterToken>
{
    public string                Needle { get; init; } = string.Empty;
    public DesignFilterTokenType Type   { get; init; }

    public bool Contains(DesignFilterToken other)
    {
        if (Type != other.Type)
            return false;

        return Needle.Contains(other.Needle);
    }

    public static bool ConvertToken(char tokenCharacter, out DesignFilterTokenType type)
    {
        type = tokenCharacter switch
        {
            'm' or 'M' => DesignFilterTokenType.Mod,
            'n' or 'N' => DesignFilterTokenType.Name,
            't' or 'T' => DesignFilterTokenType.Tag,
            'i' or 'I' => DesignFilterTokenType.Item,
            'c' or 'C' => DesignFilterTokenType.Color,
            'f' or 'F' => DesignFilterTokenType.FullContext,
            _          => DesignFilterTokenType.Default,
        };
        return type is not DesignFilterTokenType.Default;
    }

    public static bool AllowsNone(DesignFilterTokenType type)
        => type is DesignFilterTokenType.Tag or DesignFilterTokenType.Mod;

    public static bool ProcessList(List<DesignFilterToken> list, TokenModifier modifier)
        => false;
}
