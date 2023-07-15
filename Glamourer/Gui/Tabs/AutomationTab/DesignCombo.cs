using System.Linq;
using Glamourer.Automation;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class DesignCombo : FilterComboCache<Design>
{
    private readonly AutoDesignManager _manager;
    private readonly DesignFileSystem  _fileSystem;

    public DesignCombo(AutoDesignManager manager, DesignManager designs, DesignFileSystem fileSystem)
        : base(() => designs.Designs.OrderBy(d => d.Name).ToList())
    {
        _manager    = manager;
        _fileSystem = fileSystem;
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var ret = base.DrawSelectable(globalIdx, selected);

        if (_fileSystem.FindLeaf(Items[globalIdx], out var leaf))
        {
            var       fullName = leaf.FullName();
            if (fullName != Items[globalIdx].Name)
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGui.SameLine();
                ImGuiUtil.RightAlign(fullName);
            }
        }

        return ret;
    }

    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex, bool incognito)
    {
        CurrentSelection    = design?.Design ?? (Items.Count > 0 ? Items[0] : null);
        CurrentSelectionIdx = design?.Design.Index ?? (Items.Count > 0 ? 0 : -1);
        var name = (incognito ? CurrentSelection?.Incognito : CurrentSelection?.Name.Text) ?? string.Empty;
        if (Draw("##design", name, string.Empty, ImGui.GetContentRegionAvail().X,
                ImGui.GetTextLineHeight())
         && CurrentSelection != null)
        {
            if (autoDesignIndex >= 0)
                _manager.ChangeDesign(set, autoDesignIndex, CurrentSelection);
            else
                _manager.AddDesign(set, CurrentSelection);
        }
    }

    protected override string ToString(Design obj)
        => obj.Name.Text;
}
