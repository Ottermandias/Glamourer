using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.Interop.Material;
using Glamourer.Interop.Structs;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Files;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Materials;

public sealed unsafe class AdvancedDyePopup(
    MainWindowPosition mainPosition,
    Configuration config,
    StateManager stateManager,
    ActorSelector actorSelector,
    MaterialDrawer materials,
    LiveColorTablePreviewer preview) : IService
{
    private MaterialValueIndex? _drawIndex;
    private ActorIdentifier     _identifier;
    private ActorState?         _state;
    private Actor               _actor;
    private byte                _selectedMaterial = byte.MaxValue;

    private bool ShouldBeDrawn()
    {
        if (!mainPosition.IsOpen)
            return false;

        if (!config.UseAdvancedDyes)
            return false;

        if (config.Ephemeral.SelectedTab is not MainWindow.TabType.Actors)
            return false;

        if (!_drawIndex.HasValue)
            return false;

        if (actorSelector.Selection.Identifier != _identifier || !_identifier.IsValid)
            return false;

        if (_state == null)
            return false;

        _actor = actorSelector.Selection.Data.Valid ? actorSelector.Selection.Data.Objects[0] : Actor.Null;
        if (!_actor.Valid || !_actor.Model.IsCharacterBase)
            return false;

        return true;
    }

    public void DrawButton(ActorIdentifier identifier, ActorState state, MaterialValueIndex index)
    {
        using var id     = ImRaii.PushId(index.SlotIndex | ((int)index.DrawObject << 8));
        var       isOpen = identifier == _identifier && state == _state && index == _drawIndex;
        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isOpen))
        {
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Palette.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    "Open advanced dyes for this slot.", false, true))
            {
                if (isOpen)
                {
                    _drawIndex        = null;
                    _identifier       = ActorIdentifier.Invalid;
                    _state            = null;
                    _selectedMaterial = byte.MaxValue;
                }
                else
                {
                    _drawIndex        = index;
                    _identifier       = identifier;
                    _state            = state;
                    _selectedMaterial = byte.MaxValue;
                }
            }
        }
    }

    public unsafe void Draw()
    {
        if (!ShouldBeDrawn())
            return;

        var position = mainPosition.Position;
        position.X += mainPosition.Size.X;
        position.Y += ImGui.GetFrameHeightWithSpacing() * 3;
        var size = new Vector2(3 * ImGui.GetFrameHeight() + 300 * ImGuiHelpers.GlobalScale, 18.5f * ImGui.GetFrameHeightWithSpacing());
        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(size);
        var window = ImGui.Begin("###Glamourer Advanced Dyes",
            ImGuiWindowFlags.NoFocusOnAppearing
          | ImGuiWindowFlags.NoCollapse
          | ImGuiWindowFlags.NoDecoration
          | ImGuiWindowFlags.NoMove
          | ImGuiWindowFlags.NoResize);
        try
        {
            if (!window)
                return;

            using var bar = ImRaii.TabBar("tabs");
            if (!bar)
                return;

            var        model          = _actor.Model.AsCharacterBase;
            var        firstAvailable = true;
            Span<byte> label          = stackalloc byte[12];
            label[0]  = (byte)'M';
            label[1]  = (byte)'a';
            label[2]  = (byte)'t';
            label[3]  = (byte)'e';
            label[4]  = (byte)'r';
            label[5]  = (byte)'i';
            label[6]  = (byte)'a';
            label[7]  = (byte)'l';
            label[8]  = (byte)' ';
            label[9]  = (byte)'#';
            label[11] = 0;

            for (byte i = 0; i < MaterialService.MaterialsPerModel; ++i)
            {
                var texture   = model->ColorTableTextures + _drawIndex!.Value.SlotIndex * MaterialService.MaterialsPerModel + i;
                var index     = _drawIndex!.Value with { MaterialIndex = i };
                var available = *texture != null && DirectXTextureHelper.TryGetColorTable(*texture, out var table);
                if (index == preview.LastValueIndex with {RowIndex = 0})
                    table = preview.LastOriginalColorTable;

                using var disable = ImRaii.Disabled(!available);
                label[10] = (byte)('1' + i);
                var select = available && (_selectedMaterial == i || firstAvailable && _selectedMaterial == byte.MaxValue)
                    ? ImGuiTabItemFlags.SetSelected
                    : ImGuiTabItemFlags.None;

                if (available)
                    firstAvailable = false;
                if (select is ImGuiTabItemFlags.SetSelected)
                    _selectedMaterial = i;


                fixed (byte* labelPtr = label)
                {
                    using var tab = ImRaii.TabItem(labelPtr, select);
                    if (tab.Success && available)
                        DrawTable(index, table);
                }
            }
        }
        finally
        {
            ImGui.End();
        }
    }

    private void DrawTable(MaterialValueIndex materialIndex, in MtrlFile.ColorTable table)
    {
        for (byte i = 0; i < MtrlFile.ColorTable.NumRows; ++i)
        {
            var     index = materialIndex with { RowIndex = i };
            ref var row   = ref table[i];
            DrawRow(ref row, CharacterWeapon.Empty, index, table);
        }
    }

    private void DrawRow(ref MtrlFile.ColorTable.Row row, CharacterWeapon drawData, MaterialValueIndex index, in MtrlFile.ColorTable table)
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

        ImGui.SameLine();
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Crosshairs.ToIconString(), new Vector2(ImGui.GetFrameHeight()), "Locate", false, true);
        if (ImGui.IsItemHovered())
            preview.OnHover(index, _actor.Index, table);

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
        if (applied)
            stateManager.ChangeMaterialValue(_state!, index, value, ApplySettings.Manual);
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
