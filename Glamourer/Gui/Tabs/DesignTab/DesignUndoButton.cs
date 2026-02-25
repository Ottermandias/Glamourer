using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignUndoButton(DesignFileSystem fileSystem, DesignManager manager) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.ResetIcon;

    public override bool Enabled
        => !((Design)fileSystem.Selection.Selection!.Value).WriteProtected() && manager.CanUndo((Design)fileSystem.Selection.Selection!.Value);

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text(
            "Undo the last time you applied an entire design onto this design, if you accidentally overwrote your design with a different one."u8);

    public override void OnClick()
    {
        try
        {
            manager.UndoDesignChange((Design)fileSystem.Selection.Selection!.Value);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex,
                $"Could not undo last changes to {((Design)fileSystem.Selection.Selection!.Value).Name}.",
                NotificationType.Error, false);
        }
    }
}
