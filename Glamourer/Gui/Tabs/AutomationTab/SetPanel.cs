using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel
{
    private readonly AutoDesignManager _manager;
    private readonly SetSelector       _selector;

    private readonly DesignCombo   _designCombo;
    private readonly JobGroupCombo _jobGroupCombo;

    private string? _tempName;
    private int     _dragIndex = -1;

    private Action? _endAction;

    public SetPanel(SetSelector selector, AutoDesignManager manager, DesignManager designs, JobService jobs)
    {
        _selector      = selector;
        _manager       = manager;
        _designCombo   = new DesignCombo(_manager, designs);
        _jobGroupCombo = new JobGroupCombo(manager, jobs);
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

        DrawDesignTable();
    }

    private void DrawDesignTable()
    {
        using var table = ImRaii.Table("SetTable", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY);
        if (!table)
            return;

        ImGui.TableSetupColumn("##del", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Index", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Design", ImGuiTableColumnFlags.WidthFixed, 220 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Application", ImGuiTableColumnFlags.WidthFixed, 5 * ImGui.GetFrameHeight() + 4 * 2 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Job Restrictions", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var (design, idx) in Selection.Designs.WithIndex())
        {
            using var id = ImRaii.PushId(idx);
            ImGui.TableNextColumn();
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
                    "Remove this design from the set.", false, true))
                _endAction = () => _manager.DeleteDesign(Selection, idx);
            ImGui.TableNextColumn();
            ImGui.Selectable($"#{idx + 1:D2}");
            DrawDragDrop(Selection, idx);
            ImGui.TableNextColumn();
            _designCombo.Draw(Selection, design, idx);
            DrawDragDrop(Selection, idx);
            ImGui.TableNextColumn();
            DrawApplicationTypeBoxes(Selection, design, idx);
            ImGui.TableNextColumn();
            _jobGroupCombo.Draw(Selection, design, idx);
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("New");
        ImGui.TableNextColumn();
        _designCombo.Draw(Selection, null, -1);
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawDragDrop(AutoDesignSet set, int index)
    {
        const string dragDropLabel = "DesignDragDrop";
        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success && ImGuiUtil.IsDropping(dragDropLabel))
            {
                if (_dragIndex >= 0)
                {
                    var idx = _dragIndex;
                    _endAction = () => _manager.MoveDesign(set, idx, index);
                }

                _dragIndex = -1;
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source.Success && ImGui.SetDragDropPayload(dragDropLabel, nint.Zero, 0))
            {
                _dragIndex = index;
                ImGui.TextUnformatted($"Moving design #{index + 1:D2}...");
            }
        }
    }

    private void DrawApplicationTypeBoxes(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
    {
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * ImGuiHelpers.GlobalScale));
        var       newType = design.ApplicationType;
        foreach (var (type, description) in Types)
        {
            var value = design.ApplicationType.HasFlag(type);
            if (ImGui.Checkbox($"##{(byte)type}", ref value))
                newType = value ? newType | type : newType & ~type;
            ImGuiUtil.HoverTooltip(description);
            ImGui.SameLine();
        }

        _manager.ChangeApplicationType(set, autoDesignIndex, newType);
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

    private sealed class JobGroupCombo : FilterComboCache<JobGroup>
    {
        private readonly AutoDesignManager _manager;
        private readonly JobService        _jobs;

        public JobGroupCombo(AutoDesignManager manager, JobService jobs)
            : base(() => jobs.JobGroups.Values.ToList())
        {
            _manager = manager;
            _jobs    = jobs;
        }

        public void Draw(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
        {
            CurrentSelection    = design.Jobs;
            CurrentSelectionIdx = _jobs.JobGroups.Values.IndexOf(j => j.Id == design.Jobs.Id);
            if (Draw("##JobGroups", design.Jobs.Name,
                    "Select for which job groups this design should be applied.\nControl + Right-Click to set to all classes.",
                    ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight())
             && CurrentSelectionIdx >= 0)
                _manager.ChangeJobCondition(set, autoDesignIndex, CurrentSelection);
            else if (ImGui.GetIO().KeyCtrl && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _manager.ChangeJobCondition(set, autoDesignIndex, _jobs.JobGroups[1]);
        }

        protected override string ToString(JobGroup obj)
            => obj.Name;
    }

    private sealed class DesignCombo : FilterComboCache<Design>
    {
        private readonly AutoDesignManager _manager;
        private readonly DesignManager     _designs;

        public DesignCombo(AutoDesignManager manager, DesignManager designs)
            : base(() => designs.Designs.OrderBy(d => d.Name).ToList())
        {
            _designs = designs;
            _manager = manager;
        }

        public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex)
        {
            CurrentSelection    = design?.Design ?? (Items.Count > 0 ? Items[0] : null);
            CurrentSelectionIdx = design?.Design.Index ?? (Items.Count > 0 ? 0 : -1);
            if (Draw("##design", CurrentSelection?.Name.Text ?? string.Empty, string.Empty, 220 * ImGuiHelpers.GlobalScale,
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
}
