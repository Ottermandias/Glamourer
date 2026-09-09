using Dalamud.Interface.ImGuiNotification;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.Api.Preset;
using Penumbra.GameData.Gui;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ModAssociationsTab(PenumbraSubscriber penumbra, DesignFileSystem fileSystem, DesignManager manager, Configuration config)
    : IUiService
{
    private readonly ModCombo                              _modCombo = new(penumbra, fileSystem);
    private          (ModIdentifier, SettingPresetData)[]? _copy;

    private Design Selection
        => (Design)fileSystem.Selection.Selection!.Value;

    public void Draw()
    {
        using var h = DesignPanelFlag.ModAssociations.Header(config);
        if (!h.Alive)
            return;

        Im.Tooltip.OnHover(
            "This tab can store information about specific mods associated with this design.\n\n"u8
          + "It does NOT change any mod settings automatically, though there is functionality to apply desired mod settings manually.\n"u8
          + "You can also use it to quickly open the associated mod page in Penumbra.\n\n"u8
          + "It is not feasible to apply those changes automatically in general cases, since there would be no way to revert those changes, handle multiple designs applying at once, etc."u8);
        if (!h)
            return;

        DrawApplyAllButton();
        DrawTable();
        DrawCopyButtons();
    }

    private void DrawCopyButtons()
    {
        var size = new Vector2((Im.ContentRegion.Available.X - 2 * Im.Style.ItemSpacing.X) / 3, 0);
        if (Im.Button("Copy All to Clipboard"u8, size))
            _copy = Selection.AssociatedMods.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

        Im.Line.Same();

        if (ImEx.Button("Add from Clipboard"u8, size,
                _copy is not null
                    ? $"Add {_copy.Length} mod association(s) from clipboard."
                    : "Copy some mod associations to the clipboard, first."u8, _copy is null))
            foreach (var (mod, setting) in _copy!)
                manager.UpdateMod(Selection, mod, setting);

        Im.Line.Same();

        if (ImEx.Button("Set from Clipboard"u8, size,
                _copy is not null
                    ? $"Set {_copy.Length} mod association(s) from clipboard and discard existing."
                    : "Copy some mod associations to the clipboard, first."u8, _copy is null))
        {
            while (Selection.AssociatedMods.Count > 0)
                manager.RemoveMod(Selection, Selection.AssociatedMods.Keys[0]);
            foreach (var (mod, setting) in _copy!)
                manager.AddMod(Selection, mod, setting);
        }
    }

    private void DrawApplyAllButton()
    {
        var (id, name, _) = penumbra.CurrentCollection;
        if (config.Ephemeral.IncognitoMode)
            name = id.ShortGuid();
        if (ImEx.Button($"Try Applying All Associated Mods to {name}##applyAll",
                Im.ContentRegion.Available with { Y = 0 }, string.Empty, id == Guid.Empty))
            ApplyAll();
    }

    public void DrawApplyButton()
    {
        var (id, name, _) = penumbra.CurrentCollection;
        if (ImEx.Button("Apply Mod Associations"u8, Vector2.Zero,
                $"Try to apply all associated mod settings to Penumbras current collection {name}",
                Selection.AssociatedMods.Count is 0 || id == Guid.Empty))
            ApplyAll();
    }

    public void ApplyAll()
    {
        foreach (var (mod, settings) in Selection.AssociatedMods)
            penumbra.SetMod(mod, settings, StateSource.Manual, false);
    }

    private void DrawTable()
    {
        using var table = Im.Table.Begin("Mods"u8, 5, TableFlags.RowBackground);
        if (!table)
            return;

        table.SetupColumn("##Buttons"u8, TableColumnFlags.WidthFixed, Im.Style.FrameHeight * 3 + Im.Style.ItemInnerSpacing.X * 2);
        table.SetupColumn("Mod Name"u8,  TableColumnFlags.WidthStretch);
        table.SetupColumn("State"u8,     TableColumnFlags.WidthFixed, 85 * Im.Style.GlobalScale);
        table.SetupColumn("Priority"u8,  TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Priority"u8).X + Im.Style.FrameHeightWithSpacing);
        table.SetupColumn("##Options"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Applym"u8).X);
        table.HeaderRow();

        ModIdentifier?                                   removedMod = null;
        (ModIdentifier mod, SettingPresetData settings)? updatedMod = null;
        foreach (var (idx, (mod, settings)) in Selection.AssociatedMods.Index())
        {
            using var id = Im.Id.Push(idx);
            DrawAssociatedModRow(table, mod, settings, out var removedModTmp, out var updatedModTmp);
            if (removedModTmp.HasValue)
                removedMod = removedModTmp;
            if (updatedModTmp.HasValue)
                updatedMod = updatedModTmp;
        }

        DrawNewModRow(table);

        if (removedMod.HasValue)
            manager.RemoveMod(Selection, removedMod.Value);

        if (updatedMod.HasValue)
            manager.UpdateMod(Selection, updatedMod.Value.mod, updatedMod.Value.settings);
    }

    private void DrawAssociatedModRow(in Im.TableDisposable table, ModIdentifier mod, SettingPresetData settings, out ModIdentifier? removedMod,
        out (ModIdentifier, SettingPresetData)? updatedMod)
    {
        removedMod = null;
        updatedMod = null;
        table.NextColumn();
        var canDelete = config.DeleteDesignModifier.IsActive();
        if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Delete this mod from associations."u8, !canDelete))
            removedMod = mod;
        if (!canDelete)
            Im.Tooltip.OnHover($"\nHold {config.DeleteDesignModifier} to delete.");

        Im.Line.SameInner();
        if (ImEx.Icon.Button(LunaStyle.ToClipboardIcon, "Copy this mod setting to clipboard."u8))
            _copy = [(mod, settings)];

        Im.Line.SameInner();
        ImEx.Icon.Button(LunaStyle.RefreshIcon, "Update the settings of this mod association."u8);
        if (Im.Item.Hovered())
        {
            var newSettings = penumbra.GetModSettings(mod, out var source);
            if (Im.Item.Clicked())
                updatedMod = (mod, newSettings);

            using var style = ImStyleSingle.PopupBorderThickness.Push(2 * Im.Style.GlobalScale);
            using var tt    = Im.Tooltip.Begin();
            if (source.Length > 0)
                Im.Text($"Using temporary settings made by {source}.");
            Im.Separator();
            var namesDifferent = mod.Name != mod.Identifier;
            Im.Dummy(300 * Im.Style.GlobalScale);
            using (Im.Group())
            {
                if (namesDifferent)
                    Im.Text("Directory Name"u8);
                Im.Text("State"u8);
                Im.Text("Priority"u8);
                ModCombo.DrawSettingsLeft(newSettings);
            }

            Im.Line.Same(Math.Max(Im.Item.Size.X + 3 * Im.Style.ItemSpacing.X, 150 * Im.Style.GlobalScale));
            using (Im.Group())
            {
                if (namesDifferent)
                    Im.Text(mod.Identifier);
                Im.Text(newSettings.State.StringU8);
                Im.Text(newSettings._hasPriority ? $"{newSettings._priority}" : "Ignored"u8);
                ModCombo.DrawSettingsRight(newSettings);
            }
        }

        table.NextColumn();

        if (Im.Selectable($"{mod.Name}##name"))
            penumbra.Ui.OpenMod(mod);
        Im.Tooltip.OnHover($"Mod Directory:    {mod.Identifier}\n\nClick to open mod page in Penumbra.");

        table.NextColumn();
        if (settings.DrawState(Im.ContentRegion.Available with { Y = 0 }, out var newState))
            updatedMod = (mod, settings with { _state = (byte)newState });
        table.NextColumn();
        if (settings.DrawPriority(Im.ContentRegion.Available with { Y = 0 }, out var newPriority))
            updatedMod = (mod, settings with
            {
                _hasPriority = newPriority.HasValue,
                _priority = newPriority ?? 0,
            });
        table.NextColumn();
        var modIndex = !penumbra.Available ? -1 : penumbra.Mods.IndexByName(mod);
        if (ImEx.Button("Apply"u8, Im.ContentRegion.Available with { Y = 0 }, StringU8.Empty, modIndex < 0))
        {
            var text = penumbra.SetMod(mod, settings, StateSource.Manual, false);
            if (text.Length > 0)
                Glamourer.Messager.NotificationMessage(text, NotificationType.Warning, false);
        }

        DrawApplicationTooltip(modIndex, settings);
    }

    private void DrawApplicationTooltip(int modIndex, in SettingPresetData settings)
    {
        if (!Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            return;

        using var t = Im.Tooltip.Begin();
        if (modIndex < 0)
        {
            Im.Text("No mod matching the stored mod name is currently installed."u8, LunaStyle.ErrorForeground);
            return;
        }

        using var collection = penumbra.Current;
        if (collection is null)
            Im.Text("Not connected to Penumbra."u8);
        else if (collection.CanUnlock(modIndex, PenumbraSubscriber.KeyManual))
            collection.DrawPresetTooltip(modIndex, settings);
        else
            Im.Text($"The matching mod already has locking temporary settings made by {collection.GetTemporarySource(modIndex)}.",
                LunaStyle.ErrorForeground);
    }

    private void DrawNewModRow(in Im.TableDisposable table)
    {
        var currentDir = _modCombo.Selection;
        table.NextColumn();
        var tt = currentDir.Length is 0
            ? "Please select a mod first."u8
            : Selection.AssociatedMods.ContainsKey(new ModIdentifier(currentDir, _modCombo.SelectionName))
                ? "The design already contains an association with the selected mod."u8
                : StringU8.Empty;

        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, tt.Length > 0))
            manager.AddMod(Selection, new ModIdentifier(_modCombo.Selection, _modCombo.SelectionName), _modCombo.Settings);
        table.NextColumn();
        _modCombo.Draw("##new"u8, Im.ContentRegion.Available.X);
    }
}
