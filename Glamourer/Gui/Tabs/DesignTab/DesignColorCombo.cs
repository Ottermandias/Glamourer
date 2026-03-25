using Glamourer.Designs;
using ImSharp;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignColorCombo(DesignColors designColors, bool skipAutomatic) : SimpleFilterCombo<string>(SimpleFilterType.Text)
{
    public override StringU8 DisplayString(in string value)
        => new(value);

    public override string FilterString(in string value)
        => value;

    public override IEnumerable<string> GetBaseItems()
        => skipAutomatic ? designColors.Keys.OrderBy(k => k) : designColors.Keys.OrderBy(k => k).Prepend(DesignColors.AutomaticName);

    public override ColorParameter TextColor(in string value)
        => value is DesignColors.AutomaticName ? ColorParameter.Default : designColors[value];

    protected override bool DrawItem(in SimpleCacheItem<string> item, int globalIndex, bool selected)
    {
        var isAutomatic = !skipAutomatic && globalIndex is 0;
        var ret         = base.DrawItem(item, globalIndex, selected);
        if (isAutomatic)
            Im.Tooltip.OnHover(
                "The automatic color uses the colors dependent on the design state, as defined in the regular color definitions."u8);
        return ret;
    }
}
