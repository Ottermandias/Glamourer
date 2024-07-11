using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class ModAssociationsTab(PenumbraService penumbra, DesignFileSystemSelector selector, DesignManager manager)
{
    private readonly ModCombo              _modCombo = new(penumbra, Glamourer.Log);
    private          (Mod, ModSettings)[]? _copy;

    public void Draw()
    {
        using var h = ImRaii.CollapsingHeader("Mod Associations");
        ImGuiUtil.HoverTooltip(
            "This tab can store information about specific mods associated with this design.\n\n"
          + "It does NOT change any mod settings automatically, though there is functionality to apply desired mod settings manually.\n"
          + "You can also use it to quickly open the associated mod page in Penumbra.\n\n"
          + "It is not feasible to apply those changes automatically in general cases, since there would be no way to revert those changes, handle multiple designs applying at once, etc.");
        if (!h)
            return;

        DrawApplyAllButton();
        DrawTable();
        DrawCopyButtons();
    }

    private void DrawCopyButtons()
    {
        var size = new Vector2((ImGui.GetContentRegionAvail().X - 2 * ImGui.GetStyle().ItemSpacing.X) / 3, 0);
        if (ImGui.Button("Copy All to Clipboard", size))
            _copy = selector.Selected!.AssociatedMods.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

        ImGui.SameLine();

        if (ImGuiUtil.DrawDisabledButton("Add from Clipboard", size,
                _copy != null
                    ? $"Add {_copy.Length} mod association(s) from clipboard."
                    : "Copy some mod associations to the clipboard, first.", _copy == null))
            foreach (var (mod, setting) in _copy!)
                manager.UpdateMod(selector.Selected!, mod, setting);

        ImGui.SameLine();

        if (ImGuiUtil.DrawDisabledButton("Set from Clipboard", size,
                _copy != null
                    ? $"Set {_copy.Length} mod association(s) from clipboard and discard existing."
                    : "Copy some mod associations to the clipboard, first.", _copy == null))
        {
            while (selector.Selected!.AssociatedMods.Count > 0)
                manager.RemoveMod(selector.Selected!, selector.Selected!.AssociatedMods.Keys[0]);
            foreach (var (mod, setting) in _copy!)
                manager.AddMod(selector.Selected!, mod, setting);
        }
    }

    private void DrawApplyAllButton()
    {
        var (id, name) = penumbra.CurrentCollection;
        if (ImGuiUtil.DrawDisabledButton($"Try Applying All Associated Mods to {name}##applyAll",
                new Vector2(ImGui.GetContentRegionAvail().X, 0), string.Empty, id == Guid.Empty))
            ApplyAll();
    }

    public void DrawApplyButton()
    {
        var (id, name) = penumbra.CurrentCollection;
        if (ImGuiUtil.DrawDisabledButton("Apply Mod Associations", Vector2.Zero,
                $"Try to apply all associated mod settings to Penumbras current collection {name}",
                selector.Selected!.AssociatedMods.Count == 0 || id == Guid.Empty))
            ApplyAll();
    }

    public void ApplyAll()
    {
        foreach (var (mod, settings) in selector.Selected!.AssociatedMods)
            penumbra.SetMod(mod, settings);
    }

    private void DrawTable()
    {
        using var table = ImRaii.Table("Mods", 5, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("##Buttons", ImGuiTableColumnFlags.WidthFixed,
            ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemInnerSpacing.X * 2);
        ImGui.TableSetupColumn("Mod Name",       ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("State",          ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("State").X);
        ImGui.TableSetupColumn("Priority",       ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Priority").X);
        ImGui.TableSetupColumn("##Options",      ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Applym").X);
        ImGui.TableHeadersRow();

        Mod?                             removedMod = null;
        (Mod mod, ModSettings settings)? updatedMod = null;
        foreach (var ((mod, settings), idx) in selector.Selected!.AssociatedMods.WithIndex())
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
            manager.RemoveMod(selector.Selected!, removedMod.Value);

        if (updatedMod.HasValue)
            manager.UpdateMod(selector.Selected!, updatedMod.Value.mod, updatedMod.Value.settings);
    }

    private void DrawAssociatedModRow(Mod mod, ModSettings settings, out Mod? removedMod, out (Mod, ModSettings)? updatedMod)
    {
        removedMod = null;
        updatedMod = null;
        ImGui.TableNextColumn();
        var buttonSize = new Vector2(ImGui.GetFrameHeight());
        var spacing    = ImGui.GetStyle().ItemInnerSpacing.X;
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), buttonSize,
                "Delete this mod from associations.", false, true))
            removedMod = mod;

        ImGui.SameLine(0, spacing);
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clipboard.ToIconString(), buttonSize,
                "Copy this mod setting to clipboard.", false, true))
            _copy = [(mod, settings)];

        ImGui.SameLine(0, spacing);
        ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.RedoAlt.ToIconString(), buttonSize,
            "Update the settings of this mod association.", false, true);
        if (ImGui.IsItemHovered())
        {
            var newSettings = penumbra.GetModSettings(mod);
            if (ImGui.IsItemClicked())
                updatedMod = (mod, newSettings);

            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt    = ImRaii.Tooltip();
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
        
        if (ImGui.Selectable($"{mod.Name}##name"))
            penumbra.OpenModPage(mod);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Mod Directory:    {mod.DirectoryName}\n\nClick to open mod page in Penumbra.");
        ImGui.TableNextColumn();
        using (var font = ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGuiUtil.Center((settings.Enabled ? FontAwesomeIcon.Check : FontAwesomeIcon.Times).ToIconString());
        }

        ImGui.TableNextColumn();
        ImGuiUtil.RightAlign(settings.Priority.ToString());
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton("Apply", new Vector2(ImGui.GetContentRegionAvail().X, 0), string.Empty,
                !penumbra.Available))
        {
            var text = penumbra.SetMod(mod, settings);
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
        var tt = currentName.IsNullOrEmpty()
            ? "Please select a mod first."
            : selector.Selected!.AssociatedMods.ContainsKey(_modCombo.CurrentSelection.Mod)
                ? "The design already contains an association with the selected mod."
                : string.Empty;

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), new Vector2(ImGui.GetFrameHeight()), tt, tt.Length > 0,
                true))
            manager.AddMod(selector.Selected!, _modCombo.CurrentSelection.Mod, _modCombo.CurrentSelection.Settings);
        ImGui.TableNextColumn();
        _modCombo.Draw("##new", currentName.IsNullOrEmpty() ? "Select new Mod..." : currentName, string.Empty,
            ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight());
    }
}
