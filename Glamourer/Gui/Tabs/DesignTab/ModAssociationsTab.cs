using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Text.Widget;

namespace Glamourer.Gui.Tabs.DesignTab;

public class ModAssociationsTab(PenumbraService penumbra, DesignFileSystemSelector selector, DesignManager manager, Configuration config)
{
    private readonly ModCombo              _modCombo = new(penumbra, Glamourer.Log, selector);
    private          (Mod, ModSettings)[]? _copy;

    public void Draw()
    {
        using var h = DesignPanelFlag.ModAssociations.Header(config);
        if (h.Disposed)
            return;

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
            penumbra.SetMod(mod, settings, StateSource.Manual);
    }

    private void DrawTable()
    {
        using var table = ImUtf8.Table("Mods"u8, config.UseTemporarySettings ? 7 : 6, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImUtf8.TableSetupColumn("##Buttons"u8, ImGuiTableColumnFlags.WidthFixed,
            ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemInnerSpacing.X * 2);
        ImUtf8.TableSetupColumn("Mod Name"u8, ImGuiTableColumnFlags.WidthStretch);
        if (config.UseTemporarySettings)
            ImUtf8.TableSetupColumn("Remove"u8, ImGuiTableColumnFlags.WidthFixed, ImUtf8.CalcTextSize("Remove"u8).X);
        ImUtf8.TableSetupColumn("Inherit"u8,   ImGuiTableColumnFlags.WidthFixed, ImUtf8.CalcTextSize("Inherit"u8).X);
        ImUtf8.TableSetupColumn("State"u8,     ImGuiTableColumnFlags.WidthFixed, ImUtf8.CalcTextSize("State"u8).X);
        ImUtf8.TableSetupColumn("Priority"u8,  ImGuiTableColumnFlags.WidthFixed, ImUtf8.CalcTextSize("Priority"u8).X);
        ImUtf8.TableSetupColumn("##Options"u8, ImGuiTableColumnFlags.WidthFixed, ImUtf8.CalcTextSize("Applym"u8).X);
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
        var canDelete = config.DeleteDesignModifier.IsActive();
        if (canDelete)
        {
            if (ImUtf8.IconButton(FontAwesomeIcon.Trash, "Delete this mod from associations."u8))
                removedMod = mod;
        }
        else
        {
            ImUtf8.IconButton(FontAwesomeIcon.Trash, $"Delete this mod from associations.\nHold {config.DeleteDesignModifier} to delete.",
                disabled: true);
        }

        ImUtf8.SameLineInner();
        if (ImUtf8.IconButton(FontAwesomeIcon.Clipboard, "Copy this mod setting to clipboard."u8))
            _copy = [(mod, settings)];

        ImUtf8.SameLineInner();
        ImUtf8.IconButton(FontAwesomeIcon.RedoAlt, "Update the settings of this mod association."u8);
        if (ImGui.IsItemHovered())
        {
            var newSettings = penumbra.GetModSettings(mod, out var source);
            if (ImGui.IsItemClicked())
                updatedMod = (mod, newSettings);

            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt    = ImUtf8.Tooltip();
            if (source.Length > 0)
                ImUtf8.Text($"Using temporary settings made by {source}.");
            ImGui.Separator();
            var namesDifferent = mod.Name != mod.DirectoryName;
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImUtf8.Text("Directory Name"u8);
                ImUtf8.Text("Force Inherit"u8);
                ImUtf8.Text("Enabled"u8);
                ImUtf8.Text("Priority"u8);
                ModCombo.DrawSettingsLeft(newSettings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            using (ImRaii.Group())
            {
                if (namesDifferent)
                    ImUtf8.Text(mod.DirectoryName);

                ImUtf8.Text(newSettings.ForceInherit.ToString());
                ImUtf8.Text(newSettings.Enabled.ToString());
                ImUtf8.Text(newSettings.Priority.ToString());
                ModCombo.DrawSettingsRight(newSettings);
            }
        }

        ImGui.TableNextColumn();

        if (ImUtf8.Selectable($"{mod.Name}##name"))
            penumbra.OpenModPage(mod);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Mod Directory:    {mod.DirectoryName}\n\nClick to open mod page in Penumbra.");
        if (config.UseTemporarySettings)
        {
            ImGui.TableNextColumn();
            var remove = settings.Remove;
            if (TwoStateCheckbox.Instance.Draw("##Remove"u8, ref remove))
                updatedMod = (mod, settings with { Remove = remove });
            ImUtf8.HoverTooltip(
                "Remove any temporary settings applied by Glamourer instead of applying the configured settings. Only works when using temporary settings, ignored otherwise."u8);
        }

        ImGui.TableNextColumn();
        var inherit = settings.ForceInherit;
        if (TwoStateCheckbox.Instance.Draw("##ForceInherit"u8, ref inherit))
            updatedMod = (mod, settings with { ForceInherit = inherit });
        ImUtf8.HoverTooltip("Force the mod to inherit its settings from inherited collections."u8);
        ImGui.TableNextColumn();
        var enabled = settings.Enabled;
        if (TwoStateCheckbox.Instance.Draw("##Enabled"u8, ref enabled))
            updatedMod = (mod, settings with { Enabled = enabled });

        ImGui.TableNextColumn();
        var priority = settings.Priority;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImUtf8.InputScalarOnDeactivated("##Priority"u8, ref priority))
            updatedMod = (mod, settings with { Priority = priority });
        ImGui.TableNextColumn();
        if (ImGuiUtil.DrawDisabledButton("Apply", new Vector2(ImGui.GetContentRegionAvail().X, 0), string.Empty,
                !penumbra.Available))
        {
            var text = penumbra.SetMod(mod, settings, StateSource.Manual);
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
