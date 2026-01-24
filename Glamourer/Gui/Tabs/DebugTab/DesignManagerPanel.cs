using Dalamud.Interface;
using Glamourer.Designs;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DesignManagerPanel(DesignManager designManager, DesignFileSystem designFileSystem) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => new StringU8($"Design Manager ({designManager.Designs.Count} Designs)###Design Manager");

    public bool Disabled
        => false;

    public void Draw()
    {
        DrawButtons();
        foreach (var (design, idx) in designManager.Designs.WithIndex())
        {
            using var t = ImRaii.TreeNode($"{design.Name}##{idx}");
            if (!t)
                continue;

            DrawDesign(design, designFileSystem);
            var base64 = DesignBase64Migration.CreateOldBase64(design.DesignData, design.Application.Equip, design.Application.Customize,
                design.Application.Meta,
                design.WriteProtected());
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(base64);
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(base64);
        }
    }

    private void DrawButtons()
    {
        if (ImUtf8.Button("Generate 500 Test Designs"u8))
            for (var i = 0; i < 500; ++i)
            {
                var design = designManager.CreateEmpty($"Test Designs/Test Design {i}", true);
                designManager.AddTag(design, "_DebugTest");
            }

        ImUtf8.SameLineInner();
        if (ImUtf8.Button("Remove All Test Designs"u8))
        {
            var designs = designManager.Designs.Where(d => d.Tags.Contains("_DebugTest")).ToArray();
            foreach (var design in designs)
                designManager.Delete(design);
            if (designFileSystem.Find("Test Designs", out var path) && path is DesignFileSystem.Folder { TotalChildren: 0 })
                designFileSystem.Delete(path);
        }
    }

    public static void DrawDesign(DesignBase design, DesignFileSystem? fileSystem)
    {
        using var table = ImRaii.Table("##equip", 8, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (design is Design d)
        {
            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(d.Name);
            ImGuiUtil.DrawTableColumn($"({d.Index})");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Description (Hover)");
            ImGuiUtil.HoverTooltip(d.Description);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(d.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Design File System Path");
            if (fileSystem != null)
                ImGuiUtil.DrawTableColumn(fileSystem.TryGetValue(d, out var leaf) ? leaf.FullName() : "No Path Known");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Creation");
            ImGuiUtil.DrawTableColumn(d.CreationDate.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Update");
            ImGuiUtil.DrawTableColumn(d.LastEdit.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Tags");
            ImGuiUtil.DrawTableColumn(string.Join(", ", d.Tags));
            ImGui.TableNextRow();
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item       = design.DesignData.Item(slot);
            var apply      = design.DoApplyEquip(slot);
            var stain      = design.DesignData.Stain(slot);
            var applyStain = design.DoApplyStain(slot);
            var crest      = design.DesignData.Crest(slot.ToCrestFlag());
            var applyCrest = design.DoApplyCrest(slot.ToCrestFlag());
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(item.Name);
            ImGuiUtil.DrawTableColumn(item.ItemId.ToString());
            ImGuiUtil.DrawTableColumn(apply ? "Apply" : "Keep");
            ImGuiUtil.DrawTableColumn(stain.ToString());
            ImGuiUtil.DrawTableColumn(applyStain ? "Apply" : "Keep");
            ImGuiUtil.DrawTableColumn(crest.ToString());
            ImGuiUtil.DrawTableColumn(applyCrest ? "Apply" : "Keep");
        }

        foreach (var index in MetaExtensions.AllRelevant)
        {
            ImGuiUtil.DrawTableColumn(index.ToName());
            ImGuiUtil.DrawTableColumn(design.DesignData.GetMeta(index).ToString());
            ImGuiUtil.DrawTableColumn(design.DoApplyMeta(index) ? "Apply" : "Keep");
            ImGui.TableNextRow();
        }

        ImGuiUtil.DrawTableColumn("Model ID");
        ImGuiUtil.DrawTableColumn(design.DesignData.ModelId.ToString());
        ImGui.TableNextRow();

        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var value = design.DesignData.Customize[index];
            var apply = design.DoApplyCustomize(index);
            ImGuiUtil.DrawTableColumn(index.ToDefaultName());
            ImGuiUtil.DrawTableColumn(value.Value.ToString());
            ImGuiUtil.DrawTableColumn(apply ? "Apply" : "Keep");
            ImGui.TableNextRow();
        }
    }
}
