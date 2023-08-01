using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Utility;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class CollectionAssociationTab
{
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignManager            _manager;
    private readonly CollectionCombo          _collectionCombo;

    public CollectionAssociationTab(PenumbraService penumbra, DesignFileSystemSelector selector, DesignManager manager)
    {
        _selector = selector;
        _manager  = manager;
        _collectionCombo = new CollectionCombo(penumbra);
    }

    public void Draw()
    {
        if (!ImGui.CollapsingHeader("Collection Association"))
            return;
        var width = new Vector2(ImGui.GetContentRegionAvail().X, 0);
        var changed = _collectionCombo.Draw("##new", !_selector.Selected!.AssociatedCollection.IsAssociable() ? "Select Collection" : _selector.Selected!.AssociatedCollection.Name, string.Empty,
            width.X, ImGui.GetTextLineHeight());
        var currentCollection = _collectionCombo.CurrentSelection;
        if (changed)
        {
            if (!currentCollection.IsAssociable()) return;
            _manager.ChangeAssociatedCollection(_selector.Selected!, currentCollection);
        }
        if (ImGui.Button($"Remove Associated Collection"))
        {
            _manager.ChangeAssociatedCollection(_selector.Selected!, new Collection());
        }
    }
}
