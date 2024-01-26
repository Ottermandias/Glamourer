using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using ImGuiNET;
using OtterGui.Services;
using Penumbra.GameData.Files;

namespace Glamourer.Gui.Materials;

public unsafe class MaterialDrawer : IService
{
    private static readonly IReadOnlyList<MaterialValueIndex.DrawObjectType> Types =
    [
        MaterialValueIndex.DrawObjectType.Human,
        MaterialValueIndex.DrawObjectType.Mainhand,
        MaterialValueIndex.DrawObjectType.Offhand,
    ];

    public void DrawPanel(Actor actor)
    {
        if (!actor.IsCharacter)
            return;

        foreach (var type in Types)
        {
            var index = new MaterialValueIndex(type, 0, 0, 0, 0);
            if (index.TryGetModel(actor, out var model))
                DrawModelType(model, index);
        }
    }

    private void DrawModelType(Model model, MaterialValueIndex sourceIndex)
    {
        using var tree = ImRaii.TreeNode(sourceIndex.DrawObject.ToString());
        if (!tree)
            return;

        var names = model.AsCharacterBase->GetModelType() is CharacterBase.ModelType.Human
            ? SlotNamesHuman
            : SlotNames;
        for (byte i = 0; i < model.AsCharacterBase->SlotCount; ++i)
        {
            var index = sourceIndex with { SlotIndex = i };
            DrawSlot(model, names, index);
        }
    }

    private void DrawSlot(Model model, IReadOnlyList<string> names, MaterialValueIndex sourceIndex)
    {
        using var tree = ImRaii.TreeNode(names[sourceIndex.SlotIndex]);
        if (!tree)
            return;

        for (byte i = 0; i < MaterialService.MaterialsPerModel; ++i)
        {
            var index   = sourceIndex with { MaterialIndex = i };
            var texture = model.AsCharacterBase->ColorTableTextures + index.SlotIndex * MaterialService.MaterialsPerModel + i;
            if (*texture == null)
                continue;

            if (!DirectXTextureHelper.TryGetColorTable(*texture, out var table))
                continue;

            DrawMaterial(ref table, texture, index);
        }
    }

    private void DrawMaterial(ref MtrlFile.ColorTable table, Texture** texture, MaterialValueIndex sourceIndex)
    {
        using var tree = ImRaii.TreeNode($"Material {sourceIndex.MaterialIndex + 1}");
        if (!tree)
            return;

        for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
        {
            var index = sourceIndex with { RowIndex = i };
            ref var row   = ref table[i];
            DrawRow(ref table, ref row, texture, index);
        }
    }

    private void DrawRow(ref MtrlFile.ColorTable table, ref MtrlFile.ColorTable.Row row, Texture** texture, MaterialValueIndex sourceIndex)
    {
        using var id               = ImRaii.PushId(sourceIndex.RowIndex);
        var       diffuse          = row.Diffuse;
        var       specular         = row.Specular;
        var       emissive         = row.Emissive;
        var       glossStrength    = row.GlossStrength;
        var       specularStrength = row.SpecularStrength;
        if (ImGui.ColorEdit3("Diffuse", ref diffuse, ImGuiColorEditFlags.NoInputs))
        {
            var index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Diffuse };
            row.Diffuse = diffuse;
            MaterialService.ReplaceColorTable(texture, table);
        }
        ImGui.SameLine();
        if (ImGui.ColorEdit3("Specular", ref specular, ImGuiColorEditFlags.NoInputs))
        {
            var index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Specular };
            row.Specular = specular;
            MaterialService.ReplaceColorTable(texture, table);
        }
        ImGui.SameLine();
        if (ImGui.ColorEdit3("Emissive", ref emissive, ImGuiColorEditFlags.NoInputs))
        {
            var index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Emissive };
            row.Emissive = emissive;
            MaterialService.ReplaceColorTable(texture, table);
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        if (ImGui.DragFloat("Gloss", ref glossStrength, 0.1f))
        {
            var index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.GlossStrength };
            row.GlossStrength = glossStrength;
            MaterialService.ReplaceColorTable(texture, table);
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        if (ImGui.DragFloat("Specular Strength", ref specularStrength, 0.1f))
        {
            var index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.SpecularStrength };
            row.SpecularStrength = specularStrength;
            MaterialService.ReplaceColorTable(texture, table);
        }
    }

    private static readonly IReadOnlyList<string> SlotNames =
    [
        "Slot 1",
        "Slot 2",
        "Slot 3",
        "Slot 4",
        "Slot 5",
        "Slot 6",
        "Slot 7",
        "Slot 8",
        "Slot 9",
        "Slot 10",
        "Slot 11",
        "Slot 12",
        "Slot 13",
        "Slot 14",
        "Slot 15",
        "Slot 16",
        "Slot 17",
        "Slot 18",
        "Slot 19",
        "Slot 20",
    ];

    private static readonly IReadOnlyList<string> SlotNamesHuman =
    [
        "Head",
        "Body",
        "Hands",
        "Legs",
        "Feet",
        "Earrings",
        "Neck",
        "Wrists",
        "Right Finger",
        "Left Finger",
        "Slot 11",
        "Slot 12",
        "Slot 13",
        "Slot 14",
        "Slot 15",
        "Slot 16",
        "Slot 17",
        "Slot 18",
        "Slot 19",
        "Slot 20",
    ];
}
