using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs;

public unsafe class DebugTab : ITab
{
    private readonly VisorService           _visorService;
    private readonly ChangeCustomizeService _changeCustomizeService;
    private readonly UpdateSlotService      _updateSlotService;
    private readonly WeaponService          _weaponService;
    private readonly PenumbraService        _penumbra;
    private readonly ObjectTable            _objects;

    private readonly ItemManager          _items;
    private readonly ActorService         _actors;
    private readonly CustomizationService _customization;

    private int _gameObjectIndex;

    public DebugTab(ChangeCustomizeService changeCustomizeService, VisorService visorService, ObjectTable objects,
        UpdateSlotService updateSlotService, WeaponService weaponService, PenumbraService penumbra, IdentifierService identifier,
        ActorService actors, ItemManager items, CustomizationService customization)
    {
        _changeCustomizeService = changeCustomizeService;
        _visorService           = visorService;
        _objects                = objects;
        _updateSlotService      = updateSlotService;
        _weaponService          = weaponService;
        _penumbra               = penumbra;
        _actors                 = actors;
        _items                  = items;
        _customization          = customization;
    }

    public ReadOnlySpan<byte> Label
        => "Debug"u8;

    public void DrawContent()
    {
        DrawInteropHeader();
        DrawGameDataHeader();
        DrawPenumbraHeader();
    }

    #region Interop

    private void DrawInteropHeader()
    {
        if (!ImGui.CollapsingHeader("Interop"))
            return;

        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        var       actor = (Actor)_objects.GetObjectAddress(_gameObjectIndex);
        var       model = actor.Model;
        using var table = ImRaii.Table("##interopTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
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

    private void DrawVisor(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Visor");
        ImGuiUtil.DrawTableColumn("Visor State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.AsCharacter->DrawData.IsVisorToggled.ToString() : "No Character");
        ImGuiUtil.DrawTableColumn(model.IsHuman ? _visorService.GetVisorState(model).ToString() : "No Human");
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
            _visorService.SetVisorState(model, !_visorService.GetVisorState(model));
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
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, CharacterArmor.Empty);
        ImGui.SameLine();
        if (ImGui.SmallButton("Show"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, actor.GetArmor(EquipSlot.Head));
        ImGui.SameLine();
        if (ImGui.SmallButton("Toggle"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head,
                model.AsHuman->Head.Value == 0 ? actor.GetArmor(EquipSlot.Head) : CharacterArmor.Empty);
    }

    private void DrawWeaponState(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("WeaponState");
        ImGuiUtil.DrawTableColumn("Weapon State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsWeaponHidden ? "Hidden" : "Visible"
            : "No Character");
        var text = string.Empty;
        // TODO
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
            if (ImGui.SmallButton("Reset"))
                _updateSlotService.UpdateSlot(model, slot, actor.GetArmor(slot));
        }
    }

    private void DrawCustomize(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Customize");
        var actorCustomize = new Customize(actor.IsCharacter
            ? *(Penumbra.GameData.Structs.CustomizeData*)&actor.AsCharacter->DrawData.CustomizeData
            : new Penumbra.GameData.Structs.CustomizeData());
        var modelCustomize = new Customize(model.IsHuman
            ? *(Penumbra.GameData.Structs.CustomizeData*)model.AsHuman->CustomizeData
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
                modelCustomize.Set(type, (CustomizeValue)(modelCustomize[type].Value + 1));
                _changeCustomizeService.UpdateCustomize(model, modelCustomize.Data);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("--"))
            {
                modelCustomize.Set(type, (CustomizeValue)(modelCustomize[type].Value - 1));
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
                _penumbra.RedrawObject(_objects.GetObjectAddress(_gameObjectIndex), RedrawType.Redraw);
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
            var identified = _items.Identify(slot, (SetId)_setId, (byte)_variant);
            Text(identified.Name);
            ImGuiUtil.HoverTooltip(string.Join("\n",
                _items.IdentifierService.AwaitedService.Identify((SetId)_setId, (ushort)_variant, slot).Select(i => i.Name)));
        }

        var weapon = _items.Identify(EquipSlot.MainHand, (SetId)_setId, (WeaponType)_secondaryId, (byte)_variant);
        Text(weapon.Name);
        ImGuiUtil.HoverTooltip(string.Join("\n",
            _items.IdentifierService.AwaitedService.Identify((SetId)_setId, (WeaponType)_secondaryId, (ushort)_variant, EquipSlot.MainHand)));
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
                        _items.RestrictedGear.ResolveRestricted(new CharacterArmor((SetId)_setId, (byte)_variant, 0), slot, race, gender);
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

        DrawNameTable("BNPCs",      ref _bnpcFilter,      _actors.AwaitedService.Data.BNpcs.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("ENPCs",      ref _enpcFilter,      _actors.AwaitedService.Data.ENpcs.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Companions", ref _companionFilter, _actors.AwaitedService.Data.Companions.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Mounts",     ref _mountFilter,     _actors.AwaitedService.Data.Mounts.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Ornaments",  ref _ornamentFilter,  _actors.AwaitedService.Data.Ornaments.Select(kvp => (kvp.Key, kvp.Value)));
        DrawNameTable("Worlds",     ref _worldFilter,     _actors.AwaitedService.Data.Worlds.Select(kvp => ((uint)kvp.Key, kvp.Value)));
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
        ImRaii.TreeNode($"Default Sword: {_items.DefaultSword.Name} ({_items.DefaultSword.Id}) ({_items.DefaultSword.Weapon()})",
            ImGuiTreeNodeFlags.Leaf).Dispose();
        DrawNameTable("All Items (Main)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(true).Select(p => (p.Item1,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        DrawNameTable("All Items (Off)", ref _itemFilter,
            _items.ItemService.AwaitedService.AllItems(false).Select(p => (p.Item1,
                    $"{p.Item2.Name} ({(p.Item2.WeaponType == 0 ? p.Item2.Armor().ToString() : p.Item2.Weapon().ToString())})"))
                .OrderBy(p => p.Item1));
        foreach (var type in Enum.GetValues<FullEquipType>().Skip(1))
        {
            DrawNameTable(type.ToName(), ref _itemFilter,
                _items.ItemService.AwaitedService[type]
                    .Select(p => (p.Id, $"{p.Name} ({(p.WeaponType == 0 ? p.Armor().ToString() : p.Weapon().ToString())})")));
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
            p => p.Key.Value.ToString().Contains(_stainFilter) || p.Value.Name.Contains(_stainFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Key.Value.ToString("D3"));
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
                DrawCustomizationInfo(_customization.AwaitedService.GetList(clan, gender));
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

    #endregion
}
