using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Interop;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;
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
    private byte                _selectedMaterial = byte.MaxValue;
    private bool                _anyChanged;

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
                _selectedMaterial = byte.MaxValue;
                _drawIndex        = isOpen ? null : index;
            }
        }

        ImGuiUtil.HoverTooltip("Open advanced dyes for this slot.");
    }

    private (string Path, string GamePath) ResourceName(MaterialValueIndex index)
    {
        var materialHandle = (MaterialResourceHandle*)_actor.Model.AsCharacterBase->MaterialsSpan[
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

    private void DrawTabBar(ReadOnlySpan<Pointer<Texture>> textures, ref bool firstAvailable)
    {
        using var bar = ImRaii.TabBar("tabs");
        if (!bar)
            return;

        for (byte i = 0; i < MaterialService.MaterialsPerModel; ++i)
        {
            var index     = _drawIndex!.Value with { MaterialIndex = i };
            var available = index.TryGetTexture(textures, out var texture) && directX.TryGetColorTable(*texture, out var table);

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
                DrawTable(index, table);
            }
        }

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.TabItemButton($"{FontAwesomeIcon.Times.ToIconString()} ", ImGuiTabItemFlags.NoTooltip))
                _drawIndex = null;
        }

        ImGuiUtil.HoverTooltip("Close the advanced dye window.");
    }

    private void DrawContent(ReadOnlySpan<Pointer<Texture>> textures)
    {
        var firstAvailable = true;
        DrawTabBar(textures, ref firstAvailable);

        if (firstAvailable)
            ImGui.TextUnformatted("No Editable Materials available.");
    }

    private void DrawWindow(ReadOnlySpan<Pointer<Texture>> textures)
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

        var size = new Vector2(7 * ImGui.GetFrameHeight() + 3 * ImGui.GetStyle().ItemInnerSpacing.X + 300 * ImGuiHelpers.GlobalScale,
            18 * ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y + 2 * ImGui.GetStyle().ItemSpacing.Y);
        ImGui.SetNextWindowSize(size);

        var window = ImGui.Begin("###Glamourer Advanced Dyes", flags);
        try
        {
            if (window)
                DrawContent(textures);
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

        if (_drawIndex!.Value.TryGetTextures(actor, out var textures))
            DrawWindow(textures);
    }

    private void DrawTable(MaterialValueIndex materialIndex, in MtrlFile.ColorTable table)
    {
        using var disabled = ImRaii.Disabled(_state.IsLocked);
        _anyChanged = false;
        for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
        {
            var     index = materialIndex with { RowIndex = i };
            ref var row   = ref table[i];
            DrawRow(ref row, index, table);
        }

        ImGui.Separator();
        DrawAllRow(materialIndex, table);
    }

    private void DrawAllRow(MaterialValueIndex materialIndex, in MtrlFile.ColorTable table)
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
            ImGui.TextUnformatted("All Color Rows");
        }

        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SameLine(ImGui.GetWindowSize().X - 3 * buttonSize.X - 3 * spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), buttonSize, "Export this table to your clipboard.", false,
                true))
            ColorRowClipboard.Table = table;
        ImGui.SameLine(0, spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Paste.ToIconString(), buttonSize,
                "Import an exported table from your clipboard onto this table.", !ColorRowClipboard.IsTableSet, true))
            foreach (var (row, idx) in ColorRowClipboard.Table.WithIndex())
            {
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
            for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
                stateManager.ResetMaterialValue(_state, materialIndex with { RowIndex = i }, ApplySettings.Game);
    }

    private void DrawRow(ref MtrlFile.ColorTable.Row row, MaterialValueIndex index, in MtrlFile.ColorTable table)
    {
        using var id      = ImRaii.PushId(index.RowIndex);
        var       changed = _state.Materials.TryGetValue(index, out var value);
        if (!changed)
        {
            var internalRow = new ColorRow(row);
            var slot        = index.ToEquipSlot();
            var weapon = slot is EquipSlot.MainHand or EquipSlot.OffHand
                ? _state.ModelData.Weapon(slot)
                : _state.ModelData.Armor(slot).ToWeapon(0);
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
            ImGui.TextUnformatted($"Row {index.RowIndex + 1:D2}");
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
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Gloss", ref value.Model.GlossStrength, 0.01f, 0.001f, float.MaxValue, "%.3f G")
         && value.Model.GlossStrength > 0;
        ImGuiUtil.HoverTooltip("Change the gloss strength for this row.");
        ImGui.SameLine(0, spacing.X);
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        applied |= ImGui.DragFloat("##Specular Strength", ref value.Model.SpecularStrength, 0.01f, float.MinValue, float.MaxValue, "%.3f SS");
        ImGuiUtil.HoverTooltip("Change the specular strength for this row.");
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

    private LabelStruct _label = new();

    private struct LabelStruct
    {
        private fixed byte _label[12];

        public ImRaii.IEndObject TabItem(byte materialIndex, ImGuiTabItemFlags flags)
        {
            _label[10] = (byte)('1' + materialIndex);
            fixed (byte* ptr = _label)
            {
                return ImRaii.TabItem(ptr, flags | ImGuiTabItemFlags.NoTooltip);
            }
        }

        public LabelStruct()
        {
            _label[0]  = (byte)'M';
            _label[1]  = (byte)'a';
            _label[2]  = (byte)'t';
            _label[3]  = (byte)'e';
            _label[4]  = (byte)'r';
            _label[5]  = (byte)'i';
            _label[6]  = (byte)'a';
            _label[7]  = (byte)'l';
            _label[8]  = (byte)' ';
            _label[9]  = (byte)'#';
            _label[11] = 0;
        }
    }
}
