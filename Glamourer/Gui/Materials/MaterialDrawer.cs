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
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;
using Vortice.DXGI;

namespace Glamourer.Gui.Materials;

public unsafe class MaterialDrawer(StateManager _stateManager, DesignManager _designManager, Configuration _config) : IService
{
    private ActorState?        _state;
    private EquipSlot          _newSlot = EquipSlot.Head;
    private int                _newMaterialIdx;
    private int                _newRowIdx;
    private MaterialValueIndex _newKey = MaterialValueIndex.Min();

    public void DrawDesignPanel(Design design)
    {
        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        using (var table = ImRaii.Table("table", 5, ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;


            ImGui.TableSetupColumn("button",  ImGuiTableColumnFlags.WidthFixed, buttonSize.X);
            ImGui.TableSetupColumn("enabled", ImGuiTableColumnFlags.WidthFixed, buttonSize.X);
            ImGui.TableSetupColumn("values",  ImGuiTableColumnFlags.WidthFixed, ImGui.GetStyle().ItemInnerSpacing.X * 4 + 3 * buttonSize.X + 220 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("revert",  ImGuiTableColumnFlags.WidthFixed, buttonSize.X + ImGui.CalcTextSize("Revertm").X);
            ImGui.TableSetupColumn("slot",    ImGuiTableColumnFlags.WidthStretch);
            
            

            for (var i = 0; i < design.Materials.Count; ++i)
            {
                var (idx, value) = design.Materials[i];
                var key = MaterialValueIndex.FromKey(idx);
                var name = key.DrawObject switch
                {
                    MaterialValueIndex.DrawObjectType.Human    => ((uint)key.SlotIndex).ToEquipSlot().ToName(),
                    MaterialValueIndex.DrawObjectType.Mainhand => EquipSlot.MainHand.ToName(),
                    MaterialValueIndex.DrawObjectType.Offhand  => EquipSlot.OffHand.ToName(),
                    _                                          => string.Empty,
                };
                if (name.Length == 0)
                    continue;

                name = $"{name} Material #{key.MaterialIndex + 1} Row #{key.RowIndex + 1}";
                using var id            = ImRaii.PushId((int)idx);
                var       deleteEnabled = _config.DeleteDesignModifier.IsActive();
                ImGui.TableNextColumn();
                if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize,
                        $"Delete this color row.{(deleteEnabled ? string.Empty : $"\nHold {_config.DeleteDesignModifier} to delete.")}",
                        !deleteEnabled, true))
                {
                    _designManager.ChangeMaterialValue(design, key, null);
                    --i;
                }

                ImGui.TableNextColumn();
                var enabled = value.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                    _designManager.ChangeApplyMaterialValue(design, key, enabled);

                ImGui.TableNextColumn();
                var revert = value.Revert;
                using (ImRaii.Disabled(revert))
                {
                    var row = value.Value;
                    DrawRow(design, key, row);
                }

                ImGui.TableNextColumn();

                if (ImGui.Checkbox("Revert", ref revert))
                    _designManager.ChangeMaterialRevert(design, key, revert);
                ImGuiUtil.HoverTooltip(
                    "If this is checked, Glamourer will try to revert the advanced dye row to its game state instead of applying a specific row.");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(name);
            }
        }

        var exists = design.GetMaterialDataRef().TryGetValue(_newKey, out _);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), buttonSize,
                exists ? "The selected advanced dye row already exists." : "Add the selected advanced dye row.", exists, true))
            _designManager.ChangeMaterialValue(design, _newKey, ColorRow.Empty);

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        if (EquipSlotCombo.Draw("##slot", "Choose a slot for an advanced dye row.", ref _newSlot))
            _newKey = _newSlot switch
            {
                EquipSlot.MainHand => new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Mainhand, 0, (byte)_newMaterialIdx,
                    (byte)_newRowIdx),
                EquipSlot.OffHand => new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Offhand, 0, (byte)_newMaterialIdx,
                    (byte)_newRowIdx),
                _ => new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Human, (byte)_newSlot.ToIndex(), (byte)_newMaterialIdx,
                    (byte)_newRowIdx),
            };
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawMaterialIdxDrag();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawRowIdxDrag();
    }

    private void DrawMaterialIdxDrag()
    {
        _newMaterialIdx += 1;
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("Material #000").X);
        if (ImGui.DragInt("##Material", ref _newMaterialIdx, 0.01f, 1, MaterialService.MaterialsPerModel, "Material #%i"))
        {
            _newMaterialIdx = Math.Clamp(_newMaterialIdx, 1, MaterialService.MaterialsPerModel);
            _newKey         = _newKey with { MaterialIndex = (byte)(_newMaterialIdx - 1) };
        }

        _newMaterialIdx -= 1;
    }

    private void DrawRowIdxDrag()
    {
        _newRowIdx += 1;
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("Row #0000").X);
        if (ImGui.DragInt("##Row", ref _newRowIdx, 0.01f, 1, MtrlFile.ColorTable.NumRows, "Row #%i"))
        {
            _newRowIdx = Math.Clamp(_newRowIdx, 1, MtrlFile.ColorTable.NumRows);
            _newKey    = _newKey with { RowIndex = (byte)(_newRowIdx - 1) };
        }

        _newRowIdx -= 1;
    }

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
            DrawSlotMaterials(model, slot.ToName(), item, new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Human, (byte)idx, 0, 0));
        }

        var (mainhand, offhand, mh, oh) = actor.Model.GetWeapons(actor);
        if (mainhand.IsWeapon && mainhand.AsCharacterBase->SlotCount > 0)
            DrawSlotMaterials(mainhand, EquipSlot.MainHand.ToName(), mh,
                new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Mainhand, 0, 0, 0));
        if (offhand.IsWeapon && offhand.AsCharacterBase->SlotCount > 0)
            DrawSlotMaterials(offhand, EquipSlot.OffHand.ToName(), oh,
                new MaterialValueIndex(MaterialValueIndex.DrawObjectType.Offhand, 0, 0, 0));
    }


    private void DrawSlotMaterials(Model model, string name, CharacterWeapon drawData, MaterialValueIndex index)
    {
        for (byte materialIndex = 0; materialIndex < MaterialService.MaterialsPerModel; ++materialIndex)
        {
            var texture = model.AsCharacterBase->ColorTableTextures + index.SlotIndex * MaterialService.MaterialsPerModel + materialIndex;
            if (*texture == null)
                continue;

            if (!DirectXTextureHelper.TryGetColorTable(*texture, out var table))
                continue;

            using var tree = ImRaii.TreeNode($"{name} Material #{materialIndex + 1}###{name}{materialIndex}");
            if (!tree)
                continue;

            DrawMaterial(ref table, drawData, index with { MaterialIndex = materialIndex });
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

    private void DrawRow(Design design, MaterialValueIndex index, in ColorRow row)
    {
        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        var tmp     = row;
        var applied = ImGuiUtil.ColorPicker("##diffuse", "Change the diffuse value for this row.", row.Diffuse, v => tmp.Diffuse = v, "D");
        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##specular", "Change the specular value for this row.", row.Specular, v => tmp.Specular = v, "S");
        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##emissive", "Change the emissive value for this row.", row.Emissive, v => tmp.Emissive = v, "E");
        ImGui.SameLine(0, spacing.X);
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Gloss", ref tmp.GlossStrength, 0.01f, 0.001f, float.MaxValue, "%.3f G");
        ImGuiUtil.HoverTooltip("Change the gloss strength for this row.");
        ImGui.SameLine(0, spacing.X);
        ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Specular Strength", ref tmp.SpecularStrength, 0.01f, float.MinValue, float.MaxValue, "%.3f SS");
        ImGuiUtil.HoverTooltip("Change the specular strength for this row.");
        if (applied)
            _designManager.ChangeMaterialValue(design, index, tmp);
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

        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGui.TextUnformatted($"Row {index.RowIndex + 1:D2}");
        }

        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
        var applied = ImGuiUtil.ColorPicker("##diffuse", "Change the diffuse value for this row.", value.Model.Diffuse, v => value.Model.Diffuse = v, "D");

        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##specular", "Change the specular value for this row.", value.Model.Specular, v => value.Model.Specular = v, "S");
        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##emissive", "Change the emissive value for this row.", value.Model.Emissive, v => value.Model.Emissive = v, "E");
        ImGui.SameLine(0, spacing.X);
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Gloss", ref value.Model.GlossStrength, 0.01f, 0.001f, float.MaxValue, "%.3f G") && value.Model.GlossStrength > 0;
        ImGuiUtil.HoverTooltip("Change the gloss strength for this row.");
        ImGui.SameLine(0, spacing.X);
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Specular Strength", ref value.Model.SpecularStrength, 0.01f, float.MinValue, float.MaxValue, "%.3f SS");
        ImGuiUtil.HoverTooltip("Change the specular strength for this row.");
        if (applied)
            _stateManager.ChangeMaterialValue(_state!, index, value, ApplySettings.Manual);
        if (changed)
        {
            ImGui.SameLine(0, spacing.X);
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.FavoriteStarOn.Value());
                ImGui.TextUnformatted(FontAwesomeIcon.UserEdit.ToIconString());
            }
        }
    }
}
