using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using Lumina.Data.Parsing.Scd;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.NpcTab;

public class NpcPanel(
    NpcSelector _selector,
    LocalNpcAppearanceData _favorites,
    CustomizationDrawer _customizeDrawer,
    EquipmentDrawer _equipDrawer,
    DesignConverter _converter,
    DesignManager _designManager,
    StateManager _state,
    ObjectManager _objects,
    DesignColors _colors)
{
    private readonly DesignColorCombo _colorCombo = new(_colors, true);
    private          string           _newName    = string.Empty;
    private          DesignBase?      _newDesign;

    public void Draw()
    {
        using var group = ImRaii.Group();

        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        HeaderDrawer.Draw(_selector.HasSelection ? _selector.Selection.Name : "No Selection", ColorId.NormalDesign.Value(),
            ImGui.GetColorU32(ImGuiCol.FrameBg), 2, ExportToClipboardButton(), SaveAsDesignButton(), FavoriteButton());
        SaveDesignDrawPopup();
    }

    private HeaderDrawer.Button FavoriteButton()
    {
        var (desc, color) = _favorites.IsFavorite(_selector.Selection)
            ? ("Remove this NPC appearance from your favorites.", ColorId.FavoriteStarOn.Value())
            : ("Add this NPC Appearance to your favorites.", 0x80000000);
        return new HeaderDrawer.Button
        {
            Icon        = FontAwesomeIcon.Star,
            OnClick     = () => _favorites.ToggleFavorite(_selector.Selection),
            Visible     = _selector.HasSelection,
            Description = desc,
            TextColor   = color,
        };
    }

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description =
                "Copy the current NPCs appearance to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design.",
            Icon    = FontAwesomeIcon.Copy,
            OnClick = ExportToClipboard,
            Visible = _selector.HasSelection,
        };

    private HeaderDrawer.Button SaveAsDesignButton()
        => new()
        {
            Description =
                "Save this NPCs appearance as a design.\nHold Control to disable applying of customizations for the saved design.\nHold Shift to disable applying of gear for the saved design.",
            Icon    = FontAwesomeIcon.Save,
            OnClick = SaveDesignOpen,
            Visible = _selector.HasSelection,
        };

    private void ExportToClipboard()
    {
        try
        {
            var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();
            var data = ToDesignData();
            var text = _converter.ShareBase64(data, applyGear, applyCustomize, applyCrest, applyParameters);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {_selector.Selection.Name}'s data to clipboard.",
                $"Could not copy data from NPC appearance {_selector.Selection.Kind} {_selector.Selection.Id.Id} to clipboard",
                NotificationType.Error);
        }
    }

    private void SaveDesignOpen()
    {
        ImGui.OpenPopup("Save as Design");
        _newName                                                     = _selector.Selection.Name;
        var (applyGear, applyCustomize, applyCrest, applyParameters) = UiHelpers.ConvertKeysToFlags();

        var data = ToDesignData();
        _newDesign = _converter.Convert(data, applyGear, applyCustomize, applyCrest, applyParameters);
    }

    private void SaveDesignDrawPopup()
    {
        if (!ImGuiUtil.OpenNameField("Save as Design", ref _newName))
            return;

        if (_newDesign != null && _newName.Length > 0)
            _designManager.CreateClone(_newDesign, _newName, true);
        _newDesign = null;
        _newName   = string.Empty;
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || !_selector.HasSelection)
            return;

        DrawButtonRow();
        DrawCustomization();
        DrawEquipment();
        DrawAppearanceInfo();
    }

    private void DrawButtonRow()
    {
        DrawApplyToSelf();
        ImGui.SameLine();
        DrawApplyToTarget();
    }

    private void DrawCustomization()
    {
        var header = _selector.Selection.ModelId == 0
            ? "Customization"
            : $"Customization (Model Id #{_selector.Selection.ModelId})###Customization";
        using var h = ImRaii.CollapsingHeader(header);
        if (!h)
            return;

        _customizeDrawer.Draw(_selector.Selection.Customize, true, true);
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawEquipment()
    {
        using var h = ImRaii.CollapsingHeader("Equipment");
        if (!h)
            return;

        _equipDrawer.Prepare();
        var designData = ToDesignData();

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = new EquipDrawData(slot, designData) { Locked = true };
            _equipDrawer.DrawEquip(data);
        }

        var mainhandData = new EquipDrawData(EquipSlot.MainHand, designData) { Locked = true };
        var offhandData  = new EquipDrawData(EquipSlot.OffHand,  designData) { Locked = true };
        _equipDrawer.DrawWeapons(mainhandData, offhandData, false);

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromValue(MetaIndex.VisorState, _selector.Selection.VisorToggled));
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private DesignData ToDesignData()
    {
        var selection  = _selector.Selection;
        var items      = _converter.FromDrawData(selection.Equip.ToArray(), selection.Mainhand, selection.Offhand, true).ToArray();
        var designData = new DesignData { Customize = selection.Customize };
        foreach (var (slot, item, stain) in items)
        {
            designData.SetItem(slot, item);
            designData.SetStain(slot, stain);
        }

        return designData;
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero,
                "Apply the current NPC appearance to your character.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var (applyGear, applyCustomize, _, _) = UiHelpers.ConvertKeysToFlags();
            var design = _converter.Convert(ToDesignData(), applyGear, applyCustomize, 0, 0);
            _state.ApplyDesign(design, state, StateSource.Manual);
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current NPC appearance to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            var (applyGear, applyCustomize, _, _) = UiHelpers.ConvertKeysToFlags();
            var design = _converter.Convert(ToDesignData(), applyGear, applyCustomize, 0, 0);
            _state.ApplyDesign(design, state, StateSource.Manual);
        }
    }


    private void DrawAppearanceInfo()
    {
        using var h = ImRaii.CollapsingHeader("Appearance Details");
        if (!h)
            return;

        using var table = ImRaii.Table("Details", 2);
        if (!table)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Last Update Datem").X);
        ImGui.TableSetupColumn("Data", ImGuiTableColumnFlags.WidthStretch);

        var selection = _selector.Selection;
        CopyButton("NPC Name", selection.Name);
        CopyButton("NPC ID",   selection.Id.Id.ToString());
        ImGuiUtil.DrawFrameColumn("NPC Type");
        ImGui.TableNextColumn();
        var width = ImGui.GetContentRegionAvail().X;
        ImGuiUtil.DrawTextButton(selection.Kind is ObjectKind.BattleNpc ? "Battle NPC" : "Event NPC", new Vector2(width, 0),
            ImGui.GetColorU32(ImGuiCol.FrameBg));

        ImGuiUtil.DrawFrameColumn("Color");
        var color     = _favorites.GetColor(selection);
        var colorName = color.Length == 0 ? DesignColors.AutomaticName : color;
        ImGui.TableNextColumn();
        if (_colorCombo.Draw("##colorCombo", colorName,
                "Associate a color with this NPC appearance. Right-Click to revert to automatic coloring.",
                width - ImGui.GetStyle().ItemSpacing.X - ImGui.GetFrameHeight(), ImGui.GetTextLineHeight())
         && _colorCombo.CurrentSelection != null)
        {
            color = _colorCombo.CurrentSelection is DesignColors.AutomaticName ? string.Empty : _colorCombo.CurrentSelection;
            _favorites.SetColor(selection, color);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _favorites.SetColor(selection, string.Empty);
            color = string.Empty;
        }

        if (_colors.TryGetValue(color, out var currentColor))
        {
            ImGui.SameLine();
            if (DesignColorUi.DrawColorButton($"Color associated with {color}", currentColor, out var newColor))
                _colors.SetColor(color, newColor);
        }
        else if (color.Length != 0)
        {
            ImGui.SameLine();
            var       size = new Vector2(ImGui.GetFrameHeight());
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            ImGuiUtil.DrawTextButton(FontAwesomeIcon.ExclamationCircle.ToIconString(), size, 0, _colors.MissingColor);
            ImGuiUtil.HoverTooltip("The color associated with this design does not exist.");
        }

        return;

        static void CopyButton(string label, string text)
        {
            ImGuiUtil.DrawFrameColumn(label);
            ImGui.TableNextColumn();
            if (ImGui.Button(text, new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                ImGui.SetClipboardText(text);
            ImGuiUtil.HoverTooltip("Click to copy to clipboard.");
        }
    }
}
