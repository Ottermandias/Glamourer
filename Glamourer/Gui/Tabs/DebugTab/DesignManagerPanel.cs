using Glamourer.Designs;
using ImSharp;
using Luna;
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
        foreach (var (idx, design) in designManager.Designs.Index())
        {
            using var id = Im.Id.Push(idx);
            using var t  = Im.Tree.Node(design.Name.Text);
            if (!t)
                continue;

            DrawDesign(design, designFileSystem);
            var base64 = DesignBase64Migration.CreateOldBase64(design.DesignData, design.Application.Equip, design.Application.Customize,
                design.Application.Meta,
                design.WriteProtected());
            using var font = Im.Font.PushMono();
            Im.TextWrapped(base64);
            if (Im.Item.Clicked())
                Im.Clipboard.Set(base64);
        }
    }

    private void DrawButtons()
    {
        if (Im.Button("Generate 500 Test Designs"u8))
            for (var i = 0; i < 500; ++i)
            {
                var design = designManager.CreateEmpty($"Test Designs/Test Design {i}", true);
                designManager.AddTag(design, "_DebugTest");
            }

        Im.Line.SameInner();
        if (Im.Button("Remove All Test Designs"u8))
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
        using var table = Im.Table.Begin("##equip"u8, 8, TableFlags.RowBackground | TableFlags.SizingFixedFit);
        if (design is Design d)
        {
            table.DrawColumn("Name"u8);
            table.DrawColumn(d.Name.Text);
            table.DrawColumn($"({d.Index})");
            table.DrawColumn("Description (Hover)"u8);
            Im.Tooltip.OnHover(d.Description);
            table.NextRow();

            table.DrawDataPair("Identifier"u8, d.Identifier);
            table.NextRow();
            table.DrawColumn("Design File System Path"u8);
            if (fileSystem is not null)
                table.DrawColumn(fileSystem.TryGetValue(d, out var leaf) ? leaf.FullName() : "No Path Known"u8);
            table.NextRow();

            table.DrawDataPair("Creation"u8, d.CreationDate);
            table.NextRow();
            table.DrawDataPair("Update"u8, d.LastEdit);
            table.NextRow();
            table.DrawDataPair("Tags"u8, StringU8.Join(", "u8, d.Tags));
            table.NextRow();
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item       = design.DesignData.Item(slot);
            var apply      = design.DoApplyEquip(slot);
            var stain      = design.DesignData.Stain(slot);
            var applyStain = design.DoApplyStain(slot);
            var crest      = design.DesignData.Crest(slot.ToCrestFlag());
            var applyCrest = design.DoApplyCrest(slot.ToCrestFlag());
            table.DrawColumn(slot.ToNameU8());
            table.DrawColumn(item.Name);
            table.DrawColumn($"{item.ItemId}");
            table.DrawColumn(apply ? "Apply"u8 : "Keep"u8);
            table.DrawColumn($"{stain}");
            table.DrawColumn(applyStain ? "Apply"u8 : "Keep"u8);
            table.DrawColumn($"{crest}");
            table.DrawColumn(applyCrest ? "Apply"u8 : "Keep"u8);
        }

        foreach (var index in MetaExtensions.AllRelevant)
        {
            table.DrawColumn(index.ToNameU8());
            table.DrawColumn($"{design.DesignData.GetMeta(index)}");
            table.DrawColumn(design.DoApplyMeta(index) ? "Apply"u8 : "Keep"u8);
            table.NextRow();
        }

        table.DrawDataPair("Model ID"u8, design.DesignData.ModelId);
        table.NextRow();

        foreach (var index in CustomizeIndex.Values)
        {
            var value = design.DesignData.Customize[index];
            var apply = design.DoApplyCustomize(index);
            table.DrawColumn(index.ToNameU8());
            table.DrawColumn($"{value.Value}");
            table.DrawColumn(apply ? "Apply"u8 : "Keep"u8);
            table.NextRow();
        }
    }
}
