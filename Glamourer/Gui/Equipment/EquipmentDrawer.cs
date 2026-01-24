using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Events;
using Glamourer.Gui.Materials;
using Glamourer.Services;
using Glamourer.Unlocks;
using Dalamud.Bindings.ImGui;
using ImSharp;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;
using OtterGui.Text.EndObjects;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using MouseWheelType = OtterGui.Widgets.MouseWheelType;

namespace Glamourer.Gui.Equipment;

public class EquipmentDrawer
{
    private const float DefaultWidth = 280;

    private readonly ItemManager                            _items;
    private readonly GlamourerColorCombo                    _stainCombo;
    private readonly DictStain                              _stainData;
    private readonly ItemCombo[]                            _itemCombo;
    private readonly BonusItemCombo[]                       _bonusItemCombo;
    private readonly Dictionary<FullEquipType, WeaponCombo> _weaponCombo;
    private readonly TextureService                         _textures;
    private readonly Configuration                          _config;
    private readonly GPoseService                           _gPose;
    private readonly AdvancedDyePopup                       _advancedDyes;
    private readonly ItemCopyService                        _itemCopy;

    private float _requiredComboWidthUnscaled;
    private float _requiredComboWidth;

    private Stain?             _draggedStain;
    private EquipItemSlotCache _draggedItem;
    private EquipSlot          _dragTarget;

