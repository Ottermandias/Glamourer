using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignDetailTab
{
    private readonly SaveService              _saveService;
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignFileSystem         _fileSystem;
    private readonly DesignManager            _manager;
    private readonly TagButtons               _tagButtons = new();

    private string? _newPath;
    private string? _newDescription;
    private string? _newName;

    private bool _editDescriptionMode;

    public DesignDetailTab(SaveService saveService, DesignFileSystemSelector selector, DesignManager manager, DesignFileSystem fileSystem)
    {
        _saveService = saveService;
        _selector    = selector;
        _manager     = manager;
        _fileSystem  = fileSystem;
    }

    public void Draw()
    {
        if (!ImGui.CollapsingHeader("Design Details"))
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
            _newName = name;

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _manager.Rename(_selector.Selected!, name);
            _newName = null;
        }

        var identifier = _selector.Selected!.Identifier.ToString();
        ImGuiUtil.DrawFrameColumn("Unique Identifier");
        ImGui.TableNextColumn();
        var fileName   = _saveService.FileNames.DesignFile(_selector.Selected!);
        using (var mono = ImRaii.PushFont(UiBuilder.MonoFont))
        {
            if (ImGui.Button(identifier, width))
                try
                {
                    Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Glamourer.Chat.NotificationMessage(ex, $"Could not open file {fileName}.", $"Could not open file {fileName}", "Failure",
                        NotificationType.Warning);
                }
        }

        ImGuiUtil.HoverTooltip($"Open the file\n\t{fileName}\ncontaining this design in the .json-editor of your choice.");

        ImGuiUtil.DrawFrameColumn("Full Selector Path");
        ImGui.TableNextColumn();
        var path = _newPath ?? _selector.SelectedLeaf!.FullName();
        ImGui.SetNextItemWidth(width.X);
        if (ImGui.InputText("##Path", ref path, 1024))
            _newPath = path;

        if (ImGui.IsItemDeactivatedAfterEdit())
            try
            {
                _fileSystem.RenameAndMove(_selector.SelectedLeaf!, path);
                _newPath = null;
            }
            catch (Exception ex)
            {
                Glamourer.Chat.NotificationMessage(ex, ex.Message, "Could not rename or move design", "Error", NotificationType.Error);
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
