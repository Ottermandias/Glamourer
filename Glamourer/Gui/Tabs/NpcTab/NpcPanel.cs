using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using static Glamourer.Gui.Tabs.HeaderDrawer;

namespace Glamourer.Gui.Tabs.NpcTab;

public class NpcPanel
{
    private readonly DesignColorCombo       _colorCombo;
    private          string                 _newName = string.Empty;
    private          DesignBase?            _newDesign;
    private readonly NpcSelector            _selector;
    private readonly LocalNpcAppearanceData _favorites;
    private readonly CustomizationDrawer    _customizeDrawer;
    private readonly EquipmentDrawer        _equipDrawer;
    private readonly DesignConverter        _converter;
    private readonly DesignManager          _designManager;
    private readonly StateManager           _state;
    private readonly ObjectManager          _objects;
    private readonly DesignColors           _colors;
    private readonly Button[]              _leftButtons;
    private readonly Button[]              _rightButtons;

    public NpcPanel(NpcSelector selector,
        LocalNpcAppearanceData favorites,
        CustomizationDrawer customizeDrawer,
        EquipmentDrawer equipDrawer,
        DesignConverter converter,
        DesignManager designManager,
        StateManager state,
        ObjectManager objects,
        DesignColors colors)
    {
        _selector        = selector;
        _favorites       = favorites;
        _customizeDrawer = customizeDrawer;
        _equipDrawer     = equipDrawer;
        _converter       = converter;
        _designManager   = designManager;
        _state           = state;
        _objects         = objects;
        _colors          = colors;
        _colorCombo      = new DesignColorCombo(colors, true);
        _leftButtons =
        [
            new ExportToClipboardButton(this),
            new SaveAsDesignButton(this),
        ];
        _rightButtons =
        [
            new FavoriteButton(this),
        ];
    }

    public void Draw()
    {
        using var group = ImRaii.Group();

        DrawHeader();
        DrawPanel();
    }

    private void DrawHeader()
    {
        HeaderDrawer.Draw(_selector.HasSelection ? _selector.Selection.Name : "No Selection", ColorId.NormalDesign.Value(),
            ImGui.GetColorU32(ImGuiCol.FrameBg), _leftButtons, _rightButtons);
        SaveDesignDrawPopup();
    }

    private sealed class FavoriteButton(NpcPanel panel) : Button
    {
        protected override string Description
            => panel._favorites.IsFavorite(panel._selector.Selection)
                ? "Remove this NPC appearance from your favorites."
                : "Add this NPC Appearance to your favorites.";

        protected override uint TextColor
            => panel._favorites.IsFavorite(panel._selector.Selection)
                ? ColorId.FavoriteStarOn.Value()
                : 0x80000000;

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Star;

        public override bool Visible
            => panel._selector.HasSelection;

        protected override void OnClick()
            => panel._favorites.ToggleFavorite(panel._selector.Selection);
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
            var design = _converter.Convert(ToDesignData(), new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
            _state.ApplyDesign(state, design, ApplySettings.Manual);
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
            var design = _converter.Convert(ToDesignData(), new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
            _state.ApplyDesign(state, design, ApplySettings.Manual);
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
                "Associate a color with this NPC appearance.\n"
              + "Right-Click to revert to automatic coloring.\n"
              + "Hold Control and scroll the mousewheel to scroll.",
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

    private sealed class ExportToClipboardButton(NpcPanel panel) : Button
    {
        protected override string Description
            => "Copy the current NPCs appearance to your clipboard.\nHold Control to disable applying of customizations for the copied design.\nHold Shift to disable applying of gear for the copied design.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Copy;

        public override bool Visible
            => panel._selector.HasSelection;

        protected override void OnClick()
        {
            try
            {
                var data = panel.ToDesignData();
                var text = panel._converter.ShareBase64(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
                ImGui.SetClipboardText(text);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not copy {panel._selector.Selection.Name}'s data to clipboard.",
                    $"Could not copy data from NPC appearance {panel._selector.Selection.Kind} {panel._selector.Selection.Id.Id} to clipboard",
                    NotificationType.Error);
            }
        }
    }

    private sealed class SaveAsDesignButton(NpcPanel panel) : Button
    {
        protected override string Description
            => "Save this NPCs appearance as a design.\nHold Control to disable applying of customizations for the saved design.\nHold Shift to disable applying of gear for the saved design.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Save;

        public override bool Visible
            => panel._selector.HasSelection;

        protected override void OnClick()
        {
            ImGui.OpenPopup("Save as Design");
            panel._newName = panel._selector.Selection.Name;
            var data = panel.ToDesignData();
            panel._newDesign = panel._converter.Convert(data, new StateMaterialManager(), ApplicationRules.NpcFromModifiers());
        }
    }
}
