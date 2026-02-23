using Glamourer.Interop.Penumbra;
using ImSharp;
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
        using var table = Im.Table.Begin("##PenumbraTable"u8, 3, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        if (!table)
            return;

        table.DrawDataPair("Available"u8, penumbra.Available);
        table.NextColumn();
        if (Im.SmallButton("Unattach"u8))
            penumbra.Unattach();
        Im.Line.SameInner();
        if (Im.SmallButton("Reattach"u8))
            penumbra.Reattach();

        table.DrawDataPair("Version"u8, $"{penumbra.CurrentMajor}.{penumbra.CurrentMinor}");
        table.NextColumn();

        table.DrawDataPair("Attached When"u8, penumbra.AttachTime.ToLocalTime().ToLongTimeString());
        table.NextColumn();

        table.DrawFrameColumn("Draw Object"u8);
        table.NextColumn();
        var address = _drawObject.Address;
        Im.Item.SetNextWidthScaled(200);
        if (Im.Input.Scalar("##drawObjectPtr"u8, ref address, "%llx"u8, flags: InputTextFlags.CharsHexadecimal))
            _drawObject = address;
        table.NextColumn();
        if (penumbra.Available)
            Glamourer.Dynamis.DrawPointer(penumbra.GameObjectFromDrawObject(_drawObject).Address);
        else
            Im.Text("Penumbra Unavailable"u8);

        table.DrawFrameColumn("Cutscene Object"u8);
        table.NextColumn();
        Im.Item.SetNextWidthScaled(200);
        Im.Input.Scalar("##CutsceneIndex"u8, ref _gameObjectIndex);
        table.DrawColumn(penumbra.Available ? $"{penumbra.ResolveService.CutsceneParent((ushort)_gameObjectIndex)}" : "Penumbra Unavailable"u8);

        table.DrawFrameColumn("Redraw Object"u8);
        table.NextColumn();
        Im.Item.SetNextWidthScaled(200);
        Im.Input.Scalar("##redrawObject"u8, ref _gameObjectIndex);
        table.NextColumn();
        using (Im.Disabled(!penumbra.Available))
        {
            if (Im.Button("Redraw"u8))
                penumbra.RedrawObject((ObjectIndex)_gameObjectIndex, RedrawType.Redraw);
        }

        table.DrawColumn("Last Tooltip Date"u8);
        table.DrawColumn(penumbraTooltip.LastTooltip > DateTime.MinValue
            ? $"{penumbraTooltip.LastTooltip.ToLongTimeString()} ({penumbraTooltip.LastType} {penumbraTooltip.LastId})"
            : "Never"u8);
        table.NextColumn();

        table.DrawColumn("Last Click Date"u8);
        table.DrawColumn(penumbraTooltip.LastClick > DateTime.MinValue ? penumbraTooltip.LastClick.ToLongTimeString() : "Never"u8);
        table.NextColumn();

        Im.Separator();
        Im.Separator();
        foreach (var (slot, item) in penumbraTooltip.LastItems)
        {
            switch (slot)
            {
                case EquipSlot e:     table.DrawColumn($"{e.ToNameU8()} Revert-Item"); break;
                case BonusItemFlag f: table.DrawColumn($"{f.ToNameU8()} Revert-Item"); break;
                default:              table.DrawColumn("Unk Revert-Item"u8); break;
            }

            table.DrawColumn(item.Valid ? item.Name : "None"u8);
            table.NextColumn();
        }
    }
}
