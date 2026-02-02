using Glamourer.State;
using ImSharp;
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
        Im.Text($"Current Festival: {funModule.CurrentFestival}");
        Im.Text($"Festivals Enabled: {config.DisableFestivals switch { 1 => "Undecided"u8, 0 => "Enabled"u8, _ => "Disabled"u8 }}");
        Im.Text($"Popup Open: {Im.Popup.IsOpen("FestivalPopup"u8, PopupQueryFlags.AnyPopup)}");
        if (Im.Button("Force Christmas"u8))
            funModule.ForceFestival(FunModule.FestivalType.Christmas);
        if (Im.Button("Force Halloween"u8))
            funModule.ForceFestival(FunModule.FestivalType.Halloween);
        if (Im.Button("Force April First"u8))
            funModule.ForceFestival(FunModule.FestivalType.AprilFirst);
        if (Im.Button("Force None"u8))
            funModule.ForceFestival(FunModule.FestivalType.None);
        if (Im.Button("Revert"u8))
            funModule.ResetFestival();
        if (Im.Button("Reset Popup"u8))
        {
            config.DisableFestivals = 1;
            config.Save();
        }
    }
}
