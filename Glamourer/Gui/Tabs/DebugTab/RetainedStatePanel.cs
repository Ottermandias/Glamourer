using System.Linq;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.State;
using OtterGui.Raii;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class RetainedStatePanel(StateManager _stateManager, ObjectManager _objectManager) : IGameDataDrawer
{
    public string Label
        => "Retained States (Inactive Actors)";

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (identifier, state) in _stateManager.Where(kvp => !_objectManager.ContainsKey(kvp.Key)))
        {
            using var t = ImRaii.TreeNode(identifier.ToString());
            if (t)
                ActiveStatePanel.DrawState(_stateManager, ActorData.Invalid, state);
        }
    }
}
