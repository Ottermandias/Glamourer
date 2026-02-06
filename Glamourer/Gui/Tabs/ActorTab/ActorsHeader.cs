using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorsHeader : SplitButtonHeader
{
    private readonly ActorSelection  _selection;
    private readonly EphemeralConfig _config;

    public ActorsHeader(SetFromClipboardButton setFromClipboard, ExportToClipboardButton exportToClipboard, SaveAsDesignButton save,
        UndoButton undo, LockedButton locked, IncognitoButton incognito, ActorSelection selection, EphemeralConfig config)
    {
        _selection = selection;
        _config    = config;
        LeftButtons.AddButton(setFromClipboard,  100);
        LeftButtons.AddButton(exportToClipboard, 90);
        LeftButtons.AddButton(save,              80);
        LeftButtons.AddButton(undo,              70);

        RightButtons.AddButton(locked,    100);
        RightButtons.AddButton(incognito, 90);
    }

    public override ReadOnlySpan<byte> Text
        => _config.IncognitoMode ? _selection.IncognitoName : _selection.ActorName;

    public override ColorParameter TextColor
        => _selection.State is null ? ColorParameter.Default :
            _selection.Data.Valid   ? ColorId.ActorAvailable.Value() : ColorId.ActorUnavailable.Value();

    public override void Draw(Vector2 size)
    {
        var       color = ColorId.HeaderButtons.Value();
        using var _     = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }
}
