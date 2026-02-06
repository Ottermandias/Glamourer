using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ExportToClipboardButton(ActorSelection selection, DesignConverter converter) : BaseIconButton<AwesomeIcon>, IUiService
{
    public override AwesomeIcon Icon
        => LunaStyle.ToClipboardIcon;

    public override bool IsVisible
        => selection.State?.ModelData.ModelId is 0;

    public override bool Enabled
        => !(selection.State?.IsLocked ?? true);


    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text(
            "Copy the current design to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design."u8);

    public override void OnClick()
    {
        try
        {
            var text = converter.ShareBase64(selection.State!, ApplicationRules.FromModifiers(selection.State!));
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {selection.Identifier} data to clipboard.",
                $"Could not copy data from design {selection.Identifier.Incognito(null)} to clipboard", NotificationType.Error);
        }
    }
}
