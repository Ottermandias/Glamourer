using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Gui.Debug;

namespace Glamourer.Gui.Tabs.DebugTab;

public class ObjectManagerPanel(ObjectManager _objectManager, ActorManager _actors) : IGameDataDrawer
{
    public string Label
        => "Object Manager";

    public bool Disabled
        => false;

    private string _objectFilter = string.Empty;

    public void Draw()
    {
        _objectManager.Update();
        using (var table = ImRaii.Table("##data", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!table)
                return;

            ImGuiUtil.DrawTableColumn("Last Update");
            ImGuiUtil.DrawTableColumn(_objectManager.LastUpdate.ToString(CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();

            ImGuiUtil.DrawTableColumn("World");
            ImGuiUtil.DrawTableColumn(_actors.Finished ? _actors.Data.ToWorldName(_objectManager.World) : "Service Missing");
            ImGuiUtil.DrawTableColumn(_objectManager.World.ToString());

            ImGuiUtil.DrawTableColumn("Player Character");
            ImGuiUtil.DrawTableColumn($"{_objectManager.Player.Utf8Name} ({_objectManager.Player.Index})");
            ImGui.TableNextColumn();
            ImGuiUtil.CopyOnClickSelectable(_objectManager.Player.ToString());

            ImGuiUtil.DrawTableColumn("In GPose");
            ImGuiUtil.DrawTableColumn(_objectManager.IsInGPose.ToString());
            ImGui.TableNextColumn();

            if (_objectManager.IsInGPose)
            {
                ImGuiUtil.DrawTableColumn("GPose Player");
                ImGuiUtil.DrawTableColumn($"{_objectManager.GPosePlayer.Utf8Name} ({_objectManager.GPosePlayer.Index})");
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable(_objectManager.GPosePlayer.ToString());
            }

            ImGuiUtil.DrawTableColumn("Number of Players");
            ImGuiUtil.DrawTableColumn(_objectManager.Count.ToString());
            ImGui.TableNextColumn();
        }

        var filterChanged = ImGui.InputTextWithHint("##Filter", "Filter...", ref _objectFilter, 64);
        using var table2 = ImRaii.Table("##data2", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.ScrollY,
            new Vector2(-1, 20 * ImGui.GetTextLineHeightWithSpacing()));
        if (!table2)
            return;

        if (filterChanged)
            ImGui.SetScrollY(0);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();

        var remainder = ImGuiClip.FilteredClippedDraw(_objectManager, skips,
            p => p.Value.Label.Contains(_objectFilter, StringComparison.OrdinalIgnoreCase), p
                =>
            {
                ImGuiUtil.DrawTableColumn(p.Key.ToString());
                ImGuiUtil.DrawTableColumn(p.Value.Label);
                ImGuiUtil.DrawTableColumn(string.Join(", ", p.Value.Objects.OrderBy(a => a.Index).Select(a => a.Index.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeightWithSpacing());
    }
}
