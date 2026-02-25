using Glamourer.Designs.History;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class UndoButton(ActorSelection selection, EditorHistory editorHistory) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => LunaStyle.UndoIcon;

    public override bool IsVisible
        => selection.State is not null;

    public override bool Enabled
        => !(selection.State?.IsLocked ?? true) && editorHistory.CanUndo(selection.State);

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Undo the last change."u8);

    public override void OnClick()
        => editorHistory.Undo(selection.State!);
}
