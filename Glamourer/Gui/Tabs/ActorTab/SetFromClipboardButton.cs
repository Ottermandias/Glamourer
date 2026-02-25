using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.State;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class SetFromClipboardButton(ActorSelection selection, DesignConverter converter, StateManager stateManager) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => LunaStyle.FromClipboardIcon;

    public override bool IsVisible
        => selection.State is not null;

    public override bool Enabled
        => !(selection.State?.IsLocked ?? true);


    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Try to apply a design from your clipboard.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8);

    public override void OnClick()
    {
        try
        {
            var (applyGear, applyCustomize) = UiHelpers.ConvertKeysToBool();
            var text = Im.Clipboard.GetUtf16();
            var design = converter.FromBase64(text, applyCustomize, applyGear, out _)
             ?? throw new Exception("The clipboard did not contain valid data.");
            stateManager.ApplyDesign(selection.State!, design, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {selection.Identifier}.",
                $"Could not apply clipboard to design {selection.Identifier.Incognito(null)}", NotificationType.Error, false);
        }
    }
}