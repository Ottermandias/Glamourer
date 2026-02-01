using Glamourer.Designs;
using Glamourer.Services;
using ImSharp;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Luna;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class DesignTesterPanel(ItemManager items, HumanModelList humans) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Base64 Design Tester"u8;

    public bool Disabled
        => false;

    private string     _base64       = string.Empty;
    private string     _restore      = string.Empty;
    private byte[]     _base64Bytes  = [];
    private byte[]     _restoreBytes = [];
    private DesignData _parse64      = new();
    private Exception? _parse64Failure;

    public void Draw()
    {
        DrawBase64Input();
        DrawDesignData();
        DrawBytes();
    }

    private void DrawBase64Input()
    {
        Im.Item.SetNextWidthFull();
        Im.Input.Text("##base64"u8, ref _base64, "Base 64 input..."u8);
        if (!Im.Item.DeactivatedAfterEdit)
            return;

        try
        {
            _base64Bytes    = Convert.FromBase64String(_base64);
            _parse64Failure = null;
        }
        catch (Exception ex)
        {
            _base64Bytes    = [];
            _parse64Failure = ex;
        }

        if (_parse64Failure is not null)
            return;

        try
        {
            _parse64      = DesignBase64Migration.MigrateBase64(items, humans, _base64, out var ef, out var cf, out var wp, out var meta);
            _restore      = DesignBase64Migration.CreateOldBase64(in _parse64, ef, cf, meta, wp);
            _restoreBytes = Convert.FromBase64String(_restore);
        }
        catch (Exception ex)
        {
            _parse64Failure = ex;
            _restore        = string.Empty;
        }
    }

    private void DrawDesignData()
    {
        if (_parse64Failure is not null)
        {
            Im.TextWrapped($"{_parse64Failure}");
            return;
        }

        if (_restore.Length <= 0)
            return;

        DrawDesignData(_parse64);
        using var font = Im.Font.PushMono();
        Im.Text(_base64);
        foreach (var (c1, c2) in _restore.Zip(_base64))
        {
            using var color = ImGuiColor.Text.Push(0xFF4040D0, c1 != c2);
            Im.Text($"{c1}");
            Im.Line.NoSpacing();
        }

        Im.Line.New();

        foreach (var (idx, (b1, b2)) in _base64Bytes.Zip(_restoreBytes).Index())
        {
            using (Im.Group())
            {
                Im.Text($"{idx:D2}");
                Im.Text($"{b1:X2}");
                using var color = ImGuiColor.Text.Push(0xFF4040D0, b1 != b2);
                Im.Text($"{b2:X2}");
            }

            Im.Line.NoSpacing();
        }

        Im.Line.New();
    }

    private void DrawBytes()
    {
        if (_parse64Failure is null || _base64Bytes.Length <= 0)
            return;

        using var font = Im.Font.PushMono();
        foreach (var (idx, b) in _base64Bytes.Index())
        {
            using (Im.Group())
            {
                Im.Text($"{idx:D2}");
                Im.Text($"{b:X2}");
            }

            Im.Line.Same();
        }

        Im.Line.New();
    }

    public static void DrawDesignData(in DesignData data)
    {
        if (data.IsHuman)
            DrawHumanData(data);
        else
            DrawMonsterData(data);
    }

    private static void DrawHumanData(in DesignData data)
    {
        using var table = Im.Table.Begin("##equip"u8, 5, TableFlags.RowBackground | TableFlags.SizingFixedFit);
        if (!table)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item  = data.Item(slot);
            var stain = data.Stain(slot);
            var crest = data.Crest(slot.ToCrestFlag());
            table.DrawColumn(slot.ToNameU8());
            table.DrawColumn(item.Name);
            table.DrawColumn($"{item.ItemId}");
            table.DrawColumn($"{stain}");
            table.DrawColumn($"{crest}");
        }

        table.DrawDataPair("Hat Visible"u8, data.IsHatVisible());
        table.NextRow();
        table.DrawDataPair("Visor Toggled"u8, data.IsVisorToggled());
        table.NextRow();
        table.DrawDataPair("Weapon Visible"u8, data.IsWeaponVisible());
        table.NextRow();

        table.DrawDataPair("Model ID"u8,data.ModelId);
        table.NextRow();

        foreach (var index in CustomizeIndex.Values)
        {
            var value = data.Customize[index];
            table.DrawDataPair(index.ToNameU8(), value.Value);
            table.NextRow();
        }

        table.DrawDataPair("Is Wet"u8, data.IsWet());
        table.NextRow();
    }

    private static void DrawMonsterData(in DesignData data)
    {
        Im.Text($"Model ID {data.ModelId}");
        Im.Separator();
        using var font = Im.Font.PushMono();
        Im.Text("Customize Array"u8);
        Im.Separator();
        Im.TextWrapped(StringU8.Join((byte)' ', data.GetCustomizeBytes().Select(b => b.ToString("X2"))));

        Im.Text("Equipment Array"u8);
        Im.Separator();
        Im.TextWrapped(StringU8.Join((byte)' ', data.GetEquipmentBytes().Select(b => b.ToString("X2"))));
    }
}
