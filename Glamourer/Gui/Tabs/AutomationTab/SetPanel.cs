using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Structs;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Action = System.Action;
using CustomizeIndex = Glamourer.Customization.CustomizeIndex;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel
{
    private readonly AutoDesignManager      _manager;
    private readonly SetSelector            _selector;
    private readonly ItemUnlockManager      _itemUnlocks;
    private readonly CustomizeUnlockManager _customizeUnlocks;
    private readonly CustomizationService   _customizations;

    private readonly Configuration    _config;
    private readonly DesignCombo      _designCombo;
    private readonly JobGroupCombo    _jobGroupCombo;
    private readonly IdentifierDrawer _identifierDrawer;

    private string? _tempName;
    private int     _dragIndex = -1;

    private Action? _endAction;

    public SetPanel(SetSelector selector, AutoDesignManager manager, JobService jobs, ItemUnlockManager itemUnlocks, DesignCombo designCombo,
        CustomizeUnlockManager customizeUnlocks, CustomizationService customizations, IdentifierDrawer identifierDrawer, Configuration config)
    {
        _selector         = selector;
        _manager          = manager;
        _itemUnlocks      = itemUnlocks;
        _customizeUnlocks = customizeUnlocks;
        _customizations   = customizations;
        _identifierDrawer = identifierDrawer;
        _config           = config;
        _designCombo      = designCombo;
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
        => HeaderDrawer.Draw(_selector.SelectionName, 0, ImGui.GetColorU32(ImGuiCol.FrameBg), 0,
            HeaderDrawer.Button.IncognitoButton(_selector.IncognitoMode, v => _selector.IncognitoMode = v));

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection)
            return;

        var enabled = Selection.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            _manager.SetState(_selector.SelectionIndex, enabled);

        var useGame = _selector.Selection!.BaseState is AutoDesignSet.Base.Game;
        if (ImGui.Checkbox("Use Game State as Base", ref useGame))
            _manager.ChangeBaseState(_selector.SelectionIndex, useGame ? AutoDesignSet.Base.Game : AutoDesignSet.Base.Current);

        var name  = _tempName ?? Selection.Name;
        var flags = _selector.IncognitoMode ? ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None;
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("##Name", ref name, 128, flags))
            _tempName = name;

        if (ImGui.IsItemDeactivated())
        {
            _manager.Rename(_selector.SelectionIndex, name);
            _tempName = null;
        }

        ImGui.Separator();
        DrawIdentifierSelection(_selector.SelectionIndex);

        DrawDesignTable();
    }


    private void DrawDesignTable()
    {
        var requiredSizeOneLine = (1 + 6 + 2) * ImGui.GetFrameHeight()
          + (30 + 220 + 10 + 4) * ImGuiHelpers.GlobalScale
          + 5 * ImGui.GetStyle().CellPadding.X
          + 150 * ImGuiHelpers.GlobalScale;

        var singleRow = ImGui.GetContentRegionAvail().X >= requiredSizeOneLine;

        using var table = ImRaii.Table("SetTable", singleRow ? 6 : 5,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY);
        if (!table)
            return;

        ImGui.TableSetupColumn("##del",   ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Index", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        if (singleRow)
        {
            ImGui.TableSetupColumn("Design",      ImGuiTableColumnFlags.WidthFixed, 220 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Application", ImGuiTableColumnFlags.WidthFixed, 6 * ImGui.GetFrameHeight() + 10 * ImGuiHelpers.GlobalScale);
        }
        else
        {
            ImGui.TableSetupColumn("Design / Job Restrictions", ImGuiTableColumnFlags.WidthFixed, 250 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Application", ImGuiTableColumnFlags.WidthFixed, 3 * ImGui.GetFrameHeight() + 4 * ImGuiHelpers.GlobalScale);
        }

        if (singleRow)
            ImGui.TableSetupColumn("Job Restrictions", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 2 * ImGui.GetFrameHeight() + 4 * ImGuiHelpers.GlobalScale);
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
            if (singleRow)
            {
                ImGui.TableNextColumn();
                DrawApplicationTypeBoxes(Selection, design, idx, singleRow);
                ImGui.TableNextColumn();
                _jobGroupCombo.Draw(Selection, design, idx);
            }
            else
            {
                _jobGroupCombo.Draw(Selection, design, idx);
                ImGui.TableNextColumn();
                DrawApplicationTypeBoxes(Selection, design, idx, singleRow);
            }

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
        if (design.Revert)
            return;

        var size = new Vector2(ImGui.GetFrameHeight());
        size.X += ImGuiHelpers.GlobalScale;

        var (equipFlags, customizeFlags, _, _, _, _) =  design.ApplyWhat();
        var sb = new StringBuilder();
        foreach (var slot in EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand))
        {
            var flag = slot.ToFlag();
            if (!equipFlags.HasFlag(flag))
                continue;

            var item = design.Design!.DesignData.Item(slot);
            if (!_itemUnlocks.IsUnlocked(item.Id, out _))
                sb.AppendLine($"{item.Name} in {slot.ToName()} slot is not unlocked. Consider obtaining it via gameplay means!");
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

        var tt = _config.UnlockedItemMode
            ? "\nThese items will be skipped when applied automatically.\n\nTo change this, disable the Obtained Item Mode setting."
            : string.Empty;
        DrawWarning(sb, _config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All equipment to be applied is unlocked.");

        sb.Clear();
        var sb2       = new StringBuilder();
        var customize = design.Design!.DesignData.Customize;
        if (!design.Design.DesignData.IsHuman)
            sb.AppendLine("The base model id can not be changed automatically to something non-human.");

        var set = _customizations.AwaitedService.GetList(customize.Clan, customize.Gender);
        foreach (var type in CustomizationExtensions.All)
        {
            var flag = type.ToFlag();
            if (!customizeFlags.HasFlag(flag))
                continue;

            if (flag.RequiresRedraw())
                sb.AppendLine($"{type.ToDefaultName()} Customization should not be changed automatically.");
            else if (type is CustomizeIndex.Hairstyle or CustomizeIndex.FacePaint
                  && set.DataByValue(type, customize[type], out var data, customize.Face) >= 0
                  && !_customizeUnlocks.IsUnlocked(data!.Value, out _))
                sb2.AppendLine(
                    $"{type.ToDefaultName()} Customization {_customizeUnlocks.Unlockable[data.Value].Name} is not unlocked but should be applied.");
        }

        ImGui.SameLine();
        tt = _config.UnlockedItemMode
            ? "\nThese customizations will be skipped when applied automatically.\n\nTo change this, disable the Obtained Item Mode setting."
            : string.Empty;
        DrawWarning(sb2, _config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All customizations to be applied are unlocked.");
        ImGui.SameLine();
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

    private void DrawApplicationTypeBoxes(AutoDesignSet set, AutoDesign design, int autoDesignIndex, bool singleLine)
    {
        using var style      = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * ImGuiHelpers.GlobalScale));
        var       newType    = design.ApplicationType;
        var       newTypeInt = (uint)newType;
        if (ImGui.CheckboxFlags("##all", ref newTypeInt, (uint)AutoDesign.Type.All))
            newType = (AutoDesign.Type)newTypeInt;
        ImGuiUtil.HoverTooltip("Toggle all modes at once.");

        void Box(int idx)
        {
            var (type, description) = Types[idx];
            var value = design.ApplicationType.HasFlag(type);
            if (ImGui.Checkbox($"##{(byte)type}", ref value))
                newType = value ? newType | type : newType & ~type;
            ImGuiUtil.HoverTooltip(description);
        }

        ImGui.SameLine();
        Box(0);
        ImGui.SameLine();
        Box(1);
        if (singleLine)
            ImGui.SameLine();

        Box(2);
        ImGui.SameLine();
        Box(3);
        ImGui.SameLine();
        Box(4);

        _manager.ChangeApplicationType(set, autoDesignIndex, newType);
    }

    private void DrawIdentifierSelection(int setIndex)
    {
        using var id = ImRaii.PushId("Identifiers");
        _identifierDrawer.DrawWorld(200);
        _identifierDrawer.DrawName(300);
        _identifierDrawer.DrawNpcs(300);
        if (ImGuiUtil.DrawDisabledButton("Set to Retainer", new Vector2(100, 0), string.Empty, !_identifierDrawer.CanSetRetainer))
            _manager.ChangeIdentifier(setIndex, _identifierDrawer.RetainerIdentifier);
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Set to Character", new Vector2(100, 0), string.Empty, !_identifierDrawer.CanSetPlayer))
            _manager.ChangeIdentifier(setIndex, _identifierDrawer.PlayerIdentifier);
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Set to Npc", new Vector2(100, 0), string.Empty, !_identifierDrawer.CanSetNpc))
            _manager.ChangeIdentifier(setIndex, _identifierDrawer.NpcIdentifier);
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
}
