using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using Glamourer.Events;
using Glamourer.Gui.Materials;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
        foreach (var type in FullEquipType.Values)
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
    private Rgba32  _advancedMaterialColor;

    public void Prepare()
    {
        _iconSize    = new Vector2(2 * Im.Style.FrameHeight + Im.Style.ItemSpacing.Y);
        _comboLength = DefaultWidth * Im.Style.GlobalScale;
        if (_requiredComboWidthUnscaled is 0)
            _requiredComboWidthUnscaled = _items.ItemData.AllItems(true)
                    .Concat(_items.ItemData.AllItems(false))
                    .Max(i => Im.Font.CalculateSize($"{i.Item2.Name} ({i.Item2.ModelString})").X)
              / Im.Style.GlobalScale;

        _requiredComboWidth    = _requiredComboWidthUnscaled * Im.Style.GlobalScale;
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

        using var id    = Im.Id.Push((int)equipDrawData.Slot);
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        if (_config.SmallEquip)
            DrawEquipSmall(equipDrawData);
        else
            DrawEquipNormal(equipDrawData);
    }

    public void DrawBonusItem(BonusDrawData bonusDrawData)
    {
        if (_config.HideApplyCheckmarks)
            bonusDrawData.DisplayApplication = false;

        using var id    = Im.Id.Push(100 + (int)bonusDrawData.Slot);
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        if (_config.SmallEquip)
            DrawBonusItemSmall(bonusDrawData);
        else
            DrawBonusItemNormal(bonusDrawData);
    }

    public void DrawWeapons(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        if (mainhand.CurrentItem.PrimaryId.Id is 0 && !allWeapons)
            return;

        if (_config.HideApplyCheckmarks)
        {
            mainhand.DisplayApplication = false;
            offhand.DisplayApplication  = false;
        }

        using var id    = Im.Id.Push("Weapons"u8);
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        if (_config.SmallEquip)
            DrawWeaponsSmall(mainhand, offhand, allWeapons);
        else
            DrawWeaponsNormal(mainhand, offhand, allWeapons);
    }

    public static void DrawMetaToggle(in ToggleDrawData data)
    {
        if (data.DisplayApplication)
        {
            var (valueChanged, applyChanged) = UiHelpers.DrawMetaToggle(data.Label, data.CurrentValue, data.CurrentApply,
                out var newValue,
                out var newApply, data.Locked);
            if (valueChanged)
                data.SetValue(newValue);
            if (applyChanged)
                data.SetApply(newApply);
        }
        else
        {
            if (UiHelpers.DrawCheckbox(data.Label, data.Tooltip, data.CurrentValue, out var newValue, data.Locked))
                data.SetValue(newValue);
        }
    }

    public bool DrawAllStain(out StainIds ret, bool locked)
    {
        using var disabled = Im.Disabled(locked);
        var       change   = _stainCombo.Draw("Dye All Slots", Stain.None.RgbaColor, string.Empty, false, false, OtterGui.Widgets.MouseWheelType.None);
        ret = StainIds.None;
        if (change)
            if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out var stain))
                ret = StainIds.All(stain.RowIndex);
            else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                ret = StainIds.None;

        if (!locked)
        {
            if (Im.Item.RightClicked() && _config.DeleteDesignModifier.IsActive())
            {
                ret    = StainIds.None;
                change = true;
            }

            Im.Tooltip.OnHover($"{_config.DeleteDesignModifier.ToString()} and Right-click to clear.");
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
        Im.Dummy(new Vector2(StainId.NumStains * Im.Style.FrameHeight + (StainId.NumStains - 1) * Im.Style.ItemSpacing.X,
            Im.Style.FrameHeight));
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
        var right = Im.Item.RightClicked();
        var left  = Im.Item.Clicked();
        Im.Line.Same();
        using var group = Im.Group();
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
            Im.Text("(Restricted)"u8);
        }
    }

    private void DrawBonusItemNormal(in BonusDrawData bonusDrawData)
    {
        bonusDrawData.CurrentItem.DrawIcon(_textures, _iconSize, bonusDrawData.Slot);
        var right = Im.Item.RightClicked();
        var left  = Im.Item.Clicked();
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
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        mainhand.CurrentItem.DrawIcon(_textures, _iconSize, EquipSlot.MainHand);
        var left = Im.Item.Clicked();
        Im.Line.Same();
        using (Im.Group())
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
        var right = Im.Item.RightClicked();
        left = Im.Item.Clicked();
        Im.Line.Same();
        using (Im.Group())
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
        using var disabled = Im.Disabled(data.Locked);
        var       width    = (_comboLength - Im.Style.ItemInnerSpacing.X * (data.CurrentStains.Count - 1)) / data.CurrentStains.Count;
        foreach (var (index, stainId) in data.CurrentStains.Index())
        {
            using var id    = Im.Id.Push(index);
            var       found = _stainData.TryGetValue(stainId, out var stain);
            var change = small
                ? _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss)
                : _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, width);

            _itemCopy.HandleCopyPaste(data, index);
            if (!change)
                DrawStainDragDrop(data, index, stain, found);

            if (index < data.CurrentStains.Count - 1)
                Im.Line.SameInner();

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
            using var dragSource = Im.DragDrop.Source();
            if (dragSource.Success)
            {
                dragSource.SetPayload("stainDragDrop"u8);
                _draggedStain = stain;
                Im.Text($"Dragging {stain.Name}...");
            }
        }

        using var dragTarget = Im.DragDrop.Target();
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

        using var disabled = Im.Disabled(data.Locked);
        var change = combo.Draw(data.CurrentItem.Name, data.CurrentItem.ItemId, small ? _comboLength - Im.Style.FrameHeight : _comboLength,
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

        using var disabled = Im.Disabled(data.Locked);
        var change = combo.Draw(data.CurrentItem.Name, data.CurrentItem.Id.BonusItem,
            small ? _comboLength - Im.Style.FrameHeight : _comboLength,
            _requiredComboWidth);
        if (Im.Item.Hovered() && Im.Io.KeyControl)
        {
            if (Im.Keyboard.IsPressed(Key.C))
                _itemCopy.Copy(combo.CurrentSelection);
            else if (Im.Keyboard.IsPressed(Key.V))
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
            using var dragSource = Im.DragDrop.Source();
            if (dragSource.Success)
            {
                dragSource.SetPayload("equipDragDrop"u8);
                _draggedItem.Update(_items, data.CurrentItem, data.Slot);
            }
        }

        using var dragTarget = Im.DragDrop.Target();
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

    public void DrawDragDropTooltip()
    {
        var payload = Im.DragDrop.PeekPayload();
        if (!payload.Valid)
            return;

        if (!payload.CheckType("equipDragDrop"u8))
            return;

        using var tt = Im.Tooltip.Begin();
        if (_dragTarget is EquipSlot.Unknown)
            Im.Text($"Dragging {_draggedItem.Dragged.Name}...");
        else
            Im.Text($"Converting to {_draggedItem[_dragTarget].Name}...");
    }

    private static bool ResetOrClear<T>(bool locked, bool clicked, bool allowRevert, bool allowClear,
        in T currentItem, in T revertItem, in T clearItem, out T? item) where T : IEquatable<T>
    {
        if (locked)
        {
            item = default;
            return false;
        }

        clicked = clicked || Im.Item.RightClicked();

        (var tt, item, var valid) = (allowRevert && !revertItem.Equals(currentItem), allowClear && !clearItem.Equals(currentItem),
                Im.Io.KeyControl) switch
            {
                (true, true, true) => RefTuple.Create(
                    "Right-click to clear. Control and Right-Click to revert to game.\nControl and mouse wheel to scroll."u8,
                    revertItem, true),
                (true, true, false) => RefTuple.Create(
                    "Right-click to clear. Control and Right-Click to revert to game.\nControl and mouse wheel to scroll."u8,
                    clearItem, true),
                (true, false, true) => RefTuple.Create("Control and Right-Click to revert to game.\nControl and mouse wheel to scroll."u8,
                    revertItem, true),
                (true, false, false) => RefTuple.Create("Control and Right-Click to revert to game.\nControl and mouse wheel to scroll."u8,
                    (T?)default!, false),
                (false, true, _)  => RefTuple.Create("Right-click to clear.\nControl and mouse wheel to scroll."u8, clearItem,    true),
                (false, false, _) => RefTuple.Create("Control and mouse wheel to scroll."u8,                        (T?)default!, false),
            };
        Im.Tooltip.OnHover(tt);

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
        using var  style       = ImStyleDouble.ItemSpacing.Push(Im.Style.ItemInnerSpacing);
        EquipItem? changedItem = null;
        using (Im.Disabled(mainhand.Locked | unknown))
        {
            if (!mainhand.Locked && open)
                UiHelpers.OpenCombo($"##{label}");
            if (combo.Draw(mainhand.CurrentItem.Name, mainhand.CurrentItem.ItemId, small ? _comboLength - Im.Style.FrameHeight : _comboLength,
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
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled,
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
        using var disabled = Im.Disabled(locked);
        if (!locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");
        if (combo.Draw(offhand.CurrentItem.Name, offhand.CurrentItem.ItemId, small ? _comboLength - Im.Style.FrameHeight : _comboLength,
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
        using var id = Im.Id.Push((int)data.Slot);
        if (UiHelpers.DrawCheckbox("##apply"u8, "Apply this item when applying the Design."u8, data.CurrentApply, out var enabled,
                data.Locked))
            data.SetApplyItem(enabled);
    }

    private static void DrawApply(in BonusDrawData data)
    {
        using var id = Im.Id.Push((int)data.Slot);
        if (UiHelpers.DrawCheckbox("##apply"u8, "Apply this bonus item when applying the Design."u8, data.CurrentApply, out var enabled,
                data.Locked))
            data.SetApplyItem(enabled);
    }

    private static void DrawApplyStain(in EquipDrawData data)
    {
        using var id = Im.Id.Push((int)data.Slot);
        if (UiHelpers.DrawCheckbox("##applyStain"u8, "Apply this dye to the item when applying the Design."u8, data.CurrentApplyStain,
                out var enabled,
                data.Locked))
            data.SetApplyStain(enabled);
    }

    #endregion

    private void WeaponHelpMarker(bool hasAdvancedDyes, string label, string? type = null)
    {
        LunaStyle.DrawAlignedHelpMarker(
            "Changing weapons to weapons of different types can cause crashes, freezes, soft- and hard locks and cheating, "u8
          + "thus it is only allowed to change weapons to other weapons of the same type."u8);
        DrawEquipLabel(hasAdvancedDyes, label);

        if (type is null)
            return;

        var pos = Im.Item.UpperLeftCorner;
        pos.Y += Im.Style.FrameHeightWithSpacing;
        Im.Window.DrawList.Text(pos, ImGuiColor.Text.Get(), $"({type})");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private void DrawEquipLabel(bool hasAdvancedDyes, string label)
    {
        Im.Line.Same();
        using (ImGuiColor.Text.Push(_advancedMaterialColor, hasAdvancedDyes))
        {
            Im.Text(label);
        }

        if (hasAdvancedDyes)
            Im.Tooltip.OnHover("This design has advanced dyes setup for this slot."u8);
    }
}
