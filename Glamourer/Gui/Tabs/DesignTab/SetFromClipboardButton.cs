using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class SetFromClipboardButton(DesignFileSystem fileSystem, DesignConverter converter, DesignManager manager)
    : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.FromClipboardIcon;

    public override bool Enabled
        => !((Design)fileSystem.Selection.Selection!.Value).WriteProtected();

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text(
            "Try to apply a design from your clipboard over this design.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8);

    public override void OnClick()
    {
        try
        {
            var text = Im.Clipboard.GetUtf16();
            var (applyEquip, applyCustomize) = UiHelpers.ConvertKeysToBool();
            var design = converter.FromBase64(text, applyCustomize, applyEquip, out _)
             ?? throw new Exception("The clipboard did not contain valid data.");
            manager.ApplyDesign((Design)fileSystem.Selection.Selection!.Value, design);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {((Design)fileSystem.Selection.Selection!.Value).Name}.",
                $"Could not apply clipboard to design {((Design)fileSystem.Selection.Selection!.Value).Identifier}", NotificationType.Error,
                false);
        }
    }
}
