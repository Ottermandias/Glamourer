using System;
using System.Linq;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class DesignCombo : FilterComboCache<(Design, string)>
{
    public const    int    RevertDesignIndex = -1228;
    public readonly Design RevertDesign;

    private readonly AutoDesignManager _manager;
    private readonly TabSelected       _tabSelected;
    private          float             _innerWidth;

    public DesignCombo(AutoDesignManager manager, DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected,
        ItemManager items)
        : this(manager, designs, fileSystem, tabSelected, CreateRevertDesign(items))
    { }

    private DesignCombo(AutoDesignManager manager, DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected,
        Design revertDesign)
        : base(() => designs.Designs.Select(d => (d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty)).OrderBy(d => d.Item2)
            .Prepend((revertDesign, string.Empty)).ToList())
    {
        _manager     = manager;
        _tabSelected = tabSelected;
        RevertDesign = revertDesign;
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var ret = base.DrawSelectable(globalIdx, selected);
        var (design, path) = Items[globalIdx];
        if (path.Length > 0 && design.Name != path)
        {
            var start          = ImGui.GetItemRectMin();
            var pos            = start.X + ImGui.CalcTextSize(design.Name).X;
            var maxSize        = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
            var remainingSpace = maxSize - pos;
            var requiredSize   = ImGui.CalcTextSize(path).X + ImGui.GetStyle().ItemInnerSpacing.X;
            var offset         = remainingSpace - requiredSize;
            if (ImGui.GetScrollMaxY() == 0)
                offset -= ImGui.GetStyle().ItemInnerSpacing.X;

            if (offset < ImGui.GetStyle().ItemSpacing.X)
                ImGuiUtil.HoverTooltip(path);
            else
                ImGui.GetWindowDrawList().AddText(start with { X = pos + offset },
                    ImGui.GetColorU32(ImGuiCol.TextDisabled), path);
        }

        return ret;
    }

    protected override float GetFilterWidth()
        => _innerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex, bool incognito)
    {
        _innerWidth         = 400 * ImGuiHelpers.GlobalScale;
        CurrentSelectionIdx = Math.Max(Items.IndexOf(p => design?.Design == p.Item1), 0);
        CurrentSelection    = Items[CurrentSelectionIdx];
        var name = design?.Name(incognito) ?? "Select Design Here...";
        if (Draw("##design", name, string.Empty, ImGui.GetContentRegionAvail().X,
                ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection.Item1 != null)
        {
            if (autoDesignIndex >= 0)
                _manager.ChangeDesign(set, autoDesignIndex, CurrentSelection.Item1 == RevertDesign ? null : CurrentSelection.Item1);
            else
                _manager.AddDesign(set, CurrentSelection.Item1 == RevertDesign ? null : CurrentSelection.Item1);
        }

        if (design?.Design != null)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                _tabSelected.Invoke(MainWindow.TabType.Designs, design.Design);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }
    }

    protected override string ToString((Design, string) obj)
        => obj.Item1.Name.Text;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || design.Name.Lower.Contains(filter.Lower);
    }

    private static Design CreateRevertDesign(ItemManager items)
        => new(items)
        {
            Index = RevertDesignIndex,
            Name  = AutoDesign.RevertName,
        };
}
