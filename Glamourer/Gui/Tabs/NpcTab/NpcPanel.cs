using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Gui.Tabs.SettingsTab;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.NpcTab;

public sealed class NpcPanel(
    Configuration config,
    NpcSelection selection,
    CustomizationDrawer customizeDrawer,
    EquipmentDrawer equipmentDrawer,
    ActorObjectManager objects,
    StateManager stateManager,
    LocalNpcAppearanceData favorites,
    DesignColors designColors) : IPanel
{
    private readonly DesignColorCombo _combo = new(designColors, true);

    public ReadOnlySpan<byte> Id
        => "NpcPanel"u8;

    public void Draw()
    {
        using var table = Im.Table.Begin("##Panel"u8, 1, TableFlags.None, Im.ContentRegion.Available);
        if (!table || !selection.HasSelection)
            return;

        table.SetupScrollFreeze(0, 1);
        table.NextColumn();
        Im.Dummy(Vector2.Zero);
        DrawButtonRow();

        table.NextColumn();
        DrawCustomization();
        DrawEquipment();
        DrawAppearanceInfo();
    }

    private void DrawButtonRow()
    {
        DrawApplyToSelf();
        Im.Line.Same();
        DrawApplyToTarget();
    }

    private void DrawCustomization()
    {
        if (config.HideDesignPanel.HasFlag(DesignPanelFlag.Customization))
            return;

        var expand = config.AutoExpandDesignPanel.HasFlag(DesignPanelFlag.Customization);
        using var h = Im.Tree.HeaderId(selection.Data.ModelId is 0
                ? "Customization"u8
                : $"Customization (Model Id #{selection.Data.ModelId})###Customization",
            expand ? TreeNodeFlags.DefaultOpen : TreeNodeFlags.None);
        if (!h)
            return;

        customizeDrawer.Draw(selection.Data.Customize, true, true);
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
    }

    private void DrawEquipment()
    {
        using var h = DesignPanelFlag.Equipment.Header(config);
        if (!h)
            return;

        equipmentDrawer.Prepare();
        var designData = selection.ToDesignData();

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = new EquipDrawData(slot, designData) { Locked = true };
            equipmentDrawer.DrawEquip(data);
        }

        var mainhandData = new EquipDrawData(EquipSlot.MainHand, designData) { Locked = true };
        var offhandData  = new EquipDrawData(EquipSlot.OffHand,  designData) { Locked = true };
        equipmentDrawer.DrawWeapons(mainhandData, offhandData, false);

        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromValue(MetaIndex.VisorState, selection.Data.VisorToggled));
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = objects.PlayerData;
        if (!ImEx.Button("Apply to Yourself"u8, Vector2.Zero,
                "Apply the current NPC appearance to your character.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8,
                !data.Valid))
            return;

        if (stateManager.GetOrCreate(id, data.Objects[0], out var state))
        {
            var design = selection.ToDesignBase();
            stateManager.ApplyDesign(state, design, ApplySettings.Manual with { IsFinal = true });
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current NPC appearance to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8
                : "The current target can not be manipulated."u8
            : "No valid target selected."u8;
        if (!ImEx.Button("Apply to Target"u8, Vector2.Zero, tt, !data.Valid))
            return;

        if (stateManager.GetOrCreate(id, data.Objects[0], out var state))
        {
            var design = selection.ToDesignBase();
            stateManager.ApplyDesign(state, design, ApplySettings.Manual with { IsFinal = true });
        }
    }


    private void DrawAppearanceInfo()
    {
        using var h = DesignPanelFlag.AppearanceDetails.Header(config);
        if (!h)
            return;

        using var table = Im.Table.Begin("Details"u8, 2);
        if (!table)
            return;

        using var style = ImStyleDouble.ButtonTextAlign.Push(new Vector2(0, 0.5f));
        table.SetupColumn("Type"u8, TableColumnFlags.WidthFixed, Im.Font.CalculateButtonSize("Last Update Date"u8).X);
        table.SetupColumn("Data"u8, TableColumnFlags.WidthStretch);

        CopyButton(table, "NPC Name"u8, selection.Name);
        CopyButton(table, "NPC ID"u8,   $"{selection.Data.Id.Id}");
        table.DrawFrameColumn("NPC Type"u8);
        table.NextColumn();
        var width = Im.ContentRegion.Available.X;
        ImEx.TextFramed(selection.Data.Kind is ObjectKind.BattleNpc ? "Battle NPC"u8 : "Event NPC"u8, new Vector2(width, 0),
            ImGuiColor.FrameBackground.Get());

        table.DrawFrameColumn("Color"u8);
        table.NextColumn();
        var color = selection.ColorText;
        if (_combo.Draw("##colorCombo"u8, selection.ColorTextU8,
                "Associate a color with this NPC appearance.\n"u8
              + "Right-Click to revert to automatic coloring.\n"u8
              + "Hold Control and scroll the mousewheel to scroll."u8,
                width - Im.Style.ItemInnerSpacing.X - Im.Style.FrameHeight, out var newColorText))
            favorites.SetColor(selection.Data, newColorText.Item == DesignColors.AutomaticName ? string.Empty : newColorText.Item);

        if (Im.Item.RightClicked())
        {
            favorites.SetColor(selection.Data, string.Empty);
            color = string.Empty;
        }

        if (designColors.TryGetValue(color, out var currentColor))
        {
            Im.Line.SameInner();
            if (DesignColorUi.DrawColorButton($"Color associated with {color}", currentColor, out var newColor))
                designColors.SetColor(color, newColor);
        }
        else if (color.Length is not 0)
        {
            Im.Line.SameInner();
            var size = new Vector2(Im.Style.FrameHeight);
            using (AwesomeIcon.Font.Push())
            {
                ImEx.TextFramed(LunaStyle.WarningIcon.Span, size, designColors.MissingColor);
            }

            Im.Tooltip.OnHover("The color associated with this design does not exist."u8);
        }

        return;

        static void CopyButton(in Im.TableDisposable table, ReadOnlySpan<byte> label, Utf8StringHandler<HintStringHandlerBuffer> text)
        {
            table.DrawFrameColumn(label);
            table.NextColumn();
            if (!text.GetSpan(out var span))
                return;

            if (Im.Button(span, Im.ContentRegion.Available with { Y = 0 }))
                Im.Clipboard.Set(span);
            Im.Tooltip.OnHover("Click to copy to clipboard."u8);
        }
    }
}
