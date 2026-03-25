using Glamourer.Config;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class AutomationHeader : SplitButtonHeader
{
    private readonly Configuration       _config;
    private readonly AutomationSelection _selection;

    public AutomationHeader(Configuration config, AutomationSelection selection, IncognitoButton incognito)
    {
        _config    = config;
        _selection = selection;
        RightButtons.AddButton(incognito, 100);
    }

    public override void Draw(Vector2 size)
    {
        var       color = ColorId.HeaderButtons.Value();
        using var _     = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }

    public override ReadOnlySpan<byte> Text
        => _config.Ephemeral.IncognitoMode ? _selection.Incognito : _selection.Name;
}
