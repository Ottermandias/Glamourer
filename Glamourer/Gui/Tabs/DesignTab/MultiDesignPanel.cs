using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace Glamourer.Gui.Tabs.DesignTab;

public class MultiDesignPanel(DesignFileSystemSelector selector, DesignManager editor, DesignColors colors)
{
    private readonly DesignColorCombo _colorCombo = new(colors, true);

    public void Draw()
    {
        if (selector.SelectedPaths.Count == 0)
            return;

        var width = ImGuiHelpers.ScaledVector2(145, 0);
        ImGui.NewLine();
        var treeNodePos = ImGui.GetCursorPos();
        _numDesigns = DrawDesignList();
        DrawCounts(treeNodePos);
        var offset = DrawMultiTagger(width);
        DrawMultiColor(width, offset);
        DrawMultiQuickDesignBar(offset);
        DrawMultiLock(offset);
        DrawMultiResetSettings(offset);
        DrawMultiResetDyes(offset);
        DrawMultiForceRedraw(offset);
    }

    private void DrawCounts(Vector2 treeNodePos)
    {
        var startPos   = ImGui.GetCursorPos();
        var numFolders = selector.SelectedPaths.Count - _numDesigns;
        var text = (_numDesigns, numFolders) switch
        {
            (0, 0)   => string.Empty, // should not happen
            (> 0, 0) => $"{_numDesigns} Designs",
            (0, > 0) => $"{numFolders} Folders",
            _        => $"{_numDesigns} Designs, {numFolders} Folders",
        };
        ImGui.SetCursorPos(treeNodePos);
        ImUtf8.TextRightAligned(text);
        ImGui.SetCursorPos(startPos);
    }

    private void ResetCounts()
    {
        _numQuickDesignEnabled   = 0;
        _numDesignsLocked        = 0;
        _numDesignsForcedRedraw  = 0;
        _numDesignsResetSettings = 0;
        _numDesignsResetDyes     = 0;
    }

    private bool CountLeaves(DesignFileSystem.IPath path)
    {
        if (path is not DesignFileSystem.Leaf l)
            return false;

        if (l.Value.QuickDesign)
            ++_numQuickDesignEnabled;
        if (l.Value.WriteProtected())
            ++_numDesignsLocked;
        if (l.Value.ResetTemporarySettings)
            ++_numDesignsResetSettings;
        if (l.Value.ForcedRedraw)
            ++_numDesignsForcedRedraw;
        if (l.Value.ResetAdvancedDyes)
            ++_numDesignsResetDyes;
        return true;
    }

    private int DrawDesignList()
    {
        ResetCounts();
        using var tree = ImUtf8.TreeNode("Currently Selected Objects"u8, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.NoTreePushOnOpen);
        ImGui.Separator();
        if (!tree)
            return selector.SelectedPaths.Count(CountLeaves);

        var sizeType             = new Vector2(ImGui.GetFrameHeight());
        var availableSizePercent = (ImGui.GetContentRegionAvail().X - sizeType.X - 4 * ImGui.GetStyle().CellPadding.X) / 100;
        var sizeMods             = availableSizePercent * 35;
        var sizeFolders          = availableSizePercent * 65;

        var numDesigns = 0;
        using (var table = ImUtf8.Table("mods"u8, 3, ImGuiTableFlags.RowBg))
        {
            if (!table)
                return selector.SelectedPaths.Count(l => l is DesignFileSystem.Leaf);

            ImUtf8.TableSetupColumn("type"u8, ImGuiTableColumnFlags.WidthFixed, sizeType.X);
            ImUtf8.TableSetupColumn("mod"u8,  ImGuiTableColumnFlags.WidthFixed, sizeMods);
            ImUtf8.TableSetupColumn("path"u8, ImGuiTableColumnFlags.WidthFixed, sizeFolders);

            var i = 0;
            foreach (var (fullName, path) in selector.SelectedPaths.Select(p => (p.FullName(), p))
                         .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase))
            {
                using var id = ImRaii.PushId(i++);
                var (icon, text) = path is DesignFileSystem.Leaf l
                    ? (FontAwesomeIcon.FileCircleMinus, l.Value.Name.Text)
                    : (FontAwesomeIcon.FolderMinus, string.Empty);
                ImGui.TableNextColumn();
                if (ImUtf8.IconButton(icon, "Remove from selection."u8, sizeType))
                    selector.RemovePathFromMultiSelection(path);

                ImUtf8.DrawFrameColumn(text);
                ImUtf8.DrawFrameColumn(fullName);

                if (CountLeaves(path))
                    ++numDesigns;
            }
        }

