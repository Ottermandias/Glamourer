using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Dalamud.Bindings.ImGui;
using Luna;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;

namespace Glamourer.Gui.Materials;

public class MaterialDrawer(DesignManager designManager, Configuration config) : IService
{
    public const float GlossWidth            = 100;
    public const float SpecularStrengthWidth = 125;

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
          + ImUtf8.CalcTextSize("Revert"u8).X;
        DrawMultiButtons(design);
        ImUtf8.Dummy(0);
        ImGui.Separator();
        ImUtf8.Dummy(0);
        if (available > 1.95 * colorWidth)
            DrawSingleRow(design);
        else
            DrawTwoRow(design);
        DrawNew(design);
    }

    private void DrawMultiButtons(Design design)
    {
        var any      = design.Materials.Count > 0;
        var disabled = !config.DeleteDesignModifier.IsActive();
        var size     = new Vector2(200 * ImUtf8.GlobalScale, 0);
        if (ImUtf8.ButtonEx("Enable All Advanced Dyes"u8,
                any
                    ? "Enable the application of all contained advanced dyes without deleting them."u8
                    : "This design does not contain any advanced dyes."u8, size,
                !any || disabled))
            designManager.ChangeApplyMulti(design, null, null, null, null, null, null, true, null);
        ;
        if (disabled && any)
            ImUtf8.HoverTooltip($"Hold {config.DeleteDesignModifier} while clicking to enable.");
        ImGui.SameLine();
        if (ImUtf8.ButtonEx("Disable All Advanced Dyes"u8,
                any
                    ? "Disable the application of all contained advanced dyes without deleting them."u8
                    : "This design does not contain any advanced dyes."u8, size,
                !any || disabled))
            designManager.ChangeApplyMulti(design, null, null, null, null, null, null, false, null);
        if (disabled && any)
            ImUtf8.HoverTooltip($"Hold {config.DeleteDesignModifier} while clicking to disable.");

        if (ImUtf8.ButtonEx("Delete All Advanced Dyes"u8, any ? ""u8 : "This design does not contain any advanced dyes."u8, size,
                !any || disabled))
            while (design.Materials.Count > 0)
                designManager.ChangeMaterialValue(design, MaterialValueIndex.FromKey(design.Materials[0].Item1), null);

        if (disabled && any)
            ImUtf8.HoverTooltip($"Hold {config.DeleteDesignModifier} while clicking to delete.");
    }

    private void DrawName(MaterialValueIndex index)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.05f, 0.5f));
        ImUtf8.TextFramed(index.ToString(), 0, new Vector2((GlossWidth + SpecularStrengthWidth) * ImGuiHelpers.GlobalScale + _spacing, 0),
            borderColor: ImGui.GetColorU32(ImGuiCol.Text));
    }

    private void DrawSingleRow(Design design)
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
            using var disabled = ImRaii.Disabled(design.WriteProtected());
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
        var deleteEnabled = config.DeleteDesignModifier.IsActive();
        if (!ImUtf8.IconButton(FontAwesomeIcon.Trash,
                $"Delete this color row.{(deleteEnabled ? string.Empty : $"\nHold {config.DeleteDesignModifier} to delete.")}", disabled:
                !deleteEnabled || design.WriteProtected()))
            return;

        designManager.ChangeMaterialValue(design, index, null);
        --idx;
    }

    private void CopyButton(in ColorRow row)
    {
        if (ImUtf8.IconButton(FontAwesomeIcon.Clipboard, "Export this row to your clipboard."u8))
            ColorRowClipboard.Row = row;
    }

    private void PasteButton(Design design, MaterialValueIndex index)
    {
        if (ImUtf8.IconButton(FontAwesomeIcon.Paste, "Import an exported row from your clipboard onto this row."u8,
                disabled: !ColorRowClipboard.IsSet || design.WriteProtected()))
            designManager.ChangeMaterialValue(design, index, ColorRowClipboard.Row);
    }

    private void EnabledToggle(Design design, MaterialValueIndex index, bool enabled)
    {
        if (ImUtf8.Checkbox("Enabled"u8, ref enabled))
            designManager.ChangeApplyMaterialValue(design, index, enabled);
    }

    private void RevertToggle(Design design, MaterialValueIndex index, bool revert)
    {
        if (ImUtf8.Checkbox("Revert"u8, ref revert))
            designManager.ChangeMaterialRevert(design, index, revert);
        ImUtf8.HoverTooltip(
            "If this is checked, Glamourer will try to revert the advanced dye row to its game state instead of applying a specific row."u8);
    }

    public sealed class MaterialSlotCombo;

    private void DrawSlotCombo()
    {
        var width = ImUtf8.CalcTextSize(EquipSlot.OffHand.ToName()).X + ImGui.GetFrameHeightWithSpacing();
        ImGui.SetNextItemWidth(width);
        using var combo = ImUtf8.Combo("##slot"u8, _newKey.SlotName());
        if (combo)
        {
            var currentSlot = _newKey.ToEquipSlot();
            foreach (var tmpSlot in EquipSlotExtensions.FullSlots)
            {
                if (ImUtf8.Selectable(tmpSlot.ToName(), tmpSlot == currentSlot) && currentSlot != tmpSlot)
                    _newKey = MaterialValueIndex.FromSlot(tmpSlot) with
                    {
                        MaterialIndex = (byte)_newMaterialIdx,
                        RowIndex = (byte)_newRowIdx,
                    };
            }

            var currentBonus = _newKey.ToBonusSlot();
            foreach (var bonusSlot in BonusExtensions.AllFlags)
            {
                if (ImUtf8.Selectable(bonusSlot.ToName(), bonusSlot == currentBonus) && bonusSlot != currentBonus)
                    _newKey = MaterialValueIndex.FromSlot(bonusSlot) with
                    {
                        MaterialIndex = (byte)_newMaterialIdx,
                        RowIndex = (byte)_newRowIdx,
                    };
            }
        }

        ImUtf8.HoverTooltip("Choose a slot for an advanced dye row."u8);
    }

    public void DrawNew(Design design)
    {
        DrawSlotCombo();
        ImUtf8.SameLineInner();
        DrawMaterialIdxDrag();
        ImUtf8.SameLineInner();
        DrawRowIdxDrag();
        ImUtf8.SameLineInner();
        var exists = design.GetMaterialDataRef().TryGetValue(_newKey, out _);
        if (ImUtf8.ButtonEx("Add New Row"u8,
                exists ? "The selected advanced dye row already exists."u8 : "Add the selected advanced dye row."u8, Vector2.Zero,
                exists || design.WriteProtected()))
            designManager.ChangeMaterialValue(design, _newKey, ColorRow.Empty);
    }

    private void DrawMaterialIdxDrag()
    {
        ImGui.SetNextItemWidth(ImUtf8.CalcTextSize("Material AA"u8).X);
        var format = $"Material {(char)('A' + _newMaterialIdx)}";
        if (ImUtf8.DragScalar("##Material"u8, ref _newMaterialIdx, format, 0, MaterialService.MaterialsPerModel - 1, 0.01f,
                ImGuiSliderFlags.NoInput))
        {
            _newMaterialIdx = Math.Clamp(_newMaterialIdx, 0, MaterialService.MaterialsPerModel - 1);
            _newKey         = _newKey with { MaterialIndex = (byte)_newMaterialIdx };
        }

        ImUtf8.HoverTooltip("Drag this to the left or right to change its value."u8);
    }

    private void DrawRowIdxDrag()
    {
        ImGui.SetNextItemWidth(ImUtf8.CalcTextSize("Row 0000"u8).X);
        var format = $"Row {_newRowIdx / 2 + 1}{(char)(_newRowIdx % 2 + 'A')}";
        if (ImUtf8.DragScalar("##Row"u8, ref _newRowIdx, format, 0, ColorTable.NumRows - 1, 0.01f, ImGuiSliderFlags.NoInput))
        {
            _newRowIdx = Math.Clamp(_newRowIdx, 0, ColorTable.NumRows - 1);
            _newKey    = _newKey with { RowIndex = (byte)_newRowIdx };
        }

        ImUtf8.HoverTooltip("Drag this to the left or right to change its value."u8);
    }

    private void DrawRow(Design design, MaterialValueIndex index, in ColorRow row, bool disabled)
    {
        var tmp = row;
        using var _ = ImRaii.Disabled(disabled);
        var applied = ImGuiUtil.ColorPicker("##diffuse", "Change the diffuse value for this row.", row.Diffuse, v => tmp.Diffuse = v, "D");
        ImUtf8.SameLineInner();
        applied |= ImGuiUtil.ColorPicker("##specular", "Change the specular value for this row.", row.Specular, v => tmp.Specular = v, "S");
        ImUtf8.SameLineInner();
        applied |= ImGuiUtil.ColorPicker("##emissive", "Change the emissive value for this row.", row.Emissive, v => tmp.Emissive = v, "E");
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(GlossWidth * ImGuiHelpers.GlobalScale);
        applied |= AdvancedDyePopup.DragGloss(ref tmp.GlossStrength);
        ImUtf8.HoverTooltip("Change the gloss strength for this row."u8);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(SpecularStrengthWidth * ImGuiHelpers.GlobalScale);
        applied |= AdvancedDyePopup.DragSpecularStrength(ref tmp.SpecularStrength);
        ImUtf8.HoverTooltip("Change the specular strength for this row."u8);
        if (applied)
            designManager.ChangeMaterialValue(design, index, tmp);
    }
}
