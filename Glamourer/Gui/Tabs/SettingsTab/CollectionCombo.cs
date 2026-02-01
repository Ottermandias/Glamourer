using Dalamud.Interface;
using Glamourer.Interop.Penumbra;
using ImSharp;
using Luna;
using OtterGui.Widgets;
using Logger = OtterGui.Log.Logger;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

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
            using (Im.Font.PushMono())
            {
                return Im.Selectable(idShort);
            }

        var ret = Im.Selectable(name, selected);
        Im.Line.Same();
        using (Im.Font.PushMono())
        {
            using var color = ImGuiColor.Text.Push(ImGuiColor.TextDisabled.Get());
            ImEx.TextRightAligned($"({idShort})");
        }

        return ret;
    }
}
