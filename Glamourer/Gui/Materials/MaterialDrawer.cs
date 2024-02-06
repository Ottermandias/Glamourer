using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Materials;

public unsafe class MaterialDrawer(StateManager _stateManager, DesignManager _designManager) : IService
{
    private static readonly IReadOnlyList<MaterialValueIndex.DrawObjectType> Types =
    [
        MaterialValueIndex.DrawObjectType.Human,
        MaterialValueIndex.DrawObjectType.Mainhand,
        MaterialValueIndex.DrawObjectType.Offhand,
    ];

    private ActorState? _state;

    public void DrawActorPanel(Actor actor)
    {
        if (!actor.IsCharacter || !_stateManager.GetOrCreate(actor, out _state))
            return;

        var model = actor.Model;
        if (!model.IsHuman)
            return;

        if (model.AsCharacterBase->SlotCount < 10)
            return;

        // Humans should have at least 10 slots for the equipment types. Technically more.
        foreach (var (slot, idx) in EquipSlotExtensions.EqdpSlots.WithIndex())
        {
            var item = model.GetArmor(slot).ToWeapon(0);
            DrawSlotMaterials(model, slot.ToName(), item, new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Human, (byte) idx, 0, 0));
        }

        var (mainhand, offhand, mh, oh) = actor.Model.GetWeapons(actor);
        if (mainhand.IsWeapon && mainhand.AsCharacterBase->SlotCount > 0)
            DrawSlotMaterials(mainhand, EquipSlot.MainHand.ToName(), mh, new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Mainhand, 0, 0, 0));
        if (offhand.IsWeapon && offhand.AsCharacterBase->SlotCount > 0)
            DrawSlotMaterials(offhand, EquipSlot.OffHand.ToName(), oh, new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Offhand, 0, 0, 0));
    }


    private void DrawSlotMaterials(Model model, string name, CharacterWeapon drawData, MaterialValueIndex index)
    {
        var drawnMaterial = 1;
        for (byte materialIndex = 0; materialIndex < MaterialService.MaterialsPerModel; ++materialIndex)
        {
            var texture = model.AsCharacterBase->ColorTableTextures + index.SlotIndex * MaterialService.MaterialsPerModel + materialIndex;
            if (*texture == null)
                continue;

            if (!DirectXTextureHelper.TryGetColorTable(*texture, out var table))
                continue;

            using var tree = ImRaii.TreeNode($"{name} Material #{drawnMaterial++}###{name}{materialIndex}");
            if (!tree)
                continue;

            DrawMaterial(ref table, drawData, index with { MaterialIndex = materialIndex} );
        }
    }

    private void DrawMaterial(ref MtrlFile.ColorTable table, CharacterWeapon drawData, MaterialValueIndex sourceIndex)
    {
        for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
        {
            var     index = sourceIndex with { RowIndex = i };
            ref var row   = ref table[i];
            DrawRow(ref row, drawData, index);
        }
    }

    private void DrawRow(ref MtrlFile.ColorTable.Row row, CharacterWeapon drawData, MaterialValueIndex index)
    {
        using var id      = ImRaii.PushId(index.RowIndex);
        var       changed = _state!.Materials.TryGetValue(index, out var value);
        if (!changed)
        {
            var internalRow = new ColorRow(row);
            value = new MaterialValueState(internalRow, internalRow, drawData, StateSource.Manual);
        }

        var applied = ImGui.ColorEdit3("Diffuse", ref value.Model.Diffuse, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        applied |= ImGui.ColorEdit3("Specular", ref value.Model.Specular, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        applied |= ImGui.ColorEdit3("Emissive", ref value.Model.Emissive, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("Gloss", ref value.Model.GlossStrength, 0.1f);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("Specular Strength", ref value.Model.SpecularStrength, 0.1f);
        if (applied)
            _stateManager.ChangeMaterialValue(_state!, index, value, ApplySettings.Manual);
        if (changed)
        {
            ImGui.SameLine();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.FavoriteStarOn.Value());
                ImGui.TextUnformatted(FontAwesomeIcon.UserEdit.ToIconString());
            }
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
