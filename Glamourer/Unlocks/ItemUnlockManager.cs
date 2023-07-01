using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.Services;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using Cabinet = Lumina.Excel.GeneratedSheets.Cabinet;

namespace Glamourer.Unlocks;

public class ItemUnlockManager : ISavable, IDisposable
{
    private readonly SaveService            _saveService;
    private readonly ItemManager            _items;
    private readonly ClientState            _clientState;
    private readonly Framework              _framework;
    private readonly Dictionary<uint, long> _unlocked = new();


    public enum UnlockType : byte
    {
        Cabinet,
        Quest,
        Achievement,
    }

    public readonly IReadOnlyDictionary<uint, (uint, UnlockType)> _unlockable;

    public IReadOnlyDictionary<uint, long> Unlocked
        => _unlocked;

    public ItemUnlockManager(SaveService saveService, ItemManager items, ClientState clientState, DataManager gameData, Framework framework)
    {
        SignatureHelper.Initialise(this);
        _saveService = saveService;
        _items       = items;
        _clientState = clientState;
        _framework   = framework;
        _unlockable  = CreateUnlockData(gameData, items);
        Load();
        _clientState.Login += OnLogin;
        _framework.Update  += OnFramework;
        Scan();
    }

    //private Achievement.AchievementState _achievementState = Achievement.AchievementState.Invalid;

    private unsafe void OnFramework(Framework _)
    {
        //var achievement = Achievement.Instance();
        var uiState     = UIState.Instance();
    }

    public bool IsUnlocked(uint itemId)
    {
        // Pseudo items are always unlocked.
        if (itemId >= _items.ItemSheet.RowCount)
            return true;

        if (_unlocked.ContainsKey(itemId))
            return true;

        // TODO
        return false;
    }

    public unsafe bool IsGameUnlocked(uint id, UnlockType type)
    {
        var uiState = UIState.Instance();
        if (uiState == null)
            return false;

        return type switch
        {
            UnlockType.Cabinet     => uiState->Cabinet.IsCabinetLoaded() && uiState->Cabinet.IsItemInCabinet((int)id),
            UnlockType.Quest       => uiState->IsUnlockLinkUnlockedOrQuestCompleted(id),
            UnlockType.Achievement => false,
            _                      => false,
        };
    }

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
        _framework.Update  -= OnFramework;
    }

    public void Scan()
    {
        // TODO
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.UnlockFileItems;

    public void Save()
        => _saveService.QueueSave(this);

    public void Save(StreamWriter writer)
    { }

    private void Load()
    {
        var file = ToFilename(_saveService.FileNames);
        if (!File.Exists(file))
            return;

        _unlocked.Clear();
    }

    private void OnLogin(object? _, EventArgs _2)
        => Scan();

    private static Dictionary<uint, (uint, UnlockType)> CreateUnlockData(DataManager gameData, ItemManager items)
    {
        var ret     = new Dictionary<uint, (uint, UnlockType)>();
        var cabinet = gameData.GetExcelSheet<Cabinet>()!;
        foreach (var row in cabinet)
        {
            if (items.ItemService.AwaitedService.TryGetValue(row.Item.Row, out var item))
                ret.TryAdd(item.Id, (row.RowId, UnlockType.Cabinet));
        }

        var gilShop = gameData.GetExcelSheet<GilShopItem>()!;
        // TODO

        return ret;
    }
}
