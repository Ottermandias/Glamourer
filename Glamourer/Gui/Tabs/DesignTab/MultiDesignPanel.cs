using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Designs;
using Glamourer.Interop.Material;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;
using static Glamourer.Gui.Tabs.HeaderDrawer;

namespace Glamourer.Gui.Tabs.DesignTab;

public class MultiDesignPanel(
    DesignFileSystemSelector selector,
    DesignManager editor,
    DesignColors colors,
    Configuration config)
{
    private readonly Button[] _leftButtons  = [];
    private readonly Button[] _rightButtons = []; //[new IncognitoButton(config)];

    private readonly DesignColorCombo _colorCombo = new(colors, true);

    public void Draw()
    {
        if (selector.SelectedPaths.Count == 0)
            return;

        HeaderDrawer.Draw(string.Empty, 0, ImGuiColor.FrameBackground.Get().Color, _leftButtons, _rightButtons);
        using var child = ImUtf8.Child("##MultiPanel"u8, default, true);
        if (!child)
            return;

        var width       = ImGuiHelpers.ScaledVector2(145, 0);
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
        DrawAdvancedButtons(offset);
        DrawApplicationButtons(offset);
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
        _numQuickDesignEnabled      = 0;
        _numDesignsLocked           = 0;
        _numDesignsForcedRedraw     = 0;
        _numDesignsResetSettings    = 0;
        _numDesignsResetDyes        = 0;
        _numDesignsWithAdvancedDyes = 0;
        _numAdvancedDyes            = 0;
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
        if (l.Value.Materials.Count > 0)
        {
            ++_numDesignsWithAdvancedDyes;
            _numAdvancedDyes += l.Value.Materials.Count;
        }

        return true;
    }

    private int DrawDesignList()
    {
        ResetCounts();
        using var tree = Im.Tree.Node("Currently Selected Objects"u8, TreeNodeFlags.DefaultOpen | TreeNodeFlags.NoTreePushOnOpen);
        Im.Separator();
        if (!tree)
            return selector.SelectedPaths.Count(CountLeaves);

        var sizeType             = new Vector2(Im.Style.FrameHeight);
        var availableSizePercent = (Im.ContentRegion.Available.X - sizeType.X - 4 * Im.Style.CellPadding.X) / 100;
        var sizeMods             = availableSizePercent * 35;
        var sizeFolders          = availableSizePercent * 65;

        var numDesigns = 0;
        using (var table = Im.Table.Begin("mods"u8, 3, TableFlags.RowBackground))
        {
            if (!table)
                return selector.SelectedPaths.Count(l => l is DesignFileSystem.Leaf);

            table.SetupColumn("type"u8, TableColumnFlags.WidthFixed, sizeType.X);
            table.SetupColumn("mod"u8,  TableColumnFlags.WidthFixed, sizeMods);
            table.SetupColumn("path"u8, TableColumnFlags.WidthFixed, sizeFolders);

            var i = 0;
            foreach (var (fullName, path) in selector.SelectedPaths.Select(p => (p.FullName(), p))
                         .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase))
            {
                using var id = Im.Id.Push(i++);
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

        Im.Separator();
        return numDesigns;
    }

    private          string              _tag = string.Empty;
    private          int                 _numQuickDesignEnabled;
    private          int                 _numDesignsLocked;
    private          int                 _numDesignsForcedRedraw;
    private          int                 _numDesignsResetSettings;
    private          int                 _numDesignsResetDyes;
    private          int                 _numAdvancedDyes;
    private          int                 _numDesignsWithAdvancedDyes;
    private          int                 _numDesigns;
    private readonly List<Design>        _addDesigns    = [];
    private readonly List<(Design, int)> _removeDesigns = [];

    private float DrawMultiTagger(Vector2 width)
    {
        ImUtf8.TextFrameAligned("Multi Tagger:"u8);
        Im.Line.Same();
        var offset = ImGui.GetItemRectSize().X + Im.Style.WindowPadding.X;
        ImGui.SetNextItemWidth(Im.ContentRegion.Available.X - 2 * (width.X + Im.Style.ItemSpacing.X));
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
        Im.Line.Same();
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
        Im.Line.Same();
        if (ImUtf8.ButtonEx(label, tooltip, width, _removeDesigns.Count == 0))
            foreach (var (design, index) in _removeDesigns)
                editor.RemoveTag(design, index);
        Im.Separator();
        return offset;
    }

    private void DrawMultiQuickDesignBar(float offset)
    {
        ImUtf8.TextFrameAligned("Multi QDB:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numQuickDesignEnabled;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs are already displayed in the quick design bar."
            : $"Display all {_numDesigns} selected designs in the quick design bar. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Display Selected Designs in QDB"u8, tt, buttonWidth, diff == 0))
        {
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, true);
        }

        Im.Line.Same();
        tt = _numQuickDesignEnabled == 0
            ? $"All {_numDesigns} selected designs are already hidden in the quick design bar."
            : $"Hide all {_numDesigns} selected designs in the quick design bar. Changes {_numQuickDesignEnabled} designs.";
        if (ImUtf8.ButtonEx("Hide Selected Designs in QDB"u8, tt, buttonWidth, _numQuickDesignEnabled == 0))
        {
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, false);
        }

        Im.Separator();
    }

    private void DrawMultiLock(float offset)
    {
        ImUtf8.TextFrameAligned("Multi Lock:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsLocked;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs are already write protected."
            : $"Write-protect all {_numDesigns} designs. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Turn Write-Protected"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, true);

        Im.Line.Same();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs are write-protected."
            : $"Remove the write protection of the {_numDesigns} selected designs. Changes {_numDesignsLocked} designs.";
        if (ImUtf8.ButtonEx("Remove Write-Protection"u8, tt, buttonWidth, _numDesignsLocked == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiResetSettings(float offset)
    {
        ImUtf8.TextFrameAligned("Settings:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetSettings;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already reset temporary settings."
            : $"Make all {_numDesigns} selected designs reset temporary settings. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Set Reset Temp. Settings"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, true);

        Im.Line.Same();
        tt = _numDesignsResetSettings == 0
            ? $"None of the {_numDesigns} selected designs reset temporary settings."
            : $"Stop all {_numDesigns} selected designs from resetting temporary settings. Changes {_numDesignsResetSettings} designs.";
        if (ImUtf8.ButtonEx("Remove Reset Temp. Settings"u8, tt, buttonWidth, _numDesignsResetSettings == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiResetDyes(float offset)
    {
        ImUtf8.TextFrameAligned("Adv. Dyes:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetDyes;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already reset advanced dyes."
            : $"Make all {_numDesigns} selected designs reset advanced dyes. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Set Reset Dyes"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, true);

        Im.Line.Same();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs reset advanced dyes."
            : $"Stop all {_numDesigns} selected designs from resetting advanced dyes. Changes {_numDesignsResetDyes} designs.";
        if (ImUtf8.ButtonEx("Remove Reset Dyes"u8, tt, buttonWidth, _numDesignsResetDyes == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiForceRedraw(float offset)
    {
        ImUtf8.TextFrameAligned("Redrawing:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsForcedRedraw;
        var tt = diff == 0
            ? $"All {_numDesigns} selected designs already force redraws."
            : $"Make all {_numDesigns} designs force redraws. Changes {diff} designs.";
        if (ImUtf8.ButtonEx("Force Redraws"u8, tt, buttonWidth, diff == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, true);

        Im.Line.Same();
        tt = _numDesignsLocked == 0
            ? $"None of the {_numDesigns} selected designs force redraws."
            : $"Stop all {_numDesigns} selected designs from forcing redraws. Changes {_numDesignsForcedRedraw} designs.";
        if (ImUtf8.ButtonEx("Remove Forced Redraws"u8, tt, buttonWidth, _numDesignsForcedRedraw == 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, false);
        Im.Separator();
    }

    private string _colorComboSelection = string.Empty;

    private void DrawMultiColor(Vector2 width, float offset)
    {
        ImUtf8.TextFrameAligned("Multi Colors:"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        if (_colorCombo.Draw("##color"u8, _colorComboSelection, "Select a design color."u8,
                Im.ContentRegion.Available.X - 2 * (width.X + Im.Style.ItemSpacing.X), out var newSelection))
            _colorComboSelection = newSelection;

        UpdateColorCache();
        var label = _addDesigns.Count > 0
            ? $"Set for {_addDesigns.Count} Designs"
            : "Set";
        var tooltip = _addDesigns.Count is 0
            ? _colorComboSelection switch
            {
                null                       => "No color specified.",
                DesignColors.AutomaticName => "Use the other button to set to automatic.",
                _                          => $"All designs selected are already set to the color \"{_colorComboSelection}\".",
            }
            : $"Set the color of {_addDesigns.Count} designs to \"{_colorComboSelection}\"\n\n\t{string.Join("\n\t", _addDesigns.Select(m => m.Name.Text))}";
        Im.Line.Same();
        if (ImEx.Button(label, width, tooltip, _addDesigns.Count is 0))
        {
            foreach (var design in _addDesigns)
                editor.ChangeColor(design, _colorComboSelection!);
        }

        label = _removeDesigns.Count > 0
            ? $"Unset {_removeDesigns.Count} Designs"
            : "Unset";
        tooltip = _removeDesigns.Count == 0
            ? "No selected design is set to a non-automatic color."
            : $"Set {_removeDesigns.Count} designs to use automatic color again:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}";
        Im.Line.Same();
        if (ImEx.Button(label, width, tooltip, _removeDesigns.Count is 0))
        {
            foreach (var (design, _) in _removeDesigns)
                editor.ChangeColor(design, string.Empty);
        }

        Im.Separator();
    }

    private void DrawAdvancedButtons(float offset)
    {
        ImUtf8.TextFrameAligned("Delete Adv."u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var enabled = config.DeleteDesignModifier.IsActive();
        var tt = _numDesignsWithAdvancedDyes is 0
            ? "No selected designs contain any advanced dyes."
            : $"Delete {_numAdvancedDyes} advanced dyes from {_numDesignsWithAdvancedDyes} of the selected designs.";
        if (ImUtf8.ButtonEx("Delete All Advanced Dyes"u8, tt, new Vector2(Im.ContentRegion.Available.X, 0),
                !enabled || _numDesignsWithAdvancedDyes is 0))

            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
            {
                while (design.Value.Materials.Count > 0)
                    editor.ChangeMaterialValue(design.Value, MaterialValueIndex.FromKey(design.Value.Materials[0].Item1), null);
            }

        if (!enabled && _numDesignsWithAdvancedDyes is not 0)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking to delete.");
        Im.Separator();
    }

    private void DrawApplicationButtons(float offset)
    {
        ImUtf8.TextFrameAligned("Application"u8);
        ImGui.SameLine(offset, Im.Style.ItemSpacing.X);
        var   width     = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var   enabled   = config.DeleteDesignModifier.IsActive();
        bool? equip     = null;
        bool? customize = null;
        var   group     = ImUtf8.Group();
        if (ImUtf8.ButtonEx("Disable Everything"u8,
                _numDesigns > 0
                    ? $"Disable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness for all {_numDesigns} designs."
                    : "No designs selected.", width, !enabled))
        {
            equip     = false;
            customize = false;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Enable Everything"u8,
                _numDesigns > 0
                    ? $"Enable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness for all {_numDesigns} designs."
                    : "No designs selected.", width, !enabled))
        {
            equip     = true;
            customize = true;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        if (ImUtf8.ButtonEx("Equipment Only"u8,
                _numDesigns > 0
                    ? $"Enable application of anything related to gear, disable anything that is not related to gear for all {_numDesigns} designs."
                    : "No designs selected.", width, !enabled))
        {
            equip     = true;
            customize = false;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Customization Only"u8,
                _numDesigns > 0
                    ? $"Enable application of anything related to customization, disable anything that is not related to customization for all {_numDesigns} designs."
                    : "No designs selected.", width, !enabled))
        {
            equip     = false;
            customize = true;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        if (ImUtf8.ButtonEx("Default Application"u8,
                _numDesigns > 0
                    ? $"Set the application rules to the default values as if the {_numDesigns} were newly created,without any advanced features or wetness."
                    : "No designs selected.", width, !enabled))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>().Select(l => l.Value))
            {
                editor.ChangeApplyMulti(design, true, true, true, false, true, true, false, true);
                editor.ChangeApplyMeta(design, MetaIndex.Wetness, false);
            }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Disable Advanced"u8, _numDesigns > 0
                ? $"Disable all advanced dyes and customizations but keep everything else as is for all {_numDesigns} designs."
                : "No designs selected.", width, !enabled))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>().Select(l => l.Value))
                editor.ChangeApplyMulti(design, null, null, null, false, null, null, false, null);

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

        group.Dispose();
        Im.Separator();
        if (equip is null && customize is null)
            return;

        foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>().Select(l => l.Value))
        {
            editor.ChangeApplyMulti(design, equip, customize, equip, customize.HasValue && !customize.Value ? false : null, null, equip, equip,
                equip);
            if (equip.HasValue)
            {
                editor.ChangeApplyMeta(design, MetaIndex.HatState,    equip.Value);
                editor.ChangeApplyMeta(design, MetaIndex.VisorState,  equip.Value);
                editor.ChangeApplyMeta(design, MetaIndex.WeaponState, equip.Value);
            }

            if (customize.HasValue)
                editor.ChangeApplyMeta(design, MetaIndex.Wetness, customize.Value);
        }
    }

    private void UpdateTagCache()
    {
        _addDesigns.Clear();
        _removeDesigns.Clear();
        if (_tag.Length == 0)
            return;

        foreach (var leaf in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
        {
            var index = leaf.Value.Tags.AsEnumerable().IndexOf(_tag);
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
        var selection = string.IsNullOrEmpty(_colorComboSelection) ? DesignColors.AutomaticName : _colorComboSelection;
        foreach (var leaf in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
        {
            if (leaf.Value.Color.Length > 0)
                _removeDesigns.Add((leaf.Value, 0));
            if (selection != DesignColors.AutomaticName && leaf.Value.Color != selection)
                _addDesigns.Add(leaf.Value);
        }
    }
}