        ImGui.Separator();
        return numDesigns;
    }

    private          string              _tag = string.Empty;
    private          int                 _numQuickDesignEnabled;
    private          int                 _numDesignsLocked;
    private          int                 _numDesignsForcedRedraw;
    private          int                 _numDesignsResetSettings;
    private          int                 _numDesignsResetDyes;
    private          int                 _numDesigns;
    private readonly List<Design>        _addDesigns    = [];
    private readonly List<(Design, int)> _removeDesigns = [];

    private float DrawMultiTagger(Vector2 width)
    {
        ImUtf8.TextFrameAligned("Multi Tagger:"u8);
        ImGui.SameLine();
        var offset = ImGui.GetItemRectSize().X;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 2 * (width.X + ImGui.GetStyle().ItemSpacing.X));
        ImUtf8.InputText("##tag"u8, ref _tag, "Tag Name..."u8);

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
        if (ImUtf8.ButtonEx(label, tooltip, width, _addDesigns.Count == 0))
            foreach (var design in _addDesigns)
                editor.AddTag(design, _tag);

        label = _removeDesigns.Count > 0
            ? $"Remove from {_removeDesigns.Count} Designs"
            : "Remove";
        tooltip = _removeDesigns.Count == 0
            ? _tag.Length == 0
                ? "No tag specified."
                : $"No selected design contains the tag \"{_tag}\" locally."
            : $"Remove the local tag \"{_tag}\" from {_removeDesigns.Count} designs:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}";
        ImGui.SameLine();
        if (ImUtf8.ButtonEx(label, tooltip, width, _removeDesigns.Count == 0))
            foreach (var (design, index) in _removeDesigns)
                editor.RemoveTag(design, index);
        ImGui.Separator();
        return offset;
    }

    private void DrawMultiQuickDesignBar(float offset)
    {
        ImUtf8.TextFrameAligned("Multi QDB:"u8);
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numQuickDesignEnabled;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs are already displayed in the quick design bar."
            : $"Display all {_numDesigns} selected designs in the quick design bar. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Display Selected Designs in QDB"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, true);

        ImGui.SameLine();
        tt = _numQuickDesignEnabled == 0
            ? $"All {_numDesigns} selected designs are already hidden in the quick design bar."
            : $"Hide all {_numDesigns} selected designs in the quick design bar. Changes {_numQuickDesignEnabled} designs.";
        if (ImUtf8.ButtonEx("Hide Selected Designs in QDB"u8, tt, buttonWidth, _numQuickDesignEnabled == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiLock(float offset)
    {
        ImUtf8.TextFrameAligned("Multi Lock:"u8);
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsLocked;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs are already write protected."
            : $"Write-protect all {_numDesigns} designs. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Turn Write-Protected"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, true);

        ImGui.SameLine();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs are write-protected."
            : $"Remove the write protection of the {_numDesigns} selected designs. Changes {_numDesignsLocked} designs.";
        if (ImUtf8.ButtonEx("Remove Write-Protection"u8, tt, buttonWidth, _numDesignsLocked == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiResetSettings(float offset)
    {
        ImUtf8.TextFrameAligned("Settings:"u8);
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetSettings;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already reset temporary settings."
            : $"Make all {_numDesigns} selected designs reset temporary settings. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Set Reset Settings"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, true);

        ImGui.SameLine();
        tt = _numDesignsResetSettings == 0
            ? $"None of the {_numDesigns} selected designs reset temporary settings."
            : $"Stop all {_numDesigns} selected designs from resetting temporary settings. Changes {_numDesignsResetSettings} designs.";
        if (ImUtf8.ButtonEx("Remove Reset Settings"u8, tt, buttonWidth, _numDesignsResetSettings == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiResetDyes(float offset)
    {
        ImUtf8.TextFrameAligned("Adv. Dyes:"u8);
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetDyes;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already reset advanced dyes."
            : $"Make all {_numDesigns} selected designs reset advanced dyes. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Set Reset Dyes"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, true);

        ImGui.SameLine();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs reset advanced dyes."
            : $"Stop all {_numDesigns} selected designs from resetting advanced dyes. Changes {_numDesignsResetDyes} designs.";
        if (ImUtf8.ButtonEx("Remove Reset Dyes"u8, tt, buttonWidth, _numDesignsResetDyes == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiForceRedraw(float offset)
    {
        ImUtf8.TextFrameAligned("Redrawing:"u8);
        ImGui.SameLine(offset, ImGui.GetStyle().ItemSpacing.X);
        var buttonWidth = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsForcedRedraw;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already force redraws."
            : $"Make all {_numDesigns} designs force redraws. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Force Redraws"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, true);

        ImGui.SameLine();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs force redraws."
            : $"Stop all {_numDesigns} selected designs from forcing redraws. Changes {_numDesignsForcedRedraw} designs.";
        if (ImUtf8.ButtonEx("Remove Forced Redraws"u8, tt, buttonWidth, _numDesignsForcedRedraw == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, false);
        ImGui.Separator();
    }

    private void DrawMultiColor(Vector2 width, float offset)
    {
        ImUtf8.TextFrameAligned("Multi Colors:");
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
        if (ImUtf8.ButtonEx(label, tooltip, width, _addDesigns.Count == 0))
            foreach (var design in _addDesigns)
                editor.ChangeColor(design, _colorCombo.CurrentSelection!);

        label = _removeDesigns.Count > 0
            ? $"Unset {_removeDesigns.Count} Designs"
            : "Unset";
        tooltip = _removeDesigns.Count == 0
            ? "No selected design is set to a non-automatic color."
            : $"Set {_removeDesigns.Count} designs to use automatic color again:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}";
        ImGui.SameLine();
        if (ImUtf8.ButtonEx(label, tooltip, width, _removeDesigns.Count == 0))
            foreach (var (design, _) in _removeDesigns)
                editor.ChangeColor(design, string.Empty);

        ImGui.Separator();
    }

    private void UpdateTagCache()
    {
        _addDesigns.Clear();
        _removeDesigns.Clear();
        if (_tag.Length == 0)
            return;

        foreach (var leaf in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
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
        foreach (var leaf in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
        {
            if (leaf.Value.Color.Length > 0)
                _removeDesigns.Add((leaf.Value, 0));
            if (selection != DesignColors.AutomaticName && leaf.Value.Color != selection)
                _addDesigns.Add(leaf.Value);
        }
    }
}
