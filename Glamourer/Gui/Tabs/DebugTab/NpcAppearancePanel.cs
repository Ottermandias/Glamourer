using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public class NpcAppearancePanel(NpcCombo _npcCombo, StateManager _state, ObjectManager _objectManager, DesignConverter _designConverter)
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
        ImGui.Checkbox("Compare Customize (or Gear)", ref _customizeOrGear);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var resetScroll = ImGui.InputTextWithHint("##npcFilter", "Filter...", ref _npcFilter, 64);

        using var table = ImRaii.Table("npcs", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 400 * ImGuiHelpers.GlobalScale));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);

        ImGui.TableSetupColumn("Button",  ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name",    ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 300);
        ImGui.TableSetupColumn("Kind",    ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Id",      ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Model",      ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Visor",   ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Compare", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeightWithSpacing());
        ImGui.TableNextRow();
        var idx = 0;
        var remainder = ImGuiClip.FilteredClippedDraw(_npcCombo.Items, skips,
            d => d.Name.Contains(_npcFilter, StringComparison.OrdinalIgnoreCase), DrawData);
        ImGui.TableNextColumn();
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeightWithSpacing());
        return;

        void DrawData(NpcData data)
        {
            using var id       = ImRaii.PushId(idx++);
            var       disabled = !_state.GetOrCreate(_objectManager.Player, out var state);
            ImGui.TableNextColumn();
            if (ImGuiUtil.DrawDisabledButton("Apply", Vector2.Zero, string.Empty, disabled))
            {
                foreach (var (slot, item, stain) in _designConverter.FromDrawData(data.Equip.ToArray(), data.Mainhand, data.Offhand, true))
                    _state.ChangeEquip(state!, slot, item, stain, StateChanged.Source.Manual);
                _state.ChangeVisorState(state!, data.VisorToggled, StateChanged.Source.Manual);
                _state.ChangeCustomize(state!, data.Customize, CustomizeFlagExtensions.All, StateChanged.Source.Manual);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(data.Name);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(data.Kind is ObjectKind.BattleNpc ? "B" : "E");

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(data.Id.Id.ToString());

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(data.ModelId.ToString());

            using (_ = ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(data.VisorToggled ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Times.ToIconString());
            }

            using var mono = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_customizeOrGear ? data.Customize.ToString() : data.WriteGear());
        }
    }
}
