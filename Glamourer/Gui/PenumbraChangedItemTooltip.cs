using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

public sealed class PenumbraChangedItemTooltip : IDisposable
{
    private readonly PenumbraService  _penumbra;
    private readonly StateManager     _stateManager;
    private readonly ItemManager      _items;
    private readonly ObjectManager    _objects;
    private readonly CustomizeService _customize;
    private readonly GPoseService     _gpose;

    private readonly EquipItem[] _lastItems = new EquipItem[EquipFlagExtensions.NumEquipFlags / 2];

    public IEnumerable<KeyValuePair<EquipSlot, EquipItem>> LastItems
        => EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand).Zip(_lastItems)
            .Select(p => new KeyValuePair<EquipSlot, EquipItem>(p.First, p.Second));

    public ChangedItemType LastType    { get; private set; } = ChangedItemType.None;
    public uint            LastId      { get; private set; }
    public DateTime        LastTooltip { get; private set; } = DateTime.MinValue;
    public DateTime        LastClick   { get; private set; } = DateTime.MinValue;

    public PenumbraChangedItemTooltip(PenumbraService penumbra, StateManager stateManager, ItemManager items, ObjectManager objects,
        CustomizeService customize, GPoseService gpose)
    {
        _penumbra         =  penumbra;
        _stateManager     =  stateManager;
        _items            =  items;
        _objects          =  objects;
        _customize        =  customize;
        _gpose            =  gpose;
        _penumbra.Tooltip += OnPenumbraTooltip;
        _penumbra.Click   += OnPenumbraClick;
    }

    public void Dispose()
    {
        _penumbra.Tooltip -= OnPenumbraTooltip;
        _penumbra.Click   -= OnPenumbraClick;
    }

    public bool Player()
        => _objects.Player.Valid;

    public bool Player([NotNullWhen(true)] out ActorState? player)
    {
        var (identifier, data) = _objects.PlayerData;
        if (!data.Valid || !_stateManager.GetOrCreate(identifier, data.Objects[0], out player))
        {
            player = null;
            return false;
        }

        return true;
    }

    public void CreateTooltip(EquipItem item, string prefix, bool openTooltip)
    {
        if (!Player())
            return;

        var slot = item.Type.ToSlot();
        var last = _lastItems[slot.ToIndex()];
        switch (slot)
        {
            case EquipSlot.MainHand when !CanApplyWeapon(EquipSlot.MainHand, item):
            case EquipSlot.OffHand when !CanApplyWeapon(EquipSlot.OffHand,   item):
                break;
            case EquipSlot.RFinger:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix}Right-Click to apply to current actor (Right Finger).");
                    ImGui.TextUnformatted($"{prefix}Shift + Right-Click to apply to current actor (Left Finger).");
                    if (last.Valid)
                        ImGui.TextUnformatted(
                            $"{prefix}Control + Right-Click to re-apply {last.Name} to current actor (Right Finger).");

                    var last2 = _lastItems[EquipSlot.LFinger.ToIndex()];
                    if (last2.Valid)
                        ImGui.TextUnformatted(
                            $"{prefix}Shift + Control + Right-Click to re-apply {last.Name} to current actor (Left Finger).");
                }

                break;
            default:
                using (_ = !openTooltip ? null : ImRaii.Tooltip())
                {
                    ImGui.TextUnformatted($"{prefix}Right-Click to apply to current actor.");
                    if (last.Valid)
                        ImGui.TextUnformatted($"{prefix}Control + Right-Click to re-apply {last.Name} to current actor.");
                }

                break;
        }
    }

    public void ApplyItem(ActorState state, EquipItem item)
    {
        var slot = item.Type.ToSlot();
        var last = _lastItems[slot.ToIndex()];
        switch (slot)
        {
            case EquipSlot.MainHand when !CanApplyWeapon(EquipSlot.MainHand, item):
            case EquipSlot.OffHand when !CanApplyWeapon(EquipSlot.OffHand,   item):
                break;
            case EquipSlot.RFinger:
                switch (ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift)
                {
                    case (false, false):
                        Glamourer.Log.Debug($"Applying {item.Name} to Right Finger.");
                        SetLastItem(EquipSlot.RFinger, item, state);
                        _stateManager.ChangeItem(state, EquipSlot.RFinger, item, ApplySettings.Manual);
                        break;
                    case (false, true):
                        Glamourer.Log.Debug($"Applying {item.Name} to Left Finger.");
                        SetLastItem(EquipSlot.LFinger, item, state);
                        _stateManager.ChangeItem(state, EquipSlot.LFinger, item, ApplySettings.Manual);
                        break;
                    case (true, false) when last.Valid:
                        Glamourer.Log.Debug($"Re-Applying {last.Name} to Right Finger.");
                        SetLastItem(EquipSlot.RFinger, default, state);
                        _stateManager.ChangeItem(state, EquipSlot.RFinger, last, ApplySettings.Manual);
                        break;
                    case (true, true) when _lastItems[EquipSlot.LFinger.ToIndex()].Valid:
                        Glamourer.Log.Debug($"Re-Applying {last.Name} to Left Finger.");
                        SetLastItem(EquipSlot.LFinger, default, state);
                        _stateManager.ChangeItem(state, EquipSlot.LFinger, last, ApplySettings.Manual);
                        break;
                }

                return;
            default:
                if (ImGui.GetIO().KeyCtrl && last.Valid)
                {
                    Glamourer.Log.Debug($"Re-Applying {last.Name} to {slot.ToName()}.");
                    SetLastItem(slot, default, state);
                    _stateManager.ChangeItem(state, slot, last, ApplySettings.Manual);
                }
                else
                {
                    Glamourer.Log.Debug($"Applying {item.Name} to {slot.ToName()}.");
                    SetLastItem(slot, item, state);
                    _stateManager.ChangeItem(state, slot, item, ApplySettings.Manual);
                }

                return;
        }
    }

    private void OnPenumbraTooltip(ChangedItemType type, uint id)
    {
        LastType    = type;
        LastId      = id;
        LastTooltip = DateTime.UtcNow;
        if (!Player())
            return;

        switch (type)
        {
            case ChangedItemType.ItemOffhand:
            case ChangedItemType.Item:
            {
                if (!_items.ItemData.TryGetValue(id, type is ChangedItemType.Item ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
                    return;

                CreateTooltip(item, "[Glamourer] ", false);
                return;
            }
            case ChangedItemType.CustomArmor:
            {
                var (model, variant, slot) = IdentifiedItem.Split(id);
                var item = _items.Identify(slot.ToSlot(), model, variant);
                if (item.Valid)
                    CreateTooltip(item, "[Glamourer] ", false);
                return;
            }
            case ChangedItemType.Customization:
            {
                var (race, gender, index, value) = IdentifiedCustomization.Split(id);
                if (!_objects.Player.Model.IsHuman)
                    return;

                var customize = _objects.Player.Model.GetCustomize();
                if (CheckGenderRace(customize, race, gender) && VerifyValue(customize, index, value))
                    ImGui.TextUnformatted("[Glamourer] Right-Click to apply to current actor.");

                return;
            }
        }
    }

    private bool CanApplyWeapon(EquipSlot slot, EquipItem item)
    {
        if (_gpose.InGPose && slot is EquipSlot.MainHand)
            return true;

        var main     = _objects.Player.GetMainhand();
        var mainItem = _items.Identify(EquipSlot.MainHand, main.Skeleton, main.Weapon, main.Variant);
        if (slot == EquipSlot.MainHand)
            return item.Type == mainItem.Type;

        return item.Type == mainItem.Type.ValidOffhand();
    }

    private void OnPenumbraClick(MouseButton button, ChangedItemType type, uint id)
    {
        LastClick = DateTime.UtcNow;
        if (button is not MouseButton.Right)
            return;

        if (!Player(out var state))
            return;

        switch (type)
        {
            case ChangedItemType.Item:
            case ChangedItemType.ItemOffhand:
            {
                if (!_items.ItemData.TryGetValue(id, type is ChangedItemType.Item ? EquipSlot.MainHand : EquipSlot.OffHand, out var item))
                    return;

                ApplyItem(state, item);
                return;
            }
            case ChangedItemType.CustomArmor:
            {
                var (model, variant, slot) = IdentifiedItem.Split(id);
                var item = _items.Identify(slot.ToSlot(), model, variant);
                if (item.Valid)
                    ApplyItem(state, item);
                return;
            }
            case ChangedItemType.Customization:
            {
                var (race, gender, index, value) = IdentifiedCustomization.Split(id);
                var customize = state.ModelData.Customize;
                if (CheckGenderRace(customize, race, gender) && VerifyValue(customize, index, value))
                    _stateManager.ChangeCustomize(state, index, value, ApplySettings.Manual);
                return;
            }
        }
    }

    private void SetLastItem(EquipSlot slot, EquipItem item, ActorState state)
    {
        ref var last = ref _lastItems[slot.ToIndex()];
        if (!item.Valid)
        {
            last = default;
        }
        else
        {
            var oldItem = state.ModelData.Item(slot);
            if (oldItem.Id != item.Id)
                _lastItems[slot.ToIndex()] = oldItem;
        }
    }

    private static bool CheckGenderRace(in CustomizeArray customize, ModelRace race, Gender gender)
    {
        if (race is ModelRace.Unknown && gender is Gender.Unknown)
            return true;
        if (gender != customize.Gender)
            return false;
        if (race.ToRace() != customize.Race)
            return false;
        if (race is ModelRace.Highlander && customize.Clan is not SubRace.Highlander)
            return false;

        return true;
    }

    private bool VerifyValue(in CustomizeArray customize, CustomizeIndex index, CustomizeValue value)
        => _customize.IsCustomizationValid(customize.Clan, customize.Gender, customize.Face, index, value);
}
