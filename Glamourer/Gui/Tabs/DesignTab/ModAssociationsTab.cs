using Dalamud.Interface.ImGuiNotification;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class ModAssociationsTab(PenumbraService penumbra, DesignFileSystem fileSystem, DesignManager manager, Configuration config) : IUiService
{
    private readonly ModCombo              _modCombo = new(penumbra, fileSystem);
    private          (Mod, ModSettings)[]? _copy;

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
        var (id, name) = penumbra.CurrentCollection;
        if (config.Ephemeral.IncognitoMode)
            name = id.ShortGuid();
        if (ImEx.Button($"Try Applying All Associated Mods to {name}##applyAll",
                Im.ContentRegion.Available with { Y = 0 }, string.Empty, id == Guid.Empty))
            ApplyAll();
    }

    public void DrawApplyButton()
    {
        var (id, name) = penumbra.CurrentCollection;
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
        using var table = Im.Table.Begin("Mods"u8, config.UseTemporarySettings ? 7 : 6, TableFlags.RowBackground);
        if (!table)
            return;

        table.SetupColumn("##Buttons"u8, TableColumnFlags.WidthFixed, Im.Style.FrameHeight * 3 + Im.Style.ItemInnerSpacing.X * 2);
        table.SetupColumn("Mod Name"u8,  TableColumnFlags.WidthStretch);
        if (config.UseTemporarySettings)
            table.SetupColumn("Remove"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Remove"u8).X);
        table.SetupColumn("Inherit"u8,   TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Inherit"u8).X);
        table.SetupColumn("State"u8,     TableColumnFlags.WidthFixed, Im.Font.CalculateSize("State"u8).X);
        table.SetupColumn("Priority"u8,  TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Priority"u8).X);
        table.SetupColumn("##Options"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Applym"u8).X);
        table.HeaderRow();

        Mod?                             removedMod = null;
        (Mod mod, ModSettings settings)? updatedMod = null;
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

    private void DrawAssociatedModRow(in Im.TableDisposable table, Mod mod, ModSettings settings, out Mod? removedMod,
        out (Mod, ModSettings)? updatedMod)
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
            var namesDifferent = mod.Name != mod.DirectoryName;
            Im.Dummy(300 * Im.Style.GlobalScale);
            using (Im.Group())
            {
                if (namesDifferent)
                    Im.Text("Directory Name"u8);
                Im.Text("Force Inherit"u8);
                Im.Text("Enabled"u8);
                Im.Text("Priority"u8);
                ModCombo.DrawSettingsLeft(newSettings);
            }

            Im.Line.Same(Math.Max(Im.Item.Size.X + 3 * Im.Style.ItemSpacing.X, 150 * Im.Style.GlobalScale));
            using (Im.Group())
            {
                if (namesDifferent)
                    Im.Text(mod.DirectoryName);

                Im.Text($"{newSettings.ForceInherit}");
                Im.Text($"{newSettings.Enabled}");
                Im.Text($"{newSettings.Priority}");
                ModCombo.DrawSettingsRight(newSettings);
            }
        }

        table.NextColumn();

        if (Im.Selectable($"{mod.Name}##name"))
            penumbra.OpenModPage(mod);
        Im.Tooltip.OnHover($"Mod Directory:    {mod.DirectoryName}\n\nClick to open mod page in Penumbra.");
        if (config.UseTemporarySettings)
        {
            table.NextColumn();
            var remove = settings.Remove;
            if (ImEx.TwoStateCheckbox("##Remove"u8, ref remove))
                updatedMod = (mod, settings with { Remove = remove });
            Im.Tooltip.OnHover(
                "Remove any temporary settings applied by Glamourer instead of applying the configured settings. Only works when using temporary settings, ignored otherwise."u8);
        }

        table.NextColumn();
        var inherit = settings.ForceInherit;
        if (ImEx.TwoStateCheckbox("##ForceInherit"u8, ref inherit))
            updatedMod = (mod, settings with { ForceInherit = inherit });
        Im.Tooltip.OnHover("Force the mod to inherit its settings from inherited collections."u8);
        table.NextColumn();
        var enabled = settings.Enabled;
        if (ImEx.TwoStateCheckbox("##Enabled"u8, ref enabled))
            updatedMod = (mod, settings with { Enabled = enabled });

        table.NextColumn();
        var priority = settings.Priority;
        Im.Item.SetNextWidthFull();
        if (ImEx.InputOnDeactivation.Scalar("##Priority"u8, ref priority))
            updatedMod = (mod, settings with { Priority = priority });
        table.NextColumn();
        if (ImEx.Button("Apply"u8, Im.ContentRegion.Available with { Y = 0 }, StringU8.Empty, !penumbra.Available))
        {
            var text = penumbra.SetMod(mod, settings, StateSource.Manual, false);
            if (text.Length > 0)
                Glamourer.Messager.NotificationMessage(text, NotificationType.Warning, false);
        }

        DrawAssociatedModTooltip(settings);
    }

    private static void DrawAssociatedModTooltip(ModSettings settings)
    {
        if (settings is not { Enabled: true, Settings.Count: > 0 } || !Im.Item.Hovered())
            return;

        using var t = Im.Tooltip.Begin();
        Im.Text("This will also try to apply the following settings to the current collection:"u8);

        Im.Line.New();
        using (Im.Group())
        {
            ModCombo.DrawSettingsLeft(settings);
        }

        Im.Line.Same(Im.ContentRegion.Available.X / 2);
        using (Im.Group())
        {
            ModCombo.DrawSettingsRight(settings);
        }
    }

    private void DrawNewModRow(in Im.TableDisposable table)
    {
        var currentDir = _modCombo.Selection;
        table.NextColumn();
        var tt = currentDir.Length is 0
            ? "Please select a mod first."u8
            : Selection.AssociatedMods.ContainsKey(new Mod(_modCombo.SelectionName, currentDir))
                ? "The design already contains an association with the selected mod."u8
                : StringU8.Empty;

        if (ImEx.Icon.Button(LunaStyle.AddObjectIcon, tt, tt.Length > 0))
            manager.AddMod(Selection, new Mod(_modCombo.SelectionName, _modCombo.Selection), _modCombo.Settings);
        table.NextColumn();
        _modCombo.Draw("##new"u8, Im.ContentRegion.Available.X);
    }
}
