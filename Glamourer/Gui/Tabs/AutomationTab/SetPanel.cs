using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Structs;
using Glamourer.Unlocks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Action = System.Action;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel
{
    private readonly AutoDesignManager      _manager;
    private readonly SetSelector            _selector;
    private readonly ItemUnlockManager      _itemUnlocks;
    private readonly CustomizeUnlockManager _customizeUnlocks;
    private readonly CustomizationService   _customizations;

    private readonly DesignCombo   _designCombo;
    private readonly JobGroupCombo _jobGroupCombo;

    private string? _tempName;
    private int     _dragIndex = -1;

    private Action? _endAction;

    public SetPanel(SetSelector selector, AutoDesignManager manager, DesignManager designs, JobService jobs, ItemUnlockManager itemUnlocks,
        CustomizeUnlockManager customizeUnlocks, CustomizationService customizations)
    {
        _selector         = selector;
        _manager          = manager;
        _itemUnlocks      = itemUnlocks;
        _customizeUnlocks = customizeUnlocks;
        _customizations   = customizations;
        _designCombo      = new DesignCombo(_manager, designs);
        _jobGroupCombo    = new JobGroupCombo(manager, jobs);
    }

    private AutoDesignSet Selection
        => _selector.Selection!;

    public void Draw()
    {
        using var group = ImRaii.Group();
        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
        var frameHeight = ImGui.GetFrameHeightWithSpacing();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGuiUtil.DrawTextButton(_selector.SelectionName, new Vector2(-frameHeight, ImGui.GetFrameHeight()), buttonColor);
        ImGui.SameLine();
        style.Push(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale);
        using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.HeaderButtons.Value())
            .Push(ImGuiCol.Border, ColorId.HeaderButtons.Value());
        if (ImGuiUtil.DrawDisabledButton(
                $"{(_selector.IncognitoMode ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash).ToIconString()}###IncognitoMode",
                new Vector2(frameHeight, ImGui.GetFrameHeight()), string.Empty, false, true))
            _selector.IncognitoMode = !_selector.IncognitoMode;
        var hovered = ImGui.IsItemHovered();
        color.Pop(2);
        if (hovered)
            ImGui.SetTooltip(_selector.IncognitoMode ? "Toggle incognito mode off." : "Toggle incognito mode on.");
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection)
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
        using var table = ImRaii.Table("SetTable", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY);
        if (!table)
            return;

        ImGui.TableSetupColumn("##del", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Index", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Design", ImGuiTableColumnFlags.WidthFixed, 220 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Application", ImGuiTableColumnFlags.WidthFixed, 5 * ImGui.GetFrameHeight() + 4 * 2 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Job Restrictions", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Warnings", ImGuiTableColumnFlags.WidthFixed, 4 * ImGui.GetFrameHeight() + 3 * 2 * ImGuiHelpers.GlobalScale);
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
            _designCombo.Draw(Selection, design, idx, _selector.IncognitoMode);
            DrawDragDrop(Selection, idx);
            ImGui.TableNextColumn();
            DrawApplicationTypeBoxes(Selection, design, idx);
            ImGui.TableNextColumn();
            _jobGroupCombo.Draw(Selection, design, idx);
            ImGui.TableNextColumn();
            DrawWarnings(design, idx);
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("New");
        ImGui.TableNextColumn();
        _designCombo.Draw(Selection, null, -1, _selector.IncognitoMode);
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawWarnings(AutoDesign design, int idx)
    {
        var size = new Vector2(ImGui.GetFrameHeight());
        size.X += ImGuiHelpers.GlobalScale;

        var (equipFlags, customizeFlags, _, _, _, _) =  design.ApplyWhat();
        equipFlags                                   &= design.Design.ApplyEquip;
        customizeFlags                               &= design.Design.ApplyCustomize;
        var sb = new StringBuilder();
        foreach (var slot in EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand))
        {
            var flag = slot.ToFlag();
            if (!equipFlags.HasFlag(flag))
                continue;

            var item = design.Design.DesignData.Item(slot);
            if (!_itemUnlocks.IsUnlocked(item.ItemId, out _))
                sb.AppendLine($"{item.Name} in {slot.ToName()} slot is not unlocked but should be applied.");
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * ImGuiHelpers.GlobalScale, 0));


        static void DrawWarning(StringBuilder sb, uint color, Vector2 size, string suffix, string good)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale);
            if (sb.Length > 0)
            {
                sb.Append(suffix);
                using (var font = ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGuiUtil.DrawTextButton(FontAwesomeIcon.ExclamationCircle.ToIconString(), size, color);
                }

                ImGuiUtil.HoverTooltip(sb.ToString());
            }
            else
            {
                ImGuiUtil.DrawTextButton(string.Empty, size, 0);
                ImGuiUtil.HoverTooltip(good);
            }
        }

        DrawWarning(sb, 0xA03030F0, size, "\nThese items will be skipped when applied automatically. To change this, see",
            "All equipment to be applied is unlocked."); // TODO

        sb.Clear();
        var sb2       = new StringBuilder();
        var customize = design.Design.DesignData.Customize;
        var set       = _customizations.AwaitedService.GetList(customize.Clan, customize.Gender);
        foreach (var type in CustomizationExtensions.All)
        {
            var flag = type.ToFlag();
            if (!customizeFlags.HasFlag(flag))
                continue;

            if (flag.RequiresRedraw())
                sb.AppendLine($"{type.ToDefaultName()} Customization can not be changed automatically."); // TODO
            else if (type is CustomizeIndex.Hairstyle or CustomizeIndex.FacePaint
                  && set.DataByValue(type, customize[type], out var data, customize.Face) >= 0
                  && !_customizeUnlocks.IsUnlocked(data!.Value, out _))
                sb2.AppendLine(
                    $"{type.ToDefaultName()} Customization {_customizeUnlocks.Unlockable[data.Value].Name} is not unlocked but should be applied.");
        }

        ImGui.SameLine();
        DrawWarning(sb2, 0xA03030F0, size, "\nThese customizations will be skipped when applied automatically. To change this, see",
            "All customizations to be applied are unlocked."); // TODO
        ImGui.SameLine();
        DrawWarning(sb, 0xA030F0F0, size, "\nThese customizations will be skipped when applied automatically.",
            "No customizations unable to be applied automatically are set to be applied."); // TODO
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

        public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex, bool incognito)
        {
            CurrentSelection    = design?.Design ?? (Items.Count > 0 ? Items[0] : null);
            CurrentSelectionIdx = design?.Design.Index ?? (Items.Count > 0 ? 0 : -1);
            var name = (incognito ? CurrentSelection?.Incognito : CurrentSelection?.Name.Text) ?? string.Empty;
            if (Draw("##design", name, string.Empty, 220 * ImGuiHelpers.GlobalScale,
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
