using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui;
using OtterGui.Extensions;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Action = System.Action;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel(
    SetSelector selector,
    AutoDesignManager manager,
    JobService jobs,
    ItemUnlockManager itemUnlocks,
    SpecialDesignCombo designCombo,
    CustomizeUnlockManager customizeUnlocks,
    CustomizeService customizations,
    IdentifierDrawer identifierDrawer,
    Configuration config,
    RandomRestrictionDrawer randomDrawer)
{
    private readonly JobGroupCombo         _jobGroupCombo = new(manager, jobs, Glamourer.Log);
    private readonly HeaderDrawer.Button[] _rightButtons  = [new HeaderDrawer.IncognitoButton(config)];
    private          string?               _tempName;
    private          int                   _dragIndex = -1;

    private Action? _endAction;

    private AutoDesignSet Selection
        => selector.Selection!;

    public void Draw()
    {
        using var group = ImRaii.Group();
        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
        => HeaderDrawer.Draw(selector.SelectionName, 0, ImGuiColor.FrameBackground.Get().Color, [], _rightButtons);

    private void DrawPanel()
    {
        using var child = ImUtf8.Child("##Panel"u8, -Vector2.One, true);
        if (!child || !selector.HasSelection)
            return;

        var spacing = Im.Style.ItemInnerSpacing with { Y = Im.Style.ItemSpacing.Y };

        using (ImUtf8.Group())
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing))
            {
                var enabled = Selection.Enabled;
                if (ImUtf8.Checkbox("##Enabled"u8, ref enabled))
                    manager.SetState(selector.SelectionIndex, enabled);
                ImUtf8.LabeledHelpMarker("Enabled"u8,
                    "Whether the designs in this set should be applied at all. Only one set can be enabled for a character at the same time."u8);
            }

            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing))
            {
                var useGame = selector.Selection!.BaseState is AutoDesignSet.Base.Game;
                if (ImUtf8.Checkbox("##gameState"u8, ref useGame))
                    manager.ChangeBaseState(selector.SelectionIndex, useGame ? AutoDesignSet.Base.Game : AutoDesignSet.Base.Current);
                ImUtf8.LabeledHelpMarker("Use Game State as Base"u8,
                    "When this is enabled, the designs matching conditions will be applied successively on top of what your character is supposed to look like for the game. "u8
                  + "Otherwise, they will be applied on top of the characters actual current look using Glamourer."u8);
            }
        }

        Im.Line.Same();
        using (ImUtf8.Group())
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing))
            {
                var editing = config.ShowAutomationSetEditing;
                if (ImUtf8.Checkbox("##Show Editing"u8, ref editing))
                {
                    config.ShowAutomationSetEditing = editing;
                    config.Save();
                }

                ImUtf8.LabeledHelpMarker("Show Editing"u8,
                    "Show options to change the name or the associated character or NPC of this design set."u8);
            }

            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing))
            {
                var resetSettings = selector.Selection!.ResetTemporarySettings;
                if (ImGui.Checkbox("##resetSettings", ref resetSettings))
                    manager.ChangeResetSettings(selector.SelectionIndex, resetSettings);

                ImUtf8.LabeledHelpMarker("Reset Temporary Settings"u8,
                    "Always reset all temporary settings applied by Glamourer when this automation set is applied, regardless of active designs."u8);
            }
        }

        if (config.ShowAutomationSetEditing)
        {
            Im.Dummy(Vector2.Zero);
            Im.Separator();
            Im.Dummy(Vector2.Zero);

            var name  = _tempName ?? Selection.Name;
            var flags = selector.IncognitoMode ? InputTextFlags.ReadOnly | InputTextFlags.Password : InputTextFlags.None;
            ImGui.SetNextItemWidth(330 * Im.Style.GlobalScale);
            if (Im.Input.Text("Rename Set##Name"u8, ref name, StringU8.Empty, flags))
                _tempName = name;

            if (ImGui.IsItemDeactivated())
            {
                manager.Rename(selector.SelectionIndex, name);
                _tempName = null;
            }

            DrawIdentifierSelection(selector.SelectionIndex);
        }

        Im.Dummy(Vector2.Zero);
        Im.Separator();
        Im.Dummy(Vector2.Zero);
        DrawDesignTable();
        randomDrawer.Draw();
    }


    private void DrawDesignTable()
    {
        var (numCheckboxes, numSpacing) = (config.ShowAllAutomatedApplicationRules, config.ShowUnlockedItemWarnings) switch
        {
            (true, true)   => (9, 14),
            (true, false)  => (7, 10),
            (false, true)  => (4, 4),
            (false, false) => (2, 0),
        };

        var requiredSizeOneLine = numCheckboxes * Im.Style.FrameHeight
          + (30 + 220 + numSpacing) * Im.Style.GlobalScale
          + 5 * Im.Style.CellPadding.X
          + 150 * Im.Style.GlobalScale;

        var singleRow = Im.ContentRegion.Available.X >= requiredSizeOneLine || numSpacing == 0;
        var numRows = (singleRow, config.ShowUnlockedItemWarnings) switch
        {
            (true, true)   => 6,
            (true, false)  => 5,
            (false, true)  => 5,
            (false, false) => 4,
        };

        using var table = Im.Table.Begin("SetTable"u8, numRows, TableFlags.RowBackground | TableFlags.ScrollX | TableFlags.ScrollY);
        if (!table)
            return;

        table.SetupColumn("##del"u8,   TableColumnFlags.WidthFixed, Im.Style.FrameHeight);
        table.SetupColumn("##Index"u8, TableColumnFlags.WidthFixed, 30 * Im.Style.GlobalScale);

        if (singleRow)
        {
            table.SetupColumn("Design"u8, TableColumnFlags.WidthFixed, 220 * Im.Style.GlobalScale);
            if (config.ShowAllAutomatedApplicationRules)
                table.SetupColumn("Application"u8, TableColumnFlags.WidthFixed,
                    6 * Im.Style.FrameHeight + 10 * Im.Style.GlobalScale);
            else
                table.SetupColumn("Use"u8, TableColumnFlags.WidthFixed, ImGui.CalcTextSize("Use").X);
        }
        else
        {
            table.SetupColumn("Design / Job Restrictions"u8, TableColumnFlags.WidthFixed,
                250 * Im.Style.GlobalScale - (ImGui.GetScrollMaxY() > 0 ? Im.Style.ScrollbarSize : 0));
            if (config.ShowAllAutomatedApplicationRules)
                table.SetupColumn("Application"u8, TableColumnFlags.WidthFixed,
                    3 * Im.Style.FrameHeight + 4 * Im.Style.GlobalScale);
            else
                table.SetupColumn("Use"u8, TableColumnFlags.WidthFixed, ImGui.CalcTextSize("Use").X);
        }

        if (singleRow)
            table.SetupColumn("Job Restrictions"u8, TableColumnFlags.WidthStretch);

        if (config.ShowUnlockedItemWarnings)
            table.SetupColumn(""u8, TableColumnFlags.WidthFixed, 2 * Im.Style.FrameHeight + 4 * Im.Style.GlobalScale);

        table.HeaderRow();
        foreach (var (design, idx) in Selection.Designs.WithIndex())
        {
            using var id = ImUtf8.PushId(idx);
            ImGui.TableNextColumn();
            var keyValid = config.DeleteDesignModifier.IsActive();
            var tt = keyValid
                ? "Remove this design from the set."
                : $"Remove this design from the set.\nHold {config.DeleteDesignModifier} to remove.";

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(Im.Style.FrameHeight), tt, !keyValid, true))
                _endAction = () => manager.DeleteDesign(Selection, idx);
            ImGui.TableNextColumn();
            DrawSelectable(idx, design.Design);

            ImGui.TableNextColumn();
            DrawRandomEditing(Selection, design, idx);
            designCombo.Draw(Selection, design, idx);
            DrawDragDrop(Selection, idx);
            if (singleRow)
            {
                ImGui.TableNextColumn();
                DrawApplicationTypeBoxes(Selection, design, idx, singleRow);
                ImGui.TableNextColumn();
                DrawConditions(design, idx);
            }
            else
            {
                DrawConditions(design, idx);
                ImGui.TableNextColumn();
                DrawApplicationTypeBoxes(Selection, design, idx, singleRow);
            }

            if (config.ShowUnlockedItemWarnings)
            {
                ImGui.TableNextColumn();
                DrawWarnings(design);
            }
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImUtf8.TextFrameAligned("New"u8);
        ImGui.TableNextColumn();
        designCombo.Draw(Selection, null, -1);
        ImGui.TableNextRow();

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawSelectable(int idx, IDesignStandIn design)
    {
        var highlight = Rgba32.Transparent;
        var    sb        = new StringBuilder();
        if (design is Design d)
        {
            var count = design.AllLinks(true).Count();
            if (count > 1)
            {
                sb.AppendLine($"This design contains {count - 1} links to other designs.");
                highlight = ColorId.HeaderButtons.Value();
            }

            count = d.AssociatedMods.Count;
            if (count > 0)
            {
                sb.AppendLine($"This design contains {count} mod associations.");
                highlight = ColorId.ModdedItemMarker.Value();
            }

            count = design.GetMaterialData().Count(p => p.Item2.Enabled);
            if (count > 0)
            {
                sb.AppendLine($"This design contains {count} enabled advanced dyes.");
                highlight = ColorId.AdvancedDyeActive.Value();
            }
        }

        using (ImGuiColor.Text.Push(highlight, highlight.IsTransparent))
        {
            ImUtf8.Selectable($"#{idx + 1:D2}");
        }

        ImUtf8.HoverTooltip($"{sb}");

        DrawDragDrop(Selection, idx);
    }

    private int _tmpGearset = int.MaxValue;
    private int _whichIndex = -1;

    private void DrawConditions(AutoDesign design, int idx)
    {
        var usingGearset = design.GearsetIndex >= 0;
        if (ImUtf8.Button($"{(usingGearset ? "Gearset:" : "Jobs:")}##usingGearset"))
        {
            usingGearset = !usingGearset;
            manager.ChangeGearsetCondition(Selection, idx, (short)(usingGearset ? 0 : -1));
        }

        ImUtf8.HoverTooltip("Click to switch between Job and Gearset restrictions."u8);

        ImGui.SameLine(0, Im.Style.ItemInnerSpacing.X);
        if (usingGearset)
        {
            var set = 1 + (_tmpGearset == int.MaxValue || _whichIndex != idx ? design.GearsetIndex : _tmpGearset);
            ImGui.SetNextItemWidth(Im.ContentRegion.Available.X);
            if (ImUtf8.InputScalar("##whichGearset"u8, ref set))
            {
                _whichIndex = idx;
                _tmpGearset = Math.Clamp(set, 1, 100);
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                manager.ChangeGearsetCondition(Selection, idx, (short)(_tmpGearset - 1));
                _tmpGearset = int.MaxValue;
                _whichIndex = -1;
            }
        }
        else
        {
            _jobGroupCombo.Draw(Selection, design, idx);
        }
    }

    private void DrawRandomEditing(AutoDesignSet set, AutoDesign design, int designIdx)
    {
        if (design.Design is not RandomDesign)
            return;

        randomDrawer.DrawButton(set, designIdx);
        ImGui.SameLine(0, Im.Style.ItemInnerSpacing.X);
    }

    private void DrawWarnings(AutoDesign design)
    {
        if (design.Design is not DesignBase)
            return;

        var size = new Vector2(Im.Style.FrameHeight);
        size.X += Im.Style.GlobalScale;

        var collection = design.ApplyWhat();
        var sb         = new StringBuilder();
        var designData = design.Design.GetDesignData(default);
        foreach (var slot in EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand))
        {
            var flag = slot.ToFlag();
            if (!collection.Equip.HasFlag(flag))
                continue;

            var item = designData.Item(slot);
            if (!itemUnlocks.IsUnlocked(item.Id, out _))
                sb.AppendLine($"{item.Name} in {slot.ToName()} slot is not unlocked. Consider obtaining it via gameplay means!");
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * Im.Style.GlobalScale, 0));

        var tt = config.UnlockedItemMode
            ? "\nThese items will be skipped when applied automatically.\n\nTo change this, disable the Obtained Item Mode setting."
            : string.Empty;
        DrawWarning(sb, config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All equipment to be applied is unlocked.");

        sb.Clear();
        var sb2       = new StringBuilder();
        var customize = designData.Customize;
        if (!designData.IsHuman)
            sb.AppendLine("The base model id can not be changed automatically to something non-human.");

        var set = customizations.Manager.GetSet(customize.Clan, customize.Gender);
        foreach (var type in CustomizationExtensions.All)
        {
            var flag = type.ToFlag();
            if (!collection.Customize.HasFlag(flag))
                continue;

            if (flag.RequiresRedraw())
                sb.AppendLine($"{type.ToName()} Customization should not be changed automatically.");
            else if (type is CustomizeIndex.Hairstyle or CustomizeIndex.FacePaint
                  && set.DataByValue(type, customize[type], out var data, customize.Face) >= 0
                  && !customizeUnlocks.IsUnlocked(data!.Value, out _))
                sb2.AppendLine(
                    $"{type.ToName()} Customization {customizeUnlocks.Unlockable[data.Value].Name} is not unlocked but should be applied.");
        }

        Im.Line.Same();
        tt = config.UnlockedItemMode
            ? "\nThese customizations will be skipped when applied automatically.\n\nTo change this, disable the Obtained Item Mode setting."
            : string.Empty;
        DrawWarning(sb2, config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All customizations to be applied are unlocked.");
        Im.Line.Same();
        return;

        static void DrawWarning(StringBuilder sb, uint color, Vector2 size, string suffix, string good)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, Im.Style.GlobalScale);
            if (sb.Length > 0)
            {
                sb.Append(suffix);
                using (_ = ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGuiUtil.DrawTextButton(FontAwesomeIcon.ExclamationCircle.ToIconString(), size, color);
                }

                ImUtf8.HoverTooltip($"{sb}");
            }
            else
            {
                ImGuiUtil.DrawTextButton(string.Empty, size, 0);
                ImUtf8.HoverTooltip(good);
            }
        }
    }

    private void DrawDragDrop(AutoDesignSet set, int index)
    {
        const string dragDropLabel = "DesignDragDrop";
        using (var target = ImUtf8.DragDropTarget())
        {
            if (target.Success && ImGuiUtil.IsDropping(dragDropLabel))
            {
                if (_dragIndex >= 0)
                {
                    var idx = _dragIndex;
                    _endAction = () => manager.MoveDesign(set, idx, index);
                }

                _dragIndex = -1;
            }
        }

        using (var source = ImUtf8.DragDropSource())
        {
            if (source)
            {
                ImUtf8.Text($"Moving design #{index + 1:D2}...");
                if (ImGui.SetDragDropPayload(dragDropLabel, null, 0))
                {
                    _dragIndex                 = index;
                    selector.DragDesignIndex = index;
                }
            }
        }
    }

    private void DrawApplicationTypeBoxes(AutoDesignSet set, AutoDesign design, int autoDesignIndex, bool singleLine)
    {
        using var style      = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2 * Im.Style.GlobalScale));
        var       newType    = design.Type;
        var       newTypeInt = (uint)newType;
        using (ImStyleBorder.Frame.Push(ColorId.FolderLine.Value()))
        {
            if (ImGui.CheckboxFlags("##all", ref newTypeInt, (uint)ApplicationType.All))
                newType = (ApplicationType)newTypeInt;
        }

        style.Pop();
        ImUtf8.HoverTooltip("Toggle all application modes at once."u8);
        if (config.ShowAllAutomatedApplicationRules)
        {
            void Box(int idx)
            {
                var (type, description) = ApplicationTypeExtensions.Types[idx];
                var value = design.Type.HasFlag(type);
                if (ImUtf8.Checkbox($"##{(byte)type}", ref value))
                    newType = value ? newType | type : newType & ~type;
                ImUtf8.HoverTooltip(description);
            }

            Im.Line.Same();
            Box(0);
            Im.Line.Same();
            Box(1);
            if (singleLine)
                Im.Line.Same();

            Box(2);
            Im.Line.Same();
            Box(3);
            Im.Line.Same();
            Box(4);
        }

        manager.ChangeApplicationType(set, autoDesignIndex, newType);
    }

    private void DrawIdentifierSelection(int setIndex)
    {
        using var id = ImUtf8.PushId("Identifiers"u8);
        identifierDrawer.DrawWorld(130);
        Im.Line.Same();
        identifierDrawer.DrawName(200 - Im.Style.ItemSpacing.X);
        identifierDrawer.DrawNpcs(330);
        var buttonWidth = new Vector2(165 * Im.Style.GlobalScale - Im.Style.ItemSpacing.X / 2, 0);
        if (ImUtf8.ButtonEx("Set to Character"u8, string.Empty, buttonWidth, !identifierDrawer.CanSetPlayer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.PlayerIdentifier);
        Im.Line.Same();
        if (ImUtf8.ButtonEx("Set to NPC"u8, string.Empty, buttonWidth, !identifierDrawer.CanSetNpc))
            manager.ChangeIdentifier(setIndex, identifierDrawer.NpcIdentifier);

        if (ImUtf8.ButtonEx("Set to Retainer"u8, string.Empty, buttonWidth, !identifierDrawer.CanSetRetainer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.RetainerIdentifier);
        Im.Line.Same();
        if (ImUtf8.ButtonEx("Set to Mannequin"u8, string.Empty, buttonWidth, !identifierDrawer.CanSetRetainer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.MannequinIdentifier);

        if (ImUtf8.ButtonEx("Set to Owned NPC"u8, string.Empty, buttonWidth, !identifierDrawer.CanSetOwned))
            manager.ChangeIdentifier(setIndex, identifierDrawer.OwnedIdentifier);
    }

    private sealed class JobGroupCombo(AutoDesignManager manager, JobService jobs, Logger log)
        : FilterComboCache<JobGroup>(() => jobs.JobGroups.Values.ToList(), MouseWheelType.None, log)
    {
        public void Draw(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
        {
            CurrentSelection    = design.Jobs;
            CurrentSelectionIdx = jobs.JobGroups.Values.IndexOf(j => j.Id == design.Jobs.Id);
            if (Draw("##JobGroups", design.Jobs.Name.ToString(),
                    "Select for which job groups this design should be applied.\nControl + Right-Click to set to all classes.",
                    Im.ContentRegion.Available.X, Im.Style.TextHeightWithSpacing)
             && CurrentSelectionIdx >= 0)
                manager.ChangeJobCondition(set, autoDesignIndex, CurrentSelection);
            else if (Im.Io.KeyControl && Im.Item.RightClicked())
                manager.ChangeJobCondition(set, autoDesignIndex, jobs.JobGroups[1]);
        }

        protected override string ToString(JobGroup obj)
            => obj.Name.ToString();
    }
}
