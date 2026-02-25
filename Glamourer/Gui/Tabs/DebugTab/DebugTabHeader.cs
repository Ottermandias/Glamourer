using Glamourer.Gui.Tabs.DebugTab.IpcTester;
using ImSharp;
using Microsoft.Extensions.DependencyInjection;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DebugTabHeader(ReadOnlySpan<byte> label, params IGameDataDrawer[] subTrees)
{
    public StringU8                       Label    { get; } = new(label);
    public IReadOnlyList<IGameDataDrawer> SubTrees { get; } = subTrees;

    public void Draw()
    {
        using var h = Im.Tree.HeaderId(Label);
        if (!h)
            return;

        foreach (var subTree in SubTrees)
        {
            var       disabled = Im.Disabled(subTree.Disabled);
            using var tree     = Im.Tree.Node(subTree.Label);
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
            "Interop"u8,
            provider.GetRequiredService<ModelEvaluationPanel>(),
            provider.GetRequiredService<ObjectManagerPanel>(),
            provider.GetRequiredService<PenumbraPanel>(),
            provider.GetRequiredService<DynamisPanel>(),
            provider.GetRequiredService<IpcTesterPanel>(),
            provider.GetRequiredService<DatFilePanel>(),
            provider.GetRequiredService<GlamourPlatePanel>(),
            provider.GetRequiredService<AdvancedCustomizationDrawer>()
        );

    public static DebugTabHeader CreateGameData(IServiceProvider provider)
        => new
        (
            "Game Data"u8,
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
            "Designs"u8,
            provider.GetRequiredService<DesignManagerPanel>(),
            provider.GetRequiredService<DesignConverterPanel>(),
            provider.GetRequiredService<DesignTesterPanel>(),
            provider.GetRequiredService<AutoDesignPanel>()
        );

    public static DebugTabHeader CreateState(IServiceProvider provider)
        => new
        (
            "State"u8,
            provider.GetRequiredService<ActiveStatePanel>(),
            provider.GetRequiredService<RetainedStatePanel>(),
            provider.GetRequiredService<FunPanel>()
        );

    public static DebugTabHeader CreateUnlocks(IServiceProvider provider)
        => new
        (
            "Unlocks"u8,
            provider.GetRequiredService<CustomizationUnlockPanel>(),
            provider.GetRequiredService<ItemUnlockPanel>(),
            provider.GetRequiredService<UnlockableItemsPanel>(),
            provider.GetRequiredService<InventoryPanel>()
        );
}
