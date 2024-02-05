using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.DesignTab;

public class MultiDesignPanel(DesignFileSystemSelector _selector, DesignManager _editor, DesignColors _colors)
{
    private readonly DesignColorCombo _colorCombo = new(_colors, true);

    public void Draw()
    {
        if (_selector.SelectedPaths.Count == 0)
            return;

        var width = ImGuiHelpers.ScaledVector2(145, 0);
        ImGui.NewLine();
        DrawDesignList();
        var offset = DrawMultiTagger(width);
        DrawMultiColor(width, offset);
        DrawMultiQuickDesignBar(offset);
    }

    private void DrawDesignList()
    {
        using var tree = ImRaii.TreeNode("Currently Selected Objects", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.NoTreePushOnOpen);
        ImGui.Separator();
        if (!tree)
            return;

        var sizeType             = ImGui.GetFrameHeight();
        var availableSizePercent = (ImGui.GetContentRegionAvail().X - sizeType - 4 * ImGui.GetStyle().CellPadding.X) / 100;
        var sizeMods             = availableSizePercent * 35;
        var sizeFolders          = availableSizePercent * 65;

        _numQuickDesignEnabled = 0;
        _numDesigns = 0;
        using (var table = ImRaii.Table("mods", 3, ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;

            ImGui.TableSetupColumn("type", ImGuiTableColumnFlags.WidthFixed, sizeType);
            ImGui.TableSetupColumn("mod",  ImGuiTableColumnFlags.WidthFixed, sizeMods);
            ImGui.TableSetupColumn("path", ImGuiTableColumnFlags.WidthFixed, sizeFolders);

            var i = 0;
            foreach (var (fullName, path) in _selector.SelectedPaths.Select(p => (p.FullName(), p))
                         .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase))
            {
                using var id = ImRaii.PushId(i++);
                ImGui.TableNextColumn();
                var icon = (path is DesignFileSystem.Leaf ? FontAwesomeIcon.FileCircleMinus : FontAwesomeIcon.FolderMinus).ToIconString();
                if (ImGuiUtil.DrawDisabledButton(icon, new Vector2(sizeType), "Remove from selection.", false, true))
                    _selector.RemovePathFromMultiSelection(path);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(path is DesignFileSystem.Leaf l ? l.Value.Name : string.Empty);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(fullName);

                if (path is not DesignFileSystem.Leaf l2)
                    continue;

                ++_numDesigns;
                if (l2.Value.QuickDesign)
                    ++_numQuickDesignEnabled;
            }
        }

        ImGui.Separator();
    }

    private          string              _tag = string.Empty;
    private          int                 _numQuickDesignEnabled;
    private          int                 _numDesigns;
    private readonly List<Design>        _addDesigns    = [];
    private readonly List<(Design, int)> _removeDesigns = [];

