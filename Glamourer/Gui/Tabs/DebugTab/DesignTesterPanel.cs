using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class DesignTesterPanel(ItemManager _items, HumanModelList _humans) : IGameDataDrawer
{
    public string Label
        => "Base64 Design Tester";

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
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##base64", "Base 64 input...", ref _base64, 2047);
        if (!ImGui.IsItemDeactivatedAfterEdit())
            return;

        try
        {
            _base64Bytes    = Convert.FromBase64String(_base64);
            _parse64Failure = null;
        }
        catch (Exception ex)
        {
            _base64Bytes    = Array.Empty<byte>();
            _parse64Failure = ex;
        }

        if (_parse64Failure != null)
            return;

        try
        {
            _parse64 = DesignBase64Migration.MigrateBase64(_items, _humans, _base64, out var ef, out var cf, out var wp, out var ah,
                out var av,
                out var aw);
            _restore      = DesignBase64Migration.CreateOldBase64(in _parse64, ef, cf, ah, av, aw, wp);
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
        if (_parse64Failure != null)
        {
            ImGuiUtil.TextWrapped(_parse64Failure.ToString());
            return;
        }

        if (_restore.Length <= 0)
            return;

        DrawDesignData(_parse64);
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        ImGui.TextUnformatted(_base64);
        using (_ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 }))
        {
            foreach (var (c1, c2) in _restore.Zip(_base64))
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF4040D0, c1 != c2);
                ImGui.TextUnformatted(c1.ToString());
                ImGui.SameLine();
            }
        }

        ImGui.NewLine();

        foreach (var ((b1, b2), idx) in _base64Bytes.Zip(_restoreBytes).WithIndex())
        {
            using (_ = ImRaii.Group())
            {
                ImGui.TextUnformatted(idx.ToString("D2"));
                ImGui.TextUnformatted(b1.ToString("X2"));
                using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF4040D0, b1 != b2);
                ImGui.TextUnformatted(b2.ToString("X2"));
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawBytes()
    {
        if (_parse64Failure == null || _base64Bytes.Length <= 0)
            return;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        foreach (var (b, idx) in _base64Bytes.WithIndex())
        {
            using (_ = ImRaii.Group())
            {
                ImGui.TextUnformatted(idx.ToString("D2"));
                ImGui.TextUnformatted(b.ToString("X2"));
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
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
        using var table = ImRaii.Table("##equip", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item  = data.Item(slot);
            var stain = data.Stain(slot);
            var crest = data.Crest(slot.ToCrestFlag());
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(item.Name);
            ImGuiUtil.DrawTableColumn(item.ItemId.ToString());
            ImGuiUtil.DrawTableColumn(stain.ToString());
            ImGuiUtil.DrawTableColumn(crest.ToString());
        }

        ImGuiUtil.DrawTableColumn("Hat Visible");
        ImGuiUtil.DrawTableColumn(data.IsHatVisible().ToString());
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Visor Toggled");
        ImGuiUtil.DrawTableColumn(data.IsVisorToggled().ToString());
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Weapon Visible");
        ImGuiUtil.DrawTableColumn(data.IsWeaponVisible().ToString());
        ImGui.TableNextRow();

        ImGuiUtil.DrawTableColumn("Model ID");
        ImGuiUtil.DrawTableColumn(data.ModelId.ToString());
        ImGui.TableNextRow();

        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var value = data.Customize[index];
            ImGuiUtil.DrawTableColumn(index.ToDefaultName());
            ImGuiUtil.DrawTableColumn(value.Value.ToString());
            ImGui.TableNextRow();
        }

        ImGuiUtil.DrawTableColumn("Is Wet");
        ImGuiUtil.DrawTableColumn(data.IsWet().ToString());
        ImGui.TableNextRow();
    }

    private static void DrawMonsterData(in DesignData data)
    {
        ImGui.TextUnformatted($"Model ID {data.ModelId}");
        ImGui.Separator();
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        ImGui.TextUnformatted("Customize Array");
        ImGui.Separator();
        ImGuiUtil.TextWrapped(string.Join(" ", data.GetCustomizeBytes().Select(b => b.ToString("X2"))));

        ImGui.TextUnformatted("Equipment Array");
        ImGui.Separator();
        ImGuiUtil.TextWrapped(string.Join(" ", data.GetEquipmentBytes().Select(b => b.ToString("X2"))));
    }
}
