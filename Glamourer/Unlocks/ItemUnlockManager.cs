using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.Events;
using Glamourer.Services;
using Lumina.Excel.GeneratedSheets;
using Cabinet = Lumina.Excel.GeneratedSheets.Cabinet;

namespace Glamourer.Unlocks;

public class ItemUnlockManager : ISavable, IDisposable
{
    private readonly SaveService    _saveService;
    private readonly ItemManager    _items;
    private readonly ClientState    _clientState;
    private readonly Framework      _framework;
    private readonly ObjectUnlocked _event;

    private readonly Dictionary<uint, long> _unlocked = new();

    private bool _lastArmoireState;
    private bool _lastAchievementState;
    private bool _lastGlamourState;
    private bool _lastPlateState;
    private byte _currentInventory;
    private byte _currentInventoryIndex;

    [Flags]
    public enum UnlockType : byte
    {
        Quest1      = 0x01,
        Quest2      = 0x02,
        Achievement = 0x04,
        Cabinet     = 0x08,
    }

    public readonly IReadOnlyDictionary<uint, UnlockRequirements> Unlockable;

    public IReadOnlyDictionary<uint, long> Unlocked
        => _unlocked;

    public ItemUnlockManager(SaveService saveService, ItemManager items, ClientState clientState, DataManager gameData, Framework framework,
        ObjectUnlocked @event)
    {
        SignatureHelper.Initialise(this);
        _saveService = saveService;
        _items       = items;
        _clientState = clientState;
        _framework   = framework;
        _event       = @event;
        Unlockable   = CreateUnlockData(gameData, items);
        Load();
        _clientState.Login += OnLogin;
        _framework.Update  += OnFramework;
        Scan();
    }

    //private Achievement.AchievementState _achievementState = Achievement.AchievementState.Invalid;

