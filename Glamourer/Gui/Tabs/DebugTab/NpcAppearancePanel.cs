using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs.DebugTab;

public class NpcAppearancePanel(NpcCombo _npcCombo, StateManager _state, ObjectManager _objectManager, DesignConverter _designConverter) : IDebugTabTree
{
    public string Label
        => "NPC Appearance";

    public bool Disabled
        => false;

    private string _npcFilter       = string.Empty;
    private bool   _customizeOrGear = false;

    public void Draw()
    {
        ImGui.Checkbox("Compare Customize (or Gear)", ref _customizeOrGear);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##npcFilter", "Filter...", ref _npcFilter, 64);

        using var table = ImRaii.Table("npcs", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 400 * ImGuiHelpers.GlobalScale));
        if (!table)
            return;

        ImGui.TableSetupColumn("Button",  ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name",    ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 300);
        ImGui.TableSetupColumn("Kind",    ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Visor",   ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Compare", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeightWithSpacing());
        ImGui.TableNextRow();
        var idx = 0;
        var remainder = ImGuiClip.FilteredClippedDraw(_npcCombo.Items, skips,
            d => d.Name.Contains(_npcFilter, StringComparison.OrdinalIgnoreCase), Draw);
        ImGui.TableNextColumn();
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeightWithSpacing());


        return;

        void Draw(NpcData data)
        {
            using var id       = ImRaii.PushId(idx++);
            var       disabled = !_state.GetOrCreate(_objectManager.Player, out var state);
            ImGui.TableNextColumn();
            if (ImGuiUtil.DrawDisabledButton("Apply", Vector2.Zero, string.Empty, disabled, false))
            {
                foreach (var (slot, item, stain) in _designConverter.FromDrawData(data.Equip.ToArray(), data.Mainhand, data.Offhand))
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

            using (var icon = ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(data.VisorToggled ? FontAwesomeIcon.Check.ToIconString() : FontAwesomeIcon.Times.ToIconString());
            }

            using var mono = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_customizeOrGear ? data.Customize.Data.ToString() : data.WriteGear());
        }
    }
}
