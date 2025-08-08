using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.State;
using Dalamud.Bindings.ImGui;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public class NpcAppearancePanel(NpcCombo npcCombo, StateManager stateManager, ActorObjectManager objectManager, DesignConverter designConverter)
    : IGameDataDrawer
{
    public string Label
        => "NPC Appearance";

    public bool Disabled
        => false;

    private string _npcFilter = string.Empty;
    private bool   _customizeOrGear;

    public void Draw()
    {
        ImUtf8.Checkbox("Compare Customize (or Gear)"u8, ref _customizeOrGear);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var resetScroll = ImUtf8.InputText("##npcFilter"u8, ref _npcFilter, "Filter..."u8);

        using var table = ImRaii.Table("npcs", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 400 * ImGuiHelpers.GlobalScale));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);

        ImUtf8.TableSetupColumn("Button"u8,  ImGuiTableColumnFlags.WidthFixed);
        ImUtf8.TableSetupColumn("Name"u8,    ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 300);
        ImUtf8.TableSetupColumn("Kind"u8,    ImGuiTableColumnFlags.WidthFixed);
        ImUtf8.TableSetupColumn("Id"u8,      ImGuiTableColumnFlags.WidthFixed);
        ImUtf8.TableSetupColumn("Model"u8,   ImGuiTableColumnFlags.WidthFixed);
        ImUtf8.TableSetupColumn("Visor"u8,   ImGuiTableColumnFlags.WidthFixed);
        ImUtf8.TableSetupColumn("Compare"u8, ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeightWithSpacing());
        ImGui.TableNextRow();
        var idx = 0;
        var remainder = ImGuiClip.FilteredClippedDraw(npcCombo.Items, skips,
            d => d.Name.Contains(_npcFilter, StringComparison.OrdinalIgnoreCase), DrawData);
        ImGui.TableNextColumn();
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeightWithSpacing());
        return;

        void DrawData(NpcData data)
        {
            using var id       = ImRaii.PushId(idx++);
            var       disabled = !stateManager.GetOrCreate(objectManager.Player, out var state);
            ImGui.TableNextColumn();
            if (ImUtf8.ButtonEx("Apply"u8, ""u8, Vector2.Zero, disabled))
            {
                foreach (var (slot, item, stain) in designConverter.FromDrawData(data.Equip.ToArray(), data.Mainhand, data.Offhand, true))
                    stateManager.ChangeEquip(state!, slot, item, stain, ApplySettings.Manual);
                stateManager.ChangeMetaState(state!, MetaIndex.VisorState, data.VisorToggled, ApplySettings.Manual);
                stateManager.ChangeEntireCustomize(state!, data.Customize, CustomizeFlagExtensions.All, ApplySettings.Manual);
            }

            ImUtf8.DrawFrameColumn(data.Name);

            ImUtf8.DrawFrameColumn(data.Kind is ObjectKind.BattleNpc ? "B" : "E");

            ImUtf8.DrawFrameColumn(data.Id.Id.ToString());

            ImUtf8.DrawFrameColumn(data.ModelId.ToString());

            using (_ = ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImUtf8.DrawFrameColumn(data.VisorToggled ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Times.ToIconString());
            }

            using var mono = ImRaii.PushFont(UiBuilder.MonoFont);
            ImUtf8.DrawFrameColumn(_customizeOrGear ? data.Customize.ToString() : data.WriteGear());
        }
    }
}
