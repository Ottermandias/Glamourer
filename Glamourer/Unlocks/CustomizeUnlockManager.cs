using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.Customization;
using Glamourer.Services;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.Unlocks;

public class CustomizeUnlockManager : IDisposable, ISavable
{
    private readonly SaveService            _saveService;
    private readonly ClientState            _clientState;
    private readonly Dictionary<uint, long> _unlocked = new();

    public readonly IReadOnlyDictionary<CustomizeData, (uint Data, string Name)> Unlockable;

    public IReadOnlyDictionary<uint, long> Unlocked
        => _unlocked;

    public unsafe CustomizeUnlockManager(SaveService saveService, CustomizationService customizations, DataManager gameData,
        ClientState clientState)
    {
        SignatureHelper.Initialise(this);
        _saveService = saveService;
        _clientState = clientState;
        Unlockable   = CreateUnlockableCustomizations(customizations, gameData);
        Load();
        _setUnlockLinkValueHook.Enable();
        _clientState.Login += OnLogin;
        Scan();
    }

    public void Dispose()
    {
        _setUnlockLinkValueHook.Dispose();
        _clientState.Login -= OnLogin;
    }

    /// <summary> Check if a customization is unlocked for Glamourer. </summary>
    public bool IsUnlocked(CustomizeData data, out DateTimeOffset time)
    {
        if (!Unlockable.TryGetValue(data, out var pair))
        {
            time = DateTime.MaxValue;
            return true;
        }

        if (_unlocked.TryGetValue(pair.Data, out var t))
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(t);
            return true;
        }

        if (!IsUnlockedGame(pair.Data))
        {
            time = DateTimeOffset.MinValue;
            return false;
        }

        _unlocked.TryAdd(pair.Data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Save();
        time = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary> Check if a customization is currently unlocked for the game state. </summary>
    public unsafe bool IsUnlockedGame(uint dataId)
    {
        var instance = UIState.Instance();
        if (instance == null)
            return false;

        return UIState.Instance()->IsUnlockLinkUnlocked(dataId);
    }

    /// <summary> Scan and update all unlockable customizations for their current game state. </summary>
    public unsafe void Scan()
    {
        if (_clientState.LocalPlayer == null)
            return;

        Glamourer.Log.Debug("[UnlockManager] Scanning for new unlocked customizations.");
        var instance = UIState.Instance();
        if (instance == null)
            return;

        try
        {
            var count = 0;
            var time  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var (_, (id, _)) in Unlockable)
            {
                if (instance->IsUnlockLinkUnlocked(id) && _unlocked.TryAdd(id, time))
                    ++count;
            }

            if (count <= 0)
                return;

            Save();
            Glamourer.Log.Debug($"[UnlockManager] Found {count} new unlocked customizations..");
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"[UnlockManager] Error scanning for newly unlocked customizations:\n{ex}");
        }
    }

    private delegate void SetUnlockLinkValueDelegate(nint uiState, uint data, byte value);

    [Signature("48 83 EC ?? 8B C2 44 8B D2", DetourName = nameof(SetUnlockLinkValueDetour))]
    private readonly Hook<SetUnlockLinkValueDelegate> _setUnlockLinkValueHook = null!;

    private void SetUnlockLinkValueDetour(nint uiState, uint data, byte value)
    {
        _setUnlockLinkValueHook.Original(uiState, data, value);
        try
        {
            if (value == 0)
                return;

            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var (_, (id, _)) in Unlockable)
            {
                if (id != data || !_unlocked.TryAdd(id, time))
                    continue;

                Save();
                break;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"[UnlockManager] Error in SetUnlockLinkValue Hook:\n{ex}");
        }
    }

    private void OnLogin(object? _, EventArgs _2)
        => Scan();

    public string ToFilename(FilenameService fileNames)
        => fileNames.UnlockFileCustomize;

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

    /// <summary> Create a list of all unlockable hairstyles and facepaints. </summary>
    private static Dictionary<CustomizeData, (uint Data, string Name)> CreateUnlockableCustomizations(CustomizationService customizations,
        DataManager gameData)
    {
        var ret   = new Dictionary<CustomizeData, (uint Data, string Name)>();
        var sheet = gameData.GetExcelSheet<CharaMakeCustomize>(ClientLanguage.English)!;
        foreach (var clan in customizations.AwaitedService.Clans)
        {
            foreach (var gender in customizations.AwaitedService.Genders)
            {
                var list = customizations.AwaitedService.GetList(clan, gender);
                foreach (var hair in list.HairStyles)
                {
                    var x = sheet.FirstOrDefault(f => f.FeatureID == hair.Value.Value);
                    if (x?.IsPurchasable == true)
                    {
                        var name = x.FeatureID == 61
                            ? "Eternal Bond"
                            : x.HintItem.Value?.Name.ToDalamudString().ToString().Replace("Modern Aesthetics - ", string.Empty)
                         ?? string.Empty;
                        ret.TryAdd(hair, (x.Data, name));
                    }
                }

                foreach (var paint in list.FacePaints)
                {
                    var x = sheet.FirstOrDefault(f => f.FeatureID == paint.Value.Value);
                    if (x?.IsPurchasable == true)
                    {
                        var name = x.HintItem.Value?.Name.ToDalamudString().ToString().Replace("Modern Cosmetics - ", string.Empty)
                         ?? string.Empty;
                        ret.TryAdd(paint, (x.Data, name));
                    }
                }
            }
        }

        return ret;
    }
}
