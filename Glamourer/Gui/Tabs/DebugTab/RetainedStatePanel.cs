using Glamourer.State;
using ImSharp;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class RetainedStatePanel(StateManager stateManager, ActorObjectManager objectManager) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Retained States (Inactive Actors)"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (identifier, state) in stateManager.Where(kvp => !objectManager.ContainsKey(kvp.Key)))
        {
            using var t = Im.Tree.Node($"{identifier}");
            if (t)
                ActiveStatePanel.DrawState(stateManager, ActorData.Invalid, state);
        }
    }
}
