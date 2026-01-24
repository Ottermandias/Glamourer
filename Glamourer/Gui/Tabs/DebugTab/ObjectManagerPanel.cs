using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class ObjectManagerPanel(ActorObjectManager objectManager, ActorManager actors) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Object Manager"u8;

    public bool Disabled
        => false;

    private string _objectFilter = string.Empty;

    public void Draw()
    {
        objectManager.Objects.DrawDebug();

        using (var table = ImUtf8.Table("##data"u8, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!table)
                return;

            ImUtf8.DrawTableColumn("World"u8);
            ImUtf8.DrawTableColumn(actors.Finished ? actors.Data.ToWorldName(objectManager.World) : "Service Missing");
            ImUtf8.DrawTableColumn(objectManager.World.ToString());

            ImUtf8.DrawTableColumn("Player Character"u8);
            ImUtf8.DrawTableColumn($"{objectManager.Player.Utf8Name} ({objectManager.Player.Index})");
            ImGui.TableNextColumn();
            ImUtf8.CopyOnClickSelectable(objectManager.Player.ToString());

            ImUtf8.DrawTableColumn("In GPose"u8);
            ImUtf8.DrawTableColumn(objectManager.IsInGPose.ToString());
            ImGui.TableNextColumn();

            ImUtf8.DrawTableColumn("In Lobby"u8);
            ImUtf8.DrawTableColumn(objectManager.IsInLobby.ToString());
            ImGui.TableNextColumn();

            if (objectManager.IsInGPose)
            {
                ImUtf8.DrawTableColumn("GPose Player"u8);
                ImUtf8.DrawTableColumn($"{objectManager.GPosePlayer.Utf8Name} ({objectManager.GPosePlayer.Index})");
                ImGui.TableNextColumn();
                ImUtf8.CopyOnClickSelectable(objectManager.GPosePlayer.ToString());
            }

            ImUtf8.DrawTableColumn("Number of Players"u8);
            ImUtf8.DrawTableColumn(objectManager.Count.ToString());
            ImGui.TableNextColumn();
        }

        var filterChanged = ImUtf8.InputText("##Filter"u8, ref _objectFilter, "Filter..."u8);
        using var table2 = ImUtf8.Table("##data2"u8, 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.ScrollY,
            new Vector2(-1, 20 * ImGui.GetTextLineHeightWithSpacing()));
        if (!table2)
            return;

        if (filterChanged)
            ImGui.SetScrollY(0);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();

        var remainder = ImGuiClip.FilteredClippedDraw(objectManager, skips,
            p => p.Value.Label.Contains(_objectFilter, StringComparison.OrdinalIgnoreCase), p
                =>
            {
                ImUtf8.DrawTableColumn(p.Key.ToString());
                ImUtf8.DrawTableColumn(p.Value.Label);
                ImUtf8.DrawTableColumn(string.Join(", ", p.Value.Objects.OrderBy(a => a.Index).Select(a => a.Index.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeightWithSpacing());
    }
}
