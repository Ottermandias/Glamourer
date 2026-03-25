using Glamourer.Automation;
using ImSharp;

namespace Glamourer.Gui.Tabs.AutomationTab;

public readonly struct AutomationCacheItem(AutoDesignSet set, int index)
{
    public readonly AutoDesignSet Set                 = set;
    public readonly int           Index               = index;
    public readonly StringPair    Name                = new(set.Name);
    public readonly StringPair    IdentifierString    = new(set.Identifiers.First().ToString());
    public readonly StringU8      Incognito           = new($"Auto Design Set #{index + 1}");
    public readonly StringU8      IdentifierIncognito = new(set.Identifiers.First().Incognito(null));
}
