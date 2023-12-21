using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Raii;
using OtterGui.Services;

namespace Glamourer.Gui.Tabs.DebugTab;

public interface IDebugTabTree : IService
{
    public string Label { get; }
    public void   Draw();

    public bool Disabled { get; }
}

public class DebugTabHeader(string label, params IDebugTabTree[] subTrees)
{
    public string                       Label    { get; } = label;
    public IReadOnlyList<IDebugTabTree> SubTrees { get; } = subTrees;

    public void Draw()
    {
        if (!ImGui.CollapsingHeader(Label))
            return;

        foreach (var subTree in SubTrees)
        {
            using (var disabled = ImRaii.Disabled(subTree.Disabled))
            {
                using var tree = ImRaii.TreeNode(subTree.Label);
                if (!tree)
                    continue;
            }

            subTree.Draw();
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
            provider.GetRequiredService<DatFilePanel>()
        );

    public static DebugTabHeader CreateGameData(IServiceProvider provider)
        => new
        (
            "Game Data",
            provider.GetRequiredService<IdentifierPanel>(),
            provider.GetRequiredService<RestrictedGearPanel>(),
            provider.GetRequiredService<ActorManagerPanel>(),
            provider.GetRequiredService<ItemManagerPanel>(),
            provider.GetRequiredService<StainPanel>(),
            provider.GetRequiredService<CustomizationServicePanel>(),
            provider.GetRequiredService<JobPanel>(),
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
