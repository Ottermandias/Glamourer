using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.Services;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

public class PenumbraChangedItemTooltip : IDisposable
{
    private readonly PenumbraService _penumbra;
    private readonly StateManager    _stateManager;
    private readonly ItemManager     _items;
    private readonly ObjectManager   _objects;

    private readonly EquipItem[] _lastItems = new EquipItem[EquipFlagExtensions.NumEquipFlags / 2];

    public IEnumerable<KeyValuePair<EquipSlot, EquipItem>> LastItems
        => EquipSlotExtensions.EqdpSlots.Append(EquipSlot.MainHand).Append(EquipSlot.OffHand).Zip(_lastItems)
            .Select(p => new KeyValuePair<EquipSlot, EquipItem>(p.First, p.Second));

    public DateTime LastTooltip { get; private set; } = DateTime.MinValue;
    public DateTime LastClick   { get; private set; } = DateTime.MinValue;

    public PenumbraChangedItemTooltip(PenumbraService penumbra, StateManager stateManager, ItemManager items, ObjectManager objects)
    {
        _penumbra         =  penumbra;
        _stateManager     =  stateManager;
        _items            =  items;
        _objects          =  objects;
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
        var slot = item.Type.ToSlot();
        var last = _lastItems[slot.ToIndex()];
        switch (slot)
        {
            case EquipSlot.MainHand when !CanApplyWeapon(EquipSlot.MainHand, item):
            case EquipSlot.OffHand when !CanApplyWeapon(EquipSlot.OffHand,   item):
                break;
            case EquipSlot.RFinger:
                using (var tt = !openTooltip ? null : ImRaii.Tooltip())
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
                using (var tt = !openTooltip ? null : ImRaii.Tooltip())
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
                        Glamourer.Log.Information($"Applying {item.Name} to Right Finger.");
                        SetLastItem(EquipSlot.RFinger, item, state);
                        _stateManager.ChangeItem(state, EquipSlot.RFinger, item, StateChanged.Source.Manual);
                        break;
                    case (false, true):
                        Glamourer.Log.Information($"Applying {item.Name} to Left Finger.");
                        SetLastItem(EquipSlot.LFinger, item, state);
                        _stateManager.ChangeItem(state, EquipSlot.LFinger, item, StateChanged.Source.Manual);
                        break;
                    case (true, false) when last.Valid:
                        Glamourer.Log.Information($"Re-Applying {last.Name} to Right Finger.");
                        SetLastItem(EquipSlot.RFinger, default, state);
                        _stateManager.ChangeItem(state, EquipSlot.RFinger, last, StateChanged.Source.Manual);
                        break;
                    case (true, true) when _lastItems[EquipSlot.LFinger.ToIndex()].Valid:
                        Glamourer.Log.Information($"Re-Applying {last.Name} to Left Finger.");
                        SetLastItem(EquipSlot.LFinger, default, state);
                        _stateManager.ChangeItem(state, EquipSlot.LFinger, last, StateChanged.Source.Manual);
                        break;
                }

                return;
            default:
                if (ImGui.GetIO().KeyCtrl && last.Valid)
                {
                    Glamourer.Log.Information($"Re-Applying {last.Name} to {slot.ToName()}.");
                    SetLastItem(slot, default, state);
                    _stateManager.ChangeItem(state, slot, last, StateChanged.Source.Manual);
                }
                else
                {
                    Glamourer.Log.Information($"Applying {item.Name} to {slot.ToName()}.");
                    SetLastItem(slot, item, state);
                    _stateManager.ChangeItem(state, slot, item, StateChanged.Source.Manual);
                }

                return;
        }
    }

    private void OnPenumbraTooltip(ChangedItemType type, uint id)
    {
        LastTooltip = DateTime.UtcNow;
        if (!Player())
            return;

        switch (type)
        {
            case ChangedItemType.Item:
                if (!_items.ItemService.AwaitedService.TryGetValue(id, EquipSlot.MainHand, out var item))
                    return;

                CreateTooltip(item, "[Glamourer] ", false);
                return;
        }
    }

    private bool CanApplyWeapon(EquipSlot slot, EquipItem item)
    {
        var main     = _objects.Player.GetMainhand();
        var mainItem = _items.Identify(slot, main.Set, main.Type, main.Variant);
        if (slot == EquipSlot.MainHand)
            return item.Type == mainItem.Type;

        return item.Type == mainItem.Type.ValidOffhand();
    }

    private void OnPenumbraClick(MouseButton button, ChangedItemType type, uint id)
    {
        LastClick = DateTime.UtcNow;
        switch (type)
        {
            case ChangedItemType.Item:
                if (button is not MouseButton.Right)
                    return;

                if (!Player(out var state))
                    return;

                if (!_items.ItemService.AwaitedService.TryGetValue(id, EquipSlot.MainHand, out var item))
                    return;

                ApplyItem(state, item);
                return;
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
            if (oldItem.ItemId != item.ItemId)
                _lastItems[slot.ToIndex()] = oldItem;
        }
    }
}
