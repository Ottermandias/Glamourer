using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Glamourer.Api.Enums;
using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Materials;
using Glamourer.Interop;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel : IPanel
{
    private readonly FileDialogManager        _fileDialog = new();
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly DesignFileSystem         _fileSystem;
    private readonly DesignManager            _manager;
    private readonly ActorObjectManager       _objects;
    private readonly StateManager             _state;
    private readonly EquipmentDrawer          _equipmentDrawer;
    private readonly ModAssociationsTab       _modAssociations;
    private readonly Configuration            _config;
    private readonly DesignDetailTab          _designDetails;
    private readonly ImportService            _importService;
    private readonly MultiDesignPanel         _multiDesignPanel;
    private readonly CustomizeParameterDrawer _parameterDrawer;
    private readonly DesignLinkDrawer         _designLinkDrawer;
    private readonly MaterialDrawer           _materials;


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
        DesignFileSystem fileSystem)
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
        _multiDesignPanel    = multiDesignPanel;
        _parameterDrawer     = parameterDrawer;
        _designLinkDrawer    = designLinkDrawer;
        _materials           = materials;
        _fileSystem          = fileSystem;
    }


    private Design Selection
        => (Design)_fileSystem.Selection.Selection!.Value;

    private void DrawEquipment()
    {
        using var h = DesignPanelFlag.Equipment.Header(_config);
        if (!h)
            return;

        _equipmentDrawer.Prepare();

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, Selection.WriteProtected());
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromDesign(_manager, Selection, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _manager.ChangeStains(Selection, slot, newAllStain);
        }

        var mainhand = EquipDrawData.FromDesign(_manager, Selection, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromDesign(_manager, Selection, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, true);

        foreach (var slot in BonusExtensions.AllFlags)
        {
            var data = BonusDrawData.FromDesign(_manager, Selection, slot);
            _equipmentDrawer.DrawBonusItem(data);
        }

        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        DrawEquipmentMetaToggles();
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        _equipmentDrawer.DrawDragDropTooltip();
    }

    private void DrawEquipmentMetaToggles()
    {
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.HatState, _manager, Selection));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Head, _manager, Selection));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.VisorState, _manager, Selection));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.Body, _manager, Selection));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.WeaponState, _manager, Selection));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromDesign(CrestFlag.OffHand, _manager, Selection));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.EarState, _manager, Selection));
        }
    }

    private void DrawCustomize()
    {
        if (_config.HideDesignPanel.HasFlag(DesignPanelFlag.Customization))
            return;

        var expand = _config.AutoExpandDesignPanel.HasFlag(DesignPanelFlag.Customization);
        using var h = Im.Tree.HeaderId(Selection.DesignData.ModelId is 0
                ? "Customization"u8
                : $"Customization (Model Id #{Selection.DesignData.ModelId})###Customization",
            expand ? TreeNodeFlags.DefaultOpen : TreeNodeFlags.None);
        if (!h)
            return;

        if (_customizationDrawer.Draw(Selection.DesignData.Customize, Selection.Application.Customize,
                Selection.WriteProtected(), false))
            foreach (var idx in CustomizeIndex.Values)
            {
                var flag     = idx.ToFlag();
                var newValue = _customizationDrawer.ChangeApply.HasFlag(flag);
                _manager.ChangeApplyCustomize(Selection, idx, newValue);
                if (_customizationDrawer.Changed.HasFlag(flag))
                    _manager.ChangeCustomize(Selection, idx, _customizationDrawer.Customize[idx]);
            }

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromDesign(MetaIndex.Wetness, _manager, Selection));
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
    }

    private void DrawCustomizeParameters()
    {
        using var h = DesignPanelFlag.AdvancedCustomizations.Header(_config);
        if (!h)
            return;

        _parameterDrawer.Draw(_manager, Selection);
    }

    private void DrawMaterialValues()
    {
        using var h = DesignPanelFlag.AdvancedDyes.Header(_config);
        if (!h)
            return;

        _materials.Draw(Selection);
    }

    private void DrawCustomizeApplication()
    {
        using var id        = Im.Id.Push("Customizations"u8);
        var       set       = Selection.CustomizeSet;
        var       available = set.SettingAvailable | CustomizeFlag.Clan | CustomizeFlag.Gender | CustomizeFlag.BodyType;
        var flags = Selection.ApplyCustomizeExcludingBodyType is 0 ? 0ul :
            (Selection.ApplyCustomize & available) == available    ? 3ul : 1ul;
        if (Im.Checkbox("Apply All Customizations"u8, ref flags, 3ul))
        {
            var newFlags = flags is 3;
            _manager.ChangeApplyCustomize(Selection, CustomizeIndex.Clan,   newFlags);
            _manager.ChangeApplyCustomize(Selection, CustomizeIndex.Gender, newFlags);
            foreach (var index in CustomizationExtensions.AllBasic)
                _manager.ChangeApplyCustomize(Selection, index, newFlags);
        }

        var applyClan = Selection.DoApplyCustomize(CustomizeIndex.Clan);
        if (Im.Checkbox($"Apply {CustomizeIndex.Clan.ToNameU8()}", ref applyClan))
            _manager.ChangeApplyCustomize(Selection, CustomizeIndex.Clan, applyClan);

        var applyGender = Selection.DoApplyCustomize(CustomizeIndex.Gender);
        if (Im.Checkbox($"Apply {CustomizeIndex.Gender.ToNameU8()}", ref applyGender))
            _manager.ChangeApplyCustomize(Selection, CustomizeIndex.Gender, applyGender);


        foreach (var index in CustomizationExtensions.All.Where(set.IsAvailable))
        {
            var apply = Selection.DoApplyCustomize(index);
            if (Im.Checkbox($"Apply {set.Option(index)}", ref apply))
                _manager.ChangeApplyCustomize(Selection, index, apply);
        }
    }

    private void DrawCrestApplication()
    {
        using var id        = Im.Id.Push("Crests"u8);
        var       flags     = (ulong)Selection.Application.Crest;
        var       bigChange = Im.Checkbox("Apply All Crests"u8, ref flags, (ulong)CrestExtensions.AllRelevant);
        foreach (var flag in CrestExtensions.AllRelevantSet)
        {
            var apply = bigChange ? ((CrestFlag)flags & flag) == flag : Selection.DoApplyCrest(flag);
            if (Im.Checkbox($"Apply {flag.ToLabel()} Crest", ref apply) || bigChange)
                _manager.ChangeApplyCrest(Selection, flag, apply);
        }
    }

    private void DrawApplicationRules()
    {
        using var h = DesignPanelFlag.ApplicationRules.Header(_config);
        if (!h)
            return;

        using var disabled = Im.Disabled(Selection.WriteProtected());

        DrawAllButtons();

        using (Im.Group())
        {
            DrawCustomizeApplication();
            Im.FrameDummy();
            DrawCrestApplication();
            Im.FrameDummy();
            DrawMetaApplication();
        }

        Im.Line.Same(210 * Im.Style.GlobalScale + Im.Style.ItemSpacing.X);
        using (Im.Group())
        {
            void ApplyEquip(string label, EquipFlag allFlags, bool stain, IEnumerable<EquipSlot> slots)
            {
                var       flags     = (ulong)(allFlags & Selection.Application.Equip);
                using var id        = Im.Id.Push(label);
                var       bigChange = Im.Checkbox($"Apply All {label}", ref flags, (ulong)allFlags);
                if (stain)
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToStainFlag()) : Selection.DoApplyStain(slot);
                        if (Im.Checkbox($"Apply {slot.ToName()} Dye", ref apply) || bigChange)
                            _manager.ChangeApplyStains(Selection, slot, apply);
                    }
                else
                    foreach (var slot in slots)
                    {
                        var apply = bigChange ? ((EquipFlag)flags).HasFlag(slot.ToFlag()) : Selection.DoApplyEquip(slot);
                        if (Im.Checkbox($"Apply {slot.ToName()}", ref apply) || bigChange)
                            _manager.ChangeApplyItem(Selection, slot, apply);
                    }
            }

            ApplyEquip("Weapons", ApplicationTypeExtensions.WeaponFlags, false, [EquipSlot.MainHand, EquipSlot.OffHand]);

            Im.FrameDummy();
            ApplyEquip("Armor", ApplicationTypeExtensions.ArmorFlags, false, EquipSlotExtensions.EquipmentSlots);

            Im.FrameDummy();
            ApplyEquip("Accessories", ApplicationTypeExtensions.AccessoryFlags, false, EquipSlotExtensions.AccessorySlots);

            Im.FrameDummy();
            ApplyEquip("Dyes", ApplicationTypeExtensions.StainFlags, true,
                EquipSlotExtensions.FullSlots);

            Im.FrameDummy();
            DrawParameterApplication();

            Im.FrameDummy();
            DrawBonusSlotApplication();
        }
    }

    private void DrawAllButtons()
    {
        var   enabled   = _config.DeleteDesignModifier.IsActive();
        bool? equip     = null;
        bool? customize = null;
        var   size      = ImEx.ScaledVectorX(210);
        if (ImEx.Button("Disable Everything"u8, size,
                "Disable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness."u8,
                !enabled))
        {
            equip     = false;
            customize = false;
        }

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImEx.Button("Enable Everything"u8, size,
                "Enable application of everything, including any existing advanced dyes, advanced customizations, crests and wetness."u8,
                !enabled))
        {
            equip     = true;
            customize = true;
        }

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (ImEx.Button("Equipment Only"u8, size,
                "Enable application of anything related to gear, disable anything that is not related to gear."u8,
                !enabled))
        {
            equip     = true;
            customize = false;
        }

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImEx.Button("Customization Only"u8, size,
                "Enable application of anything related to customization, disable anything that is not related to customization."u8,
                !enabled))
        {
            equip     = false;
            customize = true;
        }

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (ImEx.Button("Default Application"u8, size,
                "Set the application rules to the default values as if the design was newly created, without any advanced features or wetness."u8,
                !enabled))
        {
            _manager.ChangeApplyMulti(Selection, true, true, true, false, true, true, false, true);
            _manager.ChangeApplyMeta(Selection, MetaIndex.Wetness, false);
        }

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        Im.Line.Same();
        if (ImEx.Button("Disable Advanced"u8, size, "Disable all advanced dyes and customizations but keep everything else as is."u8, !enabled))
            _manager.ChangeApplyMulti(Selection, null, null, null, false, null, null, false, null);

        if (!enabled)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, $"Hold {_config.DeleteDesignModifier} while clicking.");

        if (equip is null && customize is null)
            return;

        _manager.ChangeApplyMulti(Selection, equip, customize, equip, customize.HasValue && !customize.Value ? false : null, null,
            equip, equip, equip);
        if (equip.HasValue)
        {
            _manager.ChangeApplyMeta(Selection, MetaIndex.HatState,    equip.Value);
            _manager.ChangeApplyMeta(Selection, MetaIndex.VisorState,  equip.Value);
            _manager.ChangeApplyMeta(Selection, MetaIndex.WeaponState, equip.Value);
            _manager.ChangeApplyMeta(Selection, MetaIndex.EarState,    equip.Value);
        }

        if (customize.HasValue)
            _manager.ChangeApplyMeta(Selection, MetaIndex.Wetness, customize.Value);
    }

    private static readonly IReadOnlyList<StringU8> MetaLabels =
    [
        new("Apply Wetness"u8),
        new("Apply Hat Visibility"u8),
        new("Apply Visor State"u8),
        new("Apply Weapon Visibility"u8),
        new("Apply Viera Ear Visibility"u8),
    ];

    private void DrawMetaApplication()
    {
        using var   id        = Im.Id.Push("Meta"u8);
        const ulong all       = (ulong)MetaExtensions.All;
        var         flags     = (ulong)Selection.Application.Meta;
        var         bigChange = Im.Checkbox("Apply All Meta Changes"u8, ref flags, all);

        foreach (var (index, label) in MetaExtensions.AllRelevant.Zip(MetaLabels))
        {
            var apply = bigChange ? ((MetaFlag)flags).HasFlag(index.ToFlag()) : Selection.DoApplyMeta(index);
            if (Im.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyMeta(Selection, index, apply);
        }
    }

    private static readonly IReadOnlyList<StringU8> BonusSlotLabels =
    [
        new("Apply Facewear"u8),
    ];

    private void DrawBonusSlotApplication()
    {
        using var id        = Im.Id.Push("Bonus"u8);
        var       flags     = Selection.Application.BonusItem;
        var       bigChange = BonusExtensions.AllFlags.Count > 1 && Im.Checkbox("Apply All Bonus Slots"u8, ref flags, BonusExtensions.All);
        foreach (var (index, label) in BonusExtensions.AllFlags.Zip(BonusSlotLabels))
        {
            var apply = bigChange ? flags.HasFlag(index) : Selection.DoApplyBonusItem(index);
            if (Im.Checkbox(label, ref apply) || bigChange)
                _manager.ChangeApplyBonusItem(Selection, index, apply);
        }
    }


    private void DrawParameterApplication()
    {
        using var id        = Im.Id.Push("Parameter"u8);
        var       flags     = (ulong)Selection.Application.Parameters;
        var       bigChange = Im.Checkbox("Apply All Customize Parameters"u8, ref flags, (ulong)CustomizeParameterExtensions.All);
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            var apply = bigChange ? ((CustomizeParameterFlag)flags).HasFlag(flag) : Selection.DoApplyParameter(flag);
            if (Im.Checkbox($"Apply {flag.ToNameU8()}", ref apply) || bigChange)
                _manager.ChangeApplyParameter(Selection, flag, apply);
        }
    }

    public ReadOnlySpan<byte> Id
        => "DesignPanel"u8;

    public void Draw()
    {
        _importService.CreateDatSource();
        if (_fileSystem.Selection.OrderedNodes.Count > 1)
        {
            _multiDesignPanel.Draw();
            return;
        }

        DrawPanel();

        if (_fileSystem.Selection.Selection is null || Selection.WriteProtected())
            return;

        if (_importService.CreateDatTarget(out var dat))
        {
            _manager.ChangeCustomize(Selection, CustomizeIndex.Clan,   dat.Customize[CustomizeIndex.Clan]);
            _manager.ChangeCustomize(Selection, CustomizeIndex.Gender, dat.Customize[CustomizeIndex.Gender]);
            foreach (var idx in CustomizationExtensions.AllBasic)
                _manager.ChangeCustomize(Selection, idx, dat.Customize[idx]);
            Glamourer.Messager.NotificationMessage(
                $"Applied games .dat file {dat.Description} customizations to {Selection.Name}.", NotificationType.Success, false);
        }
        else if (_importService.CreateCharaTarget(out var designBase, out var name))
        {
            _manager.ApplyDesign(Selection, designBase);
            Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {Selection.Name}.",
                NotificationType.Success, false);
        }
    }

    private void DrawPanel()
    {
        using var table = Im.Table.Begin("##Panel"u8, 1, TableFlags.ScrollY, Im.ContentRegion.Available);
        if (!table || _fileSystem.Selection.Selection is null)
            return;

        table.SetupScrollFreeze(0, 1);
        table.NextColumn();

        Im.Dummy(Vector2.Zero);
        DrawButtonRow();
        table.NextColumn();

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
        if (!ImEx.Button("Apply to Yourself"u8, Vector2.Zero,
                "Apply the current design with its settings to your character.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8,
                !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            using var _ = Selection.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, Selection, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var tt = id.IsValid
            ? data.Valid
                ? "Apply the current design with its settings to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8
                : "The current target can not be manipulated."u8
            : "No valid target selected."u8;
        if (!ImEx.Button("Apply to Target"u8, Vector2.Zero, tt, !data.Valid))
            return;

        if (_state.GetOrCreate(id, data.Objects[0], out var state))
        {
            using var _ = Selection.TemporarilyRestrictApplication(ApplicationCollection.FromKeys());
            _state.ApplyDesign(state, Selection, ApplySettings.ManualWithLinks with { IsFinal = true });
        }
    }

    private void DrawSaveToDat()
    {
        var verified = _importService.Verify(Selection.DesignData.Customize, out _);
        var tt = verified
            ? "Export the currently configured customizations of this design to a character creation data file."u8
            : "The current design contains customizations that can not be applied during character creation."u8;
        var startPath = GetUserPath();
        if (startPath.Length is 0)
            startPath = null;
        if (ImEx.Button("Export to Dat"u8, Vector2.Zero, tt, !verified))
            _fileDialog.SaveFileDialog("Save File...", ".dat", "FFXIV_CHARA_01.dat", ".dat", (v, path) =>
            {
                if (v && _fileSystem.Selection.Selection?.GetValue<Design>() is not null)
                    _importService.SaveDesignAsDat(path, Selection.DesignData.Customize, Selection.Name);
            }, startPath);

        _fileDialog.Draw();
    }

    private static unsafe string GetUserPath()
        => Framework.Instance()->UserPathString;
}
