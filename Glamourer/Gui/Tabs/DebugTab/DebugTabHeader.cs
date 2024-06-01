using Glamourer.Gui.Tabs.DebugTab.IpcTester;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Raii;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DebugTabHeader(string label, params IGameDataDrawer[] subTrees)
{
    public string                         Label    { get; } = label;
    public IReadOnlyList<IGameDataDrawer> SubTrees { get; } = subTrees;

    public void Draw()
    {
        using var h = ImRaii.CollapsingHeader(Label);
        if (!h)
            return;

        foreach (var subTree in SubTrees)
        {
            using var disabled = ImRaii.Disabled(subTree.Disabled);
            using var tree     = ImRaii.TreeNode(subTree.Label);
            if (tree)
            {
                disabled.Dispose();
                subTree.Draw();
            }
        }
    }

    public static DebugTabHeader CreateInterop(IServiceProvider provider)
        => new
        (
            "Interop",
            provider.GetRequiredService<ModelEvaluationPanel>(),
            provider.GetRequiredService<ObjectManagerPanel>(),
            provider.GetRequiredService<PenumbraPanel>(),
            provider.GetRequiredService<IpcTesterPanel>(),
            provider.GetRequiredService<DatFilePanel>(),
            provider.GetRequiredService<GlamourPlatePanel>()
        );

    public static DebugTabHeader CreateGameData(IServiceProvider provider)
        => new
        (
            "Game Data",
            provider.GetRequiredService<DataServiceDiagnosticsDrawer>(),
            provider.GetRequiredService<IdentificationDrawer>(),
            provider.GetRequiredService<RestrictedGearDrawer>(),
            provider.GetRequiredService<ActorDataDrawer>(),
            provider.GetRequiredService<ItemDataDrawer>(),
            provider.GetRequiredService<DictStainDrawer>(),
            provider.GetRequiredService<CustomizationServicePanel>(),
            provider.GetRequiredService<DictJobDrawer>(),
            provider.GetRequiredService<DictJobGroupDrawer>(),
            provider.GetRequiredService<NpcAppearancePanel>()
        );

    public static DebugTabHeader CreateDesigns(IServiceProvider provider)
        => new
        (
            "Designs",
            provider.GetRequiredService<DesignManagerPanel>(),
            provider.GetRequiredService<DesignConverterPanel>(),
            provider.GetRequiredService<DesignTesterPanel>(),
            provider.GetRequiredService<AutoDesignPanel>()
        );

    public static DebugTabHeader CreateState(IServiceProvider provider)
        => new
        (
            "State",
            provider.GetRequiredService<ActiveStatePanel>(),
            provider.GetRequiredService<RetainedStatePanel>(),
            provider.GetRequiredService<FunPanel>()
        );

    public static DebugTabHeader CreateUnlocks(IServiceProvider provider)
        => new
        (
            "Unlocks",
            provider.GetRequiredService<CustomizationUnlockPanel>(),
            provider.GetRequiredService<ItemUnlockPanel>(),
            provider.GetRequiredService<UnlockableItemsPanel>(),
            provider.GetRequiredService<InventoryPanel>()
        );
}
