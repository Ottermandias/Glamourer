using Dalamud.Interface.ImGuiNotification;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Gui.Tabs.SettingsTab;
using Glamourer.Services;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignDetailTab : IUiService
{
    private readonly SaveService          _saveService;
    private readonly Configuration        _config;
    private readonly DesignFileSystem     _fileSystem;
    private readonly DesignManager        _manager;
    private readonly DesignColors         _colors;
    private readonly DesignColorCombo     _colorCombo;
    private readonly PredefinedTagManager _predefinedTags;

    private bool _editDescriptionMode;

    public DesignDetailTab(SaveService saveService, DesignManager manager, DesignFileSystem fileSystem,
        DesignColors colors, Configuration config, PredefinedTagManager predefinedTags)
    {
        _saveService    = saveService;
        _manager        = manager;
        _fileSystem     = fileSystem;
        _colors         = colors;
        _config         = config;
        _predefinedTags = predefinedTags;
        _colorCombo     = new DesignColorCombo(_colors, false);
    }

    public void Draw()
    {
        using var h = DesignPanelFlag.DesignDetails.Header(_config);
        if (!h)
            return;

        DrawDesignInfoTable();
        DrawDescription();
        Im.Line.New();
    }

    private Design Selected
        => (Design)_fileSystem.Selection.Selection!.Value;

    private void DrawDesignInfoTable()
    {
        using var style = ImStyleDouble.ButtonTextAlign.Push(new Vector2(0, 0.5f));
        using var table = Im.Table.Begin("Details"u8, 2);
        if (!table)
            return;

        table.SetupColumn("Type"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Reset Temporary Settings"u8).X);
        table.SetupColumn("Data"u8, TableColumnFlags.WidthStretch);

        table.DrawFrameColumn("Design Name"u8);
        table.NextColumn();
        var width = Im.ContentRegion.Available with { Y = 0 };
        Im.Item.SetNextWidth(width.X);
        if (ImEx.InputOnDeactivation.Text("##Name"u8, Selected.Name, out string newName))
            _manager.Rename(Selected, newName);

        var identifier = Selected.Identifier.ToString();
        table.DrawFrameColumn("Unique Identifier"u8);
        table.NextColumn();
        var fileName = _saveService.FileNames.DesignFile(Selected);
        using (Im.Font.PushMono())
        {
            if (Im.Button(identifier, width))
                try
                {
                    Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Glamourer.Messager.NotificationMessage(ex, $"Could not open file {fileName}.", $"Could not open file {fileName}",
                        NotificationType.Warning);
                }

            if (Im.Item.RightClicked())
                Im.Clipboard.Set(identifier);
        }

        Im.Tooltip.OnHover(
            $"Open the file\n\t{fileName}\ncontaining this design in the .json-editor of your choice.\n\nRight-Click to copy identifier to clipboard.");

        table.DrawFrameColumn("Full Selector Path"u8);
        table.NextColumn();
        Im.Item.SetNextWidth(width.X);
        if (ImEx.InputOnDeactivation.Text("##Path"u8, Selected.Path.CurrentPath, out string newPath))
            try
            {
                _fileSystem.RenameAndMove(Selected.Node!, newPath);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, ex.Message, "Could not rename or move design", NotificationType.Error);
            }

        table.DrawFrameColumn("Quick Design Bar"u8);
        table.NextColumn();
        if (Im.RadioButton("Display##qdb"u8, Selected.QuickDesign))
            _manager.SetQuickDesign(Selected, true);
        var hovered = Im.Item.Hovered();
        Im.Line.SameInner();
        if (Im.RadioButton("Hide##qdb"u8, !Selected.QuickDesign))
            _manager.SetQuickDesign(Selected, false);
        if (hovered || Im.Item.Hovered())
            Im.Tooltip.Set("Display or hide this design in your quick design bar."u8);

        var forceRedraw = Selected.ForcedRedraw;
        table.DrawFrameColumn("Force Redrawing"u8);
        table.NextColumn();
        if (Im.Checkbox("##ForceRedraw"u8, ref forceRedraw))
            _manager.ChangeForcedRedraw(Selected, forceRedraw);
        Im.Tooltip.OnHover("Set this design to always force a redraw when it is applied through any means."u8);

        var resetAdvancedDyes = Selected.ResetAdvancedDyes;
        table.DrawFrameColumn("Reset Advanced Dyes"u8);
        table.NextColumn();
        if (Im.Checkbox("##ResetAdvancedDyes"u8, ref resetAdvancedDyes))
            _manager.ChangeResetAdvancedDyes(Selected, resetAdvancedDyes);
        Im.Tooltip.OnHover("Set this design to reset any previously applied advanced dyes when it is applied through any means."u8);

        var resetTemporarySettings = Selected.ResetTemporarySettings;
        table.DrawFrameColumn("Reset Temporary Settings"u8);
        table.NextColumn();
        if (Im.Checkbox("##ResetTemporarySettings"u8, ref resetTemporarySettings))
            _manager.ChangeResetTemporarySettings(Selected, resetTemporarySettings);
        Im.Tooltip.OnHover(
            "Set this design to reset any temporary settings previously applied to the associated collection when it is applied through any means."u8);

        table.DrawFrameColumn("Color"u8);
        table.NextColumn();
        if (_colorCombo.Draw("##colorCombo"u8, Selected.Color.Length is 0 ? DesignColors.AutomaticName : Selected.Color,
                "Associate a color with this design.\n"u8
              + "Right-Click to revert to automatic coloring.\n"u8
              + "Hold Control and scroll the mousewheel to scroll."u8,
                width.X - Im.Style.ItemSpacing.X - Im.Style.FrameHeight, out var newColorName))
            _manager.ChangeColor(Selected, newColorName == DesignColors.AutomaticName ? string.Empty : newColorName);

        if (Im.Item.RightClicked())
            _manager.ChangeColor(Selected, string.Empty);

        if (_colors.TryGetValue(Selected.Color, out var currentColor))
        {
            Im.Line.Same();
            if (DesignColorUi.DrawColorButton($"Color associated with {Selected.Color}", currentColor, out var newColor))
                _colors.SetColor(Selected.Color, newColor);
        }
        else if (Selected.Color.Length is not 0)
        {
            Im.Line.Same();
            ImEx.Icon.Draw(LunaStyle.WarningIcon, _colors.MissingColor);
            Im.Tooltip.OnHover("The color associated with this design does not exist."u8);
        }

        table.DrawFrameColumn("Creation Date"u8);
        table.NextColumn();
        ImEx.TextFramed($"{Selected.CreationDate.LocalDateTime:F}", width, 0);

        table.DrawFrameColumn("Last Update Date"u8);
        table.NextColumn();
        ImEx.TextFramed($"{Selected.LastEdit.LocalDateTime:F}", width, 0);

        table.DrawFrameColumn("Tags"u8);
        table.NextColumn();
        DrawTags();
    }

    private void DrawTags()
    {
        var predefinedTagButtonOffset = _predefinedTags.Enabled
            ? Im.Style.FrameHeight + Im.Style.WindowPadding.X + (Im.Scroll.MaximumY > 0 ? Im.Style.ScrollbarSize : 0)
            : 0;
        var idx = TagButtons.Draw(StringU8.Empty, StringU8.Empty, Selected.Tags, out var editedTag, rightEndOffset: predefinedTagButtonOffset);
        if (_predefinedTags.Enabled)
            _predefinedTags.DrawAddFromSharedTagsAndUpdateTags(Selected, true);

        if (idx < 0)
            return;

        if (idx < Selected.Tags.Length)
        {
            if (editedTag.Length is 0)
                _manager.RemoveTag(Selected, idx);
            else
                _manager.RenameTag(Selected, idx, editedTag);
        }
        else
        {
            _manager.AddTag(Selected, editedTag);
        }
    }

    private void DrawDescription()
    {
        var desc = Selected.Description;
        var size = Im.ContentRegion.Available with { Y = 12 * Im.Style.TextHeightWithSpacing };
        if (!_editDescriptionMode)
        {
            using (var textBox = Im.ListBox.Begin("##desc"u8, size))
            {
                if (textBox)
                    Im.TextWrapped(desc);
            }

            if (Im.Button("Edit Description"u8))
                _editDescriptionMode = true;
        }
        else
        {
            if (ImEx.InputOnDeactivation.MultiLine("##desc"u8, desc, out string newDescription, size))
                _manager.ChangeDescription(Selected, newDescription);

            if (Im.Button("Stop Editing"u8))
                _editDescriptionMode = false;
        }
    }
}
