using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Services;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;
using TagButtons = OtterGui.Widgets.TagButtons;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignDetailTab
{
    private readonly SaveService              _saveService;
    private readonly Configuration            _config;
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignFileSystem         _fileSystem;
    private readonly DesignManager            _manager;
    private readonly DesignColors             _colors;
    private readonly DesignColorCombo         _colorCombo;
    private readonly TagButtons               _tagButtons = new();

    private string? _newPath;
    private string? _newDescription;
    private string? _newName;

    private bool                   _editDescriptionMode;
    private Design?                _changeDesign;
    private DesignFileSystem.Leaf? _changeLeaf;

    public DesignDetailTab(SaveService saveService, DesignFileSystemSelector selector, DesignManager manager, DesignFileSystem fileSystem,
        DesignColors colors, Configuration config)
    {
        _saveService = saveService;
        _selector    = selector;
        _manager     = manager;
        _fileSystem  = fileSystem;
        _colors      = colors;
        _config      = config;
        _colorCombo  = new DesignColorCombo(_colors, false);
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


    private void DrawDesignInfoTable()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        using var table = Im.Table.Begin("Details"u8, 2);
        if (!table)
            return;

        table.SetupColumn("Type"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Reset Temporary Settings"u8).X);
        table.SetupColumn("Data"u8, TableColumnFlags.WidthStretch);

        ImUtf8.DrawFrameColumn("Design Name"u8);
        ImGui.TableNextColumn();
        var width = new Vector2(Im.ContentRegion.Available.X, 0);
        var name  = _newName ?? _selector.Selected!.Name;
        ImGui.SetNextItemWidth(width.X);
        if (ImUtf8.InputText("##Name"u8, ref name))
        {
            _newName      = name;
            _changeDesign = _selector.Selected;
        }

        if (ImGui.IsItemDeactivatedAfterEdit() && _changeDesign != null)
        {
            _manager.Rename(_changeDesign, name);
            _newName      = null;
            _changeDesign = null;
        }

        var identifier = _selector.Selected!.Identifier.ToString();
        ImUtf8.DrawFrameColumn("Unique Identifier"u8);
        ImGui.TableNextColumn();
        var fileName = _saveService.FileNames.DesignFile(_selector.Selected!);
        using (Im.Font.PushMono())
        {
            if (ImGui.Button(identifier, width))
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
                ImGui.SetClipboardText(identifier);
        }

        ImUtf8.HoverTooltip(
            $"Open the file\n\t{fileName}\ncontaining this design in the .json-editor of your choice.\n\nRight-Click to copy identifier to clipboard.");

        ImUtf8.DrawFrameColumn("Full Selector Path"u8);
        ImGui.TableNextColumn();
        var path = _newPath ?? _selector.SelectedLeaf!.FullName();
        ImGui.SetNextItemWidth(width.X);
        if (ImUtf8.InputText("##Path"u8, ref path))
        {
            _newPath    = path;
            _changeLeaf = _selector.SelectedLeaf!;
        }

        if (ImGui.IsItemDeactivatedAfterEdit() && _changeLeaf != null)
            try
            {
                _fileSystem.RenameAndMove(_changeLeaf, path);
                _newPath    = null;
                _changeLeaf = null;
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, ex.Message, "Could not rename or move design", NotificationType.Error);
            }

        ImUtf8.DrawFrameColumn("Quick Design Bar"u8);
        ImGui.TableNextColumn();
        if (ImUtf8.RadioButton("Display##qdb"u8, _selector.Selected.QuickDesign))
            _manager.SetQuickDesign(_selector.Selected!, true);
        var hovered = ImGui.IsItemHovered();
        Im.Line.Same();
        if (ImUtf8.RadioButton("Hide##qdb"u8, !_selector.Selected.QuickDesign))
            _manager.SetQuickDesign(_selector.Selected!, false);
        if (hovered || ImGui.IsItemHovered())
        {
            using var tt = ImUtf8.Tooltip();
            ImUtf8.Text("Display or hide this design in your quick design bar."u8);
        }

        var forceRedraw = _selector.Selected!.ForcedRedraw;
        ImUtf8.DrawFrameColumn("Force Redrawing"u8);
        ImGui.TableNextColumn();
        if (ImUtf8.Checkbox("##ForceRedraw"u8, ref forceRedraw))
            _manager.ChangeForcedRedraw(_selector.Selected!, forceRedraw);
        ImUtf8.HoverTooltip("Set this design to always force a redraw when it is applied through any means."u8);

        var resetAdvancedDyes = _selector.Selected!.ResetAdvancedDyes;
        ImUtf8.DrawFrameColumn("Reset Advanced Dyes"u8);
        ImGui.TableNextColumn();
        if (ImUtf8.Checkbox("##ResetAdvancedDyes"u8, ref resetAdvancedDyes))
            _manager.ChangeResetAdvancedDyes(_selector.Selected!, resetAdvancedDyes);
        ImUtf8.HoverTooltip("Set this design to reset any previously applied advanced dyes when it is applied through any means."u8);

        var resetTemporarySettings = _selector.Selected!.ResetTemporarySettings;
        ImUtf8.DrawFrameColumn("Reset Temporary Settings"u8);
        ImGui.TableNextColumn();
        if (ImUtf8.Checkbox("##ResetTemporarySettings"u8, ref resetTemporarySettings))
            _manager.ChangeResetTemporarySettings(_selector.Selected!, resetTemporarySettings);
        ImUtf8.HoverTooltip(
            "Set this design to reset any temporary settings previously applied to the associated collection when it is applied through any means."u8);

        ImUtf8.DrawFrameColumn("Color"u8);
        ImGui.TableNextColumn();
        if (_colorCombo.Draw("##colorCombo"u8, _selector.Selected!.Color.Length is 0 ? DesignColors.AutomaticName : _selector.Selected!.Color,
                "Associate a color with this design.\n"u8
              + "Right-Click to revert to automatic coloring.\n"u8
              + "Hold Control and scroll the mousewheel to scroll."u8,
                width.X - Im.Style.ItemSpacing.X - Im.Style.FrameHeight, out var newColorName))
            _manager.ChangeColor(_selector.Selected!, newColorName == DesignColors.AutomaticName ? string.Empty : newColorName);

        if (Im.Item.RightClicked())
            _manager.ChangeColor(_selector.Selected!, string.Empty);

        if (_colors.TryGetValue(_selector.Selected!.Color, out var currentColor))
        {
            Im.Line.Same();
            if (DesignColorUi.DrawColorButton($"Color associated with {_selector.Selected!.Color}", currentColor, out var newColor))
                _colors.SetColor(_selector.Selected!.Color, newColor);
        }
        else if (_selector.Selected!.Color.Length != 0)
        {
            Im.Line.Same();
            ImEx.Icon.Draw(LunaStyle.WarningIcon, _colors.MissingColor);
            Im.Tooltip.OnHover("The color associated with this design does not exist."u8);
        }

        ImUtf8.DrawFrameColumn("Creation Date"u8);
        ImGui.TableNextColumn();
        ImGuiUtil.DrawTextButton(_selector.Selected!.CreationDate.LocalDateTime.ToString("F"), width, 0);

        ImUtf8.DrawFrameColumn("Last Update Date"u8);
        ImGui.TableNextColumn();
        ImGuiUtil.DrawTextButton(_selector.Selected!.LastEdit.LocalDateTime.ToString("F"), width, 0);

        ImUtf8.DrawFrameColumn("Tags"u8);
        ImGui.TableNextColumn();
        DrawTags();
    }

    private void DrawTags()
    {
        var idx = _tagButtons.Draw(string.Empty, string.Empty, _selector.Selected!.Tags, out var editedTag);
        if (idx < 0)
            return;

        if (idx < _selector.Selected!.Tags.Length)
        {
            if (editedTag.Length == 0)
                _manager.RemoveTag(_selector.Selected!, idx);
            else
                _manager.RenameTag(_selector.Selected!, idx, editedTag);
        }
        else
        {
            _manager.AddTag(_selector.Selected!, editedTag);
        }
    }

    private void DrawDescription()
    {
        var desc = _selector.Selected!.Description;
        var size = new Vector2(Im.ContentRegion.Available.X, 12 * Im.Style.TextHeightWithSpacing);
        if (!_editDescriptionMode)
        {
            using (var textBox = ImUtf8.ListBox("##desc"u8, size))
            {
                ImUtf8.TextWrapped(desc);
            }

            if (ImUtf8.Button("Edit Description"u8))
                _editDescriptionMode = true;
        }
        else
        {
            var edit = _newDescription ?? desc;
            if (ImUtf8.InputMultiLine("##desc"u8, ref edit, size))
                _newDescription = edit;

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _manager.ChangeDescription(_selector.Selected!, edit);
                _newDescription = null;
            }

            if (ImUtf8.Button("Stop Editing"u8))
                _editDescriptionMode = false;
        }
    }
}
