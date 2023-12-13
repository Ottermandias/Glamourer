using Glamourer.State;
using ImGuiNET;

namespace Glamourer.Gui.Tabs.DebugTab;

public class FunPanel(FunModule _funModule, Configuration _config) : IDebugTabTree
{
    public string Label
        => "Fun Module";

    public bool Disabled
        => false;

    public void Draw()
    {
        ImGui.TextUnformatted($"Current Festival: {_funModule.CurrentFestival}");
        ImGui.TextUnformatted($"Festivals Enabled: {_config.DisableFestivals switch { 1 => "Undecided", 0 => "Enabled", _ => "Disabled" }}");
        ImGui.TextUnformatted($"Popup Open: {ImGui.IsPopupOpen("FestivalPopup", ImGuiPopupFlags.AnyPopup)}");
        if (ImGui.Button("Force Christmas"))
            _funModule.ForceFestival(FunModule.FestivalType.Christmas);
        if (ImGui.Button("Force Halloween"))
            _funModule.ForceFestival(FunModule.FestivalType.Halloween);
        if (ImGui.Button("Force April First"))
            _funModule.ForceFestival(FunModule.FestivalType.AprilFirst);
        if (ImGui.Button("Force None"))
            _funModule.ForceFestival(FunModule.FestivalType.None);
        if (ImGui.Button("Revert"))
            _funModule.ResetFestival();
        if (ImGui.Button("Reset Popup"))
        {
            _config.DisableFestivals = 1;
            _config.Save();
        }
    }
}
