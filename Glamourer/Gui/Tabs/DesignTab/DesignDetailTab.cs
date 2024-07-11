using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignDetailTab
{
    private readonly SaveService              _saveService;
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
        DesignColors colors)
    {
        _saveService = saveService;
        _selector    = selector;
        _manager     = manager;
        _fileSystem  = fileSystem;
        _colors      = colors;
        _colorCombo  = new DesignColorCombo(_colors, false);
    }

    public void Draw()
    {
        using var h = ImRaii.CollapsingHeader("Design Details");
        if (!h)
            return;

        DrawDesignInfoTable();
        DrawDescription();
        ImGui.NewLine();
    }


    private void DrawDesignInfoTable()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        using var table = ImRaii.Table("Details", 2);
        if (!table)
            return;

        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Last Update Datem").X);
        ImGui.TableSetupColumn("Data", ImGuiTableColumnFlags.WidthStretch);

        ImGuiUtil.DrawFrameColumn("Design Name");
        ImGui.TableNextColumn();
        var width = new Vector2(ImGui.GetContentRegionAvail().X, 0);
        var name  = _newName ?? _selector.Selected!.Name;
        ImGui.SetNextItemWidth(width.X);
        if (ImGui.InputText("##Name", ref name, 128))
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
        ImGuiUtil.DrawFrameColumn("Unique Identifier");
        ImGui.TableNextColumn();
        var fileName = _saveService.FileNames.DesignFile(_selector.Selected!);
        using (var mono = ImRaii.PushFont(UiBuilder.MonoFont))
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

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.SetClipboardText(identifier);
        }

        ImGuiUtil.HoverTooltip(
            $"Open the file\n\t{fileName}\ncontaining this design in the .json-editor of your choice.\n\nRight-Click to copy identifier to clipboard.");

        ImGuiUtil.DrawFrameColumn("Full Selector Path");
        ImGui.TableNextColumn();
        var path = _newPath ?? _selector.SelectedLeaf!.FullName();
        ImGui.SetNextItemWidth(width.X);
        if (ImGui.InputText("##Path", ref path, 1024))
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

        ImGuiUtil.DrawFrameColumn("Quick Design Bar");
        ImGui.TableNextColumn();
        if (ImGui.RadioButton("Display##qdb", _selector.Selected.QuickDesign))
            _manager.SetQuickDesign(_selector.Selected!, true);
        var hovered = ImGui.IsItemHovered();
        ImGui.SameLine();
        if (ImGui.RadioButton("Hide##qdb", !_selector.Selected.QuickDesign))
            _manager.SetQuickDesign(_selector.Selected!, false);
        if (hovered || ImGui.IsItemHovered())
            ImGui.SetTooltip("Display or hide this design in your quick design bar.");

        var forceRedraw = _selector.Selected!.ForcedRedraw;
        ImGuiUtil.DrawFrameColumn("Force Redrawing");
        ImGui.TableNextColumn();
        if (ImGui.Checkbox("##ForceRedraw", ref forceRedraw))
            _manager.ChangeForcedRedraw(_selector.Selected!, forceRedraw);
        ImGuiUtil.HoverTooltip("Set this design to always force a redraw when it is applied through any means.");

        ImGuiUtil.DrawFrameColumn("Color");
        var colorName = _selector.Selected!.Color.Length == 0 ? DesignColors.AutomaticName : _selector.Selected!.Color;
        ImGui.TableNextColumn();
        if (_colorCombo.Draw("##colorCombo", colorName, "Associate a color with this design.\n"
              + "Right-Click to revert to automatic coloring.\n"
              + "Hold Control and scroll the mousewheel to scroll.",
                width.X - ImGui.GetStyle().ItemSpacing.X - ImGui.GetFrameHeight(), ImGui.GetTextLineHeight())
         && _colorCombo.CurrentSelection != null)
        {
            colorName = _colorCombo.CurrentSelection is DesignColors.AutomaticName ? string.Empty : _colorCombo.CurrentSelection;
            _manager.ChangeColor(_selector.Selected!, colorName);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            _manager.ChangeColor(_selector.Selected!, string.Empty);

        if (_colors.TryGetValue(_selector.Selected!.Color, out var currentColor))
        {
            ImGui.SameLine();
            if (DesignColorUi.DrawColorButton($"Color associated with {_selector.Selected!.Color}", currentColor, out var newColor))
                _colors.SetColor(_selector.Selected!.Color, newColor);
        }
        else if (_selector.Selected!.Color.Length != 0)
        {
            ImGui.SameLine();
            var       size = new Vector2(ImGui.GetFrameHeight());
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            ImGuiUtil.DrawTextButton(FontAwesomeIcon.ExclamationCircle.ToIconString(), size, 0, _colors.MissingColor);
            ImGuiUtil.HoverTooltip("The color associated with this design does not exist.");
        }

        ImGuiUtil.DrawFrameColumn("Creation Date");
        ImGui.TableNextColumn();
        ImGuiUtil.DrawTextButton(_selector.Selected!.CreationDate.LocalDateTime.ToString("F"), width, 0);

        ImGuiUtil.DrawFrameColumn("Last Update Date");
        ImGui.TableNextColumn();
        ImGuiUtil.DrawTextButton(_selector.Selected!.LastEdit.LocalDateTime.ToString("F"), width, 0);

        ImGuiUtil.DrawFrameColumn("Tags");
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
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeightWithSpacing());
        if (!_editDescriptionMode)
        {
            using (var textBox = ImRaii.ListBox("##desc", size))
            {
                ImGuiUtil.TextWrapped(desc);
            }

            if (ImGui.Button("Edit Description"))
                _editDescriptionMode = true;
        }
        else
        {
            var edit = _newDescription ?? desc;
            if (ImGui.InputTextMultiline("##desc", ref edit, (uint)Math.Max(2000, 4 * edit.Length), size))
                _newDescription = edit;

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _manager.ChangeDescription(_selector.Selected!, edit);
                _newDescription = null;
            }

            if (ImGui.Button("Stop Editing"))
                _editDescriptionMode = false;
        }
    }
}
