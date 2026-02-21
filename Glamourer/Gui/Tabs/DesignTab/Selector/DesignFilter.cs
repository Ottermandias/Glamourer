using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFilter : TokenizedFilter<DesignFilterTokenType, DesignFileSystemCache.DesignData, DesignFilterToken>,
    IFileSystemFilter<DesignFileSystemCache.DesignData>, IUiService
{
    protected override void DrawTooltip()
    {
        if (!Im.Item.Hovered())
            return;

        using var tt             = Im.Tooltip.Begin();
        var       highlightColor = ColorId.EnabledAutoSet.Value().ToVector();
        Im.Text("Filter designs for those where their full paths or names contain the given strings, split by spaces."u8);
        ImEx.TextMultiColored("Enter "u8).Then("m:[string]"u8, highlightColor)
            .Then(" to filter for designs with a mod association containing the string."u8).End();
        ImEx.TextMultiColored("Enter "u8).Then("t:[string]"u8, highlightColor).Then(" to filter for designs set to specific tags."u8).End();
        ImEx.TextMultiColored("Enter "u8).Then("c:[string]"u8, highlightColor)
            .Then(" to filter for designs set to specific colors."u8).End();
        ImEx.TextMultiColored("Enter "u8).Then("i:[string]"u8, highlightColor).Then(" to filter for designs containing specific items."u8)
            .End();
        ImEx.TextMultiColored("Enter "u8).Then("n:[string]"u8, highlightColor).Then(" to filter only for design names, ignoring the paths."u8)
            .End();
        ImEx.TextMultiColored("Enter "u8).Then("f:[string]"u8, highlightColor).Then(
                " to filter for designs containing the text in name, path, description, tags, mod associations, colors or contained items."u8)
            .End();
        Im.Line.New();
        ImEx.TextMultiColored("Use "u8).Then("None"u8, highlightColor).Then(" as a placeholder value that only matches empty lists or names."u8)
            .End();
        Im.Text("Regularly, a design has to match all supplied criteria separately."u8);
        ImEx.TextMultiColored("Put a "u8).Then("'-'"u8, highlightColor)
            .Then(" in front of a search token to search only for designs not matching the criterion."u8).End();
        ImEx.TextMultiColored("Put a "u8).Then("'?'"u8, highlightColor)
            .Then(" in front of a search token to search for designs matching at least one of the '?'-criteria."u8).End();
        ImEx.TextMultiColored("Wrap spaces in "u8).Then("\"[string with space]\""u8, highlightColor)
            .Then(" to match this exact combination of words."u8).End();
    }

    protected override bool Matches(in DesignFilterToken token, in DesignFileSystemCache.DesignData cacheItem)
        => token.Type switch
        {
            DesignFilterTokenType.Default => cacheItem.Node.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase)
             || cacheItem.Node.Value.Name.Text.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            DesignFilterTokenType.Mod         => CheckMods(token.Needle, cacheItem),
            DesignFilterTokenType.Tag         => CheckTags(token.Needle, cacheItem),
            DesignFilterTokenType.Color       => cacheItem.Node.Value.Color.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            DesignFilterTokenType.Item        => cacheItem.Node.Value.DesignData.ContainsName(token.Needle),
            DesignFilterTokenType.Name        => cacheItem.Node.Value.Name.Text.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            DesignFilterTokenType.FullContext => CheckFullContext(token.Needle, cacheItem),
            _                                 => true,
        };

    protected override bool MatchesNone(DesignFilterTokenType type, bool negated, in DesignFileSystemCache.DesignData cacheItem)
        => type switch
        {
            DesignFilterTokenType.Mod when negated => cacheItem.Node.Value.AssociatedMods.Count > 0,
            DesignFilterTokenType.Mod              => cacheItem.Node.Value.AssociatedMods.Count is 0,
            DesignFilterTokenType.Tag when negated => cacheItem.Node.Value.Tags.Length > 0,
            DesignFilterTokenType.Tag              => cacheItem.Node.Value.Tags.Length is 0,
            _                                      => true,
        };

    private static bool CheckMods(string needle, in DesignFileSystemCache.DesignData cacheItem)
        => cacheItem.Node.Value.AssociatedMods.Any(kvp => kvp.Key.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static bool CheckTags(string needle, in DesignFileSystemCache.DesignData cacheItem)
        => cacheItem.Node.Value.Tags.Any(t => t.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static bool CheckFullContext(string needle, in DesignFileSystemCache.DesignData cacheItem)
    {
        if (needle.Length is 0)
            return true;

        if (cacheItem.Node.FullPath.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        var design = cacheItem.Node.Value;
        if (design.Name.Text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        if (design.Description.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        if (CheckTags(needle, cacheItem))
            return true;

        if (design.Color.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        if (CheckMods(needle, cacheItem))
            return true;

        if (design.DesignData.ContainsName(needle))
            return true;

        if (design.Identifier.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public bool WouldBeVisible(in FileSystemFolderCache folder)
    {
        switch (State)
        {
            case FilterState.NoFilters: return true;
            case FilterState.NoMatches: return false;
        }

        foreach (var token in Forced)
        {
            if (token.Type switch
                {
                    DesignFilterTokenType.Name        => !folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.Default     => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    _                                 => true,
                })
                return false;
        }

        foreach (var token in Negated)
        {
            if (token.Type switch
                {
                    DesignFilterTokenType.Name        => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.Default     => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.FullContext => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    _                                 => false,
                })
                return false;
        }

        foreach (var token in General)
        {
            if (token.Type switch
                {
                    DesignFilterTokenType.Name        => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.Default     => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    DesignFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                    _                                 => false,
                })
                return true;
        }

        return General.Count is 0;
    }
}
