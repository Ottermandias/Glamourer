using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Interop;
using Dalamud.Bindings.ImGui;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignTab(DesignFileSystemSelector selector, DesignPanel panel, ImportService importService, DesignManager manager)
    : OtterGui.Widgets.ITab
{
    public ReadOnlySpan<byte> Label
        => "Designs"u8;

    public void DrawContent()
    {
        selector.Draw();
        if (importService.CreateCharaTarget(out var designBase, out var name))
        {
            var newDesign = manager.CreateClone(designBase, name, true);
            Glamourer.Messager.NotificationMessage($"Imported Anamnesis .chara file {name} as new design {newDesign.Name}", NotificationType.Success, false);
        }

        ImGui.SameLine();
        panel.Draw();
        importService.CreateCharaSource();
    }
}
