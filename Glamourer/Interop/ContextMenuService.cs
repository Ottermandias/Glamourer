using System;
using Dalamud.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Glamourer.Events;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public class ContextMenuService : IDisposable
{
    public const int ItemSearchContextItemId = 0x1738;
    public const int ChatLogContextItemId    = 0x948;

    private readonly ItemManager        _items;
    private readonly DalamudContextMenu _contextMenu;
    private readonly StateManager       _state;
    private readonly ObjectManager      _objects;
    private readonly IGameGui           _gameGui;

    public ContextMenuService(ItemManager items, StateManager state, ObjectManager objects, IGameGui gameGui, Configuration config,
        DalamudPluginInterface pi)
    {
        _contextMenu = new DalamudContextMenu(pi);
        _items       = items;
        _state       = state;
        _objects     = objects;
        _gameGui     = gameGui;
        if (config.EnableGameContextMenu)
            Enable();
    }

    public void Enable()
    {
        _contextMenu.OnOpenGameObjectContextMenu += AddGameObjectItem;
        _contextMenu.OnOpenInventoryContextMenu  += AddInventoryItem;
    }

    public void Disable()
    {
        _contextMenu.OnOpenGameObjectContextMenu -= AddGameObjectItem;
        _contextMenu.OnOpenInventoryContextMenu  -= AddInventoryItem;
    }

    public void Dispose()
    {
        Disable();
        _contextMenu.Dispose();
    }

    private static readonly SeString TryOnString = new SeStringBuilder().AddUiForeground(SeIconChar.BoxedLetterG.ToIconString(), 541)
        .AddText(" Try On").AddUiForegroundOff().BuiltString;

    private void AddInventoryItem(InventoryContextMenuOpenArgs args)
    {
        var item = CheckInventoryItem(args.ItemId);
        if (item != null)
            args.AddCustomItem(item);
    }

    private InventoryContextMenuItem? CheckInventoryItem(uint itemId)
    {
        if (itemId > 500000)
            itemId -= 500000;

        if (!_items.ItemService.AwaitedService.TryGetValue(itemId, EquipSlot.MainHand, out var item))
            return null;

        return new InventoryContextMenuItem(TryOnString, GetInventoryAction(item));
    }


    private GameObjectContextMenuItem? CheckGameObjectItem(uint itemId)
    {
        if (itemId > 500000)
            itemId -= 500000;

        if (!_items.ItemService.AwaitedService.TryGetValue(itemId, EquipSlot.MainHand, out var item))
            return null;

        return new GameObjectContextMenuItem(TryOnString, GetGameObjectAction(item));
    }

    private unsafe GameObjectContextMenuItem? CheckGameObjectItem(IntPtr agent, int offset, Func<nint, bool> validate)
        => agent != IntPtr.Zero && validate(agent) ? CheckGameObjectItem(*(uint*)(agent + offset)) : null;

    private unsafe GameObjectContextMenuItem? CheckGameObjectItem(IntPtr agent, int offset)
        => agent != IntPtr.Zero ? CheckGameObjectItem(*(uint*)(agent + offset)) : null;

    private GameObjectContextMenuItem? CheckGameObjectItem(string name, int offset, Func<nint, bool> validate)
        => CheckGameObjectItem(_gameGui.FindAgentInterface(name), offset, validate);

    private void AddGameObjectItem(GameObjectContextMenuOpenArgs args)
    {
        var item = args.ParentAddonName switch
        {
            "ItemSearch" => CheckGameObjectItem(args.Agent, ItemSearchContextItemId),
            "ChatLog"    => CheckGameObjectItem("ChatLog",  ChatLogContextItemId, ValidateChatLogContext),
            _            => null,
        };
        if (item != null)
            args.AddCustomItem(item);
    }

    private DalamudContextMenu.InventoryContextMenuItemSelectedDelegate GetInventoryAction(EquipItem item)
    {
        return _ =>
        {
            var (id, playerData) = _objects.PlayerData;
            if (!playerData.Valid)
                return;

            if (!_state.GetOrCreate(id, playerData.Objects[0], out var state))
                return;

            var slot = item.Type.ToSlot();
            _state.ChangeEquip(state, slot, item, 0, false, StateChanged.Source.Manual);
            if (item.Type.ValidOffhand().IsOffhandType())
            {
                if (item.ModelId.Id is > 1600 and < 1651
                 && _items.ItemService.AwaitedService.TryGetValue(item.ItemId, EquipSlot.Hands, out var gauntlets))
                    _state.ChangeEquip(state, EquipSlot.Hands, gauntlets, 0, false, StateChanged.Source.Manual);
                if (_items.ItemService.AwaitedService.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand))
                    _state.ChangeEquip(state, EquipSlot.OffHand, offhand, 0, false, StateChanged.Source.Manual);
            }
        };
    }

    private DalamudContextMenu.GameObjectContextMenuItemSelectedDelegate GetGameObjectAction(EquipItem item)
    {
        return _ =>
        {
            var (id, playerData) = _objects.PlayerData;
            if (!playerData.Valid)
                return;

            if (!_state.GetOrCreate(id, playerData.Objects[0], out var state))
                return;

            var slot = item.Type.ToSlot();
            _state.ChangeEquip(state, slot, item, 0, false, StateChanged.Source.Manual);
            if (item.Type.ValidOffhand().IsOffhandType())
            {
                if (item.ModelId.Id is > 1600 and < 1651
                 && _items.ItemService.AwaitedService.TryGetValue(item.ItemId, EquipSlot.Hands, out var gauntlets))
                    _state.ChangeEquip(state, EquipSlot.Hands, gauntlets, 0, false, StateChanged.Source.Manual);
                if (_items.ItemService.AwaitedService.TryGetValue(item.ItemId, EquipSlot.OffHand, out var offhand))
                    _state.ChangeEquip(state, EquipSlot.OffHand, offhand, 0, false, StateChanged.Source.Manual);
            }
        };
    }

    private static unsafe bool ValidateChatLogContext(nint agent)
        => *(uint*)(agent + ChatLogContextItemId + 8) == 3;
}
