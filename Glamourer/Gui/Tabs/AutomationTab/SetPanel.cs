using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel
{
    private readonly AutoDesignManager _manager;
    private readonly SetSelector       _selector;

    public SetPanel(SetSelector selector, AutoDesignManager manager)
    {
        _selector = selector;
        _manager  = manager;
    }

    private AutoDesignSet Selection
        => _selector.Selection!;

    public void Draw()
    {
        if (!_selector.HasSelection)
            return;

        using var group = ImRaii.Group();
        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGuiUtil.DrawTextButton(Selection.Name, -Vector2.UnitX, buttonColor);
    }

    private string? _tempName;

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##SetPanel", -Vector2.One, true);
        if (!child)
            return;

        var name = _tempName ?? Selection.Name;
        if (ImGui.InputText("##Name", ref name, 64))
            _tempName = name;

        if (ImGui.IsItemDeactivated())
        {
            _manager.Rename(_selector.SelectionIndex, name);
            _tempName = null;
        }

        ImGui.SameLine();
        var enabled = Selection.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            _manager.SetState(_selector.SelectionIndex, enabled);

        using var table = ImRaii.Table("SetTable", 4, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("##Index", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Design", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Application", ImGuiTableColumnFlags.WidthFixed, 5 * ImGui.GetFrameHeight() + 4 * 2 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Job Restrictions", ImGuiTableColumnFlags.WidthStretch);


        foreach (var (design, idx) in Selection.Designs.WithIndex())
        {
            ImGuiUtil.DrawTableColumn($"#{idx:D2}");
            ImGui.TableNextColumn();
            DrawDesignCombo(Selection, design, idx);
            ImGui.TableNextColumn();
            
            ImGui.TableNextColumn();
            DrawJobGroupCombo(Selection, design, idx);
        }
    }

    private void DrawDesignCombo(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
    {
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        using var combo = ImRaii.Combo("##design", design.Design.Name);
        if (!combo)
            return;
    }

    private void DrawJobGroupCombo(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        using var combo = ImRaii.Combo("##JobGroups", design.Jobs.Name);
        if (!combo)
            return;
    }

    private void DrawApplicationTypeBoxes(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
    {
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, 2 * ImGuiHelpers.GlobalScale);
        var       newType = design.ApplicationType;
        foreach (var (type, description) in Types)
        {
            var value = design.ApplicationType.HasFlag(type);
            if (ImGui.Checkbox($"##{(byte)type}", ref value))
                newType = value ? newType | type : newType & ~type;
            ImGuiUtil.HoverTooltip(description);
        }
    }

    private static readonly IReadOnlyList<(AutoDesign.Type, string)> Types = new[]
    {
        (AutoDesign.Type.Customizations,
            "Apply all customization changes that are enabled in this design and that are valid in a fixed design and for the given race and gender."),
        (AutoDesign.Type.Armor, "Apply all armor piece changes that are enabled in this design and that are valid in a fixed design."),
        (AutoDesign.Type.Accessories, "Apply all accessory changes that are enabled in this design and that are valid in a fixed design."),
        (AutoDesign.Type.Stains, "Apply all dye changes that are enabled in this design."),
        (AutoDesign.Type.Weapons, "Apply all weapon changes that are enabled in this design and that are valid with the current weapon worn."),
    };
}
