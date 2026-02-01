using Glamourer.Automation;
using ImSharp;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class AutoDesignPanel(AutoDesignManager autoDesignManager) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Auto Designs"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (idx, set) in autoDesignManager.Index())
        {
            using var id   = Im.Id.Push(idx);
            using var tree = Im.Tree.Node(set.Name);
            if (!tree)
                continue;

            using var table = Im.Table.Begin("##autoDesign"u8, 2, TableFlags.SizingFixedFit | TableFlags.RowBackground);
            if (!table)
                continue;

            table.DrawDataPair("Name"u8,    set.Name);
            table.DrawDataPair("Index"u8,   idx);
            table.DrawDataPair("Enabled"u8, set.Enabled);
            table.DrawDataPair("Actor"u8,   set.Identifiers[0]);

            foreach (var (designIdx, design) in set.Designs.Index())
            {
                table.DrawColumn($"{design.Design.ResolveName(false)} ({designIdx})");
                table.DrawColumn($"{design.Type} {design.Jobs.Name}");
            }
        }
    }
}
