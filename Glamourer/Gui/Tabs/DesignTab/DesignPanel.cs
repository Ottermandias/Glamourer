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
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using static Glamourer.Gui.Tabs.HeaderDrawer;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel
{
    private readonly FileDialogManager        _fileDialog = new();
    private readonly DesignFileSystemSelector _selector;
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


    public DesignPanel(DesignFileSystemSelector selector,
        CustomizationDrawer customizationDrawer,
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
        EditorHistory history)
    {
        _selector            = selector;
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
            new IncognitoButton(_config),
        ];
    }

    private void DrawHeader()
        => HeaderDrawer.Draw(SelectionName, 0, ImGui.GetColorU32(ImGuiCol.FrameBg), _leftButtons, _rightButtons);

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    private void DrawEquipment()
    {
        using var h = DesignPanelFlag.Equipment.Header(_config);
        if (!h)
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _selector.Selected!.WriteProtected());
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromDesign(_manager, _selector.Selected!, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _manager.ChangeStains(_selector.Selected, slot, newAllStain);
        }

        var mainhand = EquipDrawData.FromDesign(_manager, _selector.Selected!, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromDesign(_manager, _selector.Selected!, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, true);

        foreach (var slot in BonusExtensions.AllFlags)
        {
            var data = BonusDrawData.FromDesign(_manager, _selector.Selected!, slot);
            _equipmentDrawer.DrawBonusItem(data);
        }

        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        DrawEquipmentMetaToggles();
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
        _equipmentDrawer.DrawDragDropTooltip();
    }

    private void DrawEquipmentMetaToggles()
    {
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.HatState, _manager, _selector.Selected!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Head, _manager, _selector.Selected!));
        }

        ImGui.SameLine();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.VisorState, _manager, _selector.Selected!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Body, _manager, _selector.Selected!));
        }

        ImGui.SameLine();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.WeaponState, _manager, _selector.Selected!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.OffHand, _manager, _selector.Selected!));
        }

        ImGui.SameLine();
        using (var _ = ImRaii.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.EarState, _manager, _selector.Selected!));
        }
    }

    private void DrawCustomize()
    {
        if (_config.HideDesignPanel.HasFlag(DesignPanelFlag.Customization))
            return;

        var header = _selector.Selected!.DesignData.ModelId == 0
            ? "Customization"
            : $"Customization (Model Id #{_selector.Selected!.DesignData.ModelId})###Customization";
        var       expand = _config.AutoExpandDesignPanel.HasFlag(DesignPanelFlag.Customization);
        using var h      = ImUtf8.CollapsingHeaderId(header, expand ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
        if (!h)
            return;

        if (_customizationDrawer.Draw(_selector.Selected!.DesignData.Customize, _selector.Selected.Application.Customize,
                _selector.Selected!.WriteProtected(), false))
            foreach (var idx in Enum.GetValues<CustomizeIndex>())
            {
                var flag     = idx.ToFlag();
                var newValue = _customizationDrawer.ChangeApply.HasFlag(flag);
                _manager.ChangeApplyCustomize(_selector.Selected, idx, newValue);
                if (_customizationDrawer.Changed.HasFlag(flag))
                    _manager.ChangeCustomize(_selector.Selected, idx, _customizationDrawer.Customize[idx]);
            }

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.Wetness, _manager, _selector.Selected!));
        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2));
    }

    private void DrawCustomizeParameters()
    {
        using var h = DesignPanelFlag.AdvancedCustomizations.Header(_config);
        if (!h)
            return;

        _parameterDrawer.Draw(_manager, _selector.Selected!);
    }

    private void DrawMaterialValues()
    {
        using var h = DesignPanelFlag.AdvancedDyes.Header(_config);
        if (!h)
            return;

        _materials.Draw(_selector.Selected!);
    }

    private void DrawCustomizeApplication()
    {
        using var id        = ImUtf8.PushId("Customizations"u8);
        var       set       = _selector.Selected!.CustomizeSet;
        var       available = set.SettingAvailable | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.BodyType;
        var flags = _selector.Selected!.ApplyCustomizeExcludingBodyType == 0 ? 0 :
            (_selector.Selected!.ApplyCustomize & available) == available    ? 3 : 1;
        if (ImGui.CheckboxFlags("Apply All Customizations", ref flags, 3))
        {
            var newFlags = flags == 3;
            _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Clan,   newFlags);
            _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Gender, newFlags);
            foreach (var index in CustomizationExtensions.AllBasic)
                _manager.ChangeApplyCustomize(_selector.Selected!, index, newFlags);
        }

        var applyClan = _selector.Selected!.DoApplyCustomize(CustomizeIndex.Clan);
        if (ImUtf8.Checkbox($"Apply {CustomizeIndex.Clan.ToDefaultName()}", ref applyClan))
            _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Clan, applyClan);

        var applyGender = _selector.Selected!.DoApplyCustomize(CustomizeIndex.Gender);
        if (ImUtf8.Checkbox($"Apply {CustomizeIndex.Gender.ToDefaultName()}", ref applyGender))
            _manager.ChangeApplyCustomize(_selector.Selected!, CustomizeIndex.Gender, applyGender);


        foreach (var index in CustomizationExtensions.All.Where(set.IsAvailable))
        {
            var apply = _selector.Selected!.DoApplyCustomize(index);
            if (ImUtf8.Checkbox($"Apply {set.Option(index)}", ref apply))
                _manager.ChangeApplyCustomize(_selector.Selected!, index, apply);
        }
    }

    private void DrawCrestApplication()
    {
        using var id        = ImUtf8.PushId("Crests"u8);
        var       flags     = (uint)_selector.Selected!.Application.Crest;
        var       bigChange = ImGui.CheckboxFlags("Apply All Crests", ref flags, (uint)CrestExtensions.AllRelevant);
        foreach (var flag in CrestExtensions.AllRelevantSet)
        {
            var apply = bigChange ? ((CrestFlag)flags & flag) == flag : _selector.Selected!.DoApplyCrest(flag);
            if (ImUtf8.Checkbox($"Apply {flag.ToLabel()} Crest", ref apply) || bigChange)
                _manager.ChangeApplyCrest(_selector.Selected!, flag, apply);
        }
    }

    private void DrawApplicationRules()
    {
        using var h = DesignPanelFlag.ApplicationRules.Header(_config);
        if (!h)
            return;

        using var disabled = ImRaii.Disabled(_selector.Selected!.WriteProtected());

        DrawAllButtons();

        using (var _ = ImUtf8.Group())
        {
            DrawCustomizeApplication();
            ImUtf8.IconDummy();
            DrawCrestApplication();
            ImUtf8.IconDummy();
            DrawMetaApplication();
        }

        ImGui.SameLine(210 * ImUtf8.GlobalScale + ImGui.GetStyle().ItemSpacing.X);
        using (var _ = ImRaii.Group())
        {
            void ApplyEquip(string label, EquipFlag allFlags, bool stain, IEnumerable<EquipSlot> slots)
            {
                var       flags     = (uint)(allFlags & _selector.Selected!.Application.Equip);
                using var id        = ImUtf8.PushId(label);
                var       bigChange = ImGui.CheckboxFlags($"Apply All {label}", ref flags, (uint)allFlags);
                if (stain)
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToStainFlag()) : _selector.Selected!.DoApplyStain(slot);
                        if (ImUtf8.Checkbox($"Apply {slot.ToName()} Dye", ref apply) || bigChange)
                            _manager.ChangeApplyStains(_selector.Selected!, slot, apply);
                    }
                else
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToFlag()) : _selector.Selected!.DoApplyEquip(slot);
                        if (ImUtf8.Checkbox($"Apply {slot.ToName()}", ref apply) || bigChange)
                            _manager.ChangeApplyItem(_selector.Selected!, slot, apply);
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
        var   size      = new Vector2(210 * ImUtf8.GlobalScale, 0);
        if (ImUtf8.ButtonEx("Disable Everything"u8,
                "Disable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness."u8, size,
                !enabled))
        {
            equip     = false;
            customize = false;
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        ImGui.SameLine();
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

        ImGui.SameLine();
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
            _manager.ChangeApplyMulti(_selector.Selected!, true, true, true, false, true, true, false, true);
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.Wetness, false);
        }

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        ImGui.SameLine();
        if (ImUtf8.ButtonEx("Disable Advanced"u8, "Disable all advanced dyes and customizations but keep everything else as is."u8,
                size,
                !enabled))
            _manager.ChangeApplyMulti(_selector.Selected!, null, null, null, false, null, null, false, null);

        if (!enabled)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (equip is null && customize is null)
            return;

        _manager.ChangeApplyMulti(_selector.Selected!, equip, customize, equip, customize.HasValue && !customize.Value ? false : null, null,
            equip, equip, equip);
        if (equip.HasValue)
        {
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.HatState,    equip.Value);
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.VisorState,  equip.Value);
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.WeaponState, equip.Value);
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.EarState,    equip.Value);
        }

        if (customize.HasValue)
            _manager.ChangeApplyMeta(_selector.Selected!, MetaIndex.Wetness, customize.Value);
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
        var        flags     = (uint)_selector.Selected!.Application.Meta;
        var        bigChange = ImGui.CheckboxFlags("Apply All Meta Changes", ref flags, all);

        foreach (var (index, label) in MetaExtensions.AllRelevant.Zip(MetaLabels))
        {
            var apply = bigChange ? ((MetaFlag)flags).HasFlag(index.ToFlag()) : _selector.Selected!.DoApplyMeta(index);
            if (ImUtf8.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyMeta(_selector.Selected!, index, apply);
        }
    }

    private static readonly IReadOnlyList<string> BonusSlotLabels =
    [
        "Apply Facewear",
    ];

    private void DrawBonusSlotApplication()
    {
        using var id        = ImUtf8.PushId("Bonus"u8);
        var       flags     = _selector.Selected!.Application.BonusItem;
        var       bigChange = BonusExtensions.AllFlags.Count > 1 && ImUtf8.Checkbox("Apply All Bonus Slots"u8, ref flags, BonusExtensions.All);
        foreach (var (index, label) in BonusExtensions.AllFlags.Zip(BonusSlotLabels))
        {
            var apply = bigChange ? flags.HasFlag(index) : _selector.Selected!.DoApplyBonusItem(index);
            if (ImUtf8.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyBonusItem(_selector.Selected!, index, apply);
        }
    }


    private void DrawParameterApplication()
    {
        using var id        = ImUtf8.PushId("Parameter");
        var       flags     = (uint)_selector.Selected!.Application.Parameters;
        var       bigChange = ImGui.CheckboxFlags("Apply All Customize Parameters", ref flags, (uint)CustomizeParameterExtensions.All);
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            var apply = bigChange ? ((CustomizeParameterFlag)flags).HasFlag(flag) : _selector.Selected!.DoApplyParameter(flag);
            if (ImUtf8.Checkbox($"Apply {flag.ToName()}", ref apply) || bigChange)
                _manager.ChangeApplyParameter(_selector.Selected!, flag, apply);
        }
    }

    public void Draw()
    {
        using var group = ImUtf8.Group();
        if (_selector.SelectedPaths.Count > 1)
        {
            _multiDesignPanel.Draw();
        }
        else
        {
            DrawHeader();
            DrawPanel();

            if (_selector.Selected == null || _selector.Selected.WriteProtected())
                return;

            if (_importService.CreateDatTarget(out var dat))
            {
                _manager.ChangeCustomize(_selector.Selected!, CustomizeIndex.Clan,   dat.Customize[CustomizeIndex.Clan]);
                _manager.ChangeCustomize(_selector.Selected!, CustomizeIndex.Gender, dat.Customize[CustomizeIndex.Gender]);
                foreach (var idx in CustomizationExtensions.AllBasic)
                    _manager.ChangeCustomize(_selector.Selected!, idx, dat.Customize[idx]);
                Glamourer.Messager.NotificationMessage(
                    $"Applied games .dat file {dat.Description} customizations to {_selector.Selected.Name}.", NotificationType.Success, false);
            }
            else if (_importService.CreateCharaTarget(out var designBase, out var name))
            {
                _manager.ApplyDesign(_selector.Selected!, designBase);
                Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_selector.Selected.Name}.",
                    NotificationType.Success, false);
            }
        }

        _importService.CreateDatSource();
    }

    private void DrawPanel()
    {
        using var table = ImUtf8.Table("##Panel", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.ScrollY, ImGui.GetContentRegionAvail());
        if (!table || _selector.Selected == null)
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableNextColumn();
        if (_selector.Selected == null)
            return;

        ImGui.Dummy(Vector2.Zero);
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
        ImGui.SameLine();
        DrawApplyToTarget();
        ImGui.SameLine();
        _modAssociations.DrawApplyButton();
        ImGui.SameLine();
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
            using var _ = _selector.Selected!.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, _selector.Selected!, ApplySettings.ManualWithLinks with { IsFinal = true });
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
            using var _ = _selector.Selected!.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, _selector.Selected!, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
    }

    private void DrawSaveToDat()
    {
        var verified = _importService.Verify(_selector.Selected!.DesignData.Customize, out _);
        var tt = verified
            ? "Export the currently configured customizations of this design to a character creation data file."
            : "The current design contains customizations that can not be applied during character creation.";
        var startPath = GetUserPath();
        if (startPath.Length == 0)
            startPath = null;
        if (ImGuiUtil.DrawDisabledButton("Export to Dat", Vector2.Zero, tt, !verified))
            _fileDialog.SaveFileDialog("Save File...", ".dat", "FFXIV_CHARA_01.dat", ".dat", (v, path) =>
            {
                if (v && _selector.Selected != null)
                    _importService.SaveDesignAsDat(path, _selector.Selected!.DesignData.Customize, _selector.Selected!.Name);
            }, startPath);

        _fileDialog.Draw();
    }

    private static unsafe string GetUserPath()
        => Framework.Instance()->UserPathString;


    private sealed class LockButton(DesignPanel panel) : Button
    {
        public override bool Visible
            => panel._selector.Selected != null;

        protected override string Description
            => panel._selector.Selected!.WriteProtected()
                ? "Make this design editable."
                : "Write-protect this design.";

        protected override FontAwesomeIcon Icon
            => panel._selector.Selected!.WriteProtected()
                ? FontAwesomeIcon.Lock
                : FontAwesomeIcon.LockOpen;

        protected override void OnClick()
            => panel._manager.SetWriteProtection(panel._selector.Selected!, !panel._selector.Selected!.WriteProtected());
    }

    private sealed class SetFromClipboardButton(DesignPanel panel) : Button
    {
        public override bool Visible
            => panel._selector.Selected != null;

        protected override bool Disabled
            => panel._selector.Selected?.WriteProtected() ?? true;

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
                panel._manager.ApplyDesign(panel._selector.Selected!, design);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not apply clipboard to {panel._selector.Selected!.Name}.",
                    $"Could not apply clipboard to design {panel._selector.Selected!.Identifier}", NotificationType.Error, false);
            }
        }
    }

    private sealed class DesignUndoButton(DesignPanel panel) : Button
    {
        public override bool Visible
            => panel._selector.Selected != null;

        protected override bool Disabled
            => !panel._manager.CanUndo(panel._selector.Selected) || (panel._selector.Selected?.WriteProtected() ?? true);

        protected override string Description
            => "Undo the last time you applied an entire design onto this design, if you accidentally overwrote your design with a different one.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.SyncAlt;

        protected override void OnClick()
        {
            try
            {
                panel._manager.UndoDesignChange(panel._selector.Selected!);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not undo last changes to {panel._selector.Selected!.Name}.",
                    NotificationType.Error,
                    false);
            }
        }
    }

    private sealed class ExportToClipboardButton(DesignPanel panel) : Button
    {
        public override bool Visible
            => panel._selector.Selected != null;

        protected override string Description
            => "Copy the current design to your clipboard.";

        protected override FontAwesomeIcon Icon
            => FontAwesomeIcon.Copy;

        protected override void OnClick()
        {
            try
            {
                var text = panel._converter.ShareBase64(panel._selector.Selected!);
                ImGui.SetClipboardText(text);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not copy {panel._selector.Selected!.Name} data to clipboard.",
                    $"Could not copy data from design {panel._selector.Selected!.Identifier} to clipboard", NotificationType.Error, false);
            }
        }
    }

    private sealed class ApplyCharacterButton(DesignPanel panel) : Button
    {
        public override bool Visible
            => panel._selector.Selected != null && panel._objects.Player.Valid;

        protected override string Description
            => "Overwrite this design with your character's current state.";

        protected override bool Disabled
            => panel._selector.Selected?.WriteProtected() ?? true;

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
                panel._selector.Selected!.GetMaterialDataRef().Clear();
                panel._manager.ApplyDesign(panel._selector.Selected!, design);
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex, $"Could not apply player state to {panel._selector.Selected!.Name}.",
                    $"Could not apply player state to design {panel._selector.Selected!.Identifier}", NotificationType.Error, false);
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
            => panel._selector.Selected != null;

        protected override bool Disabled
            => (panel._selector.Selected?.WriteProtected() ?? true) || !panel._history.CanUndo(panel._selector.Selected);

        protected override void OnClick()
            => panel._history.Undo(panel._selector.Selected!);
    }
}