    private static readonly InventoryType[] ScannableInventories =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.EquippedItems,
        InventoryType.Mail,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.ArmoryMainHand,
        InventoryType.SaddleBag1,
        InventoryType.SaddleBag2,
        InventoryType.PremiumSaddleBag1,
        InventoryType.PremiumSaddleBag2,
        InventoryType.RetainerPage1,
        InventoryType.RetainerPage2,
        InventoryType.RetainerPage3,
        InventoryType.RetainerPage4,
        InventoryType.RetainerPage5,
        InventoryType.RetainerPage6,
        InventoryType.RetainerPage7,
        InventoryType.RetainerEquippedItems,
        InventoryType.RetainerMarket,
    };

    private unsafe void OnFramework(Framework _)
    {
        var uiState = UIState.Instance();
        if (uiState == null)
            return;

        var scan            = false;
        var newArmoireState = uiState->Cabinet.IsCabinetLoaded();
        if (newArmoireState != _lastArmoireState)
        {
            _lastArmoireState =  newArmoireState;
            scan              |= newArmoireState;
        }

        var newAchievementState = uiState->Achievement.IsLoaded();
        if (newAchievementState != _lastAchievementState)
        {
            _lastAchievementState =  newAchievementState;
            scan                  |= newAchievementState;
        }

        if (scan)
            Scan();

        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bool AddItem(uint itemId)
        {
            if (!_items.ItemService.AwaitedService.TryGetValue(itemId, out var equip) || !_unlocked.TryAdd(equip.Id, time))
                return false;

            _event.Invoke(ObjectUnlocked.Type.Item, equip.Id, DateTimeOffset.FromUnixTimeMilliseconds(time));
            return true;
        }

        var mirageManager = MirageManager.Instance();
        var changes       = false;
        if (mirageManager != null)
        {
            var newGlamourState = mirageManager->PrismBoxLoaded;
            if (newGlamourState != _lastGlamourState)
            {
                _lastGlamourState = newGlamourState;
                // TODO: Make independent from hardcoded value
                var span = new ReadOnlySpan<uint>(mirageManager->PrismBoxItemIds, 800);
                foreach (var item in span)
                    changes |= AddItem(item);
            }

            var newPlateState = mirageManager->GlamourPlatesLoaded;
            if (newPlateState != _lastPlateState)
            {
                _lastPlateState = newPlateState;
                foreach (var plate in mirageManager->GlamourPlatesSpan)
                {
                    // TODO: Make independent from hardcoded value
                    var span = new ReadOnlySpan<uint>(plate.ItemIds, 12);
                    foreach (var item in span)
                        changes |= AddItem(item);
                }
            }
        }

        changes = false;
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager != null)
        {
            var type      = ScannableInventories[_currentInventory];
            var container = inventoryManager->GetInventoryContainer(type);
            if (container != null && container->Loaded != 0 && _currentInventoryIndex < container->Size)
            {
                var item = container->GetInventorySlot(_currentInventoryIndex++);
                if (item != null)
                {
                    changes |= AddItem(item->ItemID);
                    changes |= AddItem(item->GlamourID);
                }
            }
            else
            {
                _currentInventory      = (byte)(_currentInventory + 1 == ScannableInventories.Length ? 0 : _currentInventory + 1);
                _currentInventoryIndex = 0;
            }
        }

        if (changes)
            Save();
    }

    public bool IsUnlocked(uint itemId, out DateTimeOffset time)
    {
        // Pseudo items are always unlocked.
        if (itemId >= _items.ItemSheet.RowCount)
        {
            time = DateTimeOffset.MinValue;
            return true;
        }

        if (_unlocked.TryGetValue(itemId, out var t))
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(t);
            return true;
        }

        if (IsGameUnlocked(itemId))
        {
            time = DateTimeOffset.UtcNow;
            if (_unlocked.TryAdd(itemId, time.ToUnixTimeMilliseconds()))
            {
                _event.Invoke(ObjectUnlocked.Type.Item, itemId, time);
                Save();
            }

            return true;
        }

        time = DateTimeOffset.MaxValue;
        return false;
    }

    public unsafe bool IsGameUnlocked(uint itemId)
    {
        if (Unlockable.TryGetValue(itemId, out var req))
            return req.IsUnlocked(this);

        // TODO inventory
        return false;
    }

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
        _framework.Update  -= OnFramework;
    }

    public void Scan()
    {
        var time    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var changes = false;
        foreach (var (itemId, unlock) in Unlockable)
        {
            if (unlock.IsUnlocked(this) && _unlocked.TryAdd(itemId, time))
            {
                _event.Invoke(ObjectUnlocked.Type.Item, itemId, DateTimeOffset.FromUnixTimeMilliseconds(time));
                changes = true;
            }
        }

        // TODO inventories

        if (changes)
            Save();
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.UnlockFileItems;

    public void Save()
        => _saveService.DelaySave(this, TimeSpan.FromSeconds(10));

    public void Save(StreamWriter writer)
        => UnlockDictionaryHelpers.Save(writer, Unlocked);

    private void Load()
        => UnlockDictionaryHelpers.Load(ToFilename(_saveService.FileNames), _unlocked,
            id => _items.ItemService.AwaitedService.TryGetValue(id, out _), "item");

    private void OnLogin(object? _, EventArgs _2)
        => Scan();

    private static Dictionary<uint, UnlockRequirements> CreateUnlockData(DataManager gameData, ItemManager items)
    {
        var ret     = new Dictionary<uint, UnlockRequirements>();
        var cabinet = gameData.GetExcelSheet<Cabinet>()!;
        foreach (var row in cabinet)
        {
            if (items.ItemService.AwaitedService.TryGetValue(row.Item.Row, out var item))
                ret.TryAdd(item.Id, new UnlockRequirements(row.RowId, 0, 0, 0, UnlockType.Cabinet));
        }

        var gilShop = gameData.GetExcelSheet<GilShopItem>()!;
        foreach (var row in gilShop)
        {
            if (!items.ItemService.AwaitedService.TryGetValue(row.Item.Row, out var item))
                continue;

            var quest1      = row.QuestRequired[0].Row;
            var quest2      = row.QuestRequired[1].Row;
            var achievement = row.AchievementRequired.Row;
            var state       = row.StateRequired;
            var type = (quest1 != 0 ? UnlockType.Quest1 : 0)
              | (quest2 != 0 ? UnlockType.Quest2 : 0)
              | (achievement != 0 ? UnlockType.Achievement : 0);
            ret.TryAdd(item.Id, new UnlockRequirements(quest1, quest2, achievement, state, type));
        }

        return ret;
    }
}
