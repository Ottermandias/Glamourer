using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.AutomationTab;

public class SetPanel(
    AutoDesignManager manager,
    JobService jobs,
    ItemUnlockManager itemUnlocks,
    SpecialDesignCombo designCombo,
    CustomizeUnlockManager customizeUnlocks,
    CustomizeService customizations,
    IdentifierDrawer identifierDrawer,
    Configuration.Configuration config,
    RandomRestrictionDrawer randomDrawer,
    AutomationSelection selection) : IPanel
{
    private readonly JobGroupCombo         _jobGroupCombo = new(manager, jobs);
    private          int                   _dragIndex = -1;

    private Action? _endAction;

    public ReadOnlySpan<byte> Id
        => "SetPanel"u8;

    public void Draw()
    {
        if (selection.Index < 0)
            return;

        using (Im.Group())
        {
            var enabled = selection.Set!.Enabled;
            if (Im.Checkbox("##Enabled"u8, ref enabled))
                manager.SetState(selection.Index, enabled);
            LunaStyle.DrawAlignedHelpMarkerLabel("Enabled"u8,
                "Whether the designs in this set should be applied at all. Only one set can be enabled for a character at the same time."u8);

            var useGame = selection.Set!.BaseState is AutoDesignSet.Base.Game;
            if (Im.Checkbox("##gameState"u8, ref useGame))
                manager.ChangeBaseState(selection.Index, useGame ? AutoDesignSet.Base.Game : AutoDesignSet.Base.Current);
            LunaStyle.DrawAlignedHelpMarkerLabel("Use Game State as Base"u8,
                "When this is enabled, the designs matching conditions will be applied successively on top of what your character is supposed to look like for the game. "u8
              + "Otherwise, they will be applied on top of the characters actual current look using Glamourer."u8);
        }

        Im.Line.Same();
        using (Im.Group())
        {
            var editing = config.ShowAutomationSetEditing;
            if (Im.Checkbox("##Show Editing"u8, ref editing))
            {
                config.ShowAutomationSetEditing = editing;
                config.Save();
            }

            LunaStyle.DrawAlignedHelpMarkerLabel("Show Editing"u8,
                "Show options to change the name or the associated character or NPC of this design set."u8);

            var resetSettings = selection.Set!.ResetTemporarySettings;
            if (Im.Checkbox("##resetSettings"u8, ref resetSettings))
                manager.ChangeResetSettings(selection.Index, resetSettings);

            LunaStyle.DrawAlignedHelpMarkerLabel("Reset Temporary Settings"u8,
                "Always reset all temporary settings applied by Glamourer when this automation set is applied, regardless of active designs."u8);
        }

        if (config.ShowAutomationSetEditing)
        {
            Im.Dummy(Vector2.Zero);
            Im.Separator();
            Im.Dummy(Vector2.Zero);

            var flags = config.Ephemeral.IncognitoMode ? InputTextFlags.ReadOnly | InputTextFlags.Password : InputTextFlags.None;
            Im.Item.SetNextWidthScaled(330);
            if (ImEx.InputOnDeactivation.Text("Rename Set##Name"u8, selection.Name, out string newName, default, flags))
                manager.Rename(selection.Index, newName);


            DrawIdentifierSelection(selection.Index);
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
                table.SetupColumn("Use"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Use"u8).X);
        }
        else
        {
            table.SetupColumn("Design / Job Restrictions"u8, TableColumnFlags.WidthFixed,
                250 * Im.Style.GlobalScale - (Im.Scroll.MaximumY > 0 ? Im.Style.ScrollbarSize : 0));
            if (config.ShowAllAutomatedApplicationRules)
                table.SetupColumn("Application"u8, TableColumnFlags.WidthFixed,
                    3 * Im.Style.FrameHeight + 4 * Im.Style.GlobalScale);
            else
                table.SetupColumn("Use"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateSize("Use"u8).X);
        }

        if (singleRow)
            table.SetupColumn("Job Restrictions"u8, TableColumnFlags.WidthStretch);

        if (config.ShowUnlockedItemWarnings)
            table.SetupColumn(""u8, TableColumnFlags.WidthFixed, 2 * Im.Style.FrameHeight + 4 * Im.Style.GlobalScale);

        table.HeaderRow();
        foreach (var (idx, design) in selection.Set!.Designs.Index())
        {
            using var id = Im.Id.Push(idx);
            table.NextColumn();
            var keyValid = config.DeleteDesignModifier.IsActive();
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, "Remove this design from the set."u8, !keyValid))
                _endAction = () => manager.DeleteDesign(selection.Set!, idx);
            if (!keyValid)
                Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {config.DeleteDesignModifier} to remove.");
            table.NextColumn();
            DrawSelectable(idx, design.Design);

            table.NextColumn();
            DrawRandomEditing(selection.Set!, design, idx);
            designCombo.Draw(selection.Set!, design, idx);
            DrawDragDrop(selection.Set!, idx);
            if (singleRow)
            {
                table.NextColumn();
                DrawApplicationTypeBoxes(selection.Set!, design, idx, singleRow);
                table.NextColumn();
                DrawConditions(design, idx);
            }
            else
            {
                DrawConditions(design, idx);
                table.NextColumn();
                DrawApplicationTypeBoxes(selection.Set!, design, idx, singleRow);
            }

            if (config.ShowUnlockedItemWarnings)
            {
                table.NextColumn();
                DrawWarnings(design);
            }
        }

        table.NextColumn();
        table.DrawFrameColumn("New"u8);
        table.NextColumn();
        designCombo.Draw(selection.Set!, null, -1);
        table.NextRow();

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawSelectable(int idx, IDesignStandIn design)
    {
        var highlight = Rgba32.Transparent;
        var sb        = new StringBuilder();
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
            Im.Selectable($"#{idx + 1:D2}");
        }

        Im.Tooltip.OnHover($"{sb}");

        DrawDragDrop(selection.Set!, idx);
    }

    private void DrawConditions(AutoDesign design, int idx)
    {
        var usingGearset = design.GearsetIndex >= 0;
        if (Im.Button(usingGearset ? "Gearset:##usingGearset"u8 : "Jobs:##usingGearset"u8))
        {
            usingGearset = !usingGearset;
            manager.ChangeGearsetCondition(selection.Set!, idx, (short)(usingGearset ? 0 : -1));
        }

        Im.Tooltip.OnHover("Click to switch between Job and Gearset restrictions."u8);

        Im.Line.SameInner();
        if (usingGearset)
        {
            Im.Item.SetNextWidthFull();
            if (ImEx.InputOnDeactivation.Scalar("##whichGearset"u8, design.GearsetIndex + 1, out var newIndex))
                manager.ChangeGearsetCondition(selection.Set!, idx, (short)(Math.Clamp(newIndex, 1, 100) - 1));
        }
        else
        {
            _jobGroupCombo.Draw(selection.Set!, design, idx);
        }
    }

    private void DrawRandomEditing(AutoDesignSet set, AutoDesign design, int designIdx)
    {
        if (design.Design is not RandomDesign)
            return;

        randomDrawer.DrawButton(set, designIdx);
        Im.Line.SameInner();
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

        using var style = ImStyleDouble.ItemSpacing.Push(new Vector2(2 * Im.Style.GlobalScale, 0));

        var tt = config.UnlockedItemMode
            ? "\nThese items will be skipped when applied automatically.\n\nTo change this, disable the Obtained Item Mode setting."
            : string.Empty;
        DrawWarning(sb, config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All equipment to be applied is unlocked."u8);

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
        DrawWarning(sb2, config.UnlockedItemMode ? 0xA03030F0 : 0x0, size, tt, "All customizations to be applied are unlocked."u8);
        Im.Line.Same();
        return;

        static void DrawWarning(StringBuilder sb, Rgba32 color, Vector2 size, string suffix, ReadOnlySpan<byte> good)
        {
            using var style = ImStyleSingle.FrameBorderThickness.Push(Im.Style.GlobalScale);
            if (sb.Length > 0)
            {
                sb.Append(suffix);
                using (AwesomeIcon.Font.Push())
                {
                    ImEx.TextFramed(LunaStyle.WarningIcon.Span, size, color);
                }

                Im.Tooltip.OnHover($"{sb}");
            }
            else
            {
                ImEx.TextFramed(StringU8.Empty, size, Rgba32.Transparent);
                Im.Tooltip.OnHover(good);
            }
        }
    }

    private void DrawDragDrop(AutoDesignSet set, int index)
    {
        using (var target = Im.DragDrop.Target())
        {
            if (target.IsDropping("DesignDragDrop"u8))
            {
                if (_dragIndex >= 0)
                {
                    var idx = _dragIndex;
                    _endAction = () => manager.MoveDesign(set, idx, index);
                }

                _dragIndex = -1;
            }
        }

        using (var source = Im.DragDrop.Source())
        {
            if (source)
            {
                Im.Text($"Moving design #{index + 1:D2}...");
                if (source.SetPayload("DesignDragDrop"u8))
                {
                    _dragIndex                   = index;
                    selection.DraggedDesignIndex = index;
                }
            }
        }
    }

    private void DrawApplicationTypeBoxes(AutoDesignSet set, AutoDesign design, int autoDesignIndex, bool singleLine)
    {
        using var style   = ImStyleDouble.ItemSpacing.Push(new Vector2(2 * Im.Style.GlobalScale));
        var       newType = design.Type;
        using (ImStyleBorder.Frame.Push(ColorId.FolderLine.Value()))
        {
            Im.Checkbox("##all"u8, ref newType, ApplicationType.All);
        }

        style.Pop();
        Im.Tooltip.OnHover("Toggle all application modes at once."u8);
        if (config.ShowAllAutomatedApplicationRules)
        {
            void Box(int idx)
            {
                var (type, description) = ApplicationTypeExtensions.Types[idx];
                using var id    = Im.Id.Push((uint)type);
                var       value = design.Type.HasFlag(type);
                if (Im.Checkbox(StringU8.Empty, ref value))
                    newType = value ? newType | type : newType & ~type;
                Im.Tooltip.OnHover(description);
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
        using var id = Im.Id.Push("Identifiers"u8);
        identifierDrawer.DrawWorld(130);
        Im.Line.Same();
        identifierDrawer.DrawName(200 - Im.Style.ItemSpacing.X);
        identifierDrawer.DrawNpcs(330);
        var buttonWidth = new Vector2(165 * Im.Style.GlobalScale - Im.Style.ItemSpacing.X / 2, 0);
        if (ImEx.Button("Set to Character"u8, buttonWidth, StringU8.Empty, !identifierDrawer.CanSetPlayer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.PlayerIdentifier);
        Im.Line.Same();
        if (ImEx.Button("Set to NPC"u8, buttonWidth, StringU8.Empty, !identifierDrawer.CanSetNpc))
            manager.ChangeIdentifier(setIndex, identifierDrawer.NpcIdentifier);

        if (ImEx.Button("Set to Retainer"u8, buttonWidth, StringU8.Empty, !identifierDrawer.CanSetRetainer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.RetainerIdentifier);
        Im.Line.Same();
        if (ImEx.Button("Set to Mannequin"u8, buttonWidth, StringU8.Empty, !identifierDrawer.CanSetRetainer))
            manager.ChangeIdentifier(setIndex, identifierDrawer.MannequinIdentifier);

        if (ImEx.Button("Set to Owned NPC"u8, buttonWidth, StringU8.Empty, !identifierDrawer.CanSetOwned))
            manager.ChangeIdentifier(setIndex, identifierDrawer.OwnedIdentifier);
    }

    private sealed class JobGroupCombo(AutoDesignManager manager, JobService jobs)
        : SimpleFilterCombo<JobGroup>(SimpleFilterType.Partwise)
    {
        public void Draw(AutoDesignSet set, AutoDesign design, int autoDesignIndex)
        {
            if (Draw("##jobGroups"u8, design.Jobs, "Select for which job groups this design should be applied.\nControl + Right-Click to set to all classes."u8, Im.ContentRegion.Available.X, out var newGroup))
                manager.ChangeJobCondition(set, autoDesignIndex, newGroup);
            else if (Im.Io.KeyControl && Im.Item.RightClicked())
                manager.ChangeJobCondition(set, autoDesignIndex, jobs.JobGroups[1]);
        }

        public override StringU8 DisplayString(in JobGroup value)
            => value.Name;

        public override string FilterString(in JobGroup value)
            => value.Name.ToString();

        public override IEnumerable<JobGroup> GetBaseItems()
            =>jobs.JobGroups.Values;
    }
}