    public EquipmentDrawer(FavoriteManager favorites, IDataManager gameData, ItemManager items, TextureService textures,
        Configuration config, GPoseService gPose, AdvancedDyePopup advancedDyes, ItemCopyService itemCopy)
    {
        _items          = items;
        _textures       = textures;
        _config         = config;
        _gPose          = gPose;
        _advancedDyes   = advancedDyes;
        _itemCopy       = itemCopy;
        _stainData      = items.Stains;
        _stainCombo     = new GlamourerColorCombo(DefaultWidth - 20, _stainData, favorites);
        _itemCombo      = EquipSlotExtensions.EqdpSlots.Select(e => new ItemCombo(gameData, items, e, Glamourer.Log, favorites)).ToArray();
        _bonusItemCombo = BonusExtensions.AllFlags.Select(f => new BonusItemCombo(gameData, items, f, Glamourer.Log, favorites)).ToArray();
        _weaponCombo    = new Dictionary<FullEquipType, WeaponCombo>(FullEquipTypeExtensions.WeaponTypes.Count * 2);
        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.ToSlot() is EquipSlot.MainHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type, Glamourer.Log, favorites));
            else if (type.ToSlot() is EquipSlot.OffHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type, Glamourer.Log, favorites));
        }

        _weaponCombo.Add(FullEquipType.Unknown, new WeaponCombo(items, FullEquipType.Unknown, Glamourer.Log, favorites));
    }

    private Vector2 _iconSize;
    private float   _comboLength;
    private uint    _advancedMaterialColor;

    public void Prepare()
    {
        _iconSize    = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
        _comboLength = DefaultWidth * ImGuiHelpers.GlobalScale;
        if (_requiredComboWidthUnscaled == 0)
            _requiredComboWidthUnscaled = _items.ItemData.AllItems(true)
                    .Concat(_items.ItemData.AllItems(false))
                    .Max(i => ImGui.CalcTextSize($"{i.Item2.Name} ({i.Item2.ModelString})").X)
              / ImGuiHelpers.GlobalScale;

        _requiredComboWidth    = _requiredComboWidthUnscaled * ImGuiHelpers.GlobalScale;
        _advancedMaterialColor = ColorId.AdvancedDyeActive.Value();
        _dragTarget            = EquipSlot.Unknown;
    }

    private bool VerifyRestrictedGear(EquipDrawData data)
    {
        if (data.Slot.IsAccessory())
            return false;

        var (changed, _) = _items.ResolveRestrictedGear(data.CurrentItem.Armor(), data.Slot, data.CurrentRace, data.CurrentGender);
        return changed;
    }

    public void DrawEquip(EquipDrawData equipDrawData)
    {
        if (_config.HideApplyCheckmarks)
            equipDrawData.DisplayApplication = false;

        using var id      = ImUtf8.PushId((int)equipDrawData.Slot);
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            DrawEquipSmall(equipDrawData);
        else
            DrawEquipNormal(equipDrawData);
    }

    public void DrawBonusItem(BonusDrawData bonusDrawData)
    {
        if (_config.HideApplyCheckmarks)
            bonusDrawData.DisplayApplication = false;

        using var id      = ImUtf8.PushId(100 + (int)bonusDrawData.Slot);
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            DrawBonusItemSmall(bonusDrawData);
        else
            DrawBonusItemNormal(bonusDrawData);
    }

    public void DrawWeapons(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        if (mainhand.CurrentItem.PrimaryId.Id == 0 && !allWeapons)
            return;

        if (_config.HideApplyCheckmarks)
        {
            mainhand.DisplayApplication = false;
            offhand.DisplayApplication  = false;
        }

        using var id      = ImUtf8.PushId("Weapons"u8);
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            DrawWeaponsSmall(mainhand, offhand, allWeapons);
        else
            DrawWeaponsNormal(mainhand, offhand, allWeapons);
    }

    public static void DrawMetaToggle(in ToggleDrawData data)
    {
        if (data.DisplayApplication)
        {
            var (valueChanged, applyChanged) = UiHelpers.DrawMetaToggle(data.Label.ToString(), data.CurrentValue, data.CurrentApply, out var newValue,
                out var newApply, data.Locked);
            if (valueChanged)
                data.SetValue(newValue);
            if (applyChanged)
                data.SetApply(newApply);
        }
        else
        {
            if (UiHelpers.DrawCheckbox(data.Label.ToString(), data.Tooltip.ToString(), data.CurrentValue, out var newValue, data.Locked))
                data.SetValue(newValue);
        }
    }

    public bool DrawAllStain(out StainIds ret, bool locked)
    {
        using var disabled = ImRaii.Disabled(locked);
        var       change   = _stainCombo.Draw("Dye All Slots", Stain.None.RgbaColor, string.Empty, false, false, MouseWheelType.None);
        ret = StainIds.None;
        if (change)
            if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out var stain))
                ret = StainIds.All(stain.RowIndex);
            else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                ret = StainIds.None;

        if (!locked)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _config.DeleteDesignModifier.IsActive())
            {
                ret    = StainIds.None;
                change = true;
            }

            ImUtf8.HoverTooltip($"{_config.DeleteDesignModifier.ToString()} and Right-click to clear.");
        }

        return change;
    }


    #region Small

    private void DrawEquipSmall(in EquipDrawData equipDrawData)
    {
        DrawStain(equipDrawData, true);
        Im.Line.Same();
        DrawItem(equipDrawData, out var label, true, false, false);
        if (equipDrawData.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(equipDrawData);
            Im.Line.Same();
            DrawApplyStain(equipDrawData);
        }
        else if (equipDrawData.IsState)
        {
            _advancedDyes.DrawButton(equipDrawData.Slot, equipDrawData.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        if (VerifyRestrictedGear(equipDrawData))
            label += " (Restricted)";

        DrawEquipLabel(equipDrawData is { IsDesign: true, HasAdvancedDyes: true }, label);
    }

    private void DrawBonusItemSmall(in BonusDrawData bonusDrawData)
    {
        ImGui.Dummy(new Vector2(StainId.NumStains * ImUtf8.FrameHeight + (StainId.NumStains - 1) * ImUtf8.ItemSpacing.X, ImUtf8.FrameHeight));
        Im.Line.Same();
        DrawBonusItem(bonusDrawData, out var label, true, false, false);
        if (bonusDrawData.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(bonusDrawData);
        }
        else if (bonusDrawData.IsState)
        {
            _advancedDyes.DrawButton(bonusDrawData.Slot, bonusDrawData.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        DrawEquipLabel(bonusDrawData is { IsDesign: true, HasAdvancedDyes: true }, label);
    }

    private void DrawWeaponsSmall(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        DrawStain(mainhand, true);
        Im.Line.Same();
        DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, true, false);
        if (mainhand.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(mainhand);
            Im.Line.Same();
            DrawApplyStain(mainhand);
        }
        else if (mainhand.IsState)
        {
            _advancedDyes.DrawButton(EquipSlot.MainHand, mainhand.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        if (allWeapons)
            mainhandLabel += $" ({mainhand.CurrentItem.Type.ToName()})";
        WeaponHelpMarker(mainhand is { IsDesign: true, HasAdvancedDyes: true }, mainhandLabel);

        if (offhand.CurrentItem.Type is FullEquipType.Unknown)
            return;

        DrawStain(offhand, true);
        Im.Line.Same();
        DrawOffhand(mainhand, offhand, out var offhandLabel, true, false, false);
        if (offhand.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(offhand);
            Im.Line.Same();
            DrawApplyStain(offhand);
        }
        else if (offhand.IsState)
        {
            _advancedDyes.DrawButton(EquipSlot.OffHand, offhand.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        WeaponHelpMarker(offhand is { IsDesign: true, HasAdvancedDyes: true }, offhandLabel);
    }

    #endregion

    #region Normal

    private void DrawEquipNormal(in EquipDrawData equipDrawData)
    {
        equipDrawData.CurrentItem.DrawIcon(_textures, _iconSize, equipDrawData.Slot);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left  = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        Im.Line.Same();
        using var group = ImRaii.Group();
        DrawItem(equipDrawData, out var label, false, right, left);
        if (equipDrawData.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(equipDrawData);
        }

        DrawEquipLabel(equipDrawData is { IsDesign: true, HasAdvancedDyes: true }, label);

        DrawStain(equipDrawData, false);
        if (equipDrawData.DisplayApplication)
        {
            Im.Line.Same();
            DrawApplyStain(equipDrawData);
        }
        else if (equipDrawData.IsState)
        {
            _advancedDyes.DrawButton(equipDrawData.Slot, equipDrawData.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        if (VerifyRestrictedGear(equipDrawData))
        {
            Im.Line.Same();
            ImUtf8.Text("(Restricted)"u8);
        }
    }

    private void DrawBonusItemNormal(in BonusDrawData bonusDrawData)
    {
        bonusDrawData.CurrentItem.DrawIcon(_textures, _iconSize, bonusDrawData.Slot);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left  = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        Im.Line.Same();
        DrawBonusItem(bonusDrawData, out var label, false, right, left);
        if (bonusDrawData.DisplayApplication)
        {
            Im.Line.Same();
            DrawApply(bonusDrawData);
        }
        else if (bonusDrawData.IsState)
        {
            _advancedDyes.DrawButton(bonusDrawData.Slot, bonusDrawData.HasAdvancedDyes ? _advancedMaterialColor : 0u);
        }

        DrawEquipLabel(bonusDrawData is { IsDesign: true, HasAdvancedDyes: true }, label);
    }

    private void DrawWeaponsNormal(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing,
            ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y });

        mainhand.CurrentItem.DrawIcon(_textures, _iconSize, EquipSlot.MainHand);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        Im.Line.Same();
        using (ImUtf8.Group())
        {
            DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, false, left);
            if (mainhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(mainhand);
            }

            WeaponHelpMarker(mainhand is { IsDesign: true, HasAdvancedDyes: true }, mainhandLabel,
                allWeapons ? mainhand.CurrentItem.Type.ToName() : null);

            DrawStain(mainhand, false);
            if (mainhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApplyStain(mainhand);
            }
            else if (mainhand.IsState)
            {
                _advancedDyes.DrawButton(EquipSlot.MainHand, mainhand.HasAdvancedDyes ? _advancedMaterialColor : 0u);
            }
        }

        if (offhand.CurrentItem.Type is FullEquipType.Unknown)
            return;

        offhand.CurrentItem.DrawIcon(_textures, _iconSize, EquipSlot.OffHand);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        Im.Line.Same();
        using (ImUtf8.Group())
        {
            DrawOffhand(mainhand, offhand, out var offhandLabel, false, right, left);
            if (offhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(offhand);
            }

            WeaponHelpMarker(offhand is { IsDesign: true, HasAdvancedDyes: true }, offhandLabel);

            DrawStain(offhand, false);
            if (offhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApplyStain(offhand);
            }
            else if (offhand.IsState)
            {
                _advancedDyes.DrawButton(EquipSlot.OffHand, offhand.HasAdvancedDyes ? _advancedMaterialColor : 0u);
            }
        }
    }

    private void DrawStain(in EquipDrawData data, bool small)
    {
        using var disabled = ImRaii.Disabled(data.Locked);
        var       width    = (_comboLength - ImUtf8.ItemInnerSpacing.X * (data.CurrentStains.Count - 1)) / data.CurrentStains.Count;
        foreach (var (stainId, index) in data.CurrentStains.WithIndex())
        {
            using var id    = ImUtf8.PushId(index);
            var       found = _stainData.TryGetValue(stainId, out var stain);
            var change = small
                ? _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss)
                : _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, width);

            _itemCopy.HandleCopyPaste(data, index);
            if (!change)
                DrawStainDragDrop(data, index, stain, found);

            if (index < data.CurrentStains.Count - 1)
                ImUtf8.SameLineInner();

            if (change)
                if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain))
                    data.SetStains(data.CurrentStains.With(index, stain.RowIndex));
                else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                    data.SetStains(data.CurrentStains.With(index, Stain.None.RowIndex));
            if (ResetOrClear(data.Locked, false, data.AllowRevert, true, stainId, data.GameStains[index], Stain.None.RowIndex,
                    out var newStain))
                data.SetStains(data.CurrentStains.With(index, newStain));
        }
    }

    private void DrawStainDragDrop(in EquipDrawData data, int index, Stain stain, bool found)
    {
        if (found)
        {
            using var dragSource = ImUtf8.DragDropSource();
            if (dragSource.Success)
            {
                DragDropSource.SetPayload("stainDragDrop"u8);
                _draggedStain = stain;
                ImUtf8.Text($"Dragging {stain.Name}...");
            }
        }

        using var dragTarget = ImUtf8.DragDropTarget();
        if (dragTarget.IsDropping("stainDragDrop"u8) && _draggedStain.HasValue)
        {
            data.SetStains(data.CurrentStains.With(index, _draggedStain.Value.RowIndex));
            _draggedStain = null;
        }
    }

    private void DrawItem(in EquipDrawData data, out string label, bool small, bool clear, bool open)
    {
        Debug.Assert(data.Slot.IsEquipment() || data.Slot.IsAccessory(), $"Called {nameof(DrawItem)} on {data.Slot}.");

        var combo = _itemCombo[data.Slot.ToIndex()];
        label = combo.Label;
        if (!data.Locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");

        using var disabled = ImRaii.Disabled(data.Locked);
        var change = combo.Draw(data.CurrentItem.Name, data.CurrentItem.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
            _requiredComboWidth);
        DrawGearDragDrop(data);
        if (change)
            data.SetItem(combo.CurrentSelection);
        else if (combo.CustomVariant.Id > 0)
            data.SetItem(_items.Identify(data.Slot, combo.CustomSetId, combo.CustomVariant));
        _itemCopy.HandleCopyPaste(data);

        if (ResetOrClear(data.Locked, clear, data.AllowRevert, true, data.CurrentItem, data.GameItem, ItemManager.NothingItem(data.Slot),
                out var item))
            data.SetItem(item);
    }

    private void DrawBonusItem(in BonusDrawData data, out string label, bool small, bool clear, bool open)
    {
        var combo = _bonusItemCombo[data.Slot.ToIndex()];
        label = combo.Label;
        if (!data.Locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");

        using var disabled = ImRaii.Disabled(data.Locked);
        var change = combo.Draw(data.CurrentItem.Name, data.CurrentItem.Id.BonusItem,
            small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
            _requiredComboWidth);
        if (ImGui.IsItemHovered() && ImGui.GetIO().KeyCtrl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.C))
                _itemCopy.Copy(combo.CurrentSelection);
            else if (ImGui.IsKeyPressed(ImGuiKey.V))
                _itemCopy.Paste(data.Slot.ToEquipType(), data.SetItem);
        }

        if (change)
            data.SetItem(combo.CurrentSelection);
        else if (combo.CustomVariant.Id > 0)
            data.SetItem(_items.Identify(data.Slot, combo.CustomSetId, combo.CustomVariant));

        if (ResetOrClear(data.Locked, clear, data.AllowRevert, true, data.CurrentItem, data.GameItem, EquipItem.BonusItemNothing(data.Slot),
                out var item))
            data.SetItem(item);
    }

    private void DrawGearDragDrop(in EquipDrawData data)
    {
        if (data.CurrentItem.Valid)
        {
            using var dragSource = ImUtf8.DragDropSource();
            if (dragSource.Success)
            {
                DragDropSource.SetPayload("equipDragDrop"u8);
                _draggedItem.Update(_items, data.CurrentItem, data.Slot);
            }
        }

        using var dragTarget = ImUtf8.DragDropTarget();
        if (!dragTarget)
            return;

        var item = _draggedItem[data.Slot];
        if (!item.Valid)
            return;

        _dragTarget = data.Slot;
        if (!dragTarget.IsDropping("equipDragDrop"u8))
            return;

        data.SetItem(item);
        _draggedItem.Clear();
    }

    public unsafe void DrawDragDropTooltip()
    {
        var payload = ImGui.GetDragDropPayload().Handle;
        if (payload is null)
            return;

        if (!MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)Unsafe.AsPointer(ref payload->DataType_0)).SequenceEqual("equipDragDrop"u8))
            return;

        using var tt = ImUtf8.Tooltip();
        if (_dragTarget is EquipSlot.Unknown)
            ImUtf8.Text($"Dragging {_draggedItem.Dragged.Name}...");
        else
            ImUtf8.Text($"Converting to {_draggedItem[_dragTarget].Name}...");
    }

    private static bool ResetOrClear<T>(bool locked, bool clicked, bool allowRevert, bool allowClear,
        in T currentItem, in T revertItem, in T clearItem, out T? item) where T : IEquatable<T>
    {
        if (locked)
        {
            item = default;
            return false;
        }

        clicked = clicked || ImGui.IsItemClicked(ImGuiMouseButton.Right);

        (var tt, item, var valid) = (allowRevert && !revertItem.Equals(currentItem), allowClear && !clearItem.Equals(currentItem),
                ImGui.GetIO().KeyCtrl) switch
            {
                (true, true, true) => ("Right-click to clear. Control and Right-Click to revert to game.\nControl and mouse wheel to scroll.",
                    revertItem, true),
                (true, true, false) => ("Right-click to clear. Control and Right-Click to revert to game.\nControl and mouse wheel to scroll.",
                    clearItem, true),
                (true, false, true)  => ("Control and Right-Click to revert to game.\nControl and mouse wheel to scroll.", revertItem, true),
                (true, false, false) => ("Control and Right-Click to revert to game.\nControl and mouse wheel to scroll.", default, false),
                (false, true, _)     => ("Right-click to clear.\nControl and mouse wheel to scroll.", clearItem, true),
                (false, false, _)    => ("Control and mouse wheel to scroll.", default, false),
            };
        ImUtf8.HoverTooltip(tt);

        return clicked && valid;
    }

    private void DrawMainhand(ref EquipDrawData mainhand, ref EquipDrawData offhand, out string label, bool drawAll, bool small,
        bool open)
    {
        if (!_weaponCombo.TryGetValue(drawAll ? FullEquipType.Unknown : mainhand.CurrentItem.Type, out var combo))
        {
            label = string.Empty;
            return;
        }

        label = combo.Label;
        var        unknown     = !_gPose.InGPose && mainhand.CurrentItem.Type is FullEquipType.Unknown;
        using var  style       = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        EquipItem? changedItem = null;
        using (var _ = ImRaii.Disabled(mainhand.Locked | unknown))
        {
            if (!mainhand.Locked && open)
                UiHelpers.OpenCombo($"##{label}");
            if (combo.Draw(mainhand.CurrentItem.Name, mainhand.CurrentItem.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
                    _requiredComboWidth))
                changedItem = combo.CurrentSelection;
            else if (combo.CustomVariant.Id > 0 && (drawAll || ItemData.ConvertWeaponId(combo.CustomSetId) == mainhand.CurrentItem.Type))
                changedItem = _items.Identify(mainhand.Slot, combo.CustomSetId, combo.CustomWeaponId, combo.CustomVariant);
            _itemCopy.HandleCopyPaste(mainhand);
            DrawGearDragDrop(mainhand);

            if (ResetOrClear(mainhand.Locked || unknown, open, mainhand.AllowRevert, false, mainhand.CurrentItem, mainhand.GameItem,
                    default,                             out var c))
                changedItem = c;

            if (changedItem != null)
            {
                mainhand.SetItem(changedItem.Value);
                if (changedItem.Value.Type.ValidOffhand() != mainhand.CurrentItem.Type.ValidOffhand())
                {
                    offhand.CurrentItem = _items.GetDefaultOffhand(changedItem.Value);
                    offhand.SetItem(offhand.CurrentItem);
                }

                mainhand.CurrentItem = changedItem.Value;
            }
        }

        if (unknown)
            ImUtf8.HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled,
                "The weapon type could not be identified, thus changing it to other weapons of that type is not possible."u8);
    }

    private void DrawOffhand(in EquipDrawData mainhand, in EquipDrawData offhand, out string label, bool small, bool clear, bool open)
    {
        if (!_weaponCombo.TryGetValue(offhand.CurrentItem.Type, out var combo))
        {
            label = string.Empty;
            return;
        }

        label = combo.Label;
        var locked = offhand.Locked
         || !_gPose.InGPose && (offhand.CurrentItem.Type.IsUnknown() || mainhand.CurrentItem.Type.IsUnknown());
        using var disabled = ImRaii.Disabled(locked);
        if (!locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");
        if (combo.Draw(offhand.CurrentItem.Name, offhand.CurrentItem.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
                _requiredComboWidth))
            offhand.SetItem(combo.CurrentSelection);
        else if (combo.CustomVariant.Id > 0 && ItemData.ConvertWeaponId(combo.CustomSetId) == offhand.CurrentItem.Type)
            offhand.SetItem(_items.Identify(mainhand.Slot, combo.CustomSetId, combo.CustomWeaponId, combo.CustomVariant));
        _itemCopy.HandleCopyPaste(offhand);
        DrawGearDragDrop(offhand);

        var defaultOffhand = _items.GetDefaultOffhand(mainhand.CurrentItem);
        if (ResetOrClear(locked, clear, offhand.AllowRevert, true, offhand.CurrentItem, offhand.GameItem, defaultOffhand, out var item))
            offhand.SetItem(item);
    }

    private static void DrawApply(in EquipDrawData data)
    {
        if (UiHelpers.DrawCheckbox($"##apply{data.Slot}", "Apply this item when applying the Design.", data.CurrentApply, out var enabled,
                data.Locked))
            data.SetApplyItem(enabled);
    }

    private static void DrawApply(in BonusDrawData data)
    {
        if (UiHelpers.DrawCheckbox($"##apply{data.Slot}", "Apply this bonus item when applying the Design.", data.CurrentApply, out var enabled,
                data.Locked))
            data.SetApplyItem(enabled);
    }

    private static void DrawApplyStain(in EquipDrawData data)
    {
        if (UiHelpers.DrawCheckbox($"##applyStain{data.Slot}", "Apply this dye to the item when applying the Design.", data.CurrentApplyStain,
                out var enabled,
                data.Locked))
            data.SetApplyStain(enabled);
    }

    #endregion

    private void WeaponHelpMarker(bool hasAdvancedDyes, string label, string? type = null)
    {
        Im.Line.Same();
        ImGuiComponents.HelpMarker(
            "Changing weapons to weapons of different types can cause crashes, freezes, soft- and hard locks and cheating, "
          + "thus it is only allowed to change weapons to other weapons of the same type.");
        DrawEquipLabel(hasAdvancedDyes, label);

        if (type == null)
            return;

        var pos = ImGui.GetItemRectMin();
        pos.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.GetWindowDrawList().AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), $"({type})");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private void DrawEquipLabel(bool hasAdvancedDyes, string label)
    {
        Im.Line.Same();
        using (ImRaii.PushColor(ImGuiCol.Text, _advancedMaterialColor, hasAdvancedDyes))
        {
            ImUtf8.Text(label);
        }

        if (hasAdvancedDyes)
            ImUtf8.HoverTooltip("This design has advanced dyes setup for this slot."u8);
    }
}
