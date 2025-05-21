using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignTab(DesignFileSystemSelector _selector, DesignPanel _panel, ImportService _importService, DesignManager _manager)
    : ITab
{
    public ReadOnlySpan<byte> Label
        => "Designs"u8;

    public void DrawContent()
    {
        _selector.Draw();
        if (_importService.CreateCharaTarget(out var designBase, out var name))
        {
            var newDesign = _manager.CreateClone(designBase, name, true);
            Glamourer.Messager.NotificationMessage($"Imported Anamnesis .chara file {name} as new design {newDesign.Name}", NotificationType.Success, false);
        }

        ImGui.SameLine();
        _panel.Draw();
        _importService.CreateCharaSource();
    }
}
