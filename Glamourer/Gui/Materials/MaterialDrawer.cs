using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Services;
using Penumbra.GameData.Files;

namespace Glamourer.Gui.Materials;

public unsafe class MaterialDrawer(StateManager _stateManager) : IService
{
    private static readonly IReadOnlyList<MaterialValueIndex.DrawObjectType> Types =
    [
        MaterialValueIndex.DrawObjectType.Human,
        MaterialValueIndex.DrawObjectType.Mainhand,
        MaterialValueIndex.DrawObjectType.Offhand,
    ];

    private ActorState? _state;

    public void DrawPanel(Actor actor)
    {
        if (!actor.IsCharacter || !_stateManager.GetOrCreate(actor, out _state))
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

            DrawMaterial(ref table, index);
        }
    }

    private void DrawMaterial(ref MtrlFile.ColorTable table, MaterialValueIndex sourceIndex)
    {
        using var tree = ImRaii.TreeNode($"Material {sourceIndex.MaterialIndex + 1}");
        if (!tree)
            return;

        for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
        {
            var     index = sourceIndex with { RowIndex = i };
            ref var row   = ref table[i];
            DrawRow(ref row, index);
        }
    }

    private void DrawRow(ref MtrlFile.ColorTable.Row row, MaterialValueIndex sourceIndex)
    {
        var r = _state!.Materials.GetValues(
            MaterialValueIndex.Min(sourceIndex.DrawObject, sourceIndex.SlotIndex, sourceIndex.MaterialIndex, sourceIndex.RowIndex),
            MaterialValueIndex.Max(sourceIndex.DrawObject, sourceIndex.SlotIndex, sourceIndex.MaterialIndex, sourceIndex.RowIndex));

        var highlightColor = ColorId.FavoriteStarOn.Value();

        using var id    = ImRaii.PushId(sourceIndex.RowIndex);
        var       index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Diffuse };
        var (diffuse, diffuseGame, changed) = MaterialValueManager.GetSpecific(r, index, out var d)
            ? (d.Model, d.Game, true)
            : (row.Diffuse, row.Diffuse, false);
        using (ImRaii.PushColor(ImGuiCol.Text, highlightColor, changed))
        {
            if (ImGui.ColorEdit3("Diffuse", ref diffuse, ImGuiColorEditFlags.NoInputs))
                _stateManager.ChangeMaterialValue(_state!, index, diffuse, diffuseGame, ApplySettings.Manual);
        }


        index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Specular };
        (var specular, var specularGame, changed) = MaterialValueManager.GetSpecific(r, index, out var s)
            ? (s.Model, s.Game, true)
            : (row.Specular, row.Specular, false);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, highlightColor, changed))
        {
            if (ImGui.ColorEdit3("Specular", ref specular, ImGuiColorEditFlags.NoInputs))
                _stateManager.ChangeMaterialValue(_state!, index, specular, specularGame, ApplySettings.Manual);
        }

        index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.Emissive };
        (var emissive, var emissiveGame, changed) = MaterialValueManager.GetSpecific(r, index, out var e)
            ? (e.Model, e.Game, true)
            : (row.Emissive, row.Emissive, false);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, highlightColor, changed))
        {
            if (ImGui.ColorEdit3("Emissive", ref emissive, ImGuiColorEditFlags.NoInputs))
                _stateManager.ChangeMaterialValue(_state!, index, emissive, emissiveGame, ApplySettings.Manual);
        }

        index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.GlossStrength };
        (var glossStrength, var glossStrengthGame, changed) = MaterialValueManager.GetSpecific(r, index, out var g)
            ? (g.Model.X, g.Game.X, true)
            : (row.GlossStrength, row.GlossStrength, false);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, highlightColor, changed))
        {
            if (ImGui.DragFloat("Gloss", ref glossStrength, 0.1f))
                _stateManager.ChangeMaterialValue(_state!, index, new Vector3(glossStrength), new Vector3(glossStrengthGame),
                    ApplySettings.Manual);
        }

        index = sourceIndex with { DataIndex = MaterialValueIndex.ColorTableIndex.SpecularStrength };
        (var specularStrength, var specularStrengthGame, changed) = MaterialValueManager.GetSpecific(r, index, out var ss)
            ? (ss.Model.X, ss.Game.X, true)
            : (row.SpecularStrength, row.SpecularStrength, false);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, highlightColor, changed))
        {
            if (ImGui.DragFloat("Specular Strength", ref specularStrength, 0.1f))
                _stateManager.ChangeMaterialValue(_state!, index, new Vector3(specularStrength), new Vector3(specularStrengthGame),
                    ApplySettings.Manual);
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
