using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Glamourer.Designs;
using Glamourer.Services;
using Glamourer.State;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public class ContextMenuService : IDisposable
{
    public const int ChatLogContextItemId = 0x958;

    private readonly ItemManager        _items;
    private readonly IContextMenu       _contextMenu;
    private readonly StateManager       _state;
    private readonly ActorObjectManager _objects;
    private          EquipItem          _lastItem;
    private readonly StainId[]          _lastStains = new StainId[StainId.NumStains];

    private readonly MenuItem _inventoryItem;

    public ContextMenuService(ItemManager items, StateManager state, ActorObjectManager objects, Configuration config,
        IContextMenu context)
    {
        _contextMenu = context;
        _items       = items;
        _state       = state;
        _objects     = objects;
        if (config.EnableGameContextMenu)
            Enable();

        _inventoryItem = new MenuItem
        {
            IsEnabled   = true,
            IsReturn    = false,
            PrefixChar  = 'G',
            Name        = "Try On",
            OnClicked   = OnClick,
            IsSubmenu   = false,
            PrefixColor = 541,
        };
    }

    private unsafe void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.MenuType is ContextMenuType.Inventory)
        {
            var arg = (MenuTargetInventory)args.Target;
            if (arg.TargetItem.HasValue && HandleItem(arg.TargetItem.Value.ItemId))
            {
                for (var i = 0; i < arg.TargetItem.Value.Stains.Length; ++i)
                    _lastStains[i] = arg.TargetItem.Value.Stains[i];
                args.AddMenuItem(_inventoryItem);
            }
        }
        else
        {
            switch (args.AddonName)
            {
                case "ItemSearch" when args.AgentPtr != nint.Zero:
                {
                    if (HandleItem((ItemId)AgentContext.Instance()->UpdateCheckerParam))
                        args.AddMenuItem(_inventoryItem);

                    break;
                }
                case "ChatLog":
                {
                    var agent = AgentChatLog.Instance();
                    if (agent == null || !ValidateChatLogContext(agent))
                        return;

                    if (HandleItem(*(ItemId*)(agent + ChatLogContextItemId)))
                    {
                        for (var i = 0; i < _lastStains.Length; ++i)
                            _lastStains[i] = 0;
                        args.AddMenuItem(_inventoryItem);
                    }

                    break;
                }
                case "RecipeNote":
                {
                    var agent = AgentRecipeNote.Instance();
                    if (agent == null)
                        return;

                    if (HandleItem(agent->ContextMenuResultItemId))
                    {
                        for (var i = 0; i < _lastStains.Length; ++i)
                            _lastStains[i] = 0;
                        args.AddMenuItem(_inventoryItem);
                    }

                    break;
                }
                case "InclusionShop":
                {
                    var agent = AgentRecipeItemContext.Instance();
                    if (agent == null)
                        return;

                    if (HandleItem(agent->ResultItemId))
                    {
                        for (var i = 0; i < _lastStains.Length; ++i)
                            _lastStains[i] = 0;
                        args.AddMenuItem(_inventoryItem);
                    }

                    break;
                }
            }
        }
    }

    public void Enable()
        => _contextMenu.OnMenuOpened += OnMenuOpened;

    public void Disable()
        => _contextMenu.OnMenuOpened -= OnMenuOpened;

    public void Dispose()
        => Disable();

    private void OnClick(IMenuItemClickedArgs _)
    {
        var (id, playerData) = _objects.PlayerData;
        if (!playerData.Valid)
            return;

        if (!_state.GetOrCreate(id, playerData.Objects[0], out var state))
            return;

        var slot = _lastItem.Type.ToSlot();
        _state.ChangeEquip(state, slot, _lastItem, _lastStains[0], ApplySettings.Manual);
        if (!_lastItem.Type.ValidOffhand().IsOffhandType())
            return;

        if (_lastItem.PrimaryId.Id is > 1600 and < 1651
         && _items.ItemData.TryGetValue(_lastItem.ItemId, EquipSlot.Hands, out var gauntlets))
            _state.ChangeEquip(state, EquipSlot.Hands, gauntlets, _lastStains[0], ApplySettings.Manual);
        if (_items.ItemData.TryGetValue(_lastItem.ItemId, EquipSlot.OffHand, out var offhand))
            _state.ChangeEquip(state, EquipSlot.OffHand, offhand, _lastStains[0], ApplySettings.Manual);
    }

    private bool HandleItem(ItemId id)
    {
        var itemId = id.StripModifiers;
        return _items.ItemData.TryGetValue(itemId, EquipSlot.MainHand, out _lastItem);
    }

    private static unsafe bool ValidateChatLogContext(AgentChatLog* agent)
        => *(&agent->ContextItemId + 8) == 3;
}
