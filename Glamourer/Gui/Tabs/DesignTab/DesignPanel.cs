using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Glamourer.Api.Enums;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.GameData;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Materials;
using Glamourer.Interop;
using Glamourer.State;
using Dalamud.Bindings.ImGui;
using ImSharp;
using Luna;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using static Glamourer.Gui.Tabs.HeaderDrawer;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel : IPanel
{
    private readonly FileDialogManager        _fileDialog = new();
    private readonly DesignSelection          _selection;
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly DesignManager            _manager;
    private readonly ActorObjectManager       _objects;
    private readonly StateManager             _state;
    private readonly EquipmentDrawer          _equipmentDrawer;
    private readonly ModAssociationsTab       _modAssociations;
    private readonly Configuration            _config;
    private readonly DesignDetailTab          _designDetails;
    private readonly ImportService            _importService;
    private readonly DesignConverter          _converter;
    private readonly MultiDesignPanel         _multiDesignPanel;
    private readonly CustomizeParameterDrawer _parameterDrawer;
    private readonly DesignLinkDrawer         _designLinkDrawer;
    private readonly MaterialDrawer           _materials;
    private readonly EditorHistory            _history;
    private readonly Button[]                 _leftButtons;
    private readonly Button[]                 _rightButtons;


    public DesignPanel(CustomizationDrawer customizationDrawer,
        DesignManager manager,
        ActorObjectManager objects,
        StateManager state,
        EquipmentDrawer equipmentDrawer,
        ModAssociationsTab modAssociations,
        Configuration config,
        DesignDetailTab designDetails,
        DesignConverter converter,
        ImportService importService,
        MultiDesignPanel multiDesignPanel,
        CustomizeParameterDrawer parameterDrawer,
        DesignLinkDrawer designLinkDrawer,
        MaterialDrawer materials,
        EditorHistory history, DesignSelection selection)
    {
        _customizationDrawer = customizationDrawer;
        _manager             = manager;
        _objects             = objects;
        _state               = state;
        _equipmentDrawer     = equipmentDrawer;
        _modAssociations     = modAssociations;
        _config              = config;
        _designDetails       = designDetails;
        _importService       = importService;
        _converter           = converter;
        _multiDesignPanel    = multiDesignPanel;
        _parameterDrawer     = parameterDrawer;
        _designLinkDrawer    = designLinkDrawer;
        _materials           = materials;
        _history             = history;
        _selection      = selection;
        _leftButtons =
        [
            new SetFromClipboardButton(this),
            new DesignUndoButton(this),
            new ExportToClipboardButton(this),
            new ApplyCharacterButton(this),
            new UndoButton(this),
        ];
        _rightButtons =
        [
            new LockButton(this),
            //new IncognitoButton(_config),
        ];
    }

    private void DrawHeader()
        => HeaderDrawer.Draw(SelectionName, 0, ImGuiColor.FrameBackground.Get().Color, _leftButtons, _rightButtons);

    private string SelectionName
        => _selection.Design == null ? "No Selection" : _config.Ephemeral.IncognitoMode ? _selection.Design.Incognito : _selection.Design.Name.Text;

    private void DrawEquipment()
    {
        using var h = DesignPanelFlag.Equipment.Header(_config);
        if (!h)
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _selection.Design!.WriteProtected());
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromDesign(_manager, _selection.Design!, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _manager.ChangeStains(_selection.Design, slot, newAllStain);
        }

        var mainhand = EquipDrawData.FromDesign(_manager, _selection.Design!, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromDesign(_manager, _selection.Design!, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, true);

        foreach (var slot in BonusExtensions.AllFlags)
        {
            var data = BonusDrawData.FromDesign(_manager, _selection.Design!, slot);
            _equipmentDrawer.DrawBonusItem(data);
        }

        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        DrawEquipmentMetaToggles();
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        _equipmentDrawer.DrawDragDropTooltip();
    }

    private void DrawEquipmentMetaToggles()
    {
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.HatState, _manager, _selection.Design!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Head, _manager, _selection.Design!));
        }

        Im.Line.Same();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.VisorState, _manager, _selection.Design!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Body, _manager, _selection.Design!));
        }

        Im.Line.Same();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.WeaponState, _manager, _selection.Design!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.OffHand, _manager, _selection.Design!));
        }

        Im.Line.Same();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.EarState, _manager, _selection.Design!));
        }
    }

    private void DrawCustomize()
    {
        if (_config.HideDesignPanel.HasFlag(DesignPanelFlag.Customization))
            return;

        var expand = _config.AutoExpandDesignPanel.HasFlag(DesignPanelFlag.Customization);
        using var h = Im.Tree.HeaderId(_selection.Design!.DesignData.ModelId is 0
                ? "Customization"
                : $"Customization (Model Id #{_selection.Design!.DesignData.ModelId})###Customization",
            expand ? TreeNodeFlags.DefaultOpen : TreeNodeFlags.None);
        if (!h)
            return;

        if (_customizationDrawer.Draw(_selection.Design!.DesignData.Customize, _selection.Design.Application.Customize,
                _selection.Design!.WriteProtected(), false))
            foreach (var idx in CustomizeIndex.Values)
            {
                var flag     = idx.ToFlag();
                var newValue = _customizationDrawer.ChangeApply.HasFlag(flag);
                _manager.ChangeApplyCustomize(_selection.Design, idx, newValue);
                if (_customizationDrawer.Changed.HasFlag(flag))
                    _manager.ChangeCustomize(_selection.Design, idx, _customizationDrawer.Customize[idx]);
            }

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.Wetness, _manager, _selection.Design!));
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
    }

    private void DrawCustomizeParameters()
    {
        using var h = DesignPanelFlag.AdvancedCustomizations.Header(_config);
        if (!h)
            return;

        _parameterDrawer.Draw(_manager, _selection.Design!);
    }

    private void DrawMaterialValues()
    {
        using var h = DesignPanelFlag.AdvancedDyes.Header(_config);
        if (!h)
            return;

        _materials.Draw(_selection.Design!);
    }

    private void DrawCustomizeApplication()
    {
        using var id        = ImUtf8.PushId("Customizations"u8);
        var       set       = _selection.Design!.CustomizeSet;
        var       available = set.SettingAvailable | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.BodyType;
        var flags = _selection.Design!.ApplyCustomizeExcludingBodyType == 0 ? 0 :
            (_selection.Design!.ApplyCustomize & available) == available    ? 3 : 1;
        if (ImGui.CheckboxFlags("Apply All Customizations", ref flags, 3))
        {
            var newFlags = flags == 3;
            _manager.ChangeApplyCustomize(_selection.Design!, CustomizeIndex.Clan,   newFlags);
            _manager.ChangeApplyCustomize(_selection.Design!, CustomizeIndex.Gender, newFlags);
            foreach (var index in CustomizationExtensions.AllBasic)
                _manager.ChangeApplyCustomize(_selection.Design!, index, newFlags);
        }

        var applyClan = _selection.Design!.DoApplyCustomize(CustomizeIndex.Clan);
        if (ImUtf8.Checkbox($"Apply {CustomizeIndex.Clan.ToNameU8()}", ref applyClan))
            _manager.ChangeApplyCustomize(_selection.Design!, CustomizeIndex.Clan, applyClan);

        var applyGender = _selection.Design!.DoApplyCustomize(CustomizeIndex.Gender);
        if (ImUtf8.Checkbox($"Apply {CustomizeIndex.Gender.ToNameU8()}", ref applyGender))
            _manager.ChangeApplyCustomize(_selection.Design!, CustomizeIndex.Gender, applyGender);


        foreach (var index in CustomizationExtensions.All.Where(set.IsAvailable))
        {
            var apply = _selection.Design!.DoApplyCustomize(index);
            if (ImUtf8.Checkbox($"Apply {set.Option(index)}", ref apply))
                _manager.ChangeApplyCustomize(_selection.Design!, index, apply);
        }
    }

    private void DrawCrestApplication()
    {
        using var id        = ImUtf8.PushId("Crests"u8);
        var       flags     = (uint)_selection.Design!.Application.Crest;
        var       bigChange = ImGui.CheckboxFlags("Apply All Crests", ref flags, (uint)CrestExtensions.AllRelevant);
        foreach (var flag in CrestExtensions.AllRelevantSet)
        {
            var apply = bigChange ? ((CrestFlag)flags & flag) == flag : _selection.Design!.DoApplyCrest(flag);
            if (ImUtf8.Checkbox($"Apply {flag.ToLabel()} Crest", ref apply) || bigChange)
                _manager.ChangeApplyCrest(_selection.Design!, flag, apply);
        }
    }

    private void DrawApplicationRules()
    {
        using var h = DesignPanelFlag.ApplicationRules.Header(_config);
        if (!h)
            return;

        using var disabled = Im.Disabled(_selection.Design!.WriteProtected());

        DrawAllButtons();

        using (var _ = ImUtf8.Group())
        {
            DrawCustomizeApplication();
            ImUtf8.IconDummy();
            DrawCrestApplication();
            ImUtf8.IconDummy();
            DrawMetaApplication();
        }

        ImGui.SameLine(210 * Im.Style.GlobalScale + Im.Style.ItemSpacing.X);
        using (var _ = ImRaii.Group())
        {
            void ApplyEquip(string label, EquipFlag allFlags, bool stain, IEnumerable<EquipSlot> slots)
            {
                var       flags     = (uint)(allFlags & _selection.Design!.Application.Equip);
                using var id        = ImUtf8.PushId(label);
                var       bigChange = ImGui.CheckboxFlags($"Apply All {label}", ref flags, (uint)allFlags);
                if (stain)
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToStainFlag()) : _selection.Design!.DoApplyStain(slot);
                        if (ImUtf8.Checkbox($"Apply {slot.ToName()} Dye", ref apply) || bigChange)
                            _manager.ChangeApplyStains(_selection.Design!, slot, apply);
                    }
                else
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToFlag()) : _selection.Design!.DoApplyEquip(slot);
                        if (ImUtf8.Checkbox($"Apply {slot.ToName()}", ref apply) || bigChange)
                            _manager.ChangeApplyItem(_selection.Design!, slot, apply);
                    }
            }

            ApplyEquip("Weapons", ApplicationTypeExtensions.WeaponFlags, false, new[]
            {
                EquipSlot.MainHand,
                EquipSlot.OffHand,
            });

            ImUtf8.IconDummy();
            ApplyEquip("Armor", ApplicationTypeExtensions.ArmorFlags, false, EquipSlotExtensions.EquipmentSlots);

            ImUtf8.IconDummy();
            ApplyEquip("Accessories", ApplicationTypeExtensions.AccessoryFlags, false, EquipSlotExtensions.AccessorySlots);

            ImUtf8.IconDummy();
            ApplyEquip("Dyes", ApplicationTypeExtensions.StainFlags, true,
                EquipSlotExtensions.FullSlots);

            ImUtf8.IconDummy();
            DrawParameterApplication();

            ImUtf8.IconDummy();
            DrawBonusSlotApplication();
        }
    }

    private void DrawAllButtons()
    {
        var   enabled   = _config.DeleteDesignModifier.IsActive();
        bool? equip     = null;
        bool? customize = null;
        var   size      = new Vector2(210 * Im.Style.GlobalScale, 0);
        if (ImUtf8.ButtonEx("Disable Everything"u8,
                "Disable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness."u8, size,
                !enabled))
        {
            equip     = false;
            customize = false;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Enable Everything"u8,
                "Enable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness."u8, size,
                !enabled))
        {
            equip     = true;
            customize = true;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (ImUtf8.ButtonEx("Equipment Only"u8,
                "Enable application of anything related to gear, disable anything that is not related to gear."u8, size,
                !enabled))
        {
            equip     = true;
            customize = false;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Customization Only"u8,
                "Enable application of anything related to customization, disable anything that is not related to customization."u8, size,
                !enabled))
        {
            equip     = false;
            customize = true;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (ImUtf8.ButtonEx("Default Application"u8,
                "Set the application rules to the default values as if the design was newly created, without any advanced features or wetness."u8,
                size,
                !enabled))
        {
            _manager.ChangeApplyMulti(_selection.Design!, true, true, true, false, true, true, false, true);
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.Wetness, false);
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImUtf8.ButtonEx("Disable Advanced"u8, "Disable all advanced dyes and customizations but keep everything else as is."u8,
                size,
                !enabled))
            _manager.ChangeApplyMulti(_selection.Design!, null, null, null, false, null, null, false, null);

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (equip is null && customize is null)
            return;

        _manager.ChangeApplyMulti(_selection.Design!, equip, customize, equip, customize.HasValue && !customize.Value ? false : null, null,
            equip, equip, equip);
        if (equip.HasValue)
        {
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.HatState,    equip.Value);
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.VisorState,  equip.Value);
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.WeaponState, equip.Value);
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.EarState,    equip.Value);
        }

        if (customize.HasValue)
            _manager.ChangeApplyMeta(_selection.Design!, MetaIndex.Wetness, customize.Value);
    }

    private static readonly IReadOnlyList<string> MetaLabels =
    [
        "Apply Wetness",
        "Apply Hat Visibility",
        "Apply Visor State",
        "Apply Weapon Visibility",
        "Apply Viera Ear Visibility",
    ];

    private void DrawMetaApplication()
    {
        using var  id        = ImUtf8.PushId("Meta");
        const uint all       = (uint)MetaExtensions.All;
        var        flags     = (uint)_selection.Design!.Application.Meta;
        var        bigChange = ImGui.CheckboxFlags("Apply All Meta Changes", ref flags, all);

        foreach (var (index, label) in MetaExtensions.AllRelevant.Zip(MetaLabels))
        {
            var apply = bigChange ? ((MetaFlag)flags).HasFlag(index.ToFlag()) : _selection.Design!.DoApplyMeta(index);
            if (ImUtf8.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selection.Design!, index, apply);
        }
    }

    private static readonly IReadOnlyList<string> BonusSlotLabels =
    [
        "Apply Facewear",
    ];

    private void DrawBonusSlotApplication()
    {
        using var id        = ImUtf8.PushId("Bonus"u8);
        var       flags     = _selection.Design!.Application.BonusItem;
        var       bigChange = BonusExtensions.AllFlags.Count > 1 && ImUtf8.Checkbox("Apply All Bonus Slots"u8, ref flags, BonusExtensions.All);
        foreach (var (index, label) in BonusExtensions.AllFlags.Zip(BonusSlotLabels))
        {
            var apply = bigChange ? flags.HasFlag(index) : _selection.Design!.DoApplyBonusItem(index);
            if (ImUtf8.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyBonusItem(_selection.Design!, index, apply);
        }
    }


    private void DrawParameterApplication()
    {
        using var id        = Im.Id.Push("Parameter"u8);
        var       flags     = (ulong)_selection.Design!.Application.Parameters;
        var       bigChange = Im.Checkbox("Apply All Customize Parameters"u8, ref flags, (ulong)CustomizeParameterExtensions.All);
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            var apply = bigChange ? ((CustomizeParameterFlag)flags).HasFlag(flag) : _selection.Design!.DoApplyParameter(flag);
            if (Im.Checkbox($"Apply {flag.ToNameU8()}", ref apply) || bigChange)
                _manager.ChangeApplyParameter(_selection.Design!, flag, apply);
        }
    }

    public ReadOnlySpan<byte> Id
        => "Designs"u8;

    public void Draw()
    {
        using var group = ImUtf8.Group();
        if (_selection.DesignPaths.Count > 1)
        {
            _multiDesignPanel.Draw();
        }
        else
        {
            DrawHeader();
            DrawPanel();

            if (_selection.Design == null || _selection.Design.WriteProtected())
                return;

            if (_importService.CreateDatTarget(out var dat))
            {
                _manager.ChangeCustomize(_selection.Design!, CustomizeIndex.Clan,   dat.Customize[CustomizeIndex.Clan]);
                _manager.ChangeCustomize(_selection.Design!, CustomizeIndex.Gender, dat.Customize[CustomizeIndex.Gender]);
                foreach (var idx in CustomizationExtensions.AllBasic)
                    _manager.ChangeCustomize(_selection.Design!, idx, dat.Customize[idx]);
                Glamourer.Messager.NotificationMessage(
                    $"Applied games .dat file {dat.Description} customizations to {_selection.Design.Name}.", NotificationType.Success, false);
            }
            else if (_importService.CreateCharaTarget(out var designBase, out var name))
            {
                _manager.ApplyDesign(_selection.Design!, designBase);
                Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_selection.Design.Name}.",
                    NotificationType.Success, false);
            }
        }

        _importService.CreateDatSource();
    }

    private void DrawPanel()
    {
        using var table = Im.Table.Begin("##Panel"u8, 1, TableFlags.BordersOuter | TableFlags.ScrollY, Im.ContentRegion.Available);
        if (!table || _selection.Design is null)
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableNextColumn();
        if (_selection.Design is null)
            return;

        Im.Dummy(Vector2.Zero);
        DrawButtonRow();
        ImGui.TableNextColumn();

        DrawCustomize();
        DrawEquipment();
        DrawCustomizeParameters();
        DrawMaterialValues();
        _designDetails.Draw();
        DrawApplicationRules();
        _modAssociations.Draw();
        _designLinkDrawer.Draw();
    }

    private void DrawButtonRow()
    {
        DrawApplyToSelf();
        Im.Line.Same();
        DrawApplyToTarget();
        Im.Line.Same();
        _modAssociations.DrawApplyButton();
        Im.Line.Same();
        DrawSaveToDat();
    }


    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        if (!ImGuiUtil.DrawDisabledButton("Apply to Yourself", Vector2.Zero,
                "Apply the current design with its settings to your character.\nHold Control to only apply gear.\nHold Shift to only apply customizations.",
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            using var _ = _selection.Design!.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, _selection.Design!, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current design with its settings to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."
                : "The current target can not be manipulated."
            : "No valid target selected.";
        if (!ImGuiUtil.DrawDisabledButton("Apply to Target", Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            using var _ = _selection.Design!.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, _selection.Design!, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
    }

    private void DrawSaveToDat()
    {
        var verified = _importService.Verify(_selection.Design!.DesignData.Customize, out _);
        var tt = verified
            ? "Export the currently configured customizations of this design to a character creation data file."
            : "The current design contains customizations that can not be applied during character creation.";
        var startPath = GetUserPath();
        if (startPath.Length == 0)
            startPath = null;
        if (ImGuiUtil.DrawDisabledButton("Export to Dat", Vector2.Zero, tt, !verified))
            _fileDialog.SaveFileDialog("Save File...", ".dat", "FFXIV_CHARA_01.dat", ".dat", (v, path) =>
            {
                if (v && _selection.Design != null)
                    _importService.SaveDesignAsDat(path, _selection.Design!.DesignData.Customize, _selection.Design!.Name);
            }, startPath);

        _fileDialog.Draw();
    }

    private static unsafe string GetUserPath()
        => Framework.Instance()->UserPathString;

}

private sealed class LockButton(DesignPanel panel) : Button
{
    public override bool Visible
        => panel._selection.Design != null;

    protected override string Description
        => panel._selection.Design!.WriteProtected()
            ? "Make this design editable."
            : "Write-protect this design.";

    protected override FontAwesomeIcon Icon
        => panel._selection.Design!.WriteProtected()
            ? FontAwesomeIcon.Lock
            : FontAwesomeIcon.LockOpen;

    protected override void OnClick()
        => panel._manager.SetWriteProtection(panel._selection.Design!, !panel._selection.Design!.WriteProtected());
}

private sealed class SetFromClipboardButton(DesignPanel panel) : Button
{
    public override bool Visible
        => panel._selection.Design != null;

    protected override bool Disabled
        => panel._selection.Design?.WriteProtected() ?? true;

    protected override string Description
        => "Try to apply a design from your clipboard over this design.\nHold Control to only apply gear.\nHold Shift to only apply customizations.";

    protected override FontAwesomeIcon Icon
        => FontAwesomeIcon.Clipboard;

    protected override void OnClick()
    {
        try
        {
            var text = ImGui.GetClipboardText();
            var (applyEquip, applyCustomize) = UiHelpers.ConvertKeysToBool();
            var design = panel._converter.FromBase64(text, applyCustomize, applyEquip, out _)
             ?? throw new Exception("The clipboard did not contain valid data.");
            panel._manager.ApplyDesign(panel._selection.Design!, design);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {panel._selection.Design!.Name}.",
                $"Could not apply clipboard to design {panel._selection.Design!.Identifier}", NotificationType.Error, false);
        }
    }
}

private sealed class DesignUndoButton(DesignPanel panel) : Button
{
    public override bool Visible
        => panel._selection.Design != null;

    protected override bool Disabled
        => !panel._manager.CanUndo(panel._selection.Design) || (panel._selection.Design?.WriteProtected() ?? true);

    protected override string Description
        => "Undo the last time you applied an entire design onto this design, if you accidentally overwrote your design with a different one.";

    protected override FontAwesomeIcon Icon
        => FontAwesomeIcon.SyncAlt;

    protected override void OnClick()
    {
        try
        {
            panel._manager.UndoDesignChange(panel._selection.Design!);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not undo last changes to {panel._selection.Design!.Name}.",
                NotificationType.Error,
                false);
        }
    }
}

