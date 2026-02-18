using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationHeader(Configuration.Configuration config, AutomationSelection selection) : IHeader
{
    public bool Collapsed
        => false;

    public void Draw(Vector2 size)
        => ImEx.TextFramed(config.Ephemeral.IncognitoMode ? selection.Incognito : selection.Name, size with { Y = Im.Style.FrameHeight });
}
