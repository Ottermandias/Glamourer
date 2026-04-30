using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Automation;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Gui.Materials;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.ActorTab;

public sealed class ActorPanel : IPanel
{
    private readonly ActorSelection           _selection;
    private readonly StateManager             _stateManager;
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly EquipmentDrawer          _equipmentDrawer;
    private readonly AutoDesignApplier        _autoDesignApplier;
    private readonly Configuration            _config;
    private readonly ActorObjectManager       _objects;
    private readonly ImportService            _importService;
    private readonly DictModelChara           _modelChara;
    private readonly CustomizeParameterDrawer _parameterDrawer;
    private readonly AdvancedDyePopup         _advancedDyes;
    private readonly DesignApplier            _designApplier;

    public event Action? OpenEquipmentBar;

    public ActorPanel(StateManager stateManager,
        CustomizationDrawer customizationDrawer,
        EquipmentDrawer equipmentDrawer,
        AutoDesignApplier autoDesignApplier,
        Configuration config,
        ActorObjectManager objects,
        DesignManager designManager,
        ImportService importService,
        DictModelChara modelChara,
        CustomizeParameterDrawer parameterDrawer,
        AdvancedDyePopup advancedDyes,
        EditorHistory editorHistory,
        ActorSelection selection,
        DesignApplier designApplier)
    {
        _stateManager        = stateManager;
        _customizationDrawer = customizationDrawer;
        _equipmentDrawer     = equipmentDrawer;
        _autoDesignApplier   = autoDesignApplier;
        _config              = config;
        _objects             = objects;
        _importService       = importService;
        _modelChara          = modelChara;
        _parameterDrawer     = parameterDrawer;
        _advancedDyes        = advancedDyes;
        _selection           = selection;
        _designApplier       = designApplier;
    }

    private CustomizeFlag CustomizeApplicationFlags
        => _selection.LockedRedraw
            ? CustomizeFlagExtensions.AllRelevant & ~CustomizeFlagExtensions.RedrawRequired
            : CustomizeFlagExtensions.AllRelevant;

    public ReadOnlySpan<byte> Id
        => "ActorPanel"u8;

    public void Draw()
    {
        DrawPanel();

        if (_selection.State is not { IsLocked: false })
            return;

        if (_importService.CreateDatTarget(out var dat))
        {
            _stateManager.ChangeEntireCustomize(_selection.State!, dat.Customize, CustomizeApplicationFlags, ApplySettings.Manual);
            Glamourer.Messager.NotificationMessage(
                $"Applied games .dat file {dat.Description} customizations to {_selection.State.Identifier}.",
                NotificationType.Success, false);
        }
        else if (_importService.CreateCharaTarget(out var designBase, out var name))
        {
            _stateManager.ApplyDesign(_selection.State!, designBase, ApplySettings.Manual);
            Glamourer.Messager.NotificationMessage($"Applied Anamnesis .chara file {name} to {_selection.State.Identifier}.",
                NotificationType.Success,
                false);
        }

        _importService.CreateDatSource();
        _importService.CreateCharaSource();
    }

    private unsafe void DrawPanel()
    {
        using var table = Im.Table.Begin("##Panel"u8, 1, TableFlags.ScrollY, Im.ContentRegion.Available);
        if (!table || _selection.State is null)
            return;

        table.SetupScrollFreeze(0, 1);
        table.NextColumn();
        Im.Dummy(Vector2.Zero);
        var transformationId = _selection.Actor.IsCharacter ? _selection.Actor.AsCharacter->CharacterData.TransformationId : 0;
        if (transformationId is not 0)
            ImEx.TextFramed($"Currently transformed to Transformation {transformationId}.", Im.ContentRegion.Available with { Y = 0 },
                Colors.SelectedRed);

        DrawApplyToSelf();
        Im.Line.Same();
        DrawApplyToTarget();

        RevertButtons();
        table.NextColumn();

        using var disabled = Im.Disabled(transformationId is not 0);
        if (_selection.State.ModelData.IsHuman)
            DrawHumanPanel();
        else
            DrawMonsterPanel();
        if (_selection.Data.Objects.Count > 0)
            _advancedDyes.Draw(_selection.Data.Objects.Last(), _selection.State, false);
    }