private sealed class ExportToClipboardButton(DesignPanel panel) : Button
{
    public override bool Visible
        => panel._selection.Design != null;

    protected override string Description
        => "Copy the current design to your clipboard.";

    protected override FontAwesomeIcon Icon
        => FontAwesomeIcon.Copy;

    protected override void OnClick()
    {
        try
        {
            var text = panel._converter.ShareBase64(panel._selection.Design!);
            ImGui.SetClipboardText(text);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not copy {panel._selection.Design!.Name} data to clipboard.",
                $"Could not copy data from design {panel._selection.Design!.Identifier} to clipboard", NotificationType.Error, false);
        }
    }
}

private sealed class ApplyCharacterButton(DesignPanel panel) : Button
{
    public override bool Visible
        => panel._selection.Design != null && panel._objects.Player.Valid;

    protected override string Description
        => "Overwrite this design with your character's current state.";

    protected override bool Disabled
        => panel._selection.Design?.WriteProtected() ?? true;

    protected override FontAwesomeIcon Icon
        => FontAwesomeIcon.UserEdit;

    protected override void OnClick()
    {
        try
        {
            var (player, actor) = panel._objects.PlayerData;
            if (!player.IsValid || !actor.Valid || !panel._state.GetOrCreate(player, actor.Objects[0], out var state))
                throw new Exception("No player state available.");

            var design = panel._converter.Convert(state, ApplicationRules.FromModifiers(state))
             ?? throw new Exception("The clipboard did not contain valid data.");
            panel._selection.Design!.GetMaterialDataRef().Clear();
            panel._manager.ApplyDesign(panel._selection.Design!, design);
        }
        catch (Exception ex)
        {
            Glamourer.Messager.NotificationMessage(ex, $"Could not apply player state to {panel._selection.Design!.Name}.",
                $"Could not apply player state to design {panel._selection.Design!.Identifier}", NotificationType.Error, false);
        }
    }
}

private sealed class UndoButton(DesignPanel panel) : Button
{
    protected override string Description
        => "Undo the last change.";

    protected override FontAwesomeIcon Icon
        => FontAwesomeIcon.Undo;

    public override bool Visible
        => panel._selection.Design != null;

    protected override bool Disabled
        => (panel._selection.Design?.WriteProtected() ?? true) || !panel._history.CanUndo(panel._selection.Design);

    protected override void OnClick()
        => panel._history.Undo(panel._selection.Design!);
}