using Glamourer.State;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class FunPanel(FunModule funModule, Configuration config) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Fun Module"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        ImGui.TextUnformatted($"Current Festival: {funModule.CurrentFestival}");
        ImGui.TextUnformatted($"Festivals Enabled: {config.DisableFestivals switch { 1 => "Undecided", 0 => "Enabled", _ => "Disabled" }}");
        ImGui.TextUnformatted($"Popup Open: {ImGui.IsPopupOpen("FestivalPopup", ImGuiPopupFlags.AnyPopup)}");
        if (ImGui.Button("Force Christmas"))
            funModule.ForceFestival(FunModule.FestivalType.Christmas);
        if (ImGui.Button("Force Halloween"))
            funModule.ForceFestival(FunModule.FestivalType.Halloween);
        if (ImGui.Button("Force April First"))
            funModule.ForceFestival(FunModule.FestivalType.AprilFirst);
        if (ImGui.Button("Force None"))
            funModule.ForceFestival(FunModule.FestivalType.None);
        if (ImGui.Button("Revert"))
            funModule.ResetFestival();
        if (ImGui.Button("Reset Popup"))
        {
            config.DisableFestivals = 1;
            config.Save();
        }
    }
}