    private void DrawHumanPanel()
    {
        DrawCustomizationsHeader();
        DrawEquipmentHeader();
        DrawParameterHeader();
        DrawDebugData();
    }

    private void DrawCustomizationsHeader()
    {
        if (_config.HideDesignPanel.HasFlag(DesignPanelFlag.Customization))
            return;

        var expand = _config.AutoExpandDesignPanel.HasFlag(DesignPanelFlag.Customization);
        using var h = Im.Tree.HeaderId(_selection.State!.ModelData.ModelId is 0
                ? "Customization###Customization"u8
                : $"Customization (Model Id #{_selection.State.ModelData.ModelId})###Customization",
            expand ? TreeNodeFlags.DefaultOpen : TreeNodeFlags.None);
        if (!h)
            return;

        if (_customizationDrawer.Draw(_selection.State!.ModelData.Customize, _selection.State.IsLocked, _selection.LockedRedraw))
            _stateManager.ChangeEntireCustomize(_selection.State, _customizationDrawer.Customize, _customizationDrawer.Changed,
                ApplySettings.Manual);

        EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.Wetness, _stateManager, _selection.State));
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
    }

    private Im.HeaderDisposable EquipmentHeaderButton()
    {
        var savedCursor = Im.Cursor.Position;
        var headerWidth = Im.ContentRegion.Available.X - ImEx.Icon.CalculateLabeledButtonSize(LunaStyle.PopOutIcon, ""u8).X;
        Im.Cursor.X += headerWidth;
        using var color = ImGuiColor.Button.Push(ImGuiColor.Header)
            .Push(ImGuiColor.ButtonHovered, ImGuiColor.HeaderHovered)
            .Push(ImGuiColor.ButtonActive,  ImGuiColor.HeaderActive);
        if (ImEx.Icon.LabeledButton(LunaStyle.PopOutIcon, "###switchToEquipBar"u8, "Switch to the Equipment Bar."u8, corners: Corners.Right))
            OpenEquipmentBar?.Invoke();
        Im.Cursor.Position = savedCursor;

        var upperLeft  = Im.Cursor.ScreenPosition;
        var lowerRight = upperLeft + new Vector2(headerWidth, Im.Style.FrameHeight);

        // We have to shave off an epsilon of width, otherwise there is a pixel that is considered as hovering both parts of the widget.
        var headerColor =
            (Im.Mouse.IsHoveringRectangle(upperLeft, lowerRight - new Vector2(0.001f, 0.0f)), Im.Mouse.IsDown(MouseButton.Left)) switch
            {
                (true, true)  => ImGuiColor.ButtonActive,
                (true, false) => ImGuiColor.ButtonHovered,
                (false, _)    => ImGuiColor.Button,
            };

        Im.DrawList.Window.Shape.RectangleFilled(upperLeft, lowerRight, headerColor, Im.Style.FrameRounding,
            ImDrawFlagsRectangle.RoundCornersLeft);

        color.Push(ImGuiColor.Header, Rgba32.Transparent)
            .Push(ImGuiColor.HeaderHovered, Rgba32.Transparent)
            .Push(ImGuiColor.HeaderActive,  Rgba32.Transparent);

        return DesignPanelFlag.Equipment.Header(_config);
    }

    private void DrawEquipmentHeader()
    {
        using var h = EquipmentHeaderButton();
        if (!h)
            return;

        _equipmentDrawer.Prepare(false);

        var usedAllStain = _equipmentDrawer.DrawAllStain(out var newAllStain, _selection.State!.IsLocked);
        Im.Line.Same();
        EquipmentDrawer.DrawKeepItemFilter(_config);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var data = EquipDrawData.FromState(_stateManager, _selection.State!, slot);
            _equipmentDrawer.DrawEquip(data);
            if (usedAllStain)
                _stateManager.ChangeStains(_selection.State, slot, newAllStain, ApplySettings.Manual);
        }

        var mainhand = EquipDrawData.FromState(_stateManager, _selection.State, EquipSlot.MainHand);
        var offhand  = EquipDrawData.FromState(_stateManager, _selection.State, EquipSlot.OffHand);
        _equipmentDrawer.DrawWeapons(mainhand, offhand, GameMain.IsInGPose());

        foreach (var slot in BonusExtensions.AllFlags)
        {
            var data = BonusDrawData.FromState(_stateManager, _selection.State!, slot);
            _equipmentDrawer.DrawBonusItem(data);
        }

        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        DrawEquipmentMetaToggles();
        Im.Dummy(new Vector2(Im.Style.TextHeight / 2));
        _equipmentDrawer.DrawDragDropTooltip();
    }

    private void DrawParameterHeader()
    {
        using var h = DesignPanelFlag.AdvancedCustomizations.Header(_config);
        if (!h)
            return;

        _parameterDrawer.Draw(_stateManager, _selection.State!);
    }

    private unsafe void DrawDebugData()
    {
        if (!_config.DebugMode)
            return;

        using var h = DesignPanelFlag.DebugData.Header(_config);
        if (!h)
            return;

        using var table = Im.Table.Begin("table"u8, 2, TableFlags.SizingFixedFit);
        if (!table)
            return;

        table.DrawColumn("Object Index"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->ObjectIndex)));
        table.DrawColumn("Name ID"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->GetNameId())));
        table.DrawColumn("Base ID"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->BaseId)));
        table.DrawColumn("Entity ID"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->EntityId)));
        table.DrawColumn("Owner ID"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->OwnerId)));
        table.DrawColumn("Game Object ID"u8);
        DrawCopyColumn(table, StringU8.Join(", "u8, _selection.Data.Objects.Select(d => d.AsObject->GetGameObjectId().ObjectId)));
        return;

        static void DrawCopyColumn(Im.TableDisposable table, Utf8StringHandler<TextStringHandlerBuffer> text)
        {
            table.DrawColumn(ref text);
            if (Im.Item.RightClicked())
                Im.Clipboard.Set(ref text);
        }
    }

    private void DrawEquipmentMetaToggles()
    {
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.HatState, _stateManager, _selection.State!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.Head, _stateManager, _selection.State!));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.VisorState, _stateManager, _selection.State!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.Body, _stateManager, _selection.State!));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.WeaponState, _stateManager, _selection.State!));
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.CrestFromState(CrestFlag.OffHand, _stateManager, _selection.State!));
        }

        Im.Line.Same();
        using (Im.Group())
        {
            EquipmentDrawer.DrawMetaToggle(ToggleDrawData.FromState(MetaIndex.EarState, _stateManager, _selection.State!));
        }
    }

    private void DrawMonsterPanel()
    {
        var names = _modelChara[_selection.State!.ModelData.ModelId];
        using (Im.ListBox.Begin("##MonsterList"u8, Im.ContentRegion.Available with { Y = 10 * Im.Style.TextHeightWithSpacing }))
        {
            if (names.Count is 0)
                Im.Text("Unknown Monster"u8);
            else
                Im.ListClipper.Draw(names, p => Im.Text($"{p.Name} ({p.Kind.ToName()} #{p.Id})"), Im.Style.TextHeightWithSpacing);
        }

        Im.Separator();
        Im.Text("Customization Data"u8);
        using (Im.Font.PushMono())
        {
            foreach (var b in _selection.State.ModelData.Customize)
            {
                using (Im.Group())
                {
                    Im.Text($" {b.Value:X2}");
                    Im.Text($"{b.Value,3}");
                }

                Im.Line.Same();
                if (Im.ContentRegion.Available.X < Im.Style.ItemSpacing.X + Im.Font.CalculateSize("XXX"u8).X)
                    Im.Line.New();
            }

            if (Im.Cursor.X is not 0)
                Im.Line.New();
        }

        Im.Separator();
        Im.Text("Equipment Data"u8);
        using (Im.Font.PushMono())
        {
            foreach (var b in _selection.State.ModelData.GetEquipmentBytes())
            {
                using (Im.Group())
                {
                    Im.Text($" {b:X2}");
                    Im.Text($"{b,3}");
                }

                Im.Line.Same();
                if (Im.ContentRegion.Available.X < Im.Style.ItemSpacing.X + Im.Font.CalculateSize("XXX"u8).X)
                    Im.Line.New();
            }

            if (Im.Cursor.X is not 0)
                Im.Line.New();
        }
    }


    private void RevertButtons()
    {
        if (ImEx.Button("Revert to Game"u8, Vector2.Zero, "Revert the character to its actual state in the game."u8,
                _selection.State!.IsLocked))
            _stateManager.ResetState(_selection.State!, StateSource.Manual, isFinal: true);

        Im.Line.Same();

        if (ImEx.Button("Reapply Automation"u8, Vector2.Zero,
                "Reapply the current automation state for the character on top of its current state.."u8,
                !_config.EnableAutoDesigns || _selection.State!.IsLocked))
        {
            _autoDesignApplier.ReapplyAutomation(_selection.Actor, _selection.Identifier, _selection.State!, false, false,
                out var forcedRedraw);
            _stateManager.ReapplyAutomationState(_selection.Actor, forcedRedraw, false, StateSource.Manual);
        }

        Im.Line.Same();
        if (ImEx.Button("Revert to Automation"u8, Vector2.Zero,
                "Try to revert the character to the state it would have using automated designs."u8,
                !_config.EnableAutoDesigns || _selection.State!.IsLocked))
        {
            _autoDesignApplier.ReapplyAutomation(_selection.Actor, _selection.Identifier, _selection.State!, true, false, out var forcedRedraw);
            _stateManager.ReapplyAutomationState(_selection.Actor, forcedRedraw, true, StateSource.Manual);
        }

        Im.Line.Same();
        if (ImEx.Button("Reapply"u8, Vector2.Zero,
                "Try to reapply the configured state if something went wrong. Should generally not be necessary."u8,
                _selection.State!.IsLocked))
            _stateManager.ReapplyState(_selection.Actor, false, StateSource.Manual, true);
    }

    private void DrawApplyToSelf()
    {
        var (id, data) = _objects.PlayerData;
        var canApply  = _designApplier.CanApplyTo(_selection.State?.ModelData, id, data);
        var selfApply = id == _selection.Identifier;
        var tt = canApply switch
        {
            DeniedApplicationReason.None =>
                "Apply the current state to your own character.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8,
            DeniedApplicationReason.SourceNonHuman => "Can not apply non-human states."u8,
            DeniedApplicationReason.TargetInvalid or DeniedApplicationReason.TargetUnavailable => "Your character is unavailable."u8,
            _ when selfApply => "You can not apply your own state onto yourself."u8,
            _ => ""u8,
        };

        if (ImEx.Button("Apply to Yourself"u8, Vector2.Zero, tt, canApply is not DeniedApplicationReason.None || selfApply))
            _designApplier.ApplyTo(_selection.State!, id, data, ApplicationRules.FromModifiers(_selection.State!));
    }

    private void DrawApplyToTarget()
    {
        var (id, data) = _objects.TargetData;
        var canApply = _designApplier.CanApplyTo(_selection.State?.ModelData, id, data);
        var tt = canApply switch
        {
            DeniedApplicationReason.None =>
                "Apply the current state to your current target.\nHold Control to only apply gear.\nHold Shift to only apply customizations."u8,
            DeniedApplicationReason.TargetUnavailable => "The current target can not be manipulated."u8,
            DeniedApplicationReason.TargetInvalid     => "No valid target selected."u8,
            DeniedApplicationReason.SourceNonHuman    => "Can not apply non-human states."u8,
            DeniedApplicationReason.TargetNonHuman    => "Can not apply states to non-humans."u8,
            _                                         => ""u8,
        };
        if (ImEx.Button("Apply to Target"u8, Vector2.Zero, tt, canApply is not DeniedApplicationReason.None))
            _designApplier.ApplyTo(_selection.State!, id, data, ApplicationRules.FromModifiers(_selection.State!));
    }
}
