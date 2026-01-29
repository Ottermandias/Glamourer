using Dalamud.Interface.Utility;
using Glamourer.Interop.Penumbra;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class PenumbraPanel(PenumbraService penumbra, PenumbraChangedItemTooltip penumbraTooltip) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Penumbra Interop"u8;

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
        ImGuiUtil.DrawTableColumn(penumbra.Available.ToString());
        ImGui.TableNextColumn();
        if (ImGui.SmallButton("Unattach"))
            penumbra.Unattach();
        Im.Line.Same();
        if (ImGui.SmallButton("Reattach"))
            penumbra.Reattach();

        ImGuiUtil.DrawTableColumn("Version");
        ImGuiUtil.DrawTableColumn($"{penumbra.CurrentMajor}.{penumbra.CurrentMinor}");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Attached When");
        ImGuiUtil.DrawTableColumn(penumbra.AttachTime.ToLocalTime().ToLongTimeString());
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Draw Object");
        ImGui.TableNextColumn();
        var address = _drawObject.Address;
        ImGui.SetNextItemWidth(200 * Im.Style.GlobalScale);
        if (ImGui.InputScalar("##drawObjectPtr", ImGuiDataType.U64, ref address, nint.Zero, nint.Zero, "%llx",
                ImGuiInputTextFlags.CharsHexadecimal))
            _drawObject = address;
        ImGuiUtil.DrawTableColumn(penumbra.Available
            ? $"0x{penumbra.GameObjectFromDrawObject(_drawObject).Address:X}"
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Cutscene Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * Im.Style.GlobalScale);
        ImGui.InputInt("##CutsceneIndex", ref _gameObjectIndex, 0, 0);
        ImGuiUtil.DrawTableColumn(penumbra.Available
            ? penumbra.CutsceneParent((ushort)_gameObjectIndex).ToString()
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Redraw Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * Im.Style.GlobalScale);
        ImGui.InputInt("##redrawObject", ref _gameObjectIndex, 0, 0);
        ImGui.TableNextColumn();
        using (_ = ImRaii.Disabled(!penumbra.Available))
        {
            if (ImGui.SmallButton("Redraw"))
                penumbra.RedrawObject((ObjectIndex)_gameObjectIndex, RedrawType.Redraw);
        }

        ImGuiUtil.DrawTableColumn("Last Tooltip Date");
        ImGuiUtil.DrawTableColumn(penumbraTooltip.LastTooltip > DateTime.MinValue
            ? $"{penumbraTooltip.LastTooltip.ToLongTimeString()} ({penumbraTooltip.LastType} {penumbraTooltip.LastId})"
            : "Never");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Last Click Date");
        ImGuiUtil.DrawTableColumn(penumbraTooltip.LastClick > DateTime.MinValue ? penumbraTooltip.LastClick.ToLongTimeString() : "Never");
        ImGui.TableNextColumn();

        ImGui.Separator();
        ImGui.Separator();
        foreach (var (slot, item) in penumbraTooltip.LastItems)
        {
            switch (slot)
            {
                case EquipSlot e:     ImGuiUtil.DrawTableColumn($"{e.ToName()} Revert-Item"); break;
                case BonusItemFlag f: ImGuiUtil.DrawTableColumn($"{f.ToName()} Revert-Item"); break;
                default:              ImGuiUtil.DrawTableColumn("Unk Revert-Item"); break;
            }

            ImGuiUtil.DrawTableColumn(item.Valid ? item.Name : "None");
            ImGui.TableNextColumn();
        }
    }
}
