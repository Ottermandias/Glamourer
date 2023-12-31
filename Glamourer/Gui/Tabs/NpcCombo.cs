using Glamourer.GameData;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs;

public class NpcCombo(NpcCustomizeSet npcCustomizeSet)
    : FilterComboCache<NpcData>(npcCustomizeSet, Glamourer.Log)
{
    protected override string ToString(NpcData obj)
        => obj.Name;
}
