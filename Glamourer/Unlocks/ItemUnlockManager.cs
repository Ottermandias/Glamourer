using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.Events;
using Glamourer.Services;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Cabinet = Lumina.Excel.Sheets.Cabinet;

namespace Glamourer.Unlocks;

public class ItemUnlockManager : ISavable, IDisposable, IReadOnlyDictionary<ItemId, long>
{
    private readonly SaveService       _saveService;
    private readonly ItemManager       _items;
    private readonly IClientState       _clientState;
    private readonly IFramework         _framework;
    private readonly ObjectUnlocked    _event;
    private readonly ObjectIdentification _identifier;

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

    public readonly IReadOnlyDictionary<ItemId, UnlockRequirements> Unlockable;

    public ItemUnlockManager(SaveService saveService, ItemManager items, IClientState clientState, IDataManager gameData, IFramework framework,
        ObjectUnlocked @event, ObjectIdentification identifier, IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);
        _saveService = saveService;
        _items       = items;
        _clientState = clientState;
        _framework   = framework;
        _event       = @event;
        _identifier  = identifier;
        Unlockable   = CreateUnlockData(gameData, items);
        Load();
        _clientState.Login += Scan;
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

    private bool AddItem(ItemId itemId, long time)
    {
        itemId = itemId.StripModifiers;
        if (!_items.ItemData.TryGetValue(itemId, EquipSlot.MainHand, out var equip)
         || !_unlocked.TryAdd(equip.ItemId.Id, time))
            return false;

        _event.Invoke(ObjectUnlocked.Type.Item, equip.ItemId.Id, DateTimeOffset.FromUnixTimeMilliseconds(time));
        var ident = _identifier.Identify(equip.PrimaryId, equip.SecondaryId, equip.Variant, equip.Type.ToSlot());
        foreach (var item in ident)
        {
            if (_unlocked.TryAdd(item.ItemId.Id, time))
                _event.Invoke(ObjectUnlocked.Type.Item, item.ItemId.Id, DateTimeOffset.FromUnixTimeMilliseconds(time));
        }

        return true;
    }

    private unsafe void OnFramework(IFramework _)
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

