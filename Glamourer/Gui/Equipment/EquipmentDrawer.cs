using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Events;
using Glamourer.Services;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public class EquipmentDrawer
{
    private const float DefaultWidth = 280;

    private readonly ItemManager                            _items;
    private readonly GlamourerColorCombo                    _stainCombo;
    private readonly DictStain                             _stainData;
    private readonly ItemCombo[]                            _itemCombo;
    private readonly Dictionary<FullEquipType, WeaponCombo> _weaponCombo;
    private readonly CodeService                            _codes;
    private readonly TextureService                         _textures;
    private readonly Configuration                          _config;
    private readonly GPoseService                           _gPose;

    private float _requiredComboWidthUnscaled;
    private float _requiredComboWidth;

    public EquipmentDrawer(FavoriteManager favorites, IDataManager gameData, ItemManager items, CodeService codes, TextureService textures,
        Configuration config, GPoseService gPose)
    {
        _items       = items;
        _codes       = codes;
        _textures    = textures;
        _config      = config;
        _gPose       = gPose;
        _stainData   = items.Stains;
        _stainCombo  = new GlamourerColorCombo(DefaultWidth - 20, _stainData, favorites);
        _itemCombo   = EquipSlotExtensions.EqdpSlots.Select(e => new ItemCombo(gameData, items, e, Glamourer.Log, favorites)).ToArray();
        _weaponCombo = new Dictionary<FullEquipType, WeaponCombo>(FullEquipTypeExtensions.WeaponTypes.Count * 2);
        foreach (var type in Enum.GetValues<FullEquipType>())
        {
            if (type.ToSlot() is EquipSlot.MainHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type, Glamourer.Log));
            else if (type.ToSlot() is EquipSlot.OffHand)
                _weaponCombo.TryAdd(type, new WeaponCombo(items, type, Glamourer.Log));
        }

        _weaponCombo.Add(FullEquipType.Unknown, new WeaponCombo(items, FullEquipType.Unknown, Glamourer.Log));
    }

    private Vector2 _iconSize;
    private float   _comboLength;

    public void Prepare()
    {
        _iconSize    = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
        _comboLength = DefaultWidth * ImGuiHelpers.GlobalScale;
        if (_requiredComboWidthUnscaled == 0)
            _requiredComboWidthUnscaled = _items.ItemData.AllItems(true)
                    .Concat(_items.ItemData.AllItems(false))
                    .Max(i => ImGui.CalcTextSize($"{i.Item2.Name} ({i.Item2.ModelString})").X)
              / ImGuiHelpers.GlobalScale;

        _requiredComboWidth = _requiredComboWidthUnscaled * ImGuiHelpers.GlobalScale;
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

        using var id      = ImRaii.PushId((int)equipDrawData.Slot);
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            DrawEquipSmall(equipDrawData);
        else if (!equipDrawData.Locked && _codes.Enabled(CodeService.CodeFlag.Artisan))
            DrawEquipArtisan(equipDrawData);
        else
            DrawEquipNormal(equipDrawData);
    }

    public void DrawWeapons(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        if (mainhand.CurrentItem.PrimaryId.Id == 0)
            return;

        if (_config.HideApplyCheckmarks)
        {
            mainhand.DisplayApplication = false;
            offhand.DisplayApplication  = false;
        }

        using var id      = ImRaii.PushId("Weapons");
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            DrawWeaponsSmall(mainhand, offhand, allWeapons);
        else if (!mainhand.Locked && _codes.Enabled(CodeService.CodeFlag.Artisan))
            DrawWeaponsArtisan(mainhand, offhand);
        else
            DrawWeaponsNormal(mainhand, offhand, allWeapons);
    }

    public static void DrawMetaToggle(in ToggleDrawData data)
    {
        if (data.DisplayApplication)
        {
            var (valueChanged, applyChanged) = UiHelpers.DrawMetaToggle(data.Label, data.CurrentValue, data.CurrentApply, out var newValue,
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

    public bool DrawAllStain(out StainId ret, bool locked)
    {
        using var disabled = ImRaii.Disabled(locked);
        var       change   = _stainCombo.Draw("Dye All Slots", Stain.None.RgbaColor, string.Empty, false, false);
        ret = Stain.None.RowIndex;
        if (change)
            if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out var stain))
                ret = stain.RowIndex;
            else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                ret = Stain.None.RowIndex;

        if (!locked)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && _config.DeleteDesignModifier.IsActive())
            {
                ret    = Stain.None.RowIndex;
                change = true;
            }

            ImGuiUtil.HoverTooltip($"{_config.DeleteDesignModifier.ToString()} and Right-click to clear.");
        }

        return change;
    }

    #region Artisan

    private void DrawEquipArtisan(EquipDrawData data)
    {
        DrawStainArtisan(data);
        ImGui.SameLine();
        DrawArmorArtisan(data);
        if (!data.DisplayApplication)
            return;

        ImGui.SameLine();
        DrawApply(data);
        ImGui.SameLine();
        DrawApplyStain(data);
    }

    private void DrawWeaponsArtisan(in EquipDrawData mainhand, in EquipDrawData offhand)
    {
        using (var _ = ImRaii.PushId(0))
        {
            DrawStainArtisan(mainhand);
            ImGui.SameLine();
            DrawWeapon(mainhand);
        }

        using (var _ = ImRaii.PushId(1))
        {
            DrawStainArtisan(offhand);
            ImGui.SameLine();
            DrawWeapon(offhand);
        }

        return;

        void DrawWeapon(in EquipDrawData current)
        {
            int setId   = current.CurrentItem.PrimaryId.Id;
            int type    = current.CurrentItem.SecondaryId.Id;
            int variant = current.CurrentItem.Variant.Id;
            ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##setId", ref setId, 0, 0))
            {
                var newSetId = (PrimaryId)Math.Clamp(setId, 0, ushort.MaxValue);
                if (newSetId.Id != current.CurrentItem.PrimaryId.Id)
                    current.ItemSetter(_items.Identify(current.Slot, newSetId, current.CurrentItem.SecondaryId, current.CurrentItem.Variant));
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##type", ref type, 0, 0))
            {
                var newType = (SecondaryId)Math.Clamp(type, 0, ushort.MaxValue);
                if (newType.Id != current.CurrentItem.SecondaryId.Id)
                    current.ItemSetter(_items.Identify(current.Slot, current.CurrentItem.PrimaryId, newType, current.CurrentItem.Variant));
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##variant", ref variant, 0, 0))
            {
                var newVariant = (Variant)Math.Clamp(variant, 0, byte.MaxValue);
                if (newVariant.Id != current.CurrentItem.Variant.Id)
                    current.ItemSetter(_items.Identify(current.Slot, current.CurrentItem.PrimaryId, current.CurrentItem.SecondaryId,
                        newVariant));
            }
        }
    }

    /// <summary> Draw an input for stain that can set arbitrary values instead of choosing valid stains. </summary>
    private static void DrawStainArtisan(EquipDrawData data)
    {
        int stainId = data.CurrentStain.Id;
        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        if (!ImGui.InputInt("##stain", ref stainId, 0, 0))
            return;

        var newStainId = (StainId)Math.Clamp(stainId, 0, byte.MaxValue);
        if (newStainId != data.CurrentStain.Id)
            data.StainSetter(newStainId);
    }

    /// <summary> Draw an input for armor that can set arbitrary values instead of choosing items. </summary>
    private void DrawArmorArtisan(EquipDrawData data)
    {
        int setId   = data.CurrentItem.PrimaryId.Id;
        int variant = data.CurrentItem.Variant.Id;
        ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##setId", ref setId, 0, 0))
        {
            var newSetId = (PrimaryId)Math.Clamp(setId, 0, ushort.MaxValue);
            if (newSetId.Id != data.CurrentItem.PrimaryId.Id)
                data.ItemSetter(_items.Identify(data.Slot, newSetId, data.CurrentItem.Variant));
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##variant", ref variant, 0, 0))
        {
            var newVariant = (byte)Math.Clamp(variant, 0, byte.MaxValue);
            if (newVariant != data.CurrentItem.Variant)
                data.ItemSetter(_items.Identify(data.Slot, data.CurrentItem.PrimaryId, newVariant));
        }
    }

    #endregion

    #region Small

    private void DrawEquipSmall(in EquipDrawData equipDrawData)
    {
        DrawStain(equipDrawData, true);
        ImGui.SameLine();
        DrawItem(equipDrawData, out var label, true, false, false);
        if (equipDrawData.DisplayApplication)
        {
            ImGui.SameLine();
            DrawApply(equipDrawData);
            ImGui.SameLine();
            DrawApplyStain(equipDrawData);
        }

        if (VerifyRestrictedGear(equipDrawData))
            label += " (Restricted)";

        ImGui.SameLine();
        ImGui.TextUnformatted(label);
    }

    private void DrawWeaponsSmall(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        DrawStain(mainhand, true);
        ImGui.SameLine();
        DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, true, false);
        if (mainhand.DisplayApplication)
        {
            ImGui.SameLine();
            DrawApply(mainhand);
            ImGui.SameLine();
            DrawApplyStain(mainhand);
        }

        if (allWeapons)
            mainhandLabel += $" ({mainhand.CurrentItem.Type.ToName()})";
        WeaponHelpMarker(mainhandLabel);

        if (offhand.CurrentItem.Type is FullEquipType.Unknown)
            return;

        DrawStain(offhand, true);
        ImGui.SameLine();
        DrawOffhand(mainhand, offhand, out var offhandLabel, true, false, false);
        if (offhand.DisplayApplication)
        {
            ImGui.SameLine();
            DrawApply(offhand);
            ImGui.SameLine();
            DrawApplyStain(offhand);
        }

        WeaponHelpMarker(offhandLabel);
    }

    #endregion

    #region Normal

    private void DrawEquipNormal(in EquipDrawData equipDrawData)
    {
        equipDrawData.CurrentItem.DrawIcon(_textures, _iconSize, equipDrawData.Slot);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left  = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        DrawItem(equipDrawData, out var label, false, right, left);
        if (equipDrawData.DisplayApplication)
        {
            ImGui.SameLine();
            DrawApply(equipDrawData);
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        DrawStain(equipDrawData, false);
        if (equipDrawData.DisplayApplication)
        {
            ImGui.SameLine();
            DrawApplyStain(equipDrawData);
        }

        if (VerifyRestrictedGear(equipDrawData))
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("(Restricted)");
        }
    }

    private void DrawWeaponsNormal(EquipDrawData mainhand, EquipDrawData offhand, bool allWeapons)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing,
            ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y });

        mainhand.CurrentItem.DrawIcon(_textures, _iconSize, EquipSlot.MainHand);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            DrawMainhand(ref mainhand, ref offhand, out var mainhandLabel, allWeapons, false, left);
            if (mainhand.DisplayApplication)
            {
                ImGui.SameLine();
                DrawApply(mainhand);
            }

            WeaponHelpMarker(mainhandLabel, allWeapons ? mainhand.CurrentItem.Type.ToName() : null);

            DrawStain(mainhand, false);
            if (mainhand.DisplayApplication)
            {
                ImGui.SameLine();
                DrawApplyStain(mainhand);
            }
        }

        if (offhand.CurrentItem.Type is FullEquipType.Unknown)
            return;

        offhand.CurrentItem.DrawIcon(_textures, _iconSize, EquipSlot.OffHand);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            DrawOffhand(mainhand, offhand, out var offhandLabel, false, right, left);
            if (offhand.DisplayApplication)
            {
                ImGui.SameLine();
                DrawApply(offhand);
            }

            WeaponHelpMarker(offhandLabel);

            DrawStain(offhand, false);
            if (offhand.DisplayApplication)
            {
                ImGui.SameLine();
                DrawApplyStain(offhand);
            }
        }
    }

    private void DrawStain(in EquipDrawData data, bool small)
    {
        var       found    = _stainData.TryGetValue(data.CurrentStain, out var stain);
        using var disabled = ImRaii.Disabled(data.Locked);
        var change = small
            ? _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss)
            : _stainCombo.Draw($"##stain{data.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, _comboLength);
        if (change)
            if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain))
                data.StainSetter(stain.RowIndex);
            else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                data.StainSetter(Stain.None.RowIndex);

        if (!data.Locked && data.CurrentStain != Stain.None.RowIndex)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                data.StainSetter(Stain.None.RowIndex);

            ImGuiUtil.HoverTooltip("Right-click to clear.");
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
        if (change)
            data.ItemSetter(combo.CurrentSelection);
        else if (combo.CustomVariant.Id > 0)
            data.ItemSetter(_items.Identify(data.Slot, combo.CustomSetId, combo.CustomVariant));

        if (!data.Locked && data.CurrentItem.PrimaryId.Id != 0)
        {
            if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
                data.ItemSetter(ItemManager.NothingItem(data.Slot));

            ImGuiUtil.HoverTooltip("Right-click to clear.");
        }
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
        var       unknown = !_gPose.InGPose && mainhand.CurrentItem.Type is FullEquipType.Unknown;
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        using (var _ = ImRaii.Disabled(mainhand.Locked | unknown))
        {
            if (!mainhand.Locked && open)
                UiHelpers.OpenCombo($"##{label}");
            if (combo.Draw(mainhand.CurrentItem.Name, mainhand.CurrentItem.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
                    _requiredComboWidth))
            {
                mainhand.ItemSetter(combo.CurrentSelection);
                if (combo.CurrentSelection.Type.ValidOffhand() != mainhand.CurrentItem.Type.ValidOffhand())
                {
                    offhand.CurrentItem = _items.GetDefaultOffhand(combo.CurrentSelection);
                    offhand.ItemSetter(offhand.CurrentItem);
                }

                mainhand.CurrentItem = combo.CurrentSelection;
            }
        }

        if (unknown && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The weapon type could not be identified, thus changing it to other weapons of that type is not possible.");
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
         || !_gPose.InGPose && (offhand.CurrentItem.Type is FullEquipType.Unknown || mainhand.CurrentItem.Type is FullEquipType.Unknown);
        using var disabled = ImRaii.Disabled(locked);
        if (!locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");
        if (combo.Draw(offhand.CurrentItem.Name, offhand.CurrentItem.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength,
                _requiredComboWidth))
            offhand.ItemSetter(combo.CurrentSelection);

        if (locked)
            return;

        var defaultOffhand = _items.GetDefaultOffhand(mainhand.CurrentItem);
        if (defaultOffhand.Id == offhand.CurrentItem.Id)
            return;

        ImGuiUtil.HoverTooltip("Right-click to set to Default.");
        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
            offhand.ItemSetter(defaultOffhand);
    }

    private static void DrawApply(in EquipDrawData data)
    {
        if (UiHelpers.DrawCheckbox($"##apply{data.Slot}", "Apply this item when applying the Design.", data.CurrentApply, out var enabled,
                data.Locked))
            data.ApplySetter(enabled);
    }

    private static void DrawApplyStain(in EquipDrawData data)
    {
        if (UiHelpers.DrawCheckbox($"##applyStain{data.Slot}", "Apply this item when applying the Design.", data.CurrentApplyStain,
                out var enabled,
                data.Locked))
            data.ApplyStainSetter(enabled);
    }

    #endregion

    private static void WeaponHelpMarker(string label, string? type = null)
    {
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Changing weapons to weapons of different types can cause crashes, freezes, soft- and hard locks and cheating, "
          + "thus it is only allowed to change weapons to other weapons of the same type.");
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        if (type == null)
            return;

        var pos = ImGui.GetItemRectMin();
        pos.Y += ImGui.GetFrameHeightWithSpacing();
        ImGui.GetWindowDrawList().AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), $"({type})");
    }
}