    private float DrawMultiTagger(Vector2 width)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Multi Tagger:");
        ImGui.SameLine();
        var offset = ImGui.GetItemRectSize().X;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 2 * (width.X + ImGui.GetStyle().ItemSpacing.X));
        ImGui.InputTextWithHint("##tag", "Tag Name...", ref _tag, 128);

        UpdateTagCache();
        var label = _addDesigns.Count > 0
            ? $"Add to {_addDesigns.Count} Designs"
            : "Add";
        var tooltip = _addDesigns.Count == 0
            ? _tag.Length == 0
                ? "No tag specified."
                : $"All designs selected already contain the tag \"{_tag}\"."
            : $"Add the tag \"{_tag}\" to {_addDesigns.Count} designs as a local tag:\n\n\t{string.Join("\n\t", _addDesigns.Select(m => m.Name.Text))}";
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(label, width, tooltip, _addDesigns.Count == 0))
            foreach (var design in _addDesigns)
                _editor.AddTag(design, _tag);

        label = _removeDesigns.Count > 0
            ? $"Remove from {_removeDesigns.Count} Designs"
            : "Remove";
        tooltip = _removeDesigns.Count == 0
            ? _tag.Length == 0
                ? "No tag specified."
                : $"No selected design contains the tag \"{_tag}\" locally."
            : $"Remove the local tag \"{_tag}\" from {_removeDesigns.Count} designs:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}";
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(label, width, tooltip, _removeDesigns.Count == 0))
            foreach (var (design, index) in _removeDesigns)
                _editor.RemoveTag(design, index);
        ImGui.Separator();
        return offset;
    }

    private void DrawMultiQuickDesignBar(float offset)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Multi QDB:");
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numQuickDesignEnabled;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs are already displayed in the quick design bar."
            : $"Display all {_numDesigns} selected designs in the quick design bar. Changes {diff} designs.";
        if (ImGuiUtil.DrawDisabledButton("Display Selected Designs in QDB", buttonWidth, tt, diff == 0))
            foreach(var design in _selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                _editor.SetQuickDesign(design.Value, true);

        ImGui.SameLine();
        tt = _numQuickDesignEnabled == 0
            ? $"All {_numDesigns} selected designs are already hidden in the quick design bar."
            : $"Hide all {_numDesigns} selected designs in the quick design bar. Changes {_numQuickDesignEnabled} designs.";
        if (ImGuiUtil.DrawDisabledButton("Hide Selected Designs in QDB", buttonWidth, tt, _numQuickDesignEnabled == 0))
            foreach (var design in _selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                _editor.SetQuickDesign(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiColor(Vector2 width, float offset)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Multi Colors:");
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        _colorCombo.Draw("##color", _colorCombo.CurrentSelection ?? string.Empty, "Select a design color.",
            ImGui.GetContentRegionAvail().X - 2 * (width.X + ImGui.GetStyle().ItemSpacing.X), ImGui.GetTextLineHeight());

        UpdateColorCache();
        var label = _addDesigns.Count > 0
            ? $"Set for {_addDesigns.Count} Designs"
            : "Set";
        var tooltip = _addDesigns.Count == 0
            ? _colorCombo.CurrentSelection switch
            {
                null                       => "No color specified.",
                DesignColors.AutomaticName => "Use the other button to set to automatic.",
                _                          => $"All designs selected are already set to the color \"{_colorCombo.CurrentSelection}\".",
            }
            : $"Set the color of {_addDesigns.Count} designs to \"{_colorCombo.CurrentSelection}\"\n\n\t{string.Join("\n\t", _addDesigns.Select(m => m.Name.Text))}";
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(label, width, tooltip, _addDesigns.Count == 0))
            foreach (var design in _addDesigns)
                _editor.ChangeColor(design, _colorCombo.CurrentSelection!);

        label = _removeDesigns.Count > 0
            ? $"Unset {_removeDesigns.Count} Designs"
            : "Unset";
        tooltip = _removeDesigns.Count == 0
            ? "No selected design is set to a non-automatic color."
            : $"Set {_removeDesigns.Count} designs to use automatic color again:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}";
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton(label, width, tooltip, _removeDesigns.Count == 0))
            foreach (var (design, _) in _removeDesigns)
                _editor.ChangeColor(design, string.Empty);

        ImGui.Separator();
    }

    private void UpdateTagCache()
    {
        _addDesigns.Clear();
        _removeDesigns.Clear();
        if (_tag.Length == 0)
            return;

        foreach (var leaf in _selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
        {
            var index = leaf.Value.Tags.IndexOf(_tag);
            if (index >= 0)
                _removeDesigns.Add((leaf.Value, index));
            else
                _addDesigns.Add(leaf.Value);
        }
    }

    private void UpdateColorCache()
    {
        _addDesigns.Clear();
        _removeDesigns.Clear();
        var selection = _colorCombo.CurrentSelection ?? DesignColors.AutomaticName;
        foreach (var leaf in _selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
        {
            if (leaf.Value.Color.Length > 0)
                _removeDesigns.Add((leaf.Value, 0));
            if (selection != DesignColors.AutomaticName && leaf.Value.Color != selection)
                _addDesigns.Add(leaf.Value);
        }
    }
}
