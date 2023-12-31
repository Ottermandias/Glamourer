using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DesignManagerPanel(DesignManager _designManager, DesignFileSystem _designFileSystem) : IGameDataDrawer
{
    public string Label
        => $"Design Manager ({_designManager.Designs.Count} Designs)###Design Manager";

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (design, idx) in _designManager.Designs.WithIndex())
        {
            using var t = ImRaii.TreeNode($"{design.Name}##{idx}");
            if (!t)
                continue;

            DrawDesign(design, _designFileSystem);
            var base64 = DesignBase64Migration.CreateOldBase64(design.DesignData, design.ApplyEquip, design.ApplyCustomizeRaw,
                design.DoApplyHatVisible(),
                design.DoApplyVisorToggle(), design.DoApplyWeaponVisible(), design.WriteProtected());
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(base64);
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(base64);
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
                ImGuiUtil.DrawTableColumn(fileSystem.FindLeaf(d, out var leaf) ? leaf.FullName() : "No Path Known");
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

        ImGuiUtil.DrawTableColumn("Hat Visible");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsHatVisible().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyHatVisible() ? "Apply" : "Keep");
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Visor Toggled");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsVisorToggled().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyVisorToggle() ? "Apply" : "Keep");
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Weapon Visible");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsWeaponVisible().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyWeaponVisible() ? "Apply" : "Keep");
        ImGui.TableNextRow();

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

        ImGuiUtil.DrawTableColumn("Is Wet");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsWet().ToString());
        ImGui.TableNextRow();
    }
}
