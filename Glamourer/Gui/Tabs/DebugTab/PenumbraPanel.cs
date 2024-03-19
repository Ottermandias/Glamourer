using Dalamud.Interface.Utility;
using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class PenumbraPanel(PenumbraService _penumbra, PenumbraChangedItemTooltip _penumbraTooltip) : IGameDataDrawer
{
    public string Label
        => "Penumbra Interop";

    public bool Disabled
        => false;

    private int   _gameObjectIndex;
    private Model _drawObject = Model.Null;

    public void Draw()
    {
        using var table = ImRaii.Table("##PenumbraTable", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn("Available");
        ImGuiUtil.DrawTableColumn(_penumbra.Available.ToString());
        ImGui.TableNextColumn();
        if (ImGui.SmallButton("Unattach"))
            _penumbra.Unattach();
        ImGui.SameLine();
        if (ImGui.SmallButton("Reattach"))
            _penumbra.Reattach();

        ImGuiUtil.DrawTableColumn("Draw Object");
        ImGui.TableNextColumn();
        var address = _drawObject.Address;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputScalar("##drawObjectPtr", ImGuiDataType.U64, (nint)(&address), nint.Zero, nint.Zero, "%llx",
                ImGuiInputTextFlags.CharsHexadecimal))
            _drawObject = address;
        ImGuiUtil.DrawTableColumn(_penumbra.Available
            ? $"0x{_penumbra.GameObjectFromDrawObject(_drawObject).Address:X}"
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Cutscene Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##CutsceneIndex", ref _gameObjectIndex, 0, 0);
        ImGuiUtil.DrawTableColumn(_penumbra.Available
            ? _penumbra.CutsceneParent((ushort) _gameObjectIndex).ToString()
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Redraw Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##redrawObject", ref _gameObjectIndex, 0, 0);
        ImGui.TableNextColumn();
        using (_ = ImRaii.Disabled(!_penumbra.Available))
        {
            if (ImGui.SmallButton("Redraw"))
                _penumbra.RedrawObject((ObjectIndex)_gameObjectIndex, RedrawType.Redraw);
        }

        ImGuiUtil.DrawTableColumn("Last Tooltip Date");
        ImGuiUtil.DrawTableColumn(_penumbraTooltip.LastTooltip > DateTime.MinValue ? _penumbraTooltip.LastTooltip.ToLongTimeString() : "Never");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Last Click Date");
        ImGuiUtil.DrawTableColumn(_penumbraTooltip.LastClick > DateTime.MinValue ? _penumbraTooltip.LastClick.ToLongTimeString() : "Never");
        ImGui.TableNextColumn();

        ImGui.Separator();
        ImGui.Separator();
        foreach (var (slot, item) in _penumbraTooltip.LastItems)
        {
            ImGuiUtil.DrawTableColumn($"{slot.ToName()} Revert-Item");
            ImGuiUtil.DrawTableColumn(item.Valid ? item.Name : "None");
            ImGui.TableNextColumn();
        }
    }
}
