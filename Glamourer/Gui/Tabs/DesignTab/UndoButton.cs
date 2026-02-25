using Glamourer.Designs;
using Glamourer.Designs.History;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class UndoButton(DesignFileSystem fileSystem, EditorHistory history) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.UndoIcon;

    public override bool Enabled
        => !((Design)fileSystem.Selection.Selection!.Value).WriteProtected() && history.CanUndo((Design)fileSystem.Selection.Selection!.Value);

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Undo the last change."u8);

    public override void OnClick()
        => history.Undo((Design)fileSystem.Selection.Selection!.Value);
}
