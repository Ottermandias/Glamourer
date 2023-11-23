﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Api;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Unlocks;
using Glamourer.Utility;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.Api.Enums;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Glamourer.Gui.Tabs;

public unsafe class DebugTab : ITab
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Configuration          _config;
    private readonly VisorService           _visorService;
    private readonly ChangeCustomizeService _changeCustomizeService;
    private readonly UpdateSlotService      _updateSlotService;
    private readonly WeaponService          _weaponService;
    private readonly MetaService            _metaService;
    private readonly InventoryService       _inventoryService;
    private readonly PenumbraService        _penumbra;
    private readonly ObjectManager          _objectManager;
    private readonly GlamourerIpc           _ipc;
    private readonly CodeService            _code;
    private readonly ImportService         _importService;

    private readonly ItemManager            _items;
    private readonly ActorService           _actors;
    private readonly CustomizationService   _customization;
    private readonly JobService             _jobs;
    private readonly CustomizeUnlockManager _customizeUnlocks;
    private readonly ItemUnlockManager      _itemUnlocks;

    private readonly DesignManager     _designManager;
    private readonly DesignFileSystem  _designFileSystem;
    private readonly AutoDesignManager _autoDesignManager;
    private readonly DesignConverter   _designConverter;
    private readonly HumanModelList    _humans;

    private readonly PenumbraChangedItemTooltip _penumbraTooltip;

    private readonly StateManager _state;
    private readonly FunModule    _funModule;

    private int _gameObjectIndex;

    public bool IsVisible
        => _config.DebugMode;

    public DebugTab(ChangeCustomizeService changeCustomizeService, VisorService visorService, UpdateSlotService updateSlotService,
        WeaponService weaponService, PenumbraService penumbra,
        ActorService actors, ItemManager items, CustomizationService customization, ObjectManager objectManager,
        DesignFileSystem designFileSystem, DesignManager designManager, StateManager state, Configuration config,
        PenumbraChangedItemTooltip penumbraTooltip, MetaService metaService, GlamourerIpc ipc, DalamudPluginInterface pluginInterface,
        AutoDesignManager autoDesignManager, JobService jobs, CodeService code, CustomizeUnlockManager customizeUnlocks,
        ItemUnlockManager itemUnlocks, DesignConverter designConverter, ImportService importService, InventoryService inventoryService,
        HumanModelList humans, FunModule funModule)
    {
        _changeCustomizeService = changeCustomizeService;
        _visorService           = visorService;
        _updateSlotService      = updateSlotService;
        _weaponService          = weaponService;
        _penumbra               = penumbra;
        _actors                 = actors;
        _items                  = items;
        _customization          = customization;
        _objectManager          = objectManager;
        _designFileSystem       = designFileSystem;
        _designManager          = designManager;
        _state                  = state;
        _config                 = config;
        _penumbraTooltip        = penumbraTooltip;
        _metaService            = metaService;
        _ipc                    = ipc;
        _pluginInterface        = pluginInterface;
        _autoDesignManager      = autoDesignManager;
        _jobs                   = jobs;
        _code                   = code;
        _customizeUnlocks       = customizeUnlocks;
        _itemUnlocks            = itemUnlocks;
        _designConverter        = designConverter;
        _importService         = importService;
        _inventoryService       = inventoryService;
        _humans                 = humans;
        _funModule              = funModule;
    }

    public ReadOnlySpan<byte> Label
        => "Debug"u8;

    public void DrawContent()
    {
        using var child = ImRaii.Child("MainWindowChild");
        if (!child)
            return;

        DrawInteropHeader();
        DrawGameDataHeader();
        DrawPenumbraHeader();
        DrawDesigns();
        DrawState();
        DrawAutoDesigns();
        DrawInventory();
        DrawUnlocks();
        DrawFun();
        DrawIpc();
    }

    #region Interop

    private void DrawInteropHeader()
    {
        if (!ImGui.CollapsingHeader("Interop"))
            return;

        DrawModelEvaluation();
        DrawObjectManager();
        DrawDatFiles();
    }

    private void DrawModelEvaluation()
    {
        using var tree = ImRaii.TreeNode("Model Evaluation");
        if (!tree)
            return;

        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        var       actor = (Actor)_objectManager.Objects.GetObjectAddress(_gameObjectIndex);
        var       model = actor.Model;
        using var table = ImRaii.Table("##evaluationTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TableHeader("Actor");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Model");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Address");
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(actor.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(model.ToString());
        ImGui.TableNextColumn();
        if (actor.IsCharacter)
        {
            ImGui.TextUnformatted(actor.AsCharacter->CharacterData.ModelCharaId.ToString());
            if (actor.AsCharacter->CharacterData.TransformationId != 0)
                ImGui.TextUnformatted($"Transformation Id: {actor.AsCharacter->CharacterData.TransformationId}");
            if (actor.AsCharacter->CharacterData.ModelCharaId_2 != -1)
                ImGui.TextUnformatted($"ModelChara2 {actor.AsCharacter->CharacterData.ModelCharaId_2}");
            if (actor.AsCharacter->CharacterData.StatusEffectVFXId != 0)
                ImGui.TextUnformatted($"Status Id: {actor.AsCharacter->CharacterData.StatusEffectVFXId}");
        }

        ImGuiUtil.DrawTableColumn("Mainhand");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetMainhand().ToString() : "No Character");

        var (mainhand, offhand, mainModel, offModel) = model.GetWeapons(actor);
        ImGuiUtil.DrawTableColumn(mainModel.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(mainhand.ToString());

        ImGuiUtil.DrawTableColumn("Offhand");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetOffhand().ToString() : "No Character");
        ImGuiUtil.DrawTableColumn(offModel.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(offhand.ToString());

        DrawVisor(actor, model);
        DrawHatState(actor, model);
        DrawWeaponState(actor, model);
        DrawWetness(actor, model);
        DrawEquip(actor, model);
        DrawCustomize(actor, model);
    }

    private string _objectFilter = string.Empty;

    private void DrawObjectManager()
    {
        using var tree = ImRaii.TreeNode("Object Manager");
        if (!tree)
            return;

        _objectManager.Update();

        using (var table = ImRaii.Table("##data", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!table)
                return;

            ImGuiUtil.DrawTableColumn("Last Update");
            ImGuiUtil.DrawTableColumn(_objectManager.LastUpdate.ToString(CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();

            ImGuiUtil.DrawTableColumn("World");
            ImGuiUtil.DrawTableColumn(_actors.Valid ? _actors.AwaitedService.Data.ToWorldName(_objectManager.World) : "Service Missing");
            ImGuiUtil.DrawTableColumn(_objectManager.World.ToString());

            ImGuiUtil.DrawTableColumn("Player Character");
            ImGuiUtil.DrawTableColumn($"{_objectManager.Player.Utf8Name} ({_objectManager.Player.Index})");
            ImGui.TableNextColumn();
            ImGuiUtil.CopyOnClickSelectable(_objectManager.Player.ToString());

            ImGuiUtil.DrawTableColumn("In GPose");
            ImGuiUtil.DrawTableColumn(_objectManager.IsInGPose.ToString());
            ImGui.TableNextColumn();

            if (_objectManager.IsInGPose)
            {
                ImGuiUtil.DrawTableColumn("GPose Player");
                ImGuiUtil.DrawTableColumn($"{_objectManager.GPosePlayer.Utf8Name} ({_objectManager.GPosePlayer.Index})");
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable(_objectManager.GPosePlayer.ToString());
            }

            ImGuiUtil.DrawTableColumn("Number of Players");
            ImGuiUtil.DrawTableColumn(_objectManager.Count.ToString());
            ImGui.TableNextColumn();
        }

        var filterChanged = ImGui.InputTextWithHint("##Filter", "Filter...", ref _objectFilter, 64);
        using var table2 = ImRaii.Table("##data2", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.ScrollY,
            new Vector2(-1, 20 * ImGui.GetTextLineHeightWithSpacing()));
        if (!table2)
            return;

        if (filterChanged)
            ImGui.SetScrollY(0);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();

        var remainder = ImGuiClip.FilteredClippedDraw(_objectManager, skips,
            p => p.Value.Label.Contains(_objectFilter, StringComparison.OrdinalIgnoreCase), p
                =>
            {
                ImGuiUtil.DrawTableColumn(p.Key.ToString());
                ImGuiUtil.DrawTableColumn(p.Value.Label);
                ImGuiUtil.DrawTableColumn(string.Join(", ", p.Value.Objects.OrderBy(a => a.Index).Select(a => a.Index.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeightWithSpacing());
    }

    private string            _datFilePath = string.Empty;
    private DatCharacterFile? _datFile     = null;

    private void DrawDatFiles()
    {
        using var tree = ImRaii.TreeNode("Character Dat File");
        if (!tree)
            return;

        ImGui.InputTextWithHint("##datFilePath", "Dat File Path...", ref _datFilePath, 256);
        var exists = _datFilePath.Length > 0 && File.Exists(_datFilePath);
        if (ImGuiUtil.DrawDisabledButton("Load##Dat", Vector2.Zero, string.Empty, !exists))
            _datFile = _importService.LoadDat(_datFilePath, out var tmp) ? tmp : null;

        if (ImGuiUtil.DrawDisabledButton("Save##Dat", Vector2.Zero, string.Empty, _datFilePath.Length == 0 || _datFile == null))
            _importService.SaveDesignAsDat(_datFilePath, _datFile!.Value.Customize, _datFile!.Value.Description);

        if (_datFile != null)
        {
            ImGui.TextUnformatted(_datFile.Value.Magic.ToString());
            ImGui.TextUnformatted(_datFile.Value.Version.ToString());
            ImGui.TextUnformatted(_datFile.Value.Time.LocalDateTime.ToString("g"));
            ImGui.TextUnformatted(_datFile.Value.Voice.ToString());
            ImGui.TextUnformatted(_datFile.Value.Customize.Data.ToString());
            ImGui.TextUnformatted(_datFile.Value.Description);
        }
    }

    private void DrawVisor(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Visor");
        ImGuiUtil.DrawTableColumn("Visor State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.AsCharacter->DrawData.IsVisorToggled.ToString() : "No Character");
        ImGuiUtil.DrawTableColumn(model.IsHuman ? VisorService.GetVisorState(model).ToString() : "No Human");
        ImGui.TableNextColumn();
        if (!model.IsHuman)
            return;

        if (ImGui.SmallButton("Set True"))
            _visorService.SetVisorState(model, true);
        ImGui.SameLine();
        if (ImGui.SmallButton("Set False"))
            _visorService.SetVisorState(model, false);
        ImGui.SameLine();
        if (ImGui.SmallButton("Toggle"))
            _visorService.SetVisorState(model, !VisorService.GetVisorState(model));
    }

    private void DrawHatState(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("HatState");
        ImGuiUtil.DrawTableColumn("Hat State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsHatHidden ? "Hidden" : actor.GetArmor(EquipSlot.Head).ToString()
            : "No Character");
        ImGuiUtil.DrawTableColumn(model.IsHuman
            ? model.AsHuman->Head.Value == 0 ? "No Hat" : model.GetArmor(EquipSlot.Head).ToString()
            : "No Human");
        ImGui.TableNextColumn();
        if (!model.IsHuman)
            return;

        if (ImGui.SmallButton("Hide"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, CharacterArmor.Empty, null);
        ImGui.SameLine();
        if (ImGui.SmallButton("Show"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, actor.GetArmor(EquipSlot.Head), actor.GetCrest(EquipSlot.Head));
        ImGui.SameLine();
        if (ImGui.SmallButton("Toggle"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head,
                model.AsHuman->Head.Value == 0 ? actor.GetArmor(EquipSlot.Head) : CharacterArmor.Empty,
                model.AsHuman->Head.Value == 0 ? actor.GetCrest(EquipSlot.Head) : null);
    }

    private void DrawWeaponState(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("WeaponState");
        ImGuiUtil.DrawTableColumn("Weapon State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsWeaponHidden ? "Hidden" : "Visible"
            : "No Character");
        var text = string.Empty;

        if (!model.IsHuman)
        {
            text = "No Model";
        }
        else if (model.AsDrawObject->Object.ChildObject == null)
        {
            text = "No Weapon";
        }
        else
        {
            var weapon = (DrawObject*)model.AsDrawObject->Object.ChildObject;
            if ((weapon->Flags & 0x09) == 0x09)
                text = "Visible";
            else
                text = "Hidden";
        }

        ImGuiUtil.DrawTableColumn(text);
        ImGui.TableNextColumn();
        if (!model.IsHuman)
            return;
    }

    private void DrawWetness(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Wetness");
        ImGuiUtil.DrawTableColumn("Wetness");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.AsCharacter->IsGPoseWet ? "GPose" : "None" : "No Character");
        var modelString = model.IsCharacterBase
            ? $"{model.AsCharacterBase->SwimmingWetness:F4} Swimming\n"
          + $"{model.AsCharacterBase->WeatherWetness:F4} Weather\n"
          + $"{model.AsCharacterBase->ForcedWetness:F4} Forced\n"
          + $"{model.AsCharacterBase->WetnessDepth:F4} Depth\n"
            : "No CharacterBase";
        ImGuiUtil.DrawTableColumn(modelString);
        ImGui.TableNextColumn();
        if (!actor.IsCharacter)
            return;

        if (ImGui.SmallButton("GPose On"))
            actor.AsCharacter->IsGPoseWet = true;
        ImGui.SameLine();
        if (ImGui.SmallButton("GPose Off"))
            actor.AsCharacter->IsGPoseWet = false;
        ImGui.SameLine();
        if (ImGui.SmallButton("GPose Toggle"))
            actor.AsCharacter->IsGPoseWet = !actor.AsCharacter->IsGPoseWet;
    }

    private void DrawEquip(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Equipment");
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            using var id2 = ImRaii.PushId((int)slot);
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetArmor(slot).ToString() : "No Character");
            ImGuiUtil.DrawTableColumn(model.IsHuman ? model.GetArmor(slot).ToString() : "No Human");
            ImGui.TableNextColumn();
            if (!model.IsHuman)
                continue;

            if (ImGui.SmallButton("Change Piece"))
                _updateSlotService.UpdateArmor(model, slot,
                    new CharacterArmor((SetId)(slot == EquipSlot.Hands ? 6064 : slot == EquipSlot.Head ? 6072 : 1), 1, 0));
            ImGui.SameLine();
            if (ImGui.SmallButton("Change Stain"))
                _updateSlotService.UpdateStain(model, slot, 5);
            ImGui.SameLine();
            if (ImGui.SmallButton("Toggle Crest"))
                _updateSlotService.UpdateCrest(model, slot, !model.IsFreeCompanyCrestVisibleOnSlot((byte)slot.ToIndex()));
            ImGui.SameLine();
            if (ImGui.SmallButton("Reset"))
                _updateSlotService.UpdateSlot(model, slot, actor.GetArmor(slot), actor.GetCrest(slot));
        }
    }

    private void DrawCustomize(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Customize");
        var actorCustomize = new Customize(actor.IsCharacter
            ? *(Penumbra.GameData.Structs.CustomizeData*)&actor.AsCharacter->DrawData.CustomizeData
            : new Penumbra.GameData.Structs.CustomizeData());
        var modelCustomize = new Customize(model.IsHuman
            ? *(Penumbra.GameData.Structs.CustomizeData*)model.AsHuman->Customize.Data
            : new Penumbra.GameData.Structs.CustomizeData());
        foreach (var type in Enum.GetValues<CustomizeIndex>())
        {
            using var id2 = ImRaii.PushId((int)type);
            ImGuiUtil.DrawTableColumn(type.ToDefaultName());
            ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actorCustomize[type].Value.ToString("X2") : "No Character");
            ImGuiUtil.DrawTableColumn(model.IsHuman ? modelCustomize[type].Value.ToString("X2") : "No Human");
            ImGui.TableNextColumn();
            if (!model.IsHuman || type.ToFlag().RequiresRedraw())
                continue;

            if (ImGui.SmallButton("++"))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value + (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize.Data);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("--"))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value - (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize.Data);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Reset"))
            {
                modelCustomize.Set(type, actorCustomize[type]);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize.Data);
            }
        }
    }

    #endregion

    #region Penumbra

    private Model _drawObject = Model.Null;

    private void DrawPenumbraHeader()
    {
        if (!ImGui.CollapsingHeader("Penumbra"))
            return;

        using var table = ImRaii.Table("##PenumbraTable", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn("Available");
        ImGuiUtil.DrawTableColumn(_penumbra.Available.ToString());
        ImGui.TableNextColumn();
        if (ImGui.SmallButton("Unattach"))
            _penumbra.Unattach();
        ImGui.SameLine();
        if (ImGui.SmallButton("Reattach"))
            _penumbra.Reattach();

        ImGuiUtil.DrawTableColumn("Draw Object");
        ImGui.TableNextColumn();
        var address = _drawObject.Address;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputScalar("##drawObjectPtr", ImGuiDataType.U64, (nint)(&address), IntPtr.Zero, IntPtr.Zero, "%llx",
                ImGuiInputTextFlags.CharsHexadecimal))
            _drawObject = address;
        ImGuiUtil.DrawTableColumn(_penumbra.Available
            ? $"0x{_penumbra.GameObjectFromDrawObject(_drawObject).Address:X}"
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Cutscene Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##CutsceneIndex", ref _gameObjectIndex, 0, 0);
        ImGuiUtil.DrawTableColumn(_penumbra.Available
            ? _penumbra.CutsceneParent(_gameObjectIndex).ToString()
            : "Penumbra Unavailable");

        ImGuiUtil.DrawTableColumn("Redraw Object");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##redrawObject", ref _gameObjectIndex, 0, 0);
        ImGui.TableNextColumn();
        using (var disabled = ImRaii.Disabled(!_penumbra.Available))
        {
            if (ImGui.SmallButton("Redraw"))
                _penumbra.RedrawObject((ObjectIndex)_gameObjectIndex, RedrawType.Redraw);
        }

        ImGuiUtil.DrawTableColumn("Last Tooltip Date");
        ImGuiUtil.DrawTableColumn(_penumbraTooltip.LastTooltip > DateTime.MinValue ? _penumbraTooltip.LastTooltip.ToLongTimeString() : "Never");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Last Click Date");
        ImGuiUtil.DrawTableColumn(_penumbraTooltip.LastClick > DateTime.MinValue ? _penumbraTooltip.LastClick.ToLongTimeString() : "Never");
        ImGui.TableNextColumn();

        ImGui.Separator();
        ImGui.Separator();
        foreach (var (slot, item) in _penumbraTooltip.LastItems)
        {
            ImGuiUtil.DrawTableColumn($"{slot.ToName()} Revert-Item");
            ImGuiUtil.DrawTableColumn(item.Valid ? item.Name : "None");
            ImGui.TableNextColumn();
        }
    }

    #endregion

    #region GameData

    private void DrawGameDataHeader()
    {
        if (!ImGui.CollapsingHeader("Game Data"))
            return;

        DrawIdentifierService();
        DrawRestrictedGear();
        DrawActorService();
        DrawItemService();
        DrawStainService();
        DrawCustomizationService();
        DrawJobService();
    }

    private void DrawJobService()
    {
        using var tree = ImRaii.TreeNode("Job Service");
        if (!tree)
            return;

        using (var t = ImRaii.TreeNode("Jobs"))
        {
            if (t)
            {
                using var table = ImRaii.Table("##jobs", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
                if (table)
                    foreach (var (id, job) in _jobs.Jobs)
                    {
                        ImGuiUtil.DrawTableColumn(id.ToString("D3"));
                        ImGuiUtil.DrawTableColumn(job.Name);
                        ImGuiUtil.DrawTableColumn(job.Abbreviation);
                    }
            }
        }

        using (var t = ImRaii.TreeNode("All Job Groups"))
        {
            if (t)
            {
                using var table = ImRaii.Table("##groups", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
                if (table)
                    foreach (var (group, idx) in _jobs.AllJobGroups.WithIndex())
                    {
                        ImGuiUtil.DrawTableColumn(idx.ToString("D3"));
                        ImGuiUtil.DrawTableColumn(group.Name);
                        ImGuiUtil.DrawTableColumn(group.Count.ToString());
                    }
            }
        }

        using (var t = ImRaii.TreeNode("Valid Job Groups"))
        {
            if (t)
            {
                using var table = ImRaii.Table("##groups", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
                if (table)
                    foreach (var (id, group) in _jobs.JobGroups)
                    {
                        ImGuiUtil.DrawTableColumn(id.ToString("D3"));
                        ImGuiUtil.DrawTableColumn(group.Name);
                        ImGuiUtil.DrawTableColumn(group.Count.ToString());
                    }
            }
        }
    }

    private string _gamePath = string.Empty;
    private int    _setId;
    private int    _secondaryId;
    private int    _variant;

    private void DrawIdentifierService()
    {
        using var disabled = ImRaii.Disabled(!_items.IdentifierService.Valid);
        using var tree     = ImRaii.TreeNode("Identifier Service");
        if (!tree || !_items.IdentifierService.Valid)
            return;

        disabled.Dispose();


        static void Text(string text)
        {
            if (text.Length > 0)
                ImGui.TextUnformatted(text);
        }

        ImGui.TextUnformatted("Parse Game Path");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##gamePath", "Enter game path...", ref _gamePath, 256);
        var fileInfo = _items.IdentifierService.AwaitedService.GamePathParser.GetFileInfo(_gamePath);
        ImGui.TextUnformatted(
            $"{fileInfo.ObjectType} {fileInfo.EquipSlot} {fileInfo.PrimaryId} {fileInfo.SecondaryId} {fileInfo.Variant} {fileInfo.BodySlot} {fileInfo.CustomizationType}");
        Text(string.Join("\n", _items.IdentifierService.AwaitedService.Identify(_gamePath).Keys));

        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Identify Model");
        ImGui.SameLine();
        DrawInputModelSet(true);

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var identified = _items.Identify(slot, (SetId)_setId, (Variant)_variant);
            Text(identified.Name);
            ImGuiUtil.HoverTooltip(string.Join("\n",
                _items.IdentifierService.AwaitedService.Identify((SetId)_setId, (Variant)_variant, slot)
                    .Select(i => $"{i.Name} {i.Id} {i.ItemId} {i.IconId}")));
        }

        var weapon = _items.Identify(EquipSlot.MainHand, (SetId)_setId, (WeaponType)_secondaryId, (Variant)_variant);
        Text(weapon.Name);
        ImGuiUtil.HoverTooltip(string.Join("\n",
            _items.IdentifierService.AwaitedService.Identify((SetId)_setId, (WeaponType)_secondaryId, (Variant)_variant, EquipSlot.MainHand)));
    }

    private void DrawRestrictedGear()
    {
        using var tree = ImRaii.TreeNode("Restricted Gear Service");
        if (!tree)
            return;

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Resolve Model");
        DrawInputModelSet(false);
        foreach (var race in Enum.GetValues<Race>().Skip(1))
        {
            foreach (var gender in new[]
                     {
                         Gender.Male,
                         Gender.Female,
                     })
            {
                foreach (var slot in EquipSlotExtensions.EqdpSlots)
                {
                    var (replaced, model) =
                        _items.RestrictedGear.ResolveRestricted(new CharacterArmor((SetId)_setId, (Variant)_variant, 0), slot, race, gender);
                    if (replaced)
                        ImGui.TextUnformatted($"{race.ToName()} - {gender} - {slot.ToName()} resolves to {model}.");
                }
            }
        }
    }

    private void DrawInputModelSet(bool withWeapon)
    {
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##SetId", ref _setId, 0, 0);
        if (withWeapon)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            ImGui.InputInt("##TypeId", ref _secondaryId, 0, 0);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("##Variant", ref _variant, 0, 0);
    }

    private string _bnpcFilter      = string.Empty;
    private string _enpcFilter      = string.Empty;
    private string _companionFilter = string.Empty;
    private string _mountFilter     = string.Empty;
    private string _ornamentFilter  = string.Empty;
    private string _worldFilter     = string.Empty;

    private void DrawActorService()
    {
        using var disabled = ImRaii.Disabled(!_actors.Valid);
        using var tree     = ImRaii.TreeNode("Actor Service");
        if (!tree || !_actors.Valid)
            return;

        disabled.Dispose();

        DrawBnpcTable();
        DrawNameTable("ENPCs",      ref _enpcFilter,      _actors.AwaitedService.Data.ENpcs.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Companions", ref _companionFilter, _actors.AwaitedService.Data.Companions.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Mounts",     ref _mountFilter,     _actors.AwaitedService.Data.Mounts.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Ornaments",  ref _ornamentFilter,  _actors.AwaitedService.Data.Ornaments.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Worlds",     ref _worldFilter,     _actors.AwaitedService.Data.Worlds.Select(kvp => ((uint)kvp.Key, kvp.Value)));
    }

    private void DrawBnpcTable()
    {
        using var _    = ImRaii.PushId(1);
        using var tree = ImRaii.TreeNode("BNPCs");
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref _bnpcFilter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("3", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextRow();
        var data = _actors.AwaitedService.Data.BNpcs.Select(kvp => (kvp.Key, kvp.Key.ToString("D5"), kvp.Value));
        var remainder = ImGuiClip.FilteredClippedDraw(data, skips,
            p => p.Item2.Contains(_bnpcFilter) || p.Item3.Contains(_bnpcFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item2);
                ImGuiUtil.DrawTableColumn(p.Item3);
                var bnpcs = _items.IdentifierService.AwaitedService.GetBnpcsFromName(p.Item1);
                ImGuiUtil.DrawTableColumn(string.Join(", ", bnpcs.Select(b => b.Id.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    private static void DrawNameTable(string label, ref string filter, IEnumerable<(uint, string)> names)
    {
        using var _    = ImRaii.PushId(label);
        using var tree = ImRaii.TreeNode(label);
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref filter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextColumn();
        var f = filter;
        var remainder = ImGuiClip.FilteredClippedDraw(names.Select(p => (p.Item1.ToString("D5"), p.Item2)), skips,
            p => p.Item1.Contains(f) || p.Item2.Contains(f, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item1);
                ImGuiUtil.DrawTableColumn(p.Item2);
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    private string _itemFilter = string.Empty;

    private void DrawItemService()
    {
        using var disabled = ImRaii.Disabled(!_items.ItemService.Valid);
        using var tree     = ImRaii.TreeNode("Item Manager");
        if (!tree || !_items.ItemService.Valid)
            return;

        disabled.Dispose();
        ImRaii.TreeNode($"Default Sword: {_items.DefaultSword.Name} ({_items.DefaultSword.ItemId}) ({_items.DefaultSword.Weapon()})",
            ImGuiTreeNodeFlags.Leaf).Dispose();
        DrawNameTable("All Items (Main)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(true).Select(p => (p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        DrawNameTable("All Items (Off)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(false).Select(p => (p.Item1.Id,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        foreach (var type in Enum.GetValues<FullEquipType>().Skip(1))
        {
            DrawNameTable(type.ToName(), ref _itemFilter,
                _items.ItemService.AwaitedService[type]
                    .Select(p => (Id: p.ItemId.Id, $"{p.Name} ({(p.WeaponType == 0 ? p.Armor().ToString() : p.Weapon().ToString())})")));
        }
    }

    private string _stainFilter = string.Empty;

    private void DrawStainService()
    {
        using var tree = ImRaii.TreeNode("Stain Service");
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref _stainFilter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextRow();
        var remainder = ImGuiClip.FilteredClippedDraw(_items.Stains, skips,
            p => p.Key.Id.ToString().Contains(_stainFilter) || p.Value.Name.Contains(_stainFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Key.Id.ToString("D3"));
                ImGui.TableNextColumn();
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetCursorScreenPos(),
                    ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()),
                    p.Value.RgbaColor, 5 * ImGuiHelpers.GlobalScale);
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
                ImGuiUtil.DrawTableColumn(p.Value.Name);
                ImGuiUtil.DrawTableColumn($"#{p.Value.R:X2}{p.Value.G:X2}{p.Value.B:X2}{(p.Value.Gloss ? ", Glossy" : string.Empty)}");
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    private void DrawCustomizationService()
    {
        using var disabled = ImRaii.Disabled(!_customization.Valid);
        using var tree     = ImRaii.TreeNode("Customization Service");
        if (!tree || !_customization.Valid)
            return;

        disabled.Dispose();

        foreach (var clan in _customization.AwaitedService.Clans)
        {
            foreach (var gender in _customization.AwaitedService.Genders)
            {
                var set = _customization.AwaitedService.GetList(clan, gender);
                DrawCustomizationInfo(set);
                DrawNpcCustomizationInfo(set);
            }
        }
    }

    private void DrawCustomizationInfo(CustomizationSet set)
    {
        using var tree = ImRaii.TreeNode($"{_customization.ClanName(set.Clan, set.Gender)} {set.Gender}");
        if (!tree)
            return;

        using var table = ImRaii.Table("data", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            ImGuiUtil.DrawTableColumn(index.ToString());
            ImGuiUtil.DrawTableColumn(set.Option(index));
            ImGuiUtil.DrawTableColumn(set.IsAvailable(index) ? "Available" : "Unavailable");
            ImGuiUtil.DrawTableColumn(set.Type(index).ToString());
            ImGuiUtil.DrawTableColumn(set.Count(index).ToString());
        }
    }

    private void DrawNpcCustomizationInfo(CustomizationSet set)
    {
        using var tree = ImRaii.TreeNode($"{_customization.ClanName(set.Clan, set.Gender)} {set.Gender} (NPC Options)");
        if (!tree)
            return;

        using var table = ImRaii.Table("npc", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (index, value) in set.NpcOptions)
        {
            ImGuiUtil.DrawTableColumn(index.ToString());
            ImGuiUtil.DrawTableColumn(value.Value.ToString());
        }
    }

    #endregion

    #region Designs

    private string     _base64       = string.Empty;
    private string     _restore      = string.Empty;
    private byte[]     _base64Bytes  = Array.Empty<byte>();
    private byte[]     _restoreBytes = Array.Empty<byte>();
    private DesignData _parse64      = new();
    private Exception? _parse64Failure;

    private void DrawDesigns()
    {
        if (!ImGui.CollapsingHeader("Designs"))
            return;

        DrawDesignManager();
        DrawDesignTester();
        DrawDesignConverter();
    }

    private void DrawDesignManager()
    {
        using var tree = ImRaii.TreeNode($"Design Manager ({_designManager.Designs.Count} Designs)###Design Manager");
        if (!tree)
            return;

        foreach (var (design, idx) in _designManager.Designs.WithIndex())
        {
            using var t = ImRaii.TreeNode($"{design.Name}##{idx}");
            if (!t)
                continue;

            DrawDesign(design);
            var base64 = DesignBase64Migration.CreateOldBase64(design.DesignData, design.ApplyEquip, design.ApplyCustomizeRaw,
                design.DoApplyHatVisible(),
                design.DoApplyVisorToggle(), design.DoApplyWeaponVisible(), design.WriteProtected());
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(base64);
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(base64);
        }
    }

    private void DrawDesignTester()
    {
        using var tree = ImRaii.TreeNode("Base64 Design Tester");
        if (!tree)
            return;

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##base64", "Base 64 input...", ref _base64, 2047);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            try
            {
                _base64Bytes    = Convert.FromBase64String(_base64);
                _parse64Failure = null;
            }
            catch (Exception ex)
            {
                _base64Bytes    = Array.Empty<byte>();
                _parse64Failure = ex;
            }

            if (_parse64Failure == null)
                try
                {
                    _parse64 = DesignBase64Migration.MigrateBase64(_items, _humans, _base64, out var ef, out var cf, out var wp, out var ah,
                        out var av,
                        out var aw);
                    _restore      = DesignBase64Migration.CreateOldBase64(in _parse64, ef, cf, ah, av, aw, wp);
                    _restoreBytes = Convert.FromBase64String(_restore);
                }
                catch (Exception ex)
                {
                    _parse64Failure = ex;
                    _restore        = string.Empty;
                }
        }

        if (_parse64Failure != null)
        {
            ImGuiUtil.TextWrapped(_parse64Failure.ToString());
        }
        else if (_restore.Length > 0)
        {
            DrawDesignData(_parse64);
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TextUnformatted(_base64);
            using (var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 }))
            {
                foreach (var (c1, c2) in _restore.Zip(_base64))
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF4040D0, c1 != c2);
                    ImGui.TextUnformatted(c1.ToString());
                    ImGui.SameLine();
                }
            }

            ImGui.NewLine();

            foreach (var ((b1, b2), idx) in _base64Bytes.Zip(_restoreBytes).WithIndex())
            {
                using (var group = ImRaii.Group())
                {
                    ImGui.TextUnformatted(idx.ToString("D2"));
                    ImGui.TextUnformatted(b1.ToString("X2"));
                    using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF4040D0, b1 != b2);
                    ImGui.TextUnformatted(b2.ToString("X2"));
                }

                ImGui.SameLine();
            }

            ImGui.NewLine();
        }

        if (_parse64Failure != null && _base64Bytes.Length > 0)
        {
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            foreach (var (b, idx) in _base64Bytes.WithIndex())
            {
                using (var group = ImRaii.Group())
                {
                    ImGui.TextUnformatted(idx.ToString("D2"));
                    ImGui.TextUnformatted(b.ToString("X2"));
                }

                ImGui.SameLine();
            }

            ImGui.NewLine();
        }
    }

    private string      _clipboardText    = string.Empty;
    private byte[]      _clipboardData    = Array.Empty<byte>();
    private byte[]      _dataUncompressed = Array.Empty<byte>();
    private byte        _version          = 0;
    private string      _textUncompressed = string.Empty;
    private JObject?    _json             = null;
    private DesignBase? _tmpDesign        = null;
    private Exception?  _clipboardProblem = null;

    private void DrawDesignConverter()
    {
        using var tree = ImRaii.TreeNode("Design Converter");
        if (!tree)
            return;

        if (ImGui.Button("Import Clipboard"))
        {
            _clipboardText    = string.Empty;
            _clipboardData    = Array.Empty<byte>();
            _dataUncompressed = Array.Empty<byte>();
            _textUncompressed = string.Empty;
            _json             = null;
            _tmpDesign        = null;
            _clipboardProblem = null;

            try
            {
                _clipboardText = ImGui.GetClipboardText();
                _clipboardData = Convert.FromBase64String(_clipboardText);
                _version       = _clipboardData[0];
                if (_version == 5)
                    _clipboardData = _clipboardData[DesignBase64Migration.Base64SizeV4..];
                _version          = _clipboardData.Decompress(out _dataUncompressed);
                _textUncompressed = Encoding.UTF8.GetString(_dataUncompressed);
                _json             = JObject.Parse(_textUncompressed);
                _tmpDesign        = _designConverter.FromBase64(_clipboardText, true, true, out _);
            }
            catch (Exception ex)
            {
                _clipboardProblem = ex;
            }
        }

        if (_clipboardText.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_clipboardText);
        }

        if (_clipboardData.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(string.Join(" ", _clipboardData.Select(b => b.ToString("X2"))));
        }

        if (_dataUncompressed.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(string.Join(" ", _dataUncompressed.Select(b => b.ToString("X2"))));
        }

        if (_textUncompressed.Length > 0)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_textUncompressed);
        }

        if (_json != null)
            ImGui.TextUnformatted("JSON Parsing Successful!");

        if (_tmpDesign != null)
            DrawDesign(_tmpDesign);

        if (_clipboardProblem != null)
        {
            using var f = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGuiUtil.TextWrapped(_clipboardProblem.ToString());
        }
    }

    public void DrawState(ActorData data, ActorState state)
    {
        using var table = ImRaii.Table("##state", 9, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn("Name");
        ImGuiUtil.DrawTableColumn(state.Identifier.ToString());
        ImGui.TableNextColumn();
        if (ImGui.Button("Reset"))
            _state.ResetState(state, StateChanged.Source.Manual);

        ImGui.TableNextRow();

        static void PrintRow<T>(string label, T actor, T model, StateChanged.Source source) where T : notnull
        {
            ImGuiUtil.DrawTableColumn(label);
            ImGuiUtil.DrawTableColumn(actor.ToString()!);
            ImGuiUtil.DrawTableColumn(model.ToString()!);
            ImGuiUtil.DrawTableColumn(source.ToString());
        }

        static string ItemString(in DesignData data, EquipSlot slot)
        {
            var item = data.Item(slot);
            return $"{item.Name} ({item.ModelId.Id}{(item.WeaponType != 0 ? $"-{item.WeaponType.Id}" : string.Empty)}-{item.Variant})";
        }

        PrintRow("Model ID", state.BaseData.ModelId, state.ModelData.ModelId, state[ActorState.MetaIndex.ModelId]);
        ImGui.TableNextRow();
        PrintRow("Wetness", state.BaseData.IsWet(), state.ModelData.IsWet(), state[ActorState.MetaIndex.Wetness]);
        ImGui.TableNextRow();

        if (state.BaseData.IsHuman && state.ModelData.IsHuman)
        {
            PrintRow("Hat Visible", state.BaseData.IsHatVisible(), state.ModelData.IsHatVisible(), state[ActorState.MetaIndex.HatState]);
            ImGui.TableNextRow();
            PrintRow("Visor Toggled", state.BaseData.IsVisorToggled(), state.ModelData.IsVisorToggled(),
                state[ActorState.MetaIndex.VisorState]);
            ImGui.TableNextRow();
            PrintRow("Weapon Visible", state.BaseData.IsWeaponVisible(), state.ModelData.IsWeaponVisible(),
                state[ActorState.MetaIndex.WeaponState]);
            ImGui.TableNextRow();
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                PrintRow(slot.ToName(), ItemString(state.BaseData, slot), ItemString(state.ModelData, slot), state[slot, ActorState.EquipField.Item]);
                ImGuiUtil.DrawTableColumn(state.BaseData.Stain(slot).Id.ToString());
                ImGuiUtil.DrawTableColumn(state.ModelData.Stain(slot).Id.ToString());
                ImGuiUtil.DrawTableColumn(state[slot, ActorState.EquipField.Stain].ToString());
                ImGuiUtil.DrawTableColumn(state.BaseData.Crest(slot).ToString());
                ImGuiUtil.DrawTableColumn(state[slot, ActorState.EquipField.Crest].ToString());
            }

            foreach (var type in Enum.GetValues<CustomizeIndex>())
            {
                PrintRow(type.ToDefaultName(), state.BaseData.Customize[type].Value, state.ModelData.Customize[type].Value, state[type]);
                ImGui.TableNextRow();
            }
        }
        else
        {
            ImGuiUtil.DrawTableColumn(string.Join(" ", state.BaseData.GetCustomizeBytes().Select(b => b.ToString("X2"))));
            ImGuiUtil.DrawTableColumn(string.Join(" ", state.ModelData.GetCustomizeBytes().Select(b => b.ToString("X2"))));
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn(string.Join(" ", state.BaseData.GetEquipmentBytes().Select(b => b.ToString("X2"))));
            ImGuiUtil.DrawTableColumn(string.Join(" ", state.ModelData.GetEquipmentBytes().Select(b => b.ToString("X2"))));
        }
    }

    public static void DrawDesignData(in DesignData data)
    {
        if (data.IsHuman)
        {
            using var table = ImRaii.Table("##equip", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
            if (!table)
                return;

            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                var item  = data.Item(slot);
                var stain = data.Stain(slot);
                ImGuiUtil.DrawTableColumn(slot.ToName());
                ImGuiUtil.DrawTableColumn(item.Name);
                ImGuiUtil.DrawTableColumn(item.ItemId.ToString());
                ImGuiUtil.DrawTableColumn(stain.ToString());
            }

            ImGuiUtil.DrawTableColumn("Hat Visible");
            ImGuiUtil.DrawTableColumn(data.IsHatVisible().ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Visor Toggled");
            ImGuiUtil.DrawTableColumn(data.IsVisorToggled().ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Weapon Visible");
            ImGuiUtil.DrawTableColumn(data.IsWeaponVisible().ToString());
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Model ID");
            ImGuiUtil.DrawTableColumn(data.ModelId.ToString());
            ImGui.TableNextRow();

            foreach (var index in Enum.GetValues<CustomizeIndex>())
            {
                var value = data.Customize[index];
                ImGuiUtil.DrawTableColumn(index.ToDefaultName());
                ImGuiUtil.DrawTableColumn(value.Value.ToString());
                ImGui.TableNextRow();
            }

            ImGuiUtil.DrawTableColumn("Is Wet");
            ImGuiUtil.DrawTableColumn(data.IsWet().ToString());
            ImGui.TableNextRow();
        }
        else
        {
            ImGui.TextUnformatted($"Model ID {data.ModelId}");
            ImGui.Separator();
            using var font = ImRaii.PushFont(UiBuilder.MonoFont);
            ImGui.TextUnformatted("Customize Array");
            ImGui.Separator();
            ImGuiUtil.TextWrapped(string.Join(" ", data.GetCustomizeBytes().Select(b => b.ToString("X2"))));

            ImGui.TextUnformatted("Equipment Array");
            ImGui.Separator();
            ImGuiUtil.TextWrapped(string.Join(" ", data.GetEquipmentBytes().Select(b => b.ToString("X2"))));
        }
    }

    private void DrawDesign(DesignBase design)
    {
        using var table = ImRaii.Table("##equip", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (design is Design d)
        {
            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(d.Name);
            ImGuiUtil.DrawTableColumn($"({d.Index})");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Description (Hover)");
            ImGuiUtil.HoverTooltip(d.Description);
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Identifier");
            ImGuiUtil.DrawTableColumn(d.Identifier.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Design File System Path");
            ImGuiUtil.DrawTableColumn(_designFileSystem.FindLeaf(d, out var leaf) ? leaf.FullName() : "No Path Known");
            ImGui.TableNextRow();

            ImGuiUtil.DrawTableColumn("Creation");
            ImGuiUtil.DrawTableColumn(d.CreationDate.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Update");
            ImGuiUtil.DrawTableColumn(d.LastEdit.ToString());
            ImGui.TableNextRow();
            ImGuiUtil.DrawTableColumn("Tags");
            ImGuiUtil.DrawTableColumn(string.Join(", ", d.Tags));
            ImGui.TableNextRow();
        }

        foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
        {
            var item       = design.DesignData.Item(slot);
            var apply      = design.DoApplyEquip(slot);
            var stain      = design.DesignData.Stain(slot);
            var applyStain = design.DoApplyStain(slot);
            var crest      = design.DesignData.Crest(slot);
            var applyCrest = design.DoApplyCrest(slot);
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(item.Name);
            ImGuiUtil.DrawTableColumn(item.ItemId.ToString());
            ImGuiUtil.DrawTableColumn(apply ? "Apply" : "Keep");
            ImGuiUtil.DrawTableColumn(stain.ToString());
            ImGuiUtil.DrawTableColumn(applyStain ? "Apply" : "Keep");
            ImGuiUtil.DrawTableColumn(crest.ToString());
            ImGuiUtil.DrawTableColumn(applyCrest ? "Apply" : "Keep");
        }

        ImGuiUtil.DrawTableColumn("Hat Visible");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsHatVisible().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyHatVisible() ? "Apply" : "Keep");
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Visor Toggled");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsVisorToggled().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyVisorToggle() ? "Apply" : "Keep");
        ImGui.TableNextRow();
        ImGuiUtil.DrawTableColumn("Weapon Visible");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsWeaponVisible().ToString());
        ImGuiUtil.DrawTableColumn(design.DoApplyWeaponVisible() ? "Apply" : "Keep");
        ImGui.TableNextRow();

        ImGuiUtil.DrawTableColumn("Model ID");
        ImGuiUtil.DrawTableColumn(design.DesignData.ModelId.ToString());
        ImGui.TableNextRow();

        foreach (var index in Enum.GetValues<CustomizeIndex>())
        {
            var value = design.DesignData.Customize[index];
            var apply = design.DoApplyCustomize(index);
            ImGuiUtil.DrawTableColumn(index.ToDefaultName());
            ImGuiUtil.DrawTableColumn(value.Value.ToString());
            ImGuiUtil.DrawTableColumn(apply ? "Apply" : "Keep");
            ImGui.TableNextRow();
        }

        ImGuiUtil.DrawTableColumn("Is Wet");
        ImGuiUtil.DrawTableColumn(design.DesignData.IsWet().ToString());
        ImGui.TableNextRow();
    }

    #endregion

    #region State

    private void DrawState()
    {
        if (!ImGui.CollapsingHeader($"State ({_state.Count})###State"))
            return;

        DrawActorTrees();
        DrawRetainedStates();
    }

    private void DrawActorTrees()
    {
        using var tree = ImRaii.TreeNode("Active Actors");
        if (!tree)
            return;

        _objectManager.Update();
        foreach (var (identifier, actors) in _objectManager)
        {
            if (ImGuiUtil.DrawDisabledButton($"{FontAwesomeIcon.Trash.ToIconString()}##{actors.Label}", new Vector2(ImGui.GetFrameHeight()),
                    string.Empty, !_state.ContainsKey(identifier), true))
                _state.DeleteState(identifier);

            ImGui.SameLine();
            using var t = ImRaii.TreeNode(actors.Label);
            if (!t)
                continue;

            if (_state.GetOrCreate(identifier, actors.Objects[0], out var state))
                DrawState(actors, state);
            else
                ImGui.TextUnformatted("Invalid actor.");
        }
    }

    private void DrawRetainedStates()
    {
        using var tree = ImRaii.TreeNode("Retained States (Inactive Actors)");
        if (!tree)
            return;

        foreach (var (identifier, state) in _state.Where(kvp => !_objectManager.ContainsKey(kvp.Key)))
        {
            using var t = ImRaii.TreeNode(identifier.ToString());
            if (t)
                DrawState(ActorData.Invalid, state);
        }
    }

    #endregion

    #region Auto Designs

    private void DrawAutoDesigns()
    {
        if (!ImGui.CollapsingHeader("Auto Designs"))
            return;

        foreach (var (set, idx) in _autoDesignManager.WithIndex())
        {
            using var id   = ImRaii.PushId(idx);
            using var tree = ImRaii.TreeNode(set.Name);
            if (!tree)
                continue;

            using var table = ImRaii.Table("##autoDesign", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
            if (!table)
                continue;

            ImGuiUtil.DrawTableColumn("Name");
            ImGuiUtil.DrawTableColumn(set.Name);

            ImGuiUtil.DrawTableColumn("Index");
            ImGuiUtil.DrawTableColumn(idx.ToString());

            ImGuiUtil.DrawTableColumn("Enabled");
            ImGuiUtil.DrawTableColumn(set.Enabled.ToString());

            ImGuiUtil.DrawTableColumn("Actor");
            ImGuiUtil.DrawTableColumn(set.Identifiers[0].ToString());

            foreach (var (design, designIdx) in set.Designs.WithIndex())
            {
                ImGuiUtil.DrawTableColumn($"{design.Name(false)} ({designIdx})");
                ImGuiUtil.DrawTableColumn($"{design.ApplicationType} {design.Jobs.Name}");
            }
        }
    }

    #endregion

    #region Unlocks

    private void DrawUnlocks()
    {
        if (!ImGui.CollapsingHeader("Unlocks"))
            return;

        DrawCustomizationUnlocks();
        DrawItemUnlocks();
        DrawUnlockableItems();
    }

    private void DrawCustomizationUnlocks()
    {
        using var tree = ImRaii.TreeNode("Customization");
        if (!tree)
            return;


        using var table = ImRaii.Table("customizationUnlocks", 6,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeight()));
        if (!table)
            return;

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(_customizeUnlocks.Unlockable, skips, t =>
        {
            ImGuiUtil.DrawTableColumn(t.Key.Index.ToDefaultName());
            ImGuiUtil.DrawTableColumn(t.Key.CustomizeId.ToString());
            ImGuiUtil.DrawTableColumn(t.Key.Value.Value.ToString());
            ImGuiUtil.DrawTableColumn(t.Value.Data.ToString());
            ImGuiUtil.DrawTableColumn(t.Value.Name);
            ImGuiUtil.DrawTableColumn(_customizeUnlocks.IsUnlocked(t.Key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"
                    : time.LocalDateTime.ToString("g")
                : "Never");
        }, _customizeUnlocks.Unlockable.Count);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }

    private void DrawItemUnlocks()
    {
        using var tree = ImRaii.TreeNode("Unlocked Items");
        if (!tree)
            return;

        using var table = ImRaii.Table("itemUnlocks", 5,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeight()));
        if (!table)
            return;

        ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name",   ImGuiTableColumnFlags.WidthFixed, 400 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Slot",   ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Model",  ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Unlock", ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(_itemUnlocks, skips, t =>
        {
            ImGuiUtil.DrawTableColumn(t.Key.ToString());
            if (_items.ItemService.AwaitedService.TryGetValue(t.Key, EquipSlot.MainHand, out var equip))
            {
                ImGuiUtil.DrawTableColumn(equip.Name);
                ImGuiUtil.DrawTableColumn(equip.Type.ToName());
                ImGuiUtil.DrawTableColumn(equip.Weapon().ToString());
            }
            else
            {
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
            }

            ImGuiUtil.DrawTableColumn(_itemUnlocks.IsUnlocked(t.Key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"
                    : time.LocalDateTime.ToString("g")
                : "Never");
        }, _itemUnlocks.Count);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }

    private void DrawUnlockableItems()
    {
        using var tree = ImRaii.TreeNode("Unlockable Items");
        if (!tree)
            return;

        using var table = ImRaii.Table("unlockableItem", 6,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(ImGui.GetContentRegionAvail().X, 12 * ImGui.GetTextLineHeight()));
        if (!table)
            return;

        ImGui.TableSetupColumn("ItemId",   ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name",     ImGuiTableColumnFlags.WidthFixed, 400 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Slot",     ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Model",    ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Unlock",   ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Criteria", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeightWithSpacing());
        ImGui.TableNextRow();
        var remainder = ImGuiClip.ClippedDraw(_itemUnlocks.Unlockable, skips, t =>
        {
            ImGuiUtil.DrawTableColumn(t.Key.ToString());
            if (_items.ItemService.AwaitedService.TryGetValue(t.Key, EquipSlot.MainHand, out var equip))
            {
                ImGuiUtil.DrawTableColumn(equip.Name);
                ImGuiUtil.DrawTableColumn(equip.Type.ToName());
                ImGuiUtil.DrawTableColumn(equip.Weapon().ToString());
            }
            else
            {
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
            }

            ImGuiUtil.DrawTableColumn(_itemUnlocks.IsUnlocked(t.Key, out var time)
                ? time == DateTimeOffset.MinValue
                    ? "Always"
                    : time.LocalDateTime.ToString("g")
                : "Never");
            ImGuiUtil.DrawTableColumn(t.Value.ToString());
        }, _itemUnlocks.Unlockable.Count);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }

    #endregion

    #region Inventory

    private void DrawInventory()
    {
        if (!ImGui.CollapsingHeader("Inventory"))
            return;

        var inventory = InventoryManager.Instance();
        if (inventory == null)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)inventory:X}");

        var equip = inventory->GetInventoryContainer(InventoryType.EquippedItems);
        if (equip == null || equip->Loaded == 0)
            return;

        ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)equip:X}");

        using var table = ImRaii.Table("items", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!table)
            return;

        for (var i = 0; i < equip->Size; ++i)
        {
            ImGuiUtil.DrawTableColumn(i.ToString());
            var item = equip->GetInventorySlot(i);
            if (item == null)
            {
                ImGuiUtil.DrawTableColumn("NULL");
                ImGui.TableNextRow();
            }
            else
            {
                ImGuiUtil.DrawTableColumn(item->ItemID.ToString());
                ImGuiUtil.DrawTableColumn(item->GlamourID.ToString());
                ImGui.TableNextColumn();
                ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)item:X}");
            }
        }
    }

    #endregion

    #region Fun

    private void DrawFun()
    {
        if (!ImGui.CollapsingHeader("Fun Module"))
            return;

        ImGui.TextUnformatted($"Current Festival: {_funModule.CurrentFestival}");
        ImGui.TextUnformatted($"Festivals Enabled: {_config.DisableFestivals switch { 1 => "Undecided", 0 => "Enabled", _ => "Disabled" }}");
        ImGui.TextUnformatted($"Popup Open: {ImGui.IsPopupOpen("FestivalPopup", ImGuiPopupFlags.AnyPopup)}");
        if (ImGui.Button("Force Christmas"))
            _funModule.ForceFestival(FunModule.FestivalType.Christmas);
        if (ImGui.Button("Force Halloween"))
            _funModule.ForceFestival(FunModule.FestivalType.Halloween);
        if (ImGui.Button("Force April First"))
            _funModule.ForceFestival(FunModule.FestivalType.AprilFirst);
        if (ImGui.Button("Force None"))
            _funModule.ForceFestival(FunModule.FestivalType.None);
        if (ImGui.Button("Revert"))
            _funModule.ResetFestival();
        if (ImGui.Button("Reset Popup"))
        {
            _config.DisableFestivals = 1;
            _config.Save();
        }
    }

    #endregion

    #region IPC

    private string _gameObjectName = string.Empty;
    private string _base64Apply    = string.Empty;

    private void DrawIpc()
    {
        if (!ImGui.CollapsingHeader("IPC Tester"))
            return;

        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        ImGui.InputTextWithHint("##gameObject", "Character Name...", ref _gameObjectName, 64);
        ImGui.InputTextWithHint("##base64",     "Design Base64...",  ref _base64Apply,    2047);
        using var table = ImRaii.Table("##ipc", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApiVersions);
        var (major, minor) = GlamourerIpc.ApiVersionsSubscriber(_pluginInterface).Invoke();
        ImGuiUtil.DrawTableColumn($"({major}, {minor})");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomization);
        ImGui.TableNextColumn();
        var base64 = GlamourerIpc.GetAllCustomizationSubscriber(_pluginInterface).Invoke(_gameObjectName);
        if (base64 != null)
            ImGuiUtil.CopyOnClickSelectable(base64);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelGetAllCustomizationFromCharacter);
        ImGui.TableNextColumn();
        base64 = GlamourerIpc.GetAllCustomizationFromCharacterSubscriber(_pluginInterface)
            .Invoke(_objectManager.Objects[_gameObjectIndex] as Character);
        if (base64 != null)
            ImGuiUtil.CopyOnClickSelectable(base64);
        else
            ImGui.TextUnformatted("Error");

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevert);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Name"))
            GlamourerIpc.RevertSubscriber(_pluginInterface).Invoke(_gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##Character"))
            GlamourerIpc.RevertCharacterSubscriber(_pluginInterface).Invoke(_objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAll);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllName"))
            GlamourerIpc.ApplyAllSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyAllToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##AllCharacter"))
            GlamourerIpc.ApplyAllToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipment);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipName"))
            GlamourerIpc.ApplyOnlyEquipmentSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyEquipmentToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##EquipCharacter"))
            GlamourerIpc.ApplyOnlyEquipmentToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomization);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeName"))
            GlamourerIpc.ApplyOnlyCustomizationSubscriber(_pluginInterface).Invoke(_base64Apply, _gameObjectName);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelApplyOnlyCustomizationToCharacter);
        ImGui.TableNextColumn();
        if (ImGui.Button("Apply##CustomizeCharacter"))
            GlamourerIpc.ApplyOnlyCustomizationToCharacterSubscriber(_pluginInterface)
                .Invoke(_base64Apply, _objectManager.Objects[_gameObjectIndex] as Character);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelUnlock);
        ImGui.TableNextColumn();
        if (ImGui.Button("Unlock##CustomizeCharacter"))
            GlamourerIpc.UnlockSubscriber(_pluginInterface)
                .Invoke(_objectManager.Objects[_gameObjectIndex] as Character, 1337);

        ImGuiUtil.DrawTableColumn(GlamourerIpc.LabelRevertToAutomation);
        ImGui.TableNextColumn();
        if (ImGui.Button("Revert##CustomizeCharacter"))
            GlamourerIpc.RevertToAutomationCharacterSubscriber(_pluginInterface)
                .Invoke(_objectManager.Objects[_gameObjectIndex] as Character, 1337);
    }

    #endregion
}
