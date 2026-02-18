using Glamourer.Designs;
using Glamourer.Interop.Material;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;

namespace Glamourer.Gui.Materials;

public class MaterialDrawer(DesignManager designManager, Configuration.Configuration config) : IService
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
        var available = Im.ContentRegion.Available.X;
        _spacing    = Im.Style.ItemInnerSpacing.X;
        _buttonSize = new Vector2(Im.Style.FrameHeight);
        var colorWidth = 4 * _buttonSize.X
          + (GlossWidth + SpecularStrengthWidth) * Im.Style.GlobalScale
          + 6 * _spacing
          + Im.Font.CalculateSize("Revert"u8).X;
        DrawMultiButtons(design);
        Im.Dummy(0);
        Im.Separator();
        Im.Dummy(0);
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
        var size     = new Vector2(200 * Im.Style.GlobalScale, 0);
        if (ImEx.Button("Enable All Advanced Dyes"u8, size,
                any
                    ? "Enable the application of all contained advanced dyes without deleting them."u8
                    : "This design does not contain any advanced dyes."u8,
                !any || disabled))
            designManager.ChangeApplyMulti(design, null, null, null, null, null, null, true, null);

        if (disabled && any)
            Im.Tooltip.OnHover($"Hold {config.DeleteDesignModifier} while clicking to enable.");
        Im.Line.Same();
        if (ImEx.Button("Disable All Advanced Dyes"u8, size,
                any
                    ? "Disable the application of all contained advanced dyes without deleting them."u8
                    : "This design does not contain any advanced dyes."u8,
                !any || disabled))
            designManager.ChangeApplyMulti(design, null, null, null, null, null, null, false, null);
        if (disabled && any)
            Im.Tooltip.OnHover($"Hold {config.DeleteDesignModifier} while clicking to disable.");

        if (ImEx.Button("Delete All Advanced Dyes"u8, size, any ? StringU8.Empty : "This design does not contain any advanced dyes."u8,
                !any || disabled))
            while (design.Materials.Count > 0)
                designManager.ChangeMaterialValue(design, MaterialValueIndex.FromKey(design.Materials[0].Item1), null);

        if (disabled && any)
            Im.Tooltip.OnHover($"Hold {config.DeleteDesignModifier} while clicking to delete.");
    }

    private void DrawName(MaterialValueIndex index)
    {
        using var style = ImStyleDouble.ButtonTextAlign.Push(new Vector2(0.05f, 0.5f));
        ImEx.TextFramed($"{index}", new Vector2((GlossWidth + SpecularStrengthWidth) * Im.Style.GlobalScale + _spacing, 0),
            borderColor: ImGuiColor.Text.Get());
    }

    private void DrawSingleRow(Design design)
    {
        for (var i = 0; i < design.Materials.Count; ++i)
        {
            using var id = Im.Id.Push(i);
            var (idx, value) = design.Materials[i];
            var key = MaterialValueIndex.FromKey(idx);

            DrawName(key);
            Im.Line.Same(0, _spacing);
            DeleteButton(design, key, ref i);
            Im.Line.Same(0, _spacing);
            CopyButton(value.Value);
            Im.Line.Same(0, _spacing);
            PasteButton(design, key);
            Im.Line.Same(0, _spacing);
            using var disabled = Im.Disabled(design.WriteProtected());
            EnabledToggle(design, key, value.Enabled);
            Im.Line.Same(0, _spacing);
            DrawRow(design, key, value.Value, value.Revert);
            Im.Line.Same(0, _spacing);
            RevertToggle(design, key, value.Revert);
        }
    }

    private void DrawTwoRow(Design design)
    {
        for (var i = 0; i < design.Materials.Count; ++i)
        {
            using var id = Im.Id.Push(i);
            var (idx, value) = design.Materials[i];
            var key = MaterialValueIndex.FromKey(idx);

            DrawName(key);
            Im.Line.Same(0, _spacing);
            DeleteButton(design, key, ref i);
            Im.Line.Same(0, _spacing);
            CopyButton(value.Value);
            Im.Line.Same(0, _spacing);
            PasteButton(design, key);
            Im.Line.Same(0, _spacing);
            EnabledToggle(design, key, value.Enabled);


            DrawRow(design, key, value.Value, value.Revert);
            Im.Line.Same(0, _spacing);
            RevertToggle(design, key, value.Revert);
            Im.Separator();
        }
    }

    private void DeleteButton(Design design, MaterialValueIndex index, ref int idx)
    {
        var deleteEnabled = config.DeleteDesignModifier.IsActive();
        if (!ImEx.Icon.Button(LunaStyle.DeleteIcon,
                $"Delete this color row.{(deleteEnabled ? string.Empty : $"\nHold {config.DeleteDesignModifier} to delete.")}",
                !deleteEnabled || design.WriteProtected()))
            return;

        designManager.ChangeMaterialValue(design, index, null);
        --idx;
    }

    private void CopyButton(in ColorRow row)
    {
        if (ImEx.Icon.Button(LunaStyle.ToClipboardIcon, "Export this row to your clipboard."u8))
            ColorRowClipboard.Row = row;
    }

    private void PasteButton(Design design, MaterialValueIndex index)
    {
        if (ImEx.Icon.Button(LunaStyle.FromClipboardIcon, "Import an exported row from your clipboard onto this row."u8,
                !ColorRowClipboard.IsSet || design.WriteProtected()))
            designManager.ChangeMaterialValue(design, index, ColorRowClipboard.Row);
    }

    private void EnabledToggle(Design design, MaterialValueIndex index, bool enabled)
    {
        if (Im.Checkbox("Enabled"u8, ref enabled))
            designManager.ChangeApplyMaterialValue(design, index, enabled);
    }

    private void RevertToggle(Design design, MaterialValueIndex index, bool revert)
    {
        if (Im.Checkbox("Revert"u8, ref revert))
            designManager.ChangeMaterialRevert(design, index, revert);
        Im.Tooltip.OnHover(
            "If this is checked, Glamourer will try to revert the advanced dye row to its game state instead of applying a specific row."u8);
    }

    public sealed class MaterialSlotCombo;

    private void DrawSlotCombo()
    {
        var width = Im.Font.CalculateSize(EquipSlot.OffHand.ToNameU8()).X + Im.Style.FrameHeightWithSpacing;
        Im.Item.SetNextWidth(width);
        using var combo = Im.Combo.Begin("##slot"u8, _newKey.SlotName());
        if (combo)
        {
            var currentSlot = _newKey.ToEquipSlot();
            foreach (var tmpSlot in EquipSlotExtensions.FullSlots)
            {
                if (Im.Selectable(tmpSlot.ToNameU8(), tmpSlot == currentSlot) && currentSlot != tmpSlot)
                    _newKey = MaterialValueIndex.FromSlot(tmpSlot) with
                    {
                        MaterialIndex = (byte)_newMaterialIdx,
                        RowIndex = (byte)_newRowIdx,
                    };
            }

            var currentBonus = _newKey.ToBonusSlot();
            foreach (var bonusSlot in BonusExtensions.AllFlags)
            {
                if (Im.Selectable(bonusSlot.ToNameU8(), bonusSlot == currentBonus) && bonusSlot != currentBonus)
                    _newKey = MaterialValueIndex.FromSlot(bonusSlot) with
                    {
                        MaterialIndex = (byte)_newMaterialIdx,
                        RowIndex = (byte)_newRowIdx,
                    };
            }
        }

        Im.Tooltip.OnHover("Choose a slot for an advanced dye row."u8);
    }

    public void DrawNew(Design design)
    {
        DrawSlotCombo();
        Im.Line.SameInner();
        DrawMaterialIdxDrag();
        Im.Line.SameInner();
        DrawRowIdxDrag();
        Im.Line.SameInner();
        var exists = design.GetMaterialDataRef().TryGetValue(_newKey, out _);
        if (ImEx.Button("Add New Row"u8, Vector2.Zero,
                exists ? "The selected advanced dye row already exists."u8 : "Add the selected advanced dye row."u8,
                exists || design.WriteProtected()))
            designManager.ChangeMaterialValue(design, _newKey, ColorRow.Empty);
    }

    private void DrawMaterialIdxDrag()
    {
        Im.Item.SetNextWidth(Im.Font.CalculateSize("Material AA"u8).X);
        if (Im.Drag("##Material"u8, ref _newMaterialIdx, $"Material {(char)('A' + _newMaterialIdx)}", 0, MaterialService.MaterialsPerModel - 1,
                0.01f, SliderFlags.NoInput))
        {
            _newMaterialIdx = Math.Clamp(_newMaterialIdx, 0, MaterialService.MaterialsPerModel - 1);
            _newKey         = _newKey with { MaterialIndex = (byte)_newMaterialIdx };
        }

        Im.Tooltip.OnHover("Drag this to the left or right to change its value."u8);
    }

    private void DrawRowIdxDrag()
    {
        Im.Item.SetNextWidth(Im.Font.CalculateSize("Row 0000"u8).X);
        if (Im.Drag("##Row"u8, ref _newRowIdx, $"Row {_newRowIdx / 2 + 1}{(char)(_newRowIdx % 2 + 'A')}", 0, ColorTable.NumRows - 1, 0.01f,
                SliderFlags.NoInput))
        {
            _newRowIdx = Math.Clamp(_newRowIdx, 0, ColorTable.NumRows - 1);
            _newKey    = _newKey with { RowIndex = (byte)_newRowIdx };
        }

        Im.Tooltip.OnHover("Drag this to the left or right to change its value."u8);
    }

    private void DrawRow(Design design, MaterialValueIndex index, in ColorRow row, bool disabled)
    {
        var tmp = row;
        using var _ = Im.Disabled(disabled);
        var applied = ImEx.ColorPickerButton("##diffuse"u8, "Change the diffuse value for this row."u8, row.Diffuse, out tmp.Diffuse, 'D');
        Im.Line.SameInner();
        applied |= ImEx.ColorPickerButton("##specular"u8, "Change the specular value for this row."u8, row.Specular, out tmp.Specular, 'S');
        Im.Line.SameInner();
        applied |= ImEx.ColorPickerButton("##emissive"u8, "Change the emissive value for this row."u8, row.Emissive, out tmp.Emissive, 'E');
        Im.Line.SameInner();
        Im.Item.SetNextWidth(GlossWidth * Im.Style.GlobalScale);
        applied |= AdvancedDyePopup.DragGloss(ref tmp.GlossStrength);
        Im.Tooltip.OnHover("Change the gloss strength for this row."u8);
        Im.Line.SameInner();
        Im.Item.SetNextWidth(SpecularStrengthWidth * Im.Style.GlobalScale);
        applied |= AdvancedDyePopup.DragSpecularStrength(ref tmp.SpecularStrength);
        Im.Tooltip.OnHover("Change the specular strength for this row."u8);
        if (applied)
            designManager.ChangeMaterialValue(design, index, tmp);
    }
}
