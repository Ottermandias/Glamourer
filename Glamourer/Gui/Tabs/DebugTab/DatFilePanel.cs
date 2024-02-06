using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData.Files;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DatFilePanel(ImportService _importService) : IGameDataDrawer
{
    public string Label
        => "Character Dat File";

    public bool Disabled
        => false;

    private string            _datFilePath = string.Empty;
    private DatCharacterFile? _datFile     = null;

    public void Draw()
    {
        ImGui.InputTextWithHint("##datFilePath", "Dat File Path...", ref _datFilePath, 256);
        var exists = _datFilePath.Length > 0 && File.Exists(_datFilePath);
        if (ImGuiUtil.DrawDisabledButton("Load##Dat", Vector2.Zero, string.Empty, !exists))
            _datFile = _importService.LoadDat(_datFilePath, out var tmp) ? tmp : null;

        if (ImGuiUtil.DrawDisabledButton("Save##Dat", Vector2.Zero, string.Empty, _datFilePath.Length == 0 || _datFile == null))
            _importService.SaveDesignAsDat(_datFilePath, _datFile!.Value.Customize, _datFile!.Value.Description);

        if (_datFile != null)
        {
            ImGui.TextUnformatted(_datFile.Value.Magic.ToString());
            ImGui.TextUnformatted(_datFile.Value.Version.ToString());
            ImGui.TextUnformatted(_datFile.Value.Time.LocalDateTime.ToString("g"));
            ImGui.TextUnformatted(_datFile.Value.Voice.ToString());
            ImGui.TextUnformatted(_datFile.Value.Customize.ToString());
            ImGui.TextUnformatted(_datFile.Value.Description);
        }
    }
}