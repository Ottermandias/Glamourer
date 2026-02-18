using Dalamud.Interface.ImGuiNotification;
using Glamourer.Configuration;
using Glamourer.Designs;
using Glamourer.Interop;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignTab(DesignFileSystemSelector selector, DesignPanel panel, ImportService importService, DesignManager manager)
    : ITab<MainTabType>
{
    public ReadOnlySpan<byte> Label
        => "Designs"u8;

    public MainTabType Identifier
        => MainTabType.Designs;

    public void DrawContent()
    {
        selector.Draw();
        if (importService.CreateCharaTarget(out var designBase, out var name))
        {
            var newDesign = manager.CreateClone(designBase, name, true);
            Glamourer.Messager.NotificationMessage($"Imported Anamnesis .chara file {name} as new design {newDesign.Name}",
                NotificationType.Success, false);
        }

        Im.Line.Same();
        panel.Draw();
        importService.CreateCharaSource();
    }

    //protected override void SetWidth(float width, ScalingMode mode)
    //    => _uiConfig.ActorsTabScale = new TwoPanelWidth(width, mode);
    //
    //protected override float MinimumWidth
    //    => LeftFooter.MinimumWidth;
    //
    //protected override float MaximumWidth
    //    => Im.Window.Width - 500 * Im.Style.GlobalScale;
}
