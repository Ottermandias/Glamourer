using Dalamud.Plugin.Services;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Materials;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImSharp;
using Luna;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class EquipmentDrawer : IUiService, IDisposable
{
    private const float DefaultWidth = 280;

    private readonly ItemManager                            _items;
    private readonly GlamourerColorCombo                    _stainCombo;
    private readonly DictStain                              _stainData;
    private readonly EquipCombo[]                           _equipCombo;
    private readonly BonusItemCombo[]                       _bonusItemCombo;
    private readonly Dictionary<FullEquipType, WeaponCombo> _weaponCombo;
    private readonly TextureService                         _textures;
    private readonly Configuration                          _config;
    private readonly GPoseService                           _gPose;
    private readonly AdvancedDyePopup                       _advancedDyes;
    private readonly ItemCopyService                        _itemCopy;
    private readonly DesignApplier                          _designApplier;
    private readonly DesignConverter                        _converter;

    private Stain?             _draggedStain;
    private EquipItemSlotCache _draggedItem;
    private EquipSlot          _dragTarget;

    public EquipmentDrawer(FavoriteManager favorites, IDataManager gameData, ItemManager items, TextureService textures,
        Configuration config, GPoseService gPose, AdvancedDyePopup advancedDyes, ItemCopyService itemCopy, DesignApplier designApplier,
        DesignConverter converter)
    {
        _items          = items;
        _textures       = textures;
        _config         = config;
        _gPose          = gPose;
        _advancedDyes   = advancedDyes;
        _itemCopy       = itemCopy;
        _designApplier  = designApplier;
        _converter      = converter;
        _stainData      = items.Stains;
        _stainCombo     = new GlamourerColorCombo(_stainData, favorites, config);
        _equipCombo     = EquipSlotExtensions.EqdpSlots.Select(e => new EquipCombo(favorites, items, config, gameData, e)).ToArray();
        _bonusItemCombo = BonusExtensions.AllFlags.Select(f => new BonusItemCombo(favorites, items, config, gameData, f)).ToArray();
        _weaponCombo    = new Dictionary<FullEquipType, WeaponCombo>(FullEquipTypeExtensions.WeaponTypes.Count * 2);
        foreach (var type in FullEquipType.Values)
        {
            if (type.ToSlot() is EquipSlot.MainHand or EquipSlot.OffHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(favorites, items, config, type));
        }

        _weaponCombo.Add(FullEquipType.Unknown, new WeaponCombo(favorites, items, config, FullEquipType.Unknown));
    }

    private delegate void DrawEquipDelegate(EquipmentDrawer parent, in EquipDrawData data);
    private delegate void DrawBonusDelegate(EquipmentDrawer parent, in BonusDrawData data);
    private delegate void DrawWeaponsDelegate(EquipmentDrawer parent, EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons);
    private delegate bool ItemComboDelegate(EquipmentDrawer parent, BaseItemCombo combo, in EquipItem item, out EquipItem newItem);

    private Vector2             _iconSize;
    private Vector2             _smallIconSize;
    private float               _comboLength;
    private float               _stainWidth;
    private Rgba32              _advancedMaterialColor;
    private DrawEquipDelegate   _drawEquip   = NormalDrawer.Equip;
    private DrawBonusDelegate   _drawBonus   = NormalDrawer.Bonus;
    private DrawWeaponsDelegate _drawWeapons = NormalDrawer.Weapons;
    private ItemComboDelegate   _drawCombo   = NormalDrawer.ItemCombo;
    private bool                _compact;

    public void Prepare(bool compact)
    {
        _iconSize              = new Vector2(2 * Im.Style.FrameHeight + Im.Style.ItemSpacing.Y);
        _smallIconSize         = new Vector2(Im.Style.FrameHeight);
        _comboLength           = DefaultWidth * Im.Style.GlobalScale;
        _advancedMaterialColor = ColorId.AdvancedDyeActive.Value();
        _dragTarget            = EquipSlot.Unknown;
        _compact               = compact;
        (_stainWidth, _drawEquip, _drawBonus, _drawWeapons, _drawCombo) = (_config.SmallEquip, _compact) switch
        {
            (false, false) => ((_comboLength - Im.Style.ItemInnerSpacing.X * (StainId.NumStains - 1)) / StainId.NumStains,
                (DrawEquipDelegate)NormalDrawer.Equip, (DrawBonusDelegate)NormalDrawer.Bonus, (DrawWeaponsDelegate)NormalDrawer.Weapons,
                (ItemComboDelegate)NormalDrawer.ItemCombo),
            (false, true) => (Im.Style.FrameHeight, CompactDrawer.Equip, CompactDrawer.Bonus, CompactDrawer.Weapons, CompactDrawer.ItemCombo),
            (true, false) => (Im.Style.FrameHeight, SmallDrawer.Equip, SmallDrawer.Bonus, SmallDrawer.Weapons, SmallDrawer.ItemCombo),
            (true, true) => (Im.Style.FrameHeight, CompactSmallDrawer.Equip, CompactSmallDrawer.Bonus, CompactSmallDrawer.Weapons,
                CompactSmallDrawer.ItemCombo),
        };
    }

    public void DrawEquip(EquipDrawData equipDrawData)
    {
        if (_config.HideApplyCheckmarks)
            equipDrawData.DisplayApplication = false;

        using var id    = Im.Id.Push((int)equipDrawData.Slot);
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        _drawEquip(this, equipDrawData);
    }

    public void DrawBonusItem(BonusDrawData bonusDrawData)
    {
        if (_config.HideApplyCheckmarks)
            bonusDrawData.DisplayApplication = false;

        using var id    = Im.Id.Push(100 + (int)bonusDrawData.Slot);
        using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

        _drawBonus(this, bonusDrawData);
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

        _drawWeapons(this, mainhand, offhand, allWeapons);
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
        var       change   = _stainCombo.Draw("Dye All Slots"u8, Stain.None, out var newAllStain, Im.Style.FrameHeight);

        Im.DrawList.Window.Text(AwesomeIcon.Font, AwesomeIcon.Font.Size, Im.Item.UpperLeftCorner + Im.Style.FramePadding,
            ImGuiColor.Text.Get(), LunaStyle.DyeIcon.Span);

        ret = StainIds.None;
        if (change)
            ret = newAllStain.RowIndex != Stain.None.RowIndex ? StainIds.All(newAllStain.RowIndex) : StainIds.None;

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

    private void DrawStain(in EquipDrawData data)
    {
        using var id       = Im.Id.Push((uint)data.Slot);
        using var disabled = Im.Disabled(data.Locked);
        foreach (var (index, stainId) in data.CurrentStains.Index())
        {
            id.Push(index);
            var found  = _stainData.TryGetValue(stainId, out var stain);
            var change = _stainCombo.Draw("##stain"u8, stain, out var newStain, _stainWidth);

            _itemCopy.HandleCopyPaste(data, index);
            if (!change)
                DrawStainDragDrop(data, index, stain, found);

            if (index < data.CurrentStains.Count - 1)
                Im.Line.SameInner();

            if (change)
                data.SetStains(data.CurrentStains.With(index, newStain.RowIndex));
            if (ResetOrClear(data.Locked, false, data.AllowRevert, true, stainId, data.GameStains[index], Stain.None.RowIndex,
                    out var newStainId))
                data.SetStains(data.CurrentStains.With(index, newStainId));
            id.Pop(index);
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


    private void DrawItem(in EquipDrawData data, out StringU8 label, bool clear, bool open)
    {
        Debug.Assert(data.Slot.IsEquipment() || data.Slot.IsAccessory(), $"Called {nameof(DrawItem)} on {data.Slot}.");

        var combo = _equipCombo[data.Slot.ToIndex()];
        label = combo.Label;
        if (!data.Locked && open)
            UiHelpers.OpenCombo(combo.Label);

        using var disabled = Im.Disabled(data.Locked);
        var       change   = _drawCombo(this, combo, data.CurrentItem, out var newItem);
        DrawGearDragDrop(data);
        if (change)
            data.SetItem(newItem);
        _itemCopy.HandleCopyPaste(data);

        if (ResetOrClear(data.Locked, clear, data.AllowRevert, true, data.CurrentItem, data.GameItem, ItemManager.NothingItem(data.Slot),
                out var item))
            data.SetItem(item);
    }

    private void DrawBonusItem(in BonusDrawData data, out StringU8 label, bool clear, bool open)
    {
        var combo = _bonusItemCombo[data.Slot.ToIndex()];
        label = combo.Label;
        if (!data.Locked && open)
            UiHelpers.OpenCombo(combo.Label);

        using var disabled = Im.Disabled(data.Locked);
        var       change   = _drawCombo(this, combo, data.CurrentItem, out var newItem);
        if (Im.Item.Hovered() && Im.Io.KeyControl)
        {
            if (Im.Keyboard.IsPressed(Key.C))
                _itemCopy.Copy(newItem);
            else if (Im.Keyboard.IsPressed(Key.V) && _itemCopy.Paste(data.Slot.ToEquipType(), out var i))
                data.SetItem(i);
        }

        if (change)
            data.SetItem(newItem);

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

    private void DrawMainhand(ref EquipDrawData mainhand, ref EquipDrawData offhand, out StringU8 label, bool drawAll, bool open)
    {
        if (!_weaponCombo.TryGetValue(drawAll ? FullEquipType.Unknown : mainhand.CurrentItem.Type, out var combo))
        {
            label = StringU8.Empty;
            return;
        }

        label = combo.Label;
        var        unknown     = !_gPose.InGPose && mainhand.CurrentItem.Type is FullEquipType.Unknown;
        using var  style       = ImStyleDouble.ItemSpacing.Push(Im.Style.ItemInnerSpacing);
        EquipItem? changedItem = null;
        using (Im.Disabled(mainhand.Locked | unknown))
        {
            if (!mainhand.Locked && open)
                UiHelpers.OpenCombo(label);
            if (_drawCombo(this, combo, mainhand.CurrentItem, out var newItem))
                changedItem = newItem;
            _itemCopy.HandleCopyPaste(mainhand);
            DrawGearDragDrop(mainhand);

            if (ResetOrClear(mainhand.Locked || unknown, open, mainhand.AllowRevert, false, mainhand.CurrentItem, mainhand.GameItem,
                    default,                             out var c))
                changedItem = c;

            if (changedItem is not null)
            {
                mainhand.SetItem(changedItem.Value);
                if (!changedItem.Value.Type.ValidOffhand().IsCompatible(mainhand.CurrentItem.Type.ValidOffhand()))
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

    private void DrawOffhand(in EquipDrawData mainhand, FullEquipType validOffhand, in EquipDrawData offhand, out StringU8 label, bool clear,
        bool open)
    {
        if (!_weaponCombo.TryGetValue(validOffhand, out var combo))
        {
            label = StringU8.Empty;
            return;
        }

        label = combo.Label;
        var locked = offhand.Locked
         || !_gPose.InGPose && validOffhand.IsUnknown();
        using var disabled = Im.Disabled(locked);
        if (!locked && open)
            UiHelpers.OpenCombo(combo.Label);
        if (_drawCombo(this, combo, offhand.CurrentItem, out var newItem))
            offhand.SetItem(newItem);
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

    private void WeaponHelpMarker(bool hasAdvancedDyes, bool isState, StringU8 label, in EquipDrawData data, StringU8? type = null)
    {
        DrawEquipLabel(hasAdvancedDyes, label, data);
        var pos = Im.Item.UpperLeftCorner;
        Im.Line.SameInner();
        LunaStyle.DrawAlignedHelpMarker(
            "Changing weapons to weapons of different types can cause crashes, freezes, soft- and hard locks and cheating, "u8
          + "thus it is only allowed to change weapons to other weapons of the same type."u8);

        if (type is null)
            return;

        pos.Y += Im.Style.FrameHeightWithSpacing + Im.Style.FramePadding.Y;
        if (isState)
            pos.X += Im.Style.FrameHeightWithSpacing;
        Im.Window.DrawList.Text(pos, ImGuiColor.Text.Get(), $"({type})");
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private void DrawEquipLabel(bool hasAdvancedDyes, StringU8 label, in EquipDrawData data)
    {
        Im.Line.Same();
        var enabled = _designApplier.CanApplyToPlayer() is DeniedApplicationReason.None;
        using (ImGuiColor.Text.Push(_advancedMaterialColor, hasAdvancedDyes))
        {
            using var id = Im.Id.Push("apply"u8);
            if (ImEx.Button(label, disabled: !enabled) && data.GetDesign(_converter) is { } design)
                _designApplier.ApplyToPlayer(design, data.Slot, Im.Io.KeyControl || !Im.Io.KeyShift, Im.Io.KeyShift || !Im.Io.KeyControl);
        }

        DrawEquipLabelTooltip(enabled, hasAdvancedDyes);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private void DrawEquipLabel(bool hasAdvancedDyes, StringU8 label, in BonusDrawData data)
    {
        Im.Line.Same();
        var enabled = _designApplier.CanApplyToPlayer() is DeniedApplicationReason.None;
        using (ImGuiColor.Text.Push(_advancedMaterialColor, hasAdvancedDyes))
        {
            using var id = Im.Id.Push("apply"u8);
            if (ImEx.Button(label, disabled: !enabled) && data.GetDesign(_converter) is { } design)
                _designApplier.ApplyToPlayer(design, data.Slot, Im.Io.KeyControl || !Im.Io.KeyShift, Im.Io.KeyShift || !Im.Io.KeyControl);
        }

        DrawEquipLabelTooltip(enabled, hasAdvancedDyes);
    }

    public static void DrawKeepItemFilter(Configuration config)
    {
        if (Im.Checkbox("Keep Item and Dye Filters After Selection"u8, config.KeepItemComboFilter))
            config.KeepItemComboFilter ^= true;
        Im.Tooltip.OnHover(
            "Whether the filter in the item and dye combos should persist after a selection or clear after an item or dye was selected.\n\nThis can also be used to restrict the mouse-wheel scrolling to matching items."u8);
    }

    public void Dispose()
    {
        _stainCombo.Dispose();
        foreach (var combo in _equipCombo)
            combo.Dispose();
        foreach (var combo in _bonusItemCombo)
            combo.Dispose();
        foreach (var combo in _weaponCombo.Values)
            combo.Dispose();
    }

    private static void DrawEquipLabelTooltip(bool enabled, bool hasAdvancedDyes)
    {
        if (!Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
            return;

        using var tt = Im.Tooltip.Begin();
        if (!enabled)
        {
            Im.Text("No current player character available to apply to."u8);
        }
        else
        {
            Im.Text(
                "Click to apply only this slot and all related dyes and advanced dyes to your current character, according to the application rules."u8);
            Im.Text("Control + Click to apply only the item itself and no dyes, regardless of application rules."u8);
            Im.Text("Shift + Click to apply only the dyes and advanced dyes and not the item, regardless of application rules."u8);
        }

        if (!hasAdvancedDyes)
            return;

        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        Im.Separator();
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        Im.Text("This design has advanced dyes setup for this slot."u8);
    }


    private bool VerifyRestrictedGear(EquipDrawData data)
    {
        if (data.Slot.IsAccessory())
            return false;

        var (changed, _) = _items.ResolveRestrictedGear(data.CurrentItem.Armor(), data.Slot, data.CurrentRace, data.CurrentGender);
        return changed;
    }

    private static class NormalDrawer
    {
        public static void Equip(EquipmentDrawer parent, in EquipDrawData data)
        {
            data.CurrentItem.DrawIcon(parent._textures, parent._iconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();
            Im.Line.Same();
            using var group = Im.Group();
            parent.DrawItem(data, out var label, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
            }

            parent.DrawEquipLabel(data is { IsDesign: true, HasAdvancedDyes: true }, label, data);

            parent.DrawStain(data);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApplyStain(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }

            if (parent.VerifyRestrictedGear(data))
            {
                Im.Line.Same();
                Im.Text("(Restricted)"u8);
            }
        }

        public static void Bonus(EquipmentDrawer parent, in BonusDrawData data)
        {
            data.CurrentItem.DrawIcon(parent._textures, parent._iconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();
            Im.Line.Same();
            using var group = Im.Group();
            parent.DrawBonusItem(data, out var label, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
                parent.DrawEquipLabel(data is { IsDesign: true, HasAdvancedDyes: true }, label, data);
            }
            else if (data.IsState)
            {
                parent.DrawEquipLabel(data is { IsDesign: true, HasAdvancedDyes: true }, label, data);
                ImEx.TextFramed(StringU8.Empty, new Vector2(parent._comboLength, Im.Style.FrameHeight));
                Im.Tooltip.OnHover("Blame Square Enix for this doing nothing. Glasses do not support dyes whatsoever."u8);
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }
        }

        public static void Weapons(EquipmentDrawer parent, EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
        {
            using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

            mainhand.CurrentItem.DrawIcon(parent._textures, parent._iconSize, EquipSlot.MainHand);
            var left = Im.Item.Clicked();
            Im.Line.Same();
            using (Im.Group())
            {
                parent.DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, left);
                if (mainhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApply(mainhand);
                }

                parent.WeaponHelpMarker(mainhand is { IsDesign: true, HasAdvancedDyes: true }, mainhand.IsState, mainhandLabel, mainhand,
                    allWeapons ? new StringU8(mainhand.CurrentItem.Type.ToName()) : null);

                parent.DrawStain(mainhand);
                if (mainhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApplyStain(mainhand);
                }
                else if (mainhand.IsState)
                {
                    parent._advancedDyes.DrawButton(EquipSlot.MainHand,
                        mainhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
                }
            }

            var validOffhand = mainhand.CurrentItem.Type.ValidOffhand();
            if (validOffhand is FullEquipType.Unknown)
                return;

            offhand.CurrentItem.DrawIcon(parent._textures, parent._iconSize, EquipSlot.OffHand);
            var right = Im.Item.RightClicked();
            left = Im.Item.Clicked();
            Im.Line.Same();
            using (Im.Group())
            {
                parent.DrawOffhand(mainhand, validOffhand, offhand, out var offhandLabel, right, left);
                if (offhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApply(offhand);
                }

                parent.WeaponHelpMarker(offhand is { IsDesign: true, HasAdvancedDyes: true }, offhand.IsState, offhandLabel, offhand);

                parent.DrawStain(offhand);
                if (offhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApplyStain(offhand);
                }
                else if (offhand.IsState)
                {
                    parent._advancedDyes.DrawButton(EquipSlot.OffHand,
                        offhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
                }
            }
        }

        public static bool ItemCombo(EquipmentDrawer parent, BaseItemCombo combo, in EquipItem item, out EquipItem newItem)
            => combo.Draw(item, out newItem, parent._comboLength);
    }

    private static class SmallDrawer
    {
        public static void Equip(EquipmentDrawer parent, in EquipDrawData data)
        {
            parent.DrawStain(data);
            Im.Line.Same();
            parent.DrawItem(data, out var label, false, false);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
                Im.Line.Same();
                DrawApplyStain(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default,
                    true);
            }

            if (parent.VerifyRestrictedGear(data))
                label += " (Restricted)"u8;

            parent.DrawEquipLabel(data is { IsDesign: true, HasAdvancedDyes: true }, label, data);
        }

        public static void Bonus(EquipmentDrawer parent, in BonusDrawData data)
        {
            Im.Dummy(new Vector2(StainId.NumStains * Im.Style.FrameHeight + (StainId.NumStains - 1) * Im.Style.ItemSpacing.X,
                Im.Style.FrameHeight));
            Im.Line.Same();
            parent.DrawBonusItem(data, out var label, false, false);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default,
                    true);
            }

            parent.DrawEquipLabel(data is { IsDesign: true, HasAdvancedDyes: true }, label, data);
        }

        public static void Weapons(EquipmentDrawer parent, EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
        {
            parent.DrawStain(mainhand);
            Im.Line.Same();
            parent.DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, false);
            if (mainhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(mainhand);
                Im.Line.Same();
                DrawApplyStain(mainhand);
            }
            else if (mainhand.IsState)
            {
                parent._advancedDyes.DrawButton(EquipSlot.MainHand,
                    mainhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }

            if (allWeapons)
                mainhandLabel = new StringU8($"{mainhandLabel} ({mainhand.CurrentItem.Type.ToName()})");
            parent.WeaponHelpMarker(mainhand is { IsDesign: true, HasAdvancedDyes: true }, mainhand.IsState, mainhandLabel, mainhand);

            var validOffhand = mainhand.CurrentItem.Type.ValidOffhand();
            if (validOffhand is FullEquipType.Unknown)
                return;

            parent.DrawStain(offhand);
            Im.Line.Same();

            parent.DrawOffhand(mainhand, validOffhand, offhand, out var offhandLabel, false, false);
            if (offhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(offhand);
                Im.Line.Same();
                DrawApplyStain(offhand);
            }
            else if (offhand.IsState)
            {
                parent._advancedDyes.DrawButton(EquipSlot.OffHand,
                    offhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }

            parent.WeaponHelpMarker(offhand is { IsDesign: true, HasAdvancedDyes: true }, offhand.IsState, offhandLabel, offhand);
        }

        public static bool ItemCombo(EquipmentDrawer parent, BaseItemCombo combo, in EquipItem item, out EquipItem newItem)
            => combo.Draw(item, out newItem, parent._comboLength - Im.Style.FrameHeight);
    }

    private static class CompactDrawer
    {
        public static void Equip(EquipmentDrawer parent, in EquipDrawData data)
        {
            data.CurrentItem.DrawIcon(parent._textures, parent._iconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();
            Im.Line.Same();
            using var group = Im.Group();
            parent.DrawItem(data, out _, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
            }

            parent.DrawStain(data);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApplyStain(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, false);
            }

            if (!parent._compact && parent.VerifyRestrictedGear(data))
            {
                Im.Line.Same();
                Im.Text("(Restricted)"u8);
            }
        }

        public static void Bonus(EquipmentDrawer parent, in BonusDrawData data)
        {
            data.CurrentItem.DrawIcon(parent._textures, parent._iconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();
            Im.Line.Same();
            parent.DrawBonusItem(data, out _, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
            }
            else if (data.IsState)
            {
                using var group = Im.Group();
                if (parent._compact)
                    Im.FrameDummy();
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, false);
            }
        }

        public static void Weapons(EquipmentDrawer parent, EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
        {
            using var style = ImStyleDouble.ItemSpacing.PushX(Im.Style.ItemInnerSpacing.X);

            mainhand.CurrentItem.DrawIcon(parent._textures, parent._iconSize, EquipSlot.MainHand);
            var left = Im.Item.Clicked();
            Im.Line.Same();
            using (Im.Group())
            {
                parent.DrawMainhand(ref mainhand, ref offhand, out _, allWeapons, left);
                if (mainhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApply(mainhand);
                }

                parent.DrawStain(mainhand);
                if (mainhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApplyStain(mainhand);
                }
                else if (mainhand.IsState)
                {
                    parent._advancedDyes.DrawButton(EquipSlot.MainHand,
                        mainhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, false);
                }
            }

            var validOffhand = mainhand.CurrentItem.Type.ValidOffhand();
            if (validOffhand is FullEquipType.Unknown)
                return;

            offhand.CurrentItem.DrawIcon(parent._textures, parent._iconSize, EquipSlot.OffHand);
            var right = Im.Item.RightClicked();
            left = Im.Item.Clicked();
            Im.Line.Same();
            using (Im.Group())
            {
                parent.DrawOffhand(mainhand, validOffhand, offhand, out _, right, left);
                if (offhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApply(offhand);
                }

                parent.DrawStain(offhand);
                if (offhand.DisplayApplication)
                {
                    Im.Line.Same();
                    DrawApplyStain(offhand);
                }
                else if (offhand.IsState)
                {
                    parent._advancedDyes.DrawButton(EquipSlot.OffHand,
                        offhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, false);
                }
            }
        }

        public static bool ItemCombo(EquipmentDrawer parent, BaseItemCombo combo, in EquipItem item, out EquipItem newItem)
            => combo.DrawBehavior(item, out newItem, parent._comboLength);
    }

    private static class CompactSmallDrawer
    {
        public static void Equip(EquipmentDrawer parent, in EquipDrawData data)
        {
            parent.DrawStain(data);
            Im.Line.Same();
            data.CurrentItem.DrawIcon(parent._textures, parent._smallIconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();

            parent.DrawItem(data, out _, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
                Im.Line.Same();
                DrawApplyStain(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }
        }

        public static void Bonus(EquipmentDrawer parent, in BonusDrawData data)
        {
            Im.Dummy(new Vector2(StainId.NumStains * Im.Style.FrameHeight + (StainId.NumStains - 1) * Im.Style.ItemSpacing.X,
                Im.Style.FrameHeight));
            Im.Line.Same();
            data.CurrentItem.DrawIcon(parent._textures, parent._smallIconSize, data.Slot);
            var right = Im.Item.RightClicked();
            var left  = Im.Item.Clicked();

            parent.DrawBonusItem(data, out _, right, left);
            if (data.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(data);
            }
            else if (data.IsState)
            {
                parent._advancedDyes.DrawButton(data.Slot, data.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }
        }

        public static void Weapons(EquipmentDrawer parent, EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
        {
            parent.DrawStain(mainhand);
            Im.Line.Same();
            mainhand.CurrentItem.DrawIcon(parent._textures, parent._smallIconSize, mainhand.Slot);
            var left = Im.Item.Clicked();

            parent.DrawMainhand(ref mainhand, ref offhand, out _, allWeapons, left);
            if (mainhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(mainhand);
                Im.Line.Same();
                DrawApplyStain(mainhand);
            }
            else if (mainhand.IsState)
            {
                parent._advancedDyes.DrawButton(EquipSlot.MainHand,
                    mainhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }

            var validOffhand = mainhand.CurrentItem.Type.ValidOffhand();
            if (validOffhand is FullEquipType.Unknown)
                return;

            parent.DrawStain(offhand);
            Im.Line.Same();
            offhand.CurrentItem.DrawIcon(parent._textures, parent._smallIconSize, offhand.Slot);
            var right = Im.Item.RightClicked();
            left = Im.Item.Clicked();

            parent.DrawOffhand(mainhand, validOffhand, offhand, out _, right, left);
            if (offhand.DisplayApplication)
            {
                Im.Line.Same();
                DrawApply(offhand);
                Im.Line.Same();
                DrawApplyStain(offhand);
            }
            else if (offhand.IsState)
            {
                parent._advancedDyes.DrawButton(EquipSlot.OffHand,
                    offhand.HasAdvancedDyes ? parent._advancedMaterialColor : ColorParameter.Default, true);
            }
        }

        public static bool ItemCombo(EquipmentDrawer parent, BaseItemCombo combo, in EquipItem item, out EquipItem newItem)
            => combo.DrawBehavior(item, out newItem, parent._comboLength - Im.Style.FrameHeight);
    }
}
