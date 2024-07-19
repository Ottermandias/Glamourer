using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using ImGuiNET;
using OtterGui;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Gui;

namespace Glamourer.Gui.Materials;

public class MaterialDrawer(DesignManager _designManager, Configuration _config) : IService
{
    public const float GlossWidth            = 100;
    public const float SpecularStrengthWidth = 125;

    private EquipSlot          _newSlot = EquipSlot.Head;
    private int                _newMaterialIdx;
    private int                _newRowIdx;
    private MaterialValueIndex _newKey = MaterialValueIndex.FromSlot(EquipSlot.Head);

    private Vector2 _buttonSize;
    private float   _spacing;

    public void Draw(Design design)
    {
        var available = ImGui.GetContentRegionAvail().X;
        _spacing    = ImGui.GetStyle().ItemInnerSpacing.X;
        _buttonSize = new Vector2(ImGui.GetFrameHeight());
        var colorWidth = 4 * _buttonSize.X
          + (GlossWidth + SpecularStrengthWidth) * ImGuiHelpers.GlobalScale
          + 6 * _spacing
          + ImGui.CalcTextSize("Revert").X;
        if (available > 1.95 * colorWidth)
            DrawSingleRow(design);
        else
            DrawTwoRow(design);
        DrawNew(design);
    }

    private void DrawName(MaterialValueIndex index)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale).Push(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        using var color = ImRaii.PushColor(ImGuiCol.Border, ImGui.GetColorU32(ImGuiCol.Text));
        ImGuiUtil.DrawTextButton(index.ToString(), new Vector2((GlossWidth + SpecularStrengthWidth) * ImGuiHelpers.GlobalScale + _spacing, 0), 0);
    }

    private void DrawSingleRow(Design design)
    {
        for (var i = 0; i < design.Materials.Count; ++i)
        {
            using var id = ImRaii.PushId(i);
            var (idx, value) = design.Materials[i];
            var       key = MaterialValueIndex.FromKey(idx);

            DrawName(key);
            ImGui.SameLine(0, _spacing);
            DeleteButton(design, key, ref i); 
            ImGui.SameLine(0, _spacing);
            CopyButton(value.Value);
            ImGui.SameLine(0, _spacing);
            PasteButton(design, key);
            ImGui.SameLine(0, _spacing);
            EnabledToggle(design, key, value.Enabled);
            ImGui.SameLine(0, _spacing);
            DrawRow(design, key, value.Value, value.Revert);
            ImGui.SameLine(0, _spacing);
            RevertToggle(design, key, value.Revert);
        }
    }

    private void DrawTwoRow(Design design)
    {
        for (var i = 0; i < design.Materials.Count; ++i)
        {
            using var id = ImRaii.PushId(i);
            var (idx, value) = design.Materials[i];
            var key = MaterialValueIndex.FromKey(idx);

            DrawName(key);
            ImGui.SameLine(0, _spacing);
            DeleteButton(design, key, ref i);
            ImGui.SameLine(0, _spacing);
            CopyButton(value.Value);
            ImGui.SameLine(0, _spacing);
            PasteButton(design, key);
            ImGui.SameLine(0, _spacing);
            EnabledToggle(design, key, value.Enabled);
            

            DrawRow(design, key, value.Value, value.Revert);
            ImGui.SameLine(0, _spacing);
            RevertToggle(design, key, value.Revert);
            ImGui.Separator();
        }
    }

    private void DeleteButton(Design design, MaterialValueIndex index, ref int idx)
    {
        var deleteEnabled = _config.DeleteDesignModifier.IsActive();
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), _buttonSize,
                $"Delete this color row.{(deleteEnabled ? string.Empty : $"\nHold {_config.DeleteDesignModifier} to delete.")}",
                !deleteEnabled, true))
            return;

        _designManager.ChangeMaterialValue(design, index, null);
        --idx;
    }

    private void CopyButton(in ColorRow row)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), _buttonSize, "Export this row to your clipboard.",
                false,
                true))
            ColorRowClipboard.Row = row;
    }

    private void PasteButton(Design design, MaterialValueIndex index)
    {
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Paste.ToIconString(), _buttonSize,
                "Import an exported row from your clipboard onto this row.", !ColorRowClipboard.IsSet, true))
            _designManager.ChangeMaterialValue(design, index, ColorRowClipboard.Row);
    }

    private void EnabledToggle(Design design, MaterialValueIndex index, bool enabled)
    {
        if (ImGui.Checkbox("Enabled", ref enabled))
            _designManager.ChangeApplyMaterialValue(design, index, enabled);
    }

    private void RevertToggle(Design design, MaterialValueIndex index, bool revert)
    {
        if (ImGui.Checkbox("Revert", ref revert))
            _designManager.ChangeMaterialRevert(design, index, revert);
        ImGuiUtil.HoverTooltip(
            "If this is checked, Glamourer will try to revert the advanced dye row to its game state instead of applying a specific row.");
    }

    public void DrawNew(Design design)
    {
        if (EquipSlotCombo.Draw("##slot", "Choose a slot for an advanced dye row.", ref _newSlot))
            _newKey = MaterialValueIndex.FromSlot(_newSlot) with
            {
                MaterialIndex = (byte)_newMaterialIdx,
                RowIndex = (byte)_newRowIdx,
            };
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawMaterialIdxDrag();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawRowIdxDrag();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        var exists = design.GetMaterialDataRef().TryGetValue(_newKey, out _);
        if (ImGuiUtil.DrawDisabledButton("Add New Row", Vector2.Zero, 
                exists ? "The selected advanced dye row already exists." : "Add the selected advanced dye row.", exists, false))
            _designManager.ChangeMaterialValue(design, _newKey, ColorRow.Empty);
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
        if (ImGui.DragInt("##Row", ref _newRowIdx, 0.01f, 1, ColorTable.NumRows, "Row #%i"))
        {
            _newRowIdx = Math.Clamp(_newRowIdx, 1, ColorTable.NumRows);
            _newKey    = _newKey with { RowIndex = (byte)(_newRowIdx - 1) };
        }

        _newRowIdx -= 1;
    }

    private void DrawRow(Design design, MaterialValueIndex index, in ColorRow row, bool disabled)
    {
        var tmp = row;
        using var _ = ImRaii.Disabled(disabled);
        var applied = ImGuiUtil.ColorPicker("##diffuse", "Change the diffuse value for this row.", row.Diffuse, v => tmp.Diffuse = v, "D");
        ImGui.SameLine(0, _spacing);
        applied |= ImGuiUtil.ColorPicker("##specular", "Change the specular value for this row.", row.Specular, v => tmp.Specular = v, "S");
        ImGui.SameLine(0, _spacing);
        applied |= ImGuiUtil.ColorPicker("##emissive", "Change the emissive value for this row.", row.Emissive, v => tmp.Emissive = v, "E");
        ImGui.SameLine(0, _spacing);
        ImGui.SetNextItemWidth(GlossWidth * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Gloss", ref tmp.GlossStrength, 0.01f, 0.001f, float.MaxValue, "%.3f G");
        ImGuiUtil.HoverTooltip("Change the gloss strength for this row.");
        ImGui.SameLine(0, _spacing);
        ImGui.SetNextItemWidth(SpecularStrengthWidth * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Specular Strength", ref tmp.SpecularStrength, 0.01f, float.MinValue, float.MaxValue, "%.3f SS");
        ImGuiUtil.HoverTooltip("Change the specular strength for this row.");
        if (applied)
            _designManager.ChangeMaterialValue(design, index, tmp);
    }
}
