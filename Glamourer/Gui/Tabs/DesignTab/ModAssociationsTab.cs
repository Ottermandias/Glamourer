﻿using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class ModAssociationsTab
{
    private readonly PenumbraService          _penumbra;
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignManager            _manager;
    private readonly ModCombo                 _modCombo;

    public ModAssociationsTab(PenumbraService penumbra, DesignFileSystemSelector selector, DesignManager manager)
    {
        _penumbra = penumbra;
        _selector = selector;
        _manager  = manager;
        _modCombo = new ModCombo(penumbra, Glamourer.Log);
    }

    public void Draw()
    {
        using var h          = ImRaii.CollapsingHeader("Mod Associations");
        ImGuiUtil.HoverTooltip(
            "This tab can store information about specific mods associated with this design.\n\n"
          + "It does NOT change any mod settings automatically, though there is functionality to apply desired mod settings manually.\n"
          + "You can also use it to quickly open the associated mod page in Penumbra.\n\n"
          + "It is not feasible to apply those changes automatically in general cases, since there would be no way to revert those changes, handle multiple designs applying at once, etc.");
        if (!h)
            return;

        DrawApplyAllButton();
        DrawTable();
    }

    private void DrawApplyAllButton()
    {
        var current = _penumbra.CurrentCollection;
        if (ImGuiUtil.DrawDisabledButton($"Try Applying All Associated Mods to {current}##applyAll",
                new Vector2(ImGui.GetContentRegionAvail().X, 0), string.Empty, current is "<Unavailable>"))
            ApplyAll();
    }

    public void DrawApplyButton()
    {
        var current = _penumbra.CurrentCollection;
        if (ImGuiUtil.DrawDisabledButton("Apply Mod Associations", Vector2.Zero,
                $"Try to apply all associated mod settings to Penumbras current collection {current}",
                _selector.Selected!.AssociatedMods.Count == 0 || current is "<Unavailable>"))
            ApplyAll();
    }

    public void ApplyAll()
    {
        foreach (var (mod, settings) in _selector.Selected!.AssociatedMods)
            _penumbra.SetMod(mod, settings);
    }

    private void DrawTable()
    {
        using var table = ImRaii.Table("Mods", 7, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("##Delete",       ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Update",       ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("Mod Name",       ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Directory Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("State",          ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("State").X);
        ImGui.TableSetupColumn("Priority",       ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Priority").X);
        ImGui.TableSetupColumn("##Options",      ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Try Applyingm").X);
        ImGui.TableHeadersRow();

        Mod? removedMod = null;
        (Mod mod, ModSettings settings)? updatedMod = null;
        foreach (var ((mod, settings), idx) in _selector.Selected!.AssociatedMods.WithIndex())
        {
            using var id = ImRaii.PushId(idx);
            DrawAssociatedModRow(mod, settings, out var removedModTmp, out var updatedModTmp);
            if (removedModTmp.HasValue)
                removedMod = removedModTmp;
            if (updatedModTmp.HasValue)
                updatedMod = updatedModTmp;
        }

        DrawNewModRow();

        if (removedMod.HasValue)
            _manager.RemoveMod(_selector.Selected!, removedMod.Value);
        
        if (updatedMod.HasValue)
            _manager.UpdateMod(_selector.Selected!, updatedMod.Value.mod, updatedMod.Value.settings);
    }

    private void DrawAssociatedModRow(Mod mod, ModSettings settings, out Mod? removedMod, out (Mod, ModSettings)? updatedMod)
    {
        removedMod = null;
        updatedMod = null;
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                "Delete this mod from associations", false, true))
            removedMod = mod;

        ImGui.TableNextColumn();
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.RedoAlt.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
            "Update the settings of this mod association", false, true);
        
        if (ImGui.IsItemHovered())
        {
            var (_, newSettings) = _penumbra.GetMods().FirstOrDefault(m => m.Mod == mod);
            if (ImGui.IsItemClicked())
                updatedMod = (mod, newSettings);
            
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Separator();
            var namesDifferent = mod.Name != mod.DirectoryName;
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted("Directory Name");
                ImGui.TextUnformatted("Enabled");
                ImGui.TextUnformatted("Priority");
                ModCombo.DrawSettingsLeft(newSettings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImGui.TextUnformatted(mod.DirectoryName);
                ImGui.TextUnformatted(newSettings.Enabled.ToString());
                ImGui.TextUnformatted(newSettings.Priority.ToString());
                ModCombo.DrawSettingsRight(newSettings);
            }
        }
        
        ImGui.TableNextColumn();
        var selected = ImGui.Selectable($"{mod.Name}##name");
        var hovered  = ImGui.IsItemHovered();
        ImGui.TableNextColumn();
        selected |= ImGui.Selectable($"{mod.DirectoryName}##directory");
        hovered  |= ImGui.IsItemHovered();
        if (selected)
            _penumbra.OpenModPage(mod);
        if (hovered)
            ImGui.SetTooltip("Click to open mod page in Penumbra.");
        ImGui.TableNextColumn();
        using (var font = ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGuiUtil.Center((settings.Enabled ? FontAwesomeIcon.Check : FontAwesomeIcon.Times).ToIconString());
        }

        ImGui.TableNextColumn();
        ImGuiUtil.RightAlign(settings.Priority.ToString());
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton("Try Applying", new Vector2(ImGui.GetContentRegionAvail().X, 0), string.Empty,
                !_penumbra.Available))
        {
            var text = _penumbra.SetMod(mod, settings);
            if (text.Length > 0)
                Glamourer.Messager.NotificationMessage(text, NotificationType.Warning, false);
        }

        DrawAssociatedModTooltip(settings);
    }

    private static void DrawAssociatedModTooltip(ModSettings settings)
    {
        if (settings is not { Enabled: true, Settings.Count: > 0 } || !ImGui.IsItemHovered())
            return;

        using var t = ImRaii.Tooltip();
        ImGui.TextUnformatted("This will also try to apply the following settings to the current collection:");

        ImGui.NewLine();
        using (var _ = ImRaii.Group())
        {
            ModCombo.DrawSettingsLeft(settings);
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2);
        using (var _ = ImRaii.Group())
        {
            ModCombo.DrawSettingsRight(settings);
        }
    }

    private void DrawNewModRow()
    {
        var currentName = _modCombo.CurrentSelection.Mod.Name;
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        var tt = currentName.IsNullOrEmpty()
            ? "Please select a mod first."
            : _selector.Selected!.AssociatedMods.ContainsKey(_modCombo.CurrentSelection.Mod)
                ? "The design already contains an association with the selected mod."
                : string.Empty;

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), new Vector2(ImGui.GetFrameHeight()), tt, tt.Length > 0,
                true))
            _manager.AddMod(_selector.Selected!, _modCombo.CurrentSelection.Mod, _modCombo.CurrentSelection.Settings);
        ImGui.TableNextColumn();
        _modCombo.Draw("##new", currentName.IsNullOrEmpty() ? "Select new Mod..." : currentName, string.Empty,
            ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight());
    }
}
