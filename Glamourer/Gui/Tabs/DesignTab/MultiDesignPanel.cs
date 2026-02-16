using Glamourer.Designs;
using Glamourer.Interop.Material;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public class MultiDesignPanel(
    DesignFileSystemSelector selector,
    DesignManager editor,
    DesignColors colors,
    Configuration config)
{
    private readonly DesignColorCombo _colorCombo = new(colors, true);

    public void Draw()
    {
        if (selector.SelectedPaths.Count == 0)
            return;

        var width       = ImEx.ScaledVectorX(145);
        var treeNodePos = Im.Cursor.Position;
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
        var startPos   = Im.Cursor.Position;
        var numFolders = selector.SelectedPaths.Count - _numDesigns;
        Im.Cursor.Position = treeNodePos;
        ImEx.TextRightAligned((_numDesigns, numFolders) switch
        {
            (0, 0)    => StringU8.Empty, // should not happen
            ( > 0, 0) => $"{_numDesigns} Designs",
            (0, > 0)  => $"{numFolders} Folders",
            _         => $"{_numDesigns} Designs, {numFolders} Folders",
        });
        Im.Cursor.Position = startPos;
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
                    ? (LunaStyle.RemoveFileIcon, l.Value.Name.Text)
                    : (LunaStyle.RemoveFolderIcon, string.Empty);
                table.NextColumn();
                if (ImEx.Icon.Button(icon, "Remove from selection."u8, sizeType))
                    selector.RemovePathFromMultiSelection(path);

                table.DrawFrameColumn(text);
                table.DrawFrameColumn(fullName);

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
        ImEx.TextFrameAligned("Multi Tagger:"u8);
        Im.Line.Same();
        var offset = Im.Item.Size.X + Im.Style.WindowPadding.X;
        Im.Item.SetNextWidth(Im.ContentRegion.Available.X - 2 * (width.X + Im.Style.ItemSpacing.X));
        Im.Input.Text("##tag"u8, ref _tag, "Tag Name..."u8);

        UpdateTagCache();
        Im.Line.Same();
        if (ImEx.Button(_addDesigns.Count > 0
                ? $"Add to {_addDesigns.Count} Designs"
                : "Add"u8, width, _addDesigns.Count is 0
                ? _tag.Length is 0
                    ? "No tag specified."u8
                    : $"All designs selected already contain the tag \"{_tag}\"."
                : $"Add the tag \"{_tag}\" to {_addDesigns.Count} designs as a local tag:\n\n\t{StringU8.Join("\n\t"u8, _addDesigns.Select(m => m.Name.Text))}", _addDesigns.Count is 0))
            foreach (var design in _addDesigns)
                editor.AddTag(design, _tag);

        Im.Line.Same();
        if (ImEx.Button(_removeDesigns.Count > 0
                ? $"Remove from {_removeDesigns.Count} Designs"
                : "Remove", width, _removeDesigns.Count is 0
                ? _tag.Length is 0
                    ? "No tag specified."u8
                    : $"No selected design contains the tag \"{_tag}\" locally."
                : $"Remove the local tag \"{_tag}\" from {_removeDesigns.Count} designs:\n\n\t{string.Join("\n\t", _removeDesigns.Select(m => m.Item1.Name.Text))}", _removeDesigns.Count is 0))
            foreach (var (design, index) in _removeDesigns)
                editor.RemoveTag(design, index);
        Im.Separator();
        return offset;
    }

    private void DrawMultiQuickDesignBar(float offset)
    {
        ImEx.TextFrameAligned("Multi QDB:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numQuickDesignEnabled;
        if (ImEx.Button("Display Selected Designs in QDB"u8, buttonWidth, diff is 0
                ? $"All {_numDesigns} selected designs are already displayed in the quick design bar."
                : $"Display all {_numDesigns} selected designs in the quick design bar. Changes {diff} designs.", diff is 0))
        {
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, true);
        }

        Im.Line.Same();
        if (ImEx.Button("Hide Selected Designs in QDB"u8, buttonWidth, _numQuickDesignEnabled is 0
                ? $"All {_numDesigns} selected designs are already hidden in the quick design bar."
                : $"Hide all {_numDesigns} selected designs in the quick design bar. Changes {_numQuickDesignEnabled} designs.", _numQuickDesignEnabled is 0))
        {
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetQuickDesign(design.Value, false);
        }

        Im.Separator();
    }

    private void DrawMultiLock(float offset)
    {
        ImEx.TextFrameAligned("Multi Lock:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsLocked;
        if (ImEx.Button("Turn Write-Protected"u8, buttonWidth, diff is 0
                ? $"All {_numDesigns} selected designs are already write protected."
                : $"Write-protect all {_numDesigns} designs. Changes {diff} designs.", diff is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, true);

        Im.Line.Same();
        if (ImEx.Button("Remove Write-Protection"u8, buttonWidth, _numDesignsLocked is 0
                ? $"None of the {_numDesigns} selected designs are write-protected."
                : $"Remove the write protection of the {_numDesigns} selected designs. Changes {_numDesignsLocked} designs.", _numDesignsLocked is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.SetWriteProtection(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiResetSettings(float offset)
    {
        ImEx.TextFrameAligned("Settings:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetSettings;
        if (ImEx.Button("Set Reset Temp. Settings"u8, buttonWidth, diff is 0
                ? $"All {_numDesigns} selected designs already reset temporary settings."
                : $"Make all {_numDesigns} selected designs reset temporary settings. Changes {diff} designs.", diff is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, true);

        Im.Line.Same();
        if (ImEx.Button("Remove Reset Temp. Settings"u8, buttonWidth, _numDesignsResetSettings is 0
                ? $"None of the {_numDesigns} selected designs reset temporary settings."
                : $"Stop all {_numDesigns} selected designs from resetting temporary settings. Changes {_numDesignsResetSettings} designs.", _numDesignsResetSettings is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetTemporarySettings(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiResetDyes(float offset)
    {
        ImEx.TextFrameAligned("Adv. Dyes:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsResetDyes;
        if (ImEx.Button("Set Reset Dyes"u8, buttonWidth, diff is 0
                ? $"All {_numDesigns} selected designs already reset advanced dyes."
                : $"Make all {_numDesigns} selected designs reset advanced dyes. Changes {diff} designs.", diff is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, true);

        Im.Line.Same();
        if (ImEx.Button("Remove Reset Dyes"u8, buttonWidth, _numDesignsLocked is 0
                ? $"None of the {_numDesigns} selected designs reset advanced dyes."
                : $"Stop all {_numDesigns} selected designs from resetting advanced dyes. Changes {_numDesignsResetDyes} designs.", _numDesignsResetDyes is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeResetAdvancedDyes(design.Value, false);
        Im.Separator();
    }

    private void DrawMultiForceRedraw(float offset)
    {
        ImEx.TextFrameAligned("Redrawing:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var buttonWidth = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var diff        = _numDesigns - _numDesignsForcedRedraw;
        if (ImEx.Button("Force Redraws"u8, buttonWidth, diff is 0
                ? $"All {_numDesigns} selected designs already force redraws."
                : $"Make all {_numDesigns} designs force redraws. Changes {diff} designs.", diff is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, true);

        Im.Line.Same();
        if (ImEx.Button("Remove Forced Redraws"u8, buttonWidth, _numDesignsLocked is 0
                ? $"None of the {_numDesigns} selected designs force redraws."
                : $"Stop all {_numDesigns} selected designs from forcing redraws. Changes {_numDesignsForcedRedraw} designs.", _numDesignsForcedRedraw is 0))
            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
                editor.ChangeForcedRedraw(design.Value, false);
        Im.Separator();
    }

    private string _colorComboSelection = string.Empty;

    private void DrawMultiColor(Vector2 width, float offset)
    {
        ImEx.TextFrameAligned("Multi Colors:"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        if (_colorCombo.Draw("##color"u8, _colorComboSelection, "Select a design color."u8,
                Im.ContentRegion.Available.X - 2 * (width.X + Im.Style.ItemSpacing.X), out var newSelection))
            _colorComboSelection = newSelection;

        UpdateColorCache();
        Im.Line.Same();
        if (ImEx.Button(_addDesigns.Count > 0
                ? $"Set for {_addDesigns.Count} Designs"
                : "Set"u8, width, _addDesigns.Count is 0
                ? _colorComboSelection switch
                {
                    null                       => "No color specified."u8,
                    DesignColors.AutomaticName => "Use the other button to set to automatic."u8,
                    _                          => $"All designs selected are already set to the color \"{_colorComboSelection}\".",
                }
                : $"Set the color of {_addDesigns.Count} designs to \"{_colorComboSelection}\"\n\n\t{StringU8.Join("\n\t"u8, _addDesigns.Select(m => m.Name.Text))}", _addDesigns.Count is 0))
        {
            foreach (var design in _addDesigns)
                editor.ChangeColor(design, _colorComboSelection!);
        }

        Im.Line.Same();
        if (ImEx.Button(_removeDesigns.Count > 0
                ? $"Unset {_removeDesigns.Count} Designs"
                : "Unset"u8, width, _removeDesigns.Count is 0
                ? "No selected design is set to a non-automatic color."u8
                : $"Set {_removeDesigns.Count} designs to use automatic color again:\n\n\t{StringU8.Join("\n\t"u8, _removeDesigns.Select(m => m.Item1.Name.Text))}", _removeDesigns.Count is 0))
        {
            foreach (var (design, _) in _removeDesigns)
                editor.ChangeColor(design, string.Empty);
        }

        Im.Separator();
    }

    private void DrawAdvancedButtons(float offset)
    {
        ImEx.TextFrameAligned("Delete Adv."u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var enabled = config.DeleteDesignModifier.IsActive();
        if (ImEx.Button("Delete All Advanced Dyes"u8, Im.ContentRegion.Available with { Y = 0 }, _numDesignsWithAdvancedDyes is 0
                ? "No selected designs contain any advanced dyes."u8
                : $"Delete {_numAdvancedDyes} advanced dyes from {_numDesignsWithAdvancedDyes} of the selected designs.", 
                !enabled || _numDesignsWithAdvancedDyes is 0))

            foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>())
            {
                while (design.Value.Materials.Count > 0)
                    editor.ChangeMaterialValue(design.Value, MaterialValueIndex.FromKey(design.Value.Materials[0].Item1), null);
            }

        if (!enabled && _numDesignsWithAdvancedDyes is not 0)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking to delete.");
        Im.Separator();
    }

    private void DrawApplicationButtons(float offset)
    {
        ImEx.TextFrameAligned("Application"u8);
        Im.Line.Same(offset, Im.Style.ItemSpacing.X);
        var   width     = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemSpacing.X) / 2, 0);
        var   enabled   = config.DeleteDesignModifier.IsActive();
        bool? equip     = null;
        bool? customize = null;
        using (Im.Group())
        {
            if (ImEx.Button("Disable Everything"u8, width,
                    _numDesigns > 0
                        ? $"Disable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness for all {_numDesigns} designs."
                        : "No designs selected."u8, !enabled))
            {
                equip     = false;
                customize = false;
            }

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

            Im.Line.Same();
            if (ImEx.Button("Enable Everything"u8, width,
                    _numDesigns > 0
                        ? $"Enable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness for all {_numDesigns} designs."
                        : "No designs selected."u8, !enabled))
            {
                equip     = true;
                customize = true;
            }

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

            if (ImEx.Button("Equipment Only"u8, width,
                    _numDesigns > 0
                        ? $"Enable application of anything related to gear, disable anything that is not related to gear for all {_numDesigns} designs."
                        : "No designs selected."u8, !enabled))
            {
                equip     = true;
                customize = false;
            }

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

            Im.Line.Same();
            if (ImEx.Button("Customization Only"u8, width,
                    _numDesigns > 0
                        ? $"Enable application of anything related to customization, disable anything that is not related to customization for all {_numDesigns} designs."
                        : "No designs selected."u8, !enabled))
            {
                equip     = false;
                customize = true;
            }

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

            if (ImEx.Button("Default Application"u8, width,
                    _numDesigns > 0
                        ? $"Set the application rules to the default values as if the {_numDesigns} were newly created,without any advanced features or wetness."
                        : "No designs selected."u8, !enabled))
                foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>().Select(l => l.Value))
                {
                    editor.ChangeApplyMulti(design, true, true, true, false, true, true, false, true);
                    editor.ChangeApplyMeta(design, MetaIndex.Wetness, false);
                }

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");

            Im.Line.Same();
            if (ImEx.Button("Disable Advanced"u8, width, _numDesigns > 0
                    ? $"Disable all advanced dyes and customizations but keep everything else as is for all {_numDesigns} designs."
                    : "No designs selected."u8, !enabled))
                foreach (var design in selector.SelectedPaths.OfType<DesignFileSystem.Leaf>().Select(l => l.Value))
                    editor.ChangeApplyMulti(design, null, null, null, false, null, null, false, null);

            if (!enabled)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} while clicking.");
        }

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
        if (_tag.Length is 0)
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
