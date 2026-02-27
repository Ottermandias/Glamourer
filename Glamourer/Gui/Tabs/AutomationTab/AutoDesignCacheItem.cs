using Glamourer.Automation;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public readonly struct AutoDesignCacheItem(AutoDesignSet set, AutoDesign design, int index)
{
    public readonly AutoDesignSet Set             = set;
    public readonly AutoDesign    Design          = design;
    public readonly StringU8      Name            = new(design.Design.ResolveName(false));
    public readonly StringU8      Incognito       = new(design.Design.ResolveName(true));
    public readonly StringU8      IndexU8         = new($"#{index + 1:D2}");
    public readonly StringU8      JobRestrictions = design.GearsetIndex is -1 ? design.Jobs.Name : StringU8.Empty;

    public readonly StringU8 GearSetRestriction =
        set.Designs[index].GearsetIndex is -1 ? StringU8.Empty : new StringU8($"{design.GearsetIndex}");

    public readonly int  Index    = index;
    public readonly bool Disabled = design.Type is 0;
}

