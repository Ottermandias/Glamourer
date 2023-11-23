using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using Glamourer.Structs;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public class EquipmentDrawer
{
    private const float DefaultWidth = 280;

    private readonly ItemManager                            _items;
    private readonly GlamourerColorCombo                    _stainCombo;
    private readonly StainData                              _stainData;
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
            _requiredComboWidthUnscaled = _items.ItemService.AwaitedService.AllItems(true)
                    .Concat(_items.ItemService.AwaitedService.AllItems(false))
                    .Max(i => ImGui.CalcTextSize($"{i.Item2.Name} ({i.Item2.ModelString})").X)
              / ImGuiHelpers.GlobalScale;

        _requiredComboWidth = _requiredComboWidthUnscaled * ImGuiHelpers.GlobalScale;
    }

    private bool VerifyRestrictedGear(EquipSlot slot, EquipItem gear, Gender gender, Race race)
    {
        if (slot.IsAccessory())
            return false;

        var (changed, _) = _items.ResolveRestrictedGear(gear.Armor(), slot, race, gender);
        return changed;
    }


    public DataChange DrawEquip(EquipSlot slot, in DesignData designData, out EquipItem rArmor, out StainId rStain, out bool rCrest, EquipFlag? cApply,
        out bool rApply, out bool rApplyStain, out bool rApplyCrest, bool locked)
        => DrawEquip(slot, designData.Item(slot), out rArmor, designData.Stain(slot), out rStain, designData.Crest(slot), out rCrest, cApply, out rApply, out rApplyStain, out rApplyCrest, locked,
            designData.Customize.Gender, designData.Customize.Race);

    public DataChange DrawEquip(EquipSlot slot, EquipItem cArmor, out EquipItem rArmor, StainId cStain, out StainId rStain, bool cCrest, out bool rCrest, EquipFlag? cApply,
        out bool rApply, out bool rApplyStain, out bool rApplyCrest, bool locked, Gender gender = Gender.Unknown, Race race = Race.Unknown)
    {
        if (_config.HideApplyCheckmarks)
            cApply = null;

        using var id      = ImRaii.PushId((int)slot);
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            return DrawEquipSmall(slot, cArmor, out rArmor, cStain, out rStain, cCrest, out rCrest, cApply, out rApply, out rApplyStain, out rApplyCrest, locked, gender, race);

        if (!locked && _codes.EnabledArtisan)
            return DrawEquipArtisan(slot, cArmor, out rArmor, cStain, out rStain, cCrest, out rCrest, cApply, out rApply, out rApplyStain, out rApplyCrest);

        return DrawEquipNormal(slot, cArmor, out rArmor, cStain, out rStain, cCrest, out rCrest, cApply, out rApply, out rApplyStain, out rApplyCrest, locked, gender, race);
    }

    public DataChange DrawWeapons(in DesignData designData, out EquipItem rMainhand, out EquipItem rOffhand, out StainId rMainhandStain,
        out StainId rOffhandStain, out bool rMainhandCrest, out bool rOffhandCrest, EquipFlag? cApply, bool allWeapons, out bool rApplyMainhand, out bool rApplyMainhandStain,
        out bool rApplyMainhandCrest, out bool rApplyOffhand, out bool rApplyOffhandStain, out bool rApplyOffhandCrest, bool locked)
        => DrawWeapons(designData.Item(EquipSlot.MainHand), out rMainhand, designData.Item(EquipSlot.OffHand), out rOffhand,
            designData.Stain(EquipSlot.MainHand), out rMainhandStain, designData.Stain(EquipSlot.OffHand), out rOffhandStain,
            designData.Crest(EquipSlot.MainHand), out rMainhandCrest, designData.Crest(EquipSlot.OffHand), out rOffhandCrest, cApply,
            allWeapons, out rApplyMainhand, out rApplyMainhandStain, out rApplyMainhandCrest, out rApplyOffhand, out rApplyOffhandStain, out rApplyOffhandCrest, locked);

    private DataChange DrawWeapons(EquipItem cMainhand, out EquipItem rMainhand, EquipItem cOffhand, out EquipItem rOffhand,
        StainId cMainhandStain, out StainId rMainhandStain, StainId cOffhandStain, out StainId rOffhandStain,
        bool cMainhandCrest, out bool rMainhandCrest, bool cOffhandCrest, out bool rOffhandCrest, EquipFlag? cApply,
        bool allWeapons, out bool rApplyMainhand, out bool rApplyMainhandStain, out bool rApplyMainhandCrest, out bool rApplyOffhand, out bool rApplyOffhandStain, out bool rApplyOffhandCrest,
        bool locked)
    {
        if (cMainhand.ModelId.Id == 0)
        {
            rOffhand            = cOffhand;
            rMainhand           = cMainhand;
            rMainhandStain      = cMainhandStain;
            rOffhandStain       = cOffhandStain;
            rMainhandCrest      = cMainhandCrest;
            rOffhandCrest       = cOffhandCrest;
            rApplyMainhand      = false;
            rApplyMainhandStain = false;
            rApplyMainhandCrest = false;
            rApplyOffhand       = false;
            rApplyOffhandStain  = false;
            rApplyOffhandCrest  = false;
            return DataChange.None;
        }

        if (_config.HideApplyCheckmarks)
            cApply = null;

        using var id      = ImRaii.PushId("Weapons");
        var       spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        if (_config.SmallEquip)
            return DrawWeaponsSmall(cMainhand, out rMainhand, cOffhand, out rOffhand, cMainhandStain, out rMainhandStain, cOffhandStain,
                out rOffhandStain, cMainhandCrest, out rMainhandCrest, cOffhandCrest, out rOffhandCrest,
                cApply, out rApplyMainhand, out rApplyMainhandStain, out rApplyMainhandCrest, out rApplyOffhand, out rApplyOffhandStain, out rApplyOffhandCrest, locked,
                allWeapons);

        if (!locked && _codes.EnabledArtisan)
            return DrawWeaponsArtisan(cMainhand, out rMainhand, cOffhand, out rOffhand, cMainhandStain, out rMainhandStain, cOffhandStain,
                out rOffhandStain, cMainhandCrest, out rMainhandCrest, cOffhandCrest, out rOffhandCrest, 
                cApply, out rApplyMainhand, out rApplyMainhandStain, out rApplyMainhandCrest, out rApplyOffhand, out rApplyOffhandStain, out rApplyOffhandCrest);

        return DrawWeaponsNormal(cMainhand, out rMainhand, cOffhand, out rOffhand, cMainhandStain, out rMainhandStain, cOffhandStain,
            out rOffhandStain, cMainhandCrest, out rMainhandCrest, cOffhandCrest, out rOffhandCrest,
            cApply, out rApplyMainhand, out rApplyMainhandStain, out rApplyMainhandCrest, out rApplyOffhand, out rApplyOffhandStain, out rApplyOffhandCrest, locked,
            allWeapons);
    }

    public static bool DrawHatState(bool currentValue, out bool newValue, bool locked)
        => UiHelpers.DrawCheckbox("Hat Visible", "Hide or show the characters head gear.", currentValue, out newValue, locked);

    public static DataChange DrawHatState(bool currentValue, bool currentApply, out bool newValue, out bool newApply, bool locked)
        => UiHelpers.DrawMetaToggle("Hat Visible", currentValue, currentApply, out newValue, out newApply, locked);

    public static bool DrawVisorState(bool currentValue, out bool newValue, bool locked)
        => UiHelpers.DrawCheckbox("Visor Toggled", "Toggle the visor state of the characters head gear.", currentValue, out newValue, locked);

    public static DataChange DrawVisorState(bool currentValue, bool currentApply, out bool newValue, out bool newApply, bool locked)
        => UiHelpers.DrawMetaToggle("Visor Toggled", currentValue, currentApply, out newValue, out newApply, locked);

    public static bool DrawWeaponState(bool currentValue, out bool newValue, bool locked)
        => UiHelpers.DrawCheckbox("Weapon Visible", "Hide or show the characters weapons when not drawn.", currentValue, out newValue, locked);

    public static DataChange DrawWeaponState(bool currentValue, bool currentApply, out bool newValue, out bool newApply, bool locked)
        => UiHelpers.DrawMetaToggle("Weapon Visible", currentValue, currentApply, out newValue, out newApply, locked);

    private bool DrawMainhand(EquipItem current, bool drawAll, out EquipItem weapon, out string label, bool locked, bool small, bool open)
    {
        weapon = current;
        if (!_weaponCombo.TryGetValue(drawAll ? FullEquipType.Unknown : current.Type, out var combo))
        {
            label = string.Empty;
            return false;
        }

        label = combo.Label;

        var       unknown = !_gPose.InGPose && current.Type is FullEquipType.Unknown;
        var       ret     = false;
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
        using (var disabled = ImRaii.Disabled(locked | unknown))
        {
            if (!locked && open)
                UiHelpers.OpenCombo($"##{label}");
            if (combo.Draw(weapon.Name, weapon.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength, _requiredComboWidth))
            {
                ret    = true;
                weapon = combo.CurrentSelection;
            }
        }

        if (unknown && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The weapon type could not be identified, thus changing it to other weapons of that type is not possible.");

        return ret;
    }

    private bool DrawOffhand(EquipItem mainhand, EquipItem current, out EquipItem weapon, out string label, bool locked, bool small, bool clear,
        bool open)
    {
        weapon = current;
        if (!_weaponCombo.TryGetValue(current.Type, out var combo))
        {
            label = string.Empty;
            return false;
        }

        label  =  combo.Label;
        locked |= !_gPose.InGPose && (current.Type is FullEquipType.Unknown || mainhand.Type is FullEquipType.Unknown);
        using var disabled = ImRaii.Disabled(locked);
        if (!locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");
        var change = combo.Draw(weapon.Name, weapon.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength, _requiredComboWidth);
        if (change)
            weapon = combo.CurrentSelection;

        if (!locked)
        {
            var defaultOffhand = _items.GetDefaultOffhand(mainhand);
            if (defaultOffhand.Id != weapon.Id)
            {
                ImGuiUtil.HoverTooltip("Right-click to set to Default.");
                if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    change = true;
                    weapon = defaultOffhand;
                }
            }
        }

        return change;
    }

    private bool DrawApply(EquipSlot slot, EquipFlag flags, out bool enabled, bool locked)
        => UiHelpers.DrawCheckbox($"##apply{slot}", "Apply this item when applying the Design.", flags.HasFlag(slot.ToFlag()), out enabled,
            locked);

    private bool DrawApplyStain(EquipSlot slot, EquipFlag flags, out bool enabled, bool locked)
        => UiHelpers.DrawCheckbox($"##applyStain{slot}", "Apply this dye when applying the Design.", flags.HasFlag(slot.ToStainFlag()),
            out enabled, locked);

    private bool DrawItem(EquipSlot slot, EquipItem current, out EquipItem armor, out string label, bool locked, bool small, bool clear,
        bool open)
    {
        Debug.Assert(slot.IsEquipment() || slot.IsAccessory(), $"Called {nameof(DrawItem)} on {slot}.");
        var combo = _itemCombo[slot.ToIndex()];
        label = combo.Label;
        armor = current;
        if (!locked && open)
            UiHelpers.OpenCombo($"##{combo.Label}");

        using var disabled = ImRaii.Disabled(locked);
        var change = combo.Draw(armor.Name, armor.ItemId, small ? _comboLength - ImGui.GetFrameHeight() : _comboLength, _requiredComboWidth);
        if (change)
            armor = combo.CurrentSelection;

        if (!locked && armor.ModelId.Id != 0)
        {
            if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change = true;
                armor  = ItemManager.NothingItem(slot);
            }

            ImGuiUtil.HoverTooltip("Right-click to clear.");
        }

        return change;
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

    private bool DrawStain(EquipSlot slot, StainId current, out StainId ret, bool locked, bool small)
    {
        var       found    = _stainData.TryGetValue(current, out var stain);
        using var disabled = ImRaii.Disabled(locked);
        var change = small
            ? _stainCombo.Draw($"##stain{slot}", stain.RgbaColor, stain.Name, found, stain.Gloss)
            : _stainCombo.Draw($"##stain{slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, _comboLength);
        ret = current;
        if (change)
            if (_stainData.TryGetValue(_stainCombo.CurrentSelection.Key, out stain))
                ret = stain.RowIndex;
            else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                ret = Stain.None.RowIndex;

        if (!locked && ret != Stain.None.RowIndex)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ret    = Stain.None.RowIndex;
                change = true;
            }

            ImGuiUtil.HoverTooltip("Right-click to clear.");
        }

        return change;
    }

    internal static bool CanHaveCrest(EquipSlot slot)
        => slot is EquipSlot.OffHand or EquipSlot.Head or EquipSlot.Body;

    private DataChange DrawCrest(EquipSlot slot, bool current, EquipFlag? cApply, out bool ret, out bool rApply, bool locked, bool small, bool change2)
    {
        var changes = DataChange.None;
        if (cApply.HasValue)
        {
            var apply          = cApply.Value.HasFlag(slot.ToCrestFlag());
            var flags          = (sbyte)(apply ? current ? 1 : -1 : 0);
            using var style    = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
            using (var disabled = ImRaii.Disabled(locked))
            {
                if (new TristateCheckbox(ColorId.TriStateCross.Value(), ColorId.TriStateCheck.Value(), ColorId.TriStateNeutral.Value()).Draw($"##crest{slot}", flags, out flags))
                {
                    (ret, rApply) = flags switch
                    {
                        -1 => (false, true),
                        0  => (current, false),
                        _  => (true, true),
                    };
                }
                else
                {
                    ret    = current;
                    rApply = apply;
                }
            }
            ImGuiUtil.HoverTooltip($"Display your Free Company crest on this equipment.\n\nThis attribute will be {(apply ? current ? "enabled." : "disabled." : "kept as is.")}");
            if (ret != current)
                changes |= change2 ? DataChange.Crest2 : DataChange.Crest;
            if (rApply != apply)
                changes |= change2 ? DataChange.ApplyCrest2 : DataChange.ApplyCrest;
        }
        else
        {
            rApply = false;
            if (UiHelpers.DrawCheckbox($"##crest{slot}", "Display your Free Company crest on this equipment.", current, out ret, locked))
                changes |= change2 ? DataChange.Crest2 : DataChange.Crest;
        }
        return changes;
    }

    /// <summary> Draw an input for armor that can set arbitrary values instead of choosing items. </summary>
    private bool DrawArmorArtisan(EquipSlot slot, EquipItem current, out EquipItem armor)
    {
        int setId   = current.ModelId.Id;
        int variant = current.Variant.Id;
        var ret     = false;
        armor = current;
        ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##setId", ref setId, 0, 0))
        {
            var newSetId = (SetId)Math.Clamp(setId, 0, ushort.MaxValue);
            if (newSetId.Id != current.ModelId.Id)
            {
                armor = _items.Identify(slot, newSetId, current.Variant);
                ret   = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##variant", ref variant, 0, 0))
        {
            var newVariant = (byte)Math.Clamp(variant, 0, byte.MaxValue);
            if (newVariant != current.Variant)
            {
                armor = _items.Identify(slot, current.ModelId, newVariant);
                ret   = true;
            }
        }

        return ret;
    }

    /// <summary> Draw an input for stain that can set arbitrary values instead of choosing valid stains. </summary>
    private bool DrawStainArtisan(EquipSlot slot, StainId current, out StainId stain)
    {
        int stainId = current.Id;
        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##stain", ref stainId, 0, 0))
        {
            var newStainId = (StainId)Math.Clamp(stainId, 0, byte.MaxValue);
            if (newStainId != current)
            {
                stain = newStainId;
                return true;
            }
        }

        stain = current;
        return false;
    }

    private DataChange DrawEquipArtisan(EquipSlot slot, EquipItem cArmor, out EquipItem rArmor, StainId cStain, out StainId rStain, bool cCrest, out bool rCrest,
        EquipFlag? cApply, out bool rApply, out bool rApplyStain, out bool rApplyCrest)
    {
        var changes = DataChange.None;
        if (DrawStainArtisan(slot, cStain, out rStain))
            changes |= DataChange.Stain;
        ImGui.SameLine();
        if (DrawArmorArtisan(slot, cArmor, out rArmor))
            changes |= DataChange.Item;
        if (CanHaveCrest(slot))
        {
            ImGui.SameLine();
            changes |= DrawCrest(slot, cCrest, cApply, out rCrest, out rApplyCrest, false, true, false);
        }
        else
        {
            rCrest      = cCrest;
            rApplyCrest = false;
        }
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApply(slot, cApply.Value, out rApply, false))
                changes |= DataChange.ApplyItem;
            ImGui.SameLine();
            if (DrawApplyStain(slot, cApply.Value, out rApplyStain, false))
                changes |= DataChange.ApplyStain;
        }
        else
        {
            rApply      = false;
            rApplyStain = false;
        }

        return changes;
    }

    private DataChange DrawEquipSmall(EquipSlot slot, EquipItem cArmor, out EquipItem rArmor, StainId cStain, out StainId rStain, bool cCrest, out bool rCrest,
        EquipFlag? cApply, out bool rApply, out bool rApplyStain, out bool rApplyCrest, bool locked, Gender gender, Race race)
    {
        var changes = DataChange.None;
        if (DrawStain(slot, cStain, out rStain, locked, true))
            changes |= DataChange.Stain;
        ImGui.SameLine();
        if (DrawItem(slot, cArmor, out rArmor, out var label, locked, true, false, false))
            changes |= DataChange.Item;
        if (CanHaveCrest(slot))
        {
            ImGui.SameLine();
            changes |= DrawCrest(slot, cCrest, cApply, out rCrest, out rApplyCrest, locked, true, false);
        }
        else
        {
            rCrest      = cCrest;
            rApplyCrest = false;
        }
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApply(slot, cApply.Value, out rApply, false))
                changes |= DataChange.ApplyItem;
            ImGui.SameLine();
            if (DrawApplyStain(slot, cApply.Value, out rApplyStain, false))
                changes |= DataChange.ApplyStain;
        }
        else
        {
            rApply      = false;
            rApplyStain = false;
        }

        if (VerifyRestrictedGear(slot, rArmor, gender, race))
            label += " (Restricted)";

        ImGui.SameLine();
        ImGui.TextUnformatted(label);

        return changes;
    }

    private DataChange DrawEquipNormal(EquipSlot slot, EquipItem cArmor, out EquipItem rArmor, StainId cStain, out StainId rStain, bool cCrest, out bool rCrest,
        EquipFlag? cApply, out bool rApply, out bool rApplyStain, out bool rApplyCrest, bool locked, Gender gender, Race race)
    {
        var changes = DataChange.None;
        cArmor.DrawIcon(_textures, _iconSize, slot);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left  = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using var group = ImRaii.Group();
        if (DrawItem(slot, cArmor, out rArmor, out var label, locked, false, right, left))
            changes |= DataChange.Item;
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApply(slot, cApply.Value, out rApply, locked))
                changes |= DataChange.ApplyItem;
        }
        else
        {
            rApply = true;
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        if (DrawStain(slot, cStain, out rStain, locked, false))
            changes |= DataChange.Stain;
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApplyStain(slot, cApply.Value, out rApplyStain, locked))
                changes |= DataChange.ApplyStain;
        }
        else
        {
            rApplyStain = true;
        }
        if (CanHaveCrest(slot))
        {
            ImGui.SameLine();
            changes |= DrawCrest(slot, cCrest, cApply, out rCrest, out rApplyCrest, locked, false, false);
        }
        else
        {
            rCrest      = cCrest;
            rApplyCrest = true;
        }

        if (VerifyRestrictedGear(slot, rArmor, gender, race))
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("(Restricted)");
        }

        return changes;
    }

    private static void WeaponHelpMarker(string label, string? type = null)
    {
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Changing weapons to weapons of different types can cause crashes, freezes, soft- and hard locks and cheating, "
          + "thus it is only allowed to change weapons to other weapons of the same type.");
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        if (type != null)
        {
            var pos = ImGui.GetItemRectMin();
            pos.Y += ImGui.GetFrameHeightWithSpacing();
            ImGui.GetWindowDrawList().AddText(pos, ImGui.GetColorU32(ImGuiCol.Text), $"({type})");
        }
    }

    private DataChange DrawWeaponsSmall(EquipItem cMainhand, out EquipItem rMainhand, EquipItem cOffhand, out EquipItem rOffhand,
        StainId cMainhandStain, out StainId rMainhandStain, StainId cOffhandStain, out StainId rOffhandStain,
        bool cMainhandCrest, out bool rMainhandCrest, bool cOffhandCrest, out bool rOffhandCrest, EquipFlag? cApply,
        out bool rApplyMainhand, out bool rApplyMainhandStain, out bool rApplyMainhandCrest, out bool rApplyOffhand, out bool rApplyOffhandStain, out bool rApplyOffhandCrest, bool locked,
        bool allWeapons)
    {
        var changes = DataChange.None;
        if (DrawStain(EquipSlot.MainHand, cMainhandStain, out rMainhandStain, locked, true))
            changes |= DataChange.Stain;
        ImGui.SameLine();

        rOffhand = cOffhand;
        if (DrawMainhand(cMainhand, allWeapons, out rMainhand, out var mainhandLabel, locked, true, false))
        {
            changes |= DataChange.Item;
            if (rMainhand.Type.ValidOffhand() != cMainhand.Type.ValidOffhand())
            {
                rOffhand =  _items.GetDefaultOffhand(rMainhand);
                changes  |= DataChange.Item2;
            }
        }

        if (CanHaveCrest(EquipSlot.MainHand))
        {
            ImGui.SameLine();
            changes |= DrawCrest(EquipSlot.MainHand, cMainhandCrest, cApply, out rMainhandCrest, out rApplyMainhandCrest, locked, false, false);
        }
        else
        {
            rMainhandCrest      = cMainhandCrest;
            rApplyMainhandCrest = true;
        }
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApply(EquipSlot.MainHand, cApply.Value, out rApplyMainhand, locked))
                changes |= DataChange.ApplyItem;
            ImGui.SameLine();
            if (DrawApplyStain(EquipSlot.MainHand, cApply.Value, out rApplyMainhandStain, locked))
                changes |= DataChange.ApplyStain;
        }
        else
        {
            rApplyMainhand      = true;
            rApplyMainhandStain = true;
        }

        if (allWeapons)
            mainhandLabel += $" ({cMainhand.Type.ToName()})";
        WeaponHelpMarker(mainhandLabel);

        if (rOffhand.Type is FullEquipType.Unknown)
        {
            rOffhandStain      = cOffhandStain;
            rOffhandCrest      = cOffhandCrest;
            rApplyOffhand      = false;
            rApplyOffhandStain = false;
            rApplyOffhandCrest = false;
            return changes;
        }

        if (DrawStain(EquipSlot.OffHand, cOffhandStain, out rOffhandStain, locked, true))
            changes |= DataChange.Stain2;

        ImGui.SameLine();
        if (DrawOffhand(rMainhand, rOffhand, out rOffhand, out var offhandLabel, locked, true, false, false))
            changes |= DataChange.Item2;

        if (CanHaveCrest(EquipSlot.OffHand))
        {
            ImGui.SameLine();
            changes |= DrawCrest(EquipSlot.OffHand, cOffhandCrest, cApply, out rOffhandCrest, out rApplyOffhandCrest, locked, false, true);
        }
        else
        {
            rOffhandCrest      = cOffhandCrest;
            rApplyOffhandCrest = true;
        }
        if (cApply.HasValue)
        {
            ImGui.SameLine();
            if (DrawApply(EquipSlot.OffHand, cApply.Value, out rApplyOffhand, locked))
                changes |= DataChange.ApplyItem2;
            ImGui.SameLine();
            if (DrawApplyStain(EquipSlot.OffHand, cApply.Value, out rApplyOffhandStain, locked))
                changes |= DataChange.ApplyStain2;
        }
        else
        {
            rApplyOffhand      = true;
            rApplyOffhandStain = true;
        }

        WeaponHelpMarker(offhandLabel);

        return changes;
    }

    private DataChange DrawWeaponsNormal(EquipItem cMainhand, out EquipItem rMainhand, EquipItem cOffhand, out EquipItem rOffhand,
        StainId cMainhandStain, out StainId rMainhandStain, StainId cOffhandStain, out StainId rOffhandStain,
        bool cMainhandCrest, out bool rMainhandCrest, bool cOffhandCrest, out bool rOffhandCrest, EquipFlag? cApply,
        out bool rApplyMainhand, out bool rApplyMainhandStain, out bool rApplyMainhandCrest, out bool rApplyOffhand, out bool rApplyOffhandStain, out bool rApplyOffhandCrest, bool locked,
        bool allWeapons)
    {
        var changes = DataChange.None;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing,
            ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y });

        cMainhand.DrawIcon(_textures, _iconSize, EquipSlot.MainHand);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            rOffhand = cOffhand;
            if (DrawMainhand(cMainhand, allWeapons, out rMainhand, out var mainhandLabel, locked, false, left))
            {
                changes |= DataChange.Item;
                if (rMainhand.Type.ValidOffhand() != cMainhand.Type.ValidOffhand())
                {
                    rOffhand =  _items.GetDefaultOffhand(rMainhand);
                    changes  |= DataChange.Item2;
                }
            }

            if (cApply.HasValue)
            {
                ImGui.SameLine();
                if (DrawApply(EquipSlot.MainHand, cApply.Value, out rApplyMainhand, locked))
                    changes |= DataChange.ApplyItem;
            }
            else
            {
                rApplyMainhand = true;
            }

            WeaponHelpMarker(mainhandLabel, allWeapons ? cMainhand.Type.ToName() : null);

            if (DrawStain(EquipSlot.MainHand, cMainhandStain, out rMainhandStain, locked, false))
                changes |= DataChange.Stain;
            if (cApply.HasValue)
            {
                ImGui.SameLine();
                if (DrawApplyStain(EquipSlot.MainHand, cApply.Value, out rApplyMainhandStain, locked))
                    changes |= DataChange.ApplyStain;
            }
            else
            {
                rApplyMainhandStain = true;
            }

            if (CanHaveCrest(EquipSlot.MainHand))
            {
                ImGui.SameLine();
                changes |= DrawCrest(EquipSlot.MainHand, cMainhandCrest, cApply, out rMainhandCrest, out rApplyMainhandCrest, locked, false, false);
            }
            else
            {
                rMainhandCrest      = cMainhandCrest;
                rApplyMainhandCrest = true;
            }
        }

        if (rOffhand.Type is FullEquipType.Unknown)
        {
            rOffhandStain      = cOffhandStain;
            rOffhandCrest      = cOffhandCrest;
            rApplyOffhand      = false;
            rApplyOffhandStain = false;
            rApplyOffhandCrest = false;
            return changes;
        }

        rOffhand.DrawIcon(_textures, _iconSize, EquipSlot.OffHand);
        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        left = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            if (DrawOffhand(rMainhand, rOffhand, out rOffhand, out var offhandLabel, locked, false, right, left))
                changes |= DataChange.Item2;
            if (cApply.HasValue)
            {
                ImGui.SameLine();
                if (DrawApply(EquipSlot.OffHand, cApply.Value, out rApplyOffhand, locked))
                    changes |= DataChange.ApplyItem2;
            }
            else
            {
                rApplyOffhand = true;
            }

            WeaponHelpMarker(offhandLabel);

            if (DrawStain(EquipSlot.OffHand, cOffhandStain, out rOffhandStain, locked, false))
                changes |= DataChange.Stain2;
            if (cApply.HasValue)
            {
                ImGui.SameLine();
                if (DrawApplyStain(EquipSlot.OffHand, cApply.Value, out rApplyOffhandStain, locked))
                    changes |= DataChange.ApplyStain2;
            }
            else
            {
                rApplyOffhandStain = true;
            }

            if (CanHaveCrest(EquipSlot.OffHand))
            {
                ImGui.SameLine();
                changes |= DrawCrest(EquipSlot.OffHand, cOffhandCrest, cApply, out rOffhandCrest, out rApplyOffhandCrest, locked, false, true);
            }
            else
            {
                rOffhandCrest      = cOffhandCrest;
                rApplyOffhandCrest = true;
            }
        }

        return changes;
    }

    private DataChange DrawWeaponsArtisan(EquipItem cMainhand, out EquipItem rMainhand, EquipItem cOffhand, out EquipItem rOffhand,
        StainId cMainhandStain, out StainId rMainhandStain, StainId cOffhandStain, out StainId rOffhandStain,
        bool cMainhandCrest, out bool rMainhandCrest, bool cOffhandCrest, out bool rOffhandCrest, EquipFlag? cApply,
        out bool rApplyMainhand, out bool rApplyMainhandStain, out bool rApplyMainhandCrest, out bool rApplyOffhand, out bool rApplyOffhandStain, out bool rApplyOffhandCrest)
    {
        rApplyMainhand      = (cApply ?? 0).HasFlag(EquipFlag.Mainhand);
        rApplyMainhandStain = (cApply ?? 0).HasFlag(EquipFlag.MainhandStain);
        rApplyOffhand       = (cApply ?? 0).HasFlag(EquipFlag.Offhand);
        rApplyOffhandStain  = (cApply ?? 0).HasFlag(EquipFlag.OffhandStain);

        bool DrawWeapon(EquipItem current, out EquipItem ret)
        {
            int setId   = current.ModelId.Id;
            int type    = current.WeaponType.Id;
            int variant = current.Variant.Id;
            ret = current;
            var changed = false;

            ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##setId", ref setId, 0, 0))
            {
                var newSetId = (SetId)Math.Clamp(setId, 0, ushort.MaxValue);
                if (newSetId.Id != current.ModelId.Id)
                {
                    ret     = _items.Identify(EquipSlot.MainHand, newSetId, current.WeaponType, current.Variant);
                    changed = true;
                }
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##type", ref type, 0, 0))
            {
                var newType = (WeaponType)Math.Clamp(type, 0, ushort.MaxValue);
                if (newType.Id != current.WeaponType.Id)
                {
                    ret     = _items.Identify(EquipSlot.MainHand, current.ModelId, newType, current.Variant);
                    changed = true;
                }
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("##variant", ref variant, 0, 0))
            {
                var newVariant = (Variant)Math.Clamp(variant, 0, byte.MaxValue);
                if (newVariant.Id != current.Variant.Id)
                {
                    ret     = _items.Identify(EquipSlot.MainHand, current.ModelId, current.WeaponType, newVariant);
                    changed = true;
                }
            }

            return changed;
        }

        var ret = DataChange.None;
        using (var id = ImRaii.PushId(0))
        {
            if (DrawStainArtisan(EquipSlot.MainHand, cMainhandStain, out rMainhandStain))
                ret |= DataChange.Stain;
            ImGui.SameLine();
            if (DrawWeapon(cMainhand, out rMainhand))
                ret |= DataChange.Item;
            if (CanHaveCrest(EquipSlot.MainHand))
            {
                ImGui.SameLine();
                ret |= DrawCrest(EquipSlot.MainHand, cMainhandCrest, cApply, out rMainhandCrest, out rApplyMainhandCrest, false, true, false);
            }
            else
            {
                rMainhandCrest      = cMainhandCrest;
                rApplyMainhandCrest = true;
            }
        }

        using (var id = ImRaii.PushId(1))
        {
            if (DrawStainArtisan(EquipSlot.OffHand, cOffhandStain, out rOffhandStain))
                ret |= DataChange.Stain2;
            ImGui.SameLine();
            if (DrawWeapon(cOffhand, out rOffhand))
                ret |= DataChange.Item2;
            if (CanHaveCrest(EquipSlot.OffHand))
            {
                ImGui.SameLine();
                ret |= DrawCrest(EquipSlot.OffHand, cOffhandCrest, cApply, out rOffhandCrest, out rApplyOffhandCrest, false, true, true);
            }
            else
            {
                rOffhandCrest      = cOffhandCrest;
                rApplyOffhandCrest = true;
            }
        }

        return ret;
    }
}