        var time          = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var mirageManager = MirageManager.Instance();
        var changes       = false;
        if (mirageManager != null)
        {
            var newGlamourState = mirageManager->PrismBoxLoaded;
            if (newGlamourState != _lastGlamourState)
            {
                _lastGlamourState = newGlamourState;
                var span = mirageManager->PrismBoxItemIds;
                foreach (var item in span)
                    changes |= AddItem(item, time);
            }

            var newPlateState = mirageManager->GlamourPlatesLoaded;
            if (newPlateState != _lastPlateState)
            {
                _lastPlateState = newPlateState;
                foreach (var plate in mirageManager->GlamourPlates)
                {
                    var span = plate.ItemIds;
                    foreach (var item in span)
                        changes |= AddItem(item, time);
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
                Glamourer.Log.Excessive($"[UnlockScanner] Scanning {_currentInventory} {type} {_currentInventoryIndex}/{container->Size}.");
                var item = container->GetInventorySlot(_currentInventoryIndex++);
                if (item != null)
                {
                    changes |= AddItem(item->ItemId,    time);
                    changes |= AddItem(item->GlamourId, time);
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

    public bool IsUnlocked(CustomItemId itemId, out DateTimeOffset time)
    {
        // Pseudo items are always unlocked.
        if (itemId.Id >= (uint) _items.ItemSheet.Count)
        {
            time = DateTimeOffset.MinValue;
            return true;
        }

        var id = itemId.Item.Id;
        if (_unlocked.TryGetValue(id, out var t))
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(t);
            return true;
        }

        if (IsGameUnlocked(id))
        {
            time = DateTimeOffset.UtcNow;
            if (_unlocked.TryAdd(id, time.ToUnixTimeMilliseconds()))
            {
                _event.Invoke(ObjectUnlocked.Type.Item, id, time);
                Save();
            }

            return true;
        }

        time = DateTimeOffset.MaxValue;
        return false;
    }

    public unsafe bool IsGameUnlocked(ItemId itemId)
    {
        if (Unlockable.TryGetValue(itemId, out var req))
            return req.IsUnlocked(this);

        // TODO inventory
        return false;
    }

    public void Dispose()
    {
        _clientState.Login -= Scan;
        _framework.Update  -= OnFramework;
    }

    public void Scan()
    {
        var time    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var changes = false;
        foreach (var (itemId, unlock) in Unlockable)
        {
            if (unlock.IsUnlocked(this) && _unlocked.TryAdd(itemId.Id, time))
            {
                _event.Invoke(ObjectUnlocked.Type.Item, itemId.Id, DateTimeOffset.FromUnixTimeMilliseconds(time));
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
        => UnlockDictionaryHelpers.Save(writer, _unlocked);

    private void Load()
    {
        var version = UnlockDictionaryHelpers.Load(ToFilename(_saveService.FileNames), _unlocked,
            id => _items.ItemData.TryGetValue(id, EquipSlot.MainHand, out _), "item");
        UpdateModels(version);
    }

    private static Dictionary<ItemId, UnlockRequirements> CreateUnlockData(IDataManager gameData, ItemManager items)
    {
        var ret     = new Dictionary<ItemId, UnlockRequirements>();
        var cabinet = gameData.GetExcelSheet<Cabinet>();
        foreach (var row in cabinet)
        {
            if (items.ItemData.TryGetValue(row.Item.RowId, EquipSlot.MainHand, out var item))
                ret.TryAdd(item.ItemId, new UnlockRequirements(row.RowId, 0, 0, 0, UnlockType.Cabinet));
        }

        var gilShopItem = gameData.GetSubrowExcelSheet<GilShopItem>();
        var gilShop     = gameData.GetExcelSheet<GilShop>();
        foreach (var row in gilShopItem.SelectMany(g => g))
        {
            if (!items.ItemData.TryGetValue(row.Item.RowId, EquipSlot.MainHand, out var item))
                continue;

            var quest1      = row.QuestRequired[0].RowId;
            var quest2      = row.QuestRequired[1].RowId;
            var achievement = row.AchievementRequired.RowId;
            var state       = row.StateRequired;

            if (gilShop.TryGetRow(row.RowId, out var shop) && shop.Quest.RowId != 0)
            {
                if (quest1 == 0)
                    quest1 = shop.Quest.RowId;
                else if (quest2 == 0)
                    quest2 = shop.Quest.RowId;
            }

            var type = (quest1 != 0 ? UnlockType.Quest1 : 0)
              | (quest2 != 0 ? UnlockType.Quest2 : 0)
              | (achievement != 0 ? UnlockType.Achievement : 0);
            ret.TryAdd(item.ItemId, new UnlockRequirements(quest1, quest2, achievement, state, type));
        }

        return ret;
    }

    private void UpdateModels(int version)
    {
        if (version > 1)
            return;

        foreach (var (item, time) in _unlocked.ToArray())
        {
            if (!_items.ItemData.TryGetValue(item, EquipSlot.MainHand, out var equip))
                continue;

            var ident = _identifier.Identify(equip.PrimaryId, equip.SecondaryId, equip.Variant, equip.Type.ToSlot());
            foreach (var item2 in ident)
            {
                if (_unlocked.TryAdd(item2.ItemId.Id, time))
                    _event.Invoke(ObjectUnlocked.Type.Item, item2.ItemId.Id, DateTimeOffset.FromUnixTimeMilliseconds(time));
            }
        }
    }

    public IEnumerator<KeyValuePair<ItemId, long>> GetEnumerator()
        => _unlocked.Select(kvp => new KeyValuePair<ItemId, long>(kvp.Key, kvp.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _unlocked.Count;

    public bool ContainsKey(ItemId key)
        => _unlocked.ContainsKey(key.Id);

    public bool TryGetValue(ItemId key, out long value)
        => _unlocked.TryGetValue(key.Id, out value);

    public long this[ItemId key]
        => _unlocked[key.Id];

    public IEnumerable<ItemId> Keys
        => _unlocked.Keys.Select(i => (ItemId)i);

    public IEnumerable<long> Values
        => _unlocked.Values;
}
