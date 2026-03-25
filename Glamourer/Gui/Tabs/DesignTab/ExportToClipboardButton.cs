using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ExportToClipboardButton(DesignFileSystem fileSystem, DesignConverter converter) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.ToClipboardIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Copy the current design to your clipboard."u8);

    public override void OnClick()
    {
        var design = (Design)fileSystem.Selection.Selection!.Value;
        try
        {
            var text = converter.ShareBase64(design);
            Im.Clipboard.Set(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {design.Name} data to clipboard.",
                $"Could not copy data from design {design.Identifier} to clipboard", NotificationType.Error, false);
        }
    }
}
