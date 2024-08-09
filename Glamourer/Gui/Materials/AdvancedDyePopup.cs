using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Interop;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using Penumbra.String;

namespace Glamourer.Gui.Materials;

public sealed unsafe class AdvancedDyePopup(
    Configuration config,
    StateManager stateManager,
    LiveColorTablePreviewer preview,
    DirectXService directX) : IService
{
    private MaterialValueIndex? _drawIndex;
    private ActorState          _state = null!;
    private Actor               _actor;
    private ColorRow.Mode       _mode;
    private byte                _selectedMaterial = byte.MaxValue;
    private bool                _anyChanged;
    private bool                _forceFocus;

    private const int RowsPerPage = 16;
    private       int _rowOffset;

    private bool ShouldBeDrawn()
    {
        if (!config.UseAdvancedDyes)
            return false;

        if (_drawIndex is not { Valid: true })
            return false;

        if (!_actor.IsCharacter || !_state.ModelData.IsHuman || !_actor.Model.IsHuman)
            return false;

        return true;
    }

    public void DrawButton(EquipSlot slot)
        => DrawButton(MaterialValueIndex.FromSlot(slot));

    public void DrawButton(BonusItemFlag slot)
        => DrawButton(MaterialValueIndex.FromSlot(slot));

    private void DrawButton(MaterialValueIndex index)
    {
        if (!config.UseAdvancedDyes)
            return;

        ImGui.SameLine();
        using var id     = ImRaii.PushId(index.SlotIndex | ((int)index.DrawObject << 8));
        var       isOpen = index == _drawIndex;

        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isOpen)
                   .Push(ImGuiCol.Text,   ColorId.HeaderButtons.Value(), isOpen)
                   .Push(ImGuiCol.Border, ColorId.HeaderButtons.Value(), isOpen))
        {
            using var frame = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2 * ImGuiHelpers.GlobalScale, isOpen);
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Palette.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    string.Empty, false, true))
            {
                _forceFocus       = true;
                _selectedMaterial = byte.MaxValue;
                _drawIndex        = isOpen ? null : index;
            }
        }

        ImGuiUtil.HoverTooltip("Open advanced dyes for this slot.");
    }

    private (string Path, string GamePath) ResourceName(MaterialValueIndex index)
    {
        var materialHandle =
            (MaterialResourceHandle*)_actor.Model.AsCharacterBase->MaterialsSpan[
                index.MaterialIndex + index.SlotIndex * MaterialService.MaterialsPerModel].Value;
        var model       = _actor.Model.AsCharacterBase->ModelsSpan[index.SlotIndex].Value;
        var modelHandle = model == null ? null : model->ModelResourceHandle;
        var path = materialHandle == null
            ? string.Empty
            : ByteString.FromSpanUnsafe(materialHandle->ResourceHandle.FileName.AsSpan(), true).ToString();
        var gamePath = modelHandle == null
            ? string.Empty
            : modelHandle->GetMaterialFileNameBySlotAsString(index.MaterialIndex);
        return (path, gamePath);
    }

    private void DrawTabBar(ReadOnlySpan<Pointer<Texture>> textures, ReadOnlySpan<Pointer<Material>> materials, ref bool firstAvailable)
    {
        using var bar = ImRaii.TabBar("tabs");
        if (!bar)
            return;

        var table = new ColorTable.Table();
        for (byte i = 0; i < MaterialService.MaterialsPerModel; ++i)
        {
            var index = _drawIndex!.Value with { MaterialIndex = i };
            var available = index.TryGetTexture(textures, materials, out var texture, out _mode)
             && directX.TryGetColorTable(*texture, out table);


            if (index == preview.LastValueIndex with { RowIndex = 0 })
                table = preview.LastOriginalColorTable;

            using var disable = ImRaii.Disabled(!available);
            var select = available && firstAvailable && _selectedMaterial == byte.MaxValue
                ? ImGuiTabItemFlags.SetSelected
                : ImGuiTabItemFlags.None;

            if (available)
                firstAvailable = false;

            using var tab = _label.TabItem(i, select);
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                using var enabled = ImRaii.Enabled();
                var (path, gamePath) = ResourceName(index);
                if (gamePath.Length == 0 || path.Length == 0)
                    ImGui.SetTooltip("This material does not exist.");
                else if (!available)
                    ImGui.SetTooltip($"This material does not have an associated color set.\n\n{gamePath}\n{path}");
                else
                    ImGui.SetTooltip($"{gamePath}\n{path}");
            }

            if ((tab.Success || select is ImGuiTabItemFlags.SetSelected) && available)
            {
                _selectedMaterial = i;
                DrawToggle();
                DrawTable(index, table);
            }
        }
    }

    private void DrawToggle()
    {
        var       buttonWidth = new Vector2(ImGui.GetContentRegionAvail().X / 2, 0);
        using var font        = ImRaii.PushFont(UiBuilder.MonoFont);
        using var hoverColor  = ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.TabHovered));

        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(_rowOffset == 0 ? ImGuiCol.TabActive : ImGuiCol.Tab)))
        {
            if (ToggleButton.ButtonEx("Row Pairs 1-8 ", buttonWidth, ImGuiButtonFlags.MouseButtonLeft, ImDrawFlags.RoundCornersLeft))
                _rowOffset = 0;
        }

        ImGui.SameLine(0, 0);

        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(_rowOffset == RowsPerPage ? ImGuiCol.TabActive : ImGuiCol.Tab)))
        {
            if (ToggleButton.ButtonEx("Row Pairs 9-16", buttonWidth, ImGuiButtonFlags.MouseButtonLeft, ImDrawFlags.RoundCornersRight))
                _rowOffset = RowsPerPage;
        }
    }

    private void DrawContent(ReadOnlySpan<Pointer<Texture>> textures, ReadOnlySpan<Pointer<Material>> materials)
    {
        var firstAvailable = true;
        DrawTabBar(textures, materials, ref firstAvailable);

        if (firstAvailable)
            ImGui.TextUnformatted("No Editable Materials available.");
    }

    private void DrawWindow(ReadOnlySpan<Pointer<Texture>> textures, ReadOnlySpan<Pointer<Material>> materials)
    {
        var flags = ImGuiWindowFlags.NoFocusOnAppearing
          | ImGuiWindowFlags.NoCollapse
          | ImGuiWindowFlags.NoDecoration
          | ImGuiWindowFlags.NoResize;

        // Set position to the right of the main window when attached
        // The downwards offset is implicit through child position.
        if (config.KeepAdvancedDyesAttached)
        {
            var position = ImGui.GetWindowPos();
            position.X += ImGui.GetWindowSize().X + ImGui.GetStyle().WindowPadding.X;
            ImGui.SetNextWindowPos(position);
            flags |= ImGuiWindowFlags.NoMove;
        }

        var width = 7 * ImGui.GetFrameHeight() // Buttons
          + 3 * ImGui.GetStyle().ItemSpacing.X // around text
          + 7 * ImGui.GetStyle().ItemInnerSpacing.X
          + 200 * ImGuiHelpers.GlobalScale             // Drags
          + 7 * UiBuilder.MonoFont.GetCharAdvance(' ') // Row
          + 2 * ImGui.GetStyle().WindowPadding.X;
        var height = 19 * ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + 3 * ImGui.GetStyle().ItemSpacing.Y;
        ImGui.SetNextWindowSize(new Vector2(width, height));

        var window = ImGui.Begin("###Glamourer Advanced Dyes", flags);
        if (ImGui.IsWindowAppearing() || _forceFocus)
        {
            ImGui.SetWindowFocus();
            _forceFocus = false;
        }

        try
        {
            if (window)
                DrawContent(textures, materials);
        }
        finally
        {
            ImGui.End();
        }
    }

    public void Draw(Actor actor, ActorState state)
    {
        _actor = actor;
        _state = state;
        if (!ShouldBeDrawn())
            return;

        if (_drawIndex!.Value.TryGetTextures(actor, out var textures, out var materials))
            DrawWindow(textures, materials);
    }

    private void DrawTable(MaterialValueIndex materialIndex, ColorTable.Table table)
    {
        if (!materialIndex.Valid)
            return;

        using var disabled = ImRaii.Disabled(_state.IsLocked);
        _anyChanged = false;
        for (byte i = 0; i < RowsPerPage; ++i)
        {
            var     actualI = (byte)(i + _rowOffset);
            var     index   = materialIndex with { RowIndex = actualI };
            ref var row     = ref table[actualI];
            DrawRow(ref row, index, table);
        }

        ImGui.Separator();
        DrawAllRow(materialIndex, table);
    }

    private void DrawAllRow(MaterialValueIndex materialIndex, in ColorTable.Table table)
    {
        using var id         = ImRaii.PushId(100);
        var       buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Crosshairs.ToIconString(), buttonSize, "Highlight all affected colors on the character.",
            false, true);
        if (ImGui.IsItemHovered())
            preview.OnHover(materialIndex with { RowIndex = byte.MaxValue }, _actor.Index, table);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            ImGui.TextUnformatted("All Color Row Pairs (1-16)");
        }

        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SameLine(ImGui.GetWindowSize().X - 3 * buttonSize.X - 2 * spacing - ImGui.GetStyle().WindowPadding.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), buttonSize, "Export this table to your clipboard.", false,
                true))
            ColorRowClipboard.Table = table;
        ImGui.SameLine(0, spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Paste.ToIconString(), buttonSize,
                "Import an exported table from your clipboard onto this table.", !ColorRowClipboard.IsTableSet, true))
            for (var idx = 0; idx < ColorTable.NumRows; ++idx)
            {
                var row         = ColorRowClipboard.Table[idx];
                var internalRow = new ColorRow(row);
                var slot        = materialIndex.ToEquipSlot();
                var weapon = slot is EquipSlot.MainHand or EquipSlot.OffHand
                    ? _state.ModelData.Weapon(slot)
                    : _state.ModelData.Armor(slot).ToWeapon(0);
                var value = new MaterialValueState(internalRow, internalRow, weapon, StateSource.Manual);
                stateManager.ChangeMaterialValue(_state, materialIndex with { RowIndex = (byte)idx }, value, ApplySettings.Manual);
            }

        ImGui.SameLine(0, spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UndoAlt.ToIconString(), buttonSize, "Reset this table to game state.", !_anyChanged,
                true))
            for (byte i = 0; i < ColorTable.NumRows; ++i)
                stateManager.ResetMaterialValue(_state, materialIndex with { RowIndex = i }, ApplySettings.Game);
    }

    private void DrawRow(ref ColorTableRow row, MaterialValueIndex index, in ColorTable.Table table)
    {
        using var id      = ImRaii.PushId(index.RowIndex);
        var       changed = _state.Materials.TryGetValue(index, out var value);
        if (!changed)
        {
            var internalRow = new ColorRow(row);
            var slot        = index.ToEquipSlot();
            var weapon = slot switch
            {
                EquipSlot.MainHand => _state.ModelData.Weapon(EquipSlot.MainHand),
                EquipSlot.OffHand  => _state.ModelData.Weapon(EquipSlot.OffHand),
                EquipSlot.Unknown  => _state.ModelData.BonusItem((index.SlotIndex - 16u).ToBonusSlot()).ToArmor().ToWeapon(0),
                _                  => _state.ModelData.Armor(slot).ToWeapon(0),
            };
            value = new MaterialValueState(internalRow, internalRow, weapon, StateSource.Manual);
        }
        else
        {
            _anyChanged = true;
            value       = new MaterialValueState(value.Game, value.Model, value.DrawData, StateSource.Manual);
        }

        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Crosshairs.ToIconString(), buttonSize, "Highlight the affected colors on the character.",
            false, true);
        if (ImGui.IsItemHovered())
            preview.OnHover(index, _actor.Index, table);

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            var rowIndex  = index.RowIndex / 2 + 1;
            var rowSuffix = (index.RowIndex & 1) == 0 ? 'A' : 'B';
            ImGui.TextUnformatted($"Row {rowIndex,2}{rowSuffix}");
        }

        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
        var applied = ImGuiUtil.ColorPicker("##diffuse", "Change the diffuse value for this row.", value.Model.Diffuse,
            v => value.Model.Diffuse = v, "D");

        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##specular", "Change the specular value for this row.", value.Model.Specular,
            v => value.Model.Specular = v, "S");

        ImGui.SameLine(0, spacing.X);
        applied |= ImGuiUtil.ColorPicker("##emissive", "Change the emissive value for this row.", value.Model.Emissive,
            v => value.Model.Emissive = v, "E");

        ImGui.SameLine(0, spacing.X);
        if (_mode is not ColorRow.Mode.Dawntrail)
        {
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            applied |= DragGloss(ref value.Model.GlossStrength);
            ImGuiUtil.HoverTooltip("Change the gloss strength for this row.");
        }
        else
        {
            ImGui.Dummy(new Vector2(100 * ImGuiHelpers.GlobalScale, 0));
        }

        ImGui.SameLine(0, spacing.X);
        if (_mode is not ColorRow.Mode.Dawntrail)
        {
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            applied |= DragSpecularStrength(ref value.Model.SpecularStrength);
            ImGuiUtil.HoverTooltip("Change the specular strength for this row.");
        }
        else
        {
            ImGui.Dummy(new Vector2(100 * ImGuiHelpers.GlobalScale, 0));
        }

        ImGui.SameLine(0, spacing.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), buttonSize, "Export this row to your clipboard.", false,
                true))
            ColorRowClipboard.Row = value.Model;
        ImGui.SameLine(0, spacing.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Paste.ToIconString(), buttonSize,
                "Import an exported row from your clipboard onto this row.", !ColorRowClipboard.IsSet, true))
        {
            value.Model = ColorRowClipboard.Row;
            applied     = true;
        }

        ImGui.SameLine(0, spacing.X);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UndoAlt.ToIconString(), buttonSize, "Reset this row to game state.", !changed, true))
            stateManager.ResetMaterialValue(_state, index, ApplySettings.Game);

        if (applied)
            stateManager.ChangeMaterialValue(_state, index, value, ApplySettings.Manual);
    }

    public static bool DragGloss(ref float value)
    {
        var tmp      = value;
        var minValue = ImGui.GetIO().KeyCtrl ? 0f : (float)Half.Epsilon;
        if (!ImUtf8.DragScalar("##Gloss"u8, ref tmp, "%.1f G"u8, 0.001f, minValue, Math.Max(0.01f, 0.005f * value), ImGuiSliderFlags.AlwaysClamp))
            return false;

        var tmp2 = Math.Clamp(tmp, minValue, (float)Half.MaxValue);
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    public static bool DragSpecularStrength(ref float value)
    {
        var tmp = value * 100f;
        if (!ImUtf8.DragScalar("##SpecularStrength"u8, ref tmp, "%.0f%% SS"u8, 0f, (float)Half.MaxValue * 100f, 0.05f,
                ImGuiSliderFlags.AlwaysClamp))
            return false;

        var tmp2 = Math.Clamp(tmp, 0f, (float)Half.MaxValue * 100f) / 100f;
        if (tmp2 == value)
            return false;

        value = tmp2;
        return true;
    }

    private LabelStruct _label = new();

    private struct LabelStruct
    {
        private fixed byte _label[5];

        public ImRaii.IEndObject TabItem(byte materialIndex, ImGuiTabItemFlags flags)
        {
            _label[4] = (byte)('A' + materialIndex);
            fixed (byte* ptr = _label)
            {
                return ImRaii.TabItem(ptr, flags | ImGuiTabItemFlags.NoTooltip);
            }
        }

        public LabelStruct()
        {
            _label[0] = (byte)'M';
            _label[1] = (byte)'a';
            _label[2] = (byte)'t';
            _label[3] = (byte)' ';
            _label[5] = 0;
        }
    }
}
