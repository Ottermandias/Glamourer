using Dalamud.Interface;
using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.SettingsTab;

public sealed class CollectionCombo(Configuration config, PenumbraService penumbra, Logger log)
    : FilterComboCache<(Guid Id, string IdShort, string Name)>(
        () => penumbra.GetCollections().Select(kvp => (kvp.Key, kvp.Key.ToString()[..8], kvp.Value)).ToArray(),
        MouseWheelType.Control, log), IUiService
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (_, idShort, name) = Items[globalIdx];
        if (config.Ephemeral.IncognitoMode)
            using (ImRaii.PushFont(UiBuilder.MonoFont))
            {
                return ImGui.Selectable(idShort);
            }

        var ret = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGuiUtil.RightAlign($"({idShort})");
        }

        return ret;
    }
}
