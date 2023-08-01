using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class CollectionCombo : FilterComboCache<Collection>
{
    public CollectionCombo(PenumbraService penumbra)
        : base(penumbra.GetAllCollections)
    {
        SearchByParts = false;
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var collection = Items[globalIdx];
        bool ret;
        using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text)))
        {
            ret = ImGui.Selectable(collection.Name, selected);
        }
        return ret;
    }
}
