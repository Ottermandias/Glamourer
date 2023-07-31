using System.Linq;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class DesignCombo : FilterComboCache<Design>
{
    public const    int    RevertDesignIndex = -1228;
    public readonly Design RevertDesign;

    private readonly AutoDesignManager _manager;
    private readonly DesignFileSystem  _fileSystem;
    private readonly TabSelected       _tabSelected;

    public DesignCombo(AutoDesignManager manager, DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected,
        ItemManager items)
        : base(() => designs.Designs.OrderBy(d => d.Name).Prepend(CreateRevertDesign(items)).ToList())
    {
        _manager     = manager;
        _fileSystem  = fileSystem;
        _tabSelected = tabSelected;
        RevertDesign = Items[0];
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var ret = base.DrawSelectable(globalIdx, selected);

        if (ImGui.IsItemHovered() && _fileSystem.FindLeaf(Items[globalIdx], out var leaf))
        {
            var fullName = leaf.FullName();
            if (!fullName.StartsWith(Items[globalIdx].Name))
            {
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted(fullName);
            }
        }

        return ret;
    }

    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex, bool incognito)
    {
        CurrentSelection    = design?.Design ?? RevertDesign;
        CurrentSelectionIdx = (design?.Design?.Index ?? -1) + 1;
        var name = design?.Name(incognito) ?? string.Empty;
        if (Draw("##design", name, string.Empty, ImGui.GetContentRegionAvail().X,
                ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection != null)
        {
            if (autoDesignIndex >= 0)
                _manager.ChangeDesign(set, autoDesignIndex, CurrentSelection == RevertDesign ? null : CurrentSelection);
            else
                _manager.AddDesign(set, CurrentSelection == RevertDesign ? null : CurrentSelection);
        }

        if (design?.Design != null)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                _tabSelected.Invoke(MainWindow.TabType.Designs, design.Design);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }
    }

    protected override string ToString(Design obj)
        => obj.Name.Text;

    private static Design CreateRevertDesign(ItemManager items)
        => new(items)
        {
            Index = RevertDesignIndex,
            Name  = AutoDesign.RevertName,
        };
}
