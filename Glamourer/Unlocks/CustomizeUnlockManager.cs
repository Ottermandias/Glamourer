using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Glamourer.GameData;
using Glamourer.Events;
using Glamourer.Interop;
using Glamourer.Services;
using Lumina.Excel.Sheets;
using Penumbra.GameData;
using Penumbra.GameData.Enums;

namespace Glamourer.Unlocks;

public class CustomizeUnlockManager : IDisposable, ISavable
{
    private readonly SaveService            _saveService;
    private readonly IClientState           _clientState;
    private readonly ObjectUnlocked         _event;
    private readonly ObjectManager          _objects;
    private readonly Dictionary<uint, long> _unlocked = new();

    public readonly IReadOnlyDictionary<CustomizeData, (uint Data, string Name)> Unlockable;

    public IReadOnlyDictionary<uint, long> Unlocked
        => _unlocked;

    public CustomizeUnlockManager(SaveService saveService, CustomizeService customizations, IDataManager gameData,
        IClientState clientState, ObjectUnlocked @event, IGameInteropProvider interop, ObjectManager objects)
    {
        interop.InitializeFromAttributes(this);
        _saveService = saveService;
        _clientState = clientState;
        _event       = @event;
        _objects     = objects;
        Unlockable   = CreateUnlockableCustomizations(customizations, gameData);
        Load();
        _setUnlockLinkValueHook.Enable();
        _clientState.Login += Scan;
        Scan();
    }

    public void Dispose()
    {
        _setUnlockLinkValueHook.Dispose();
        _clientState.Login -= Scan;
    }

    /// <summary> Check if a customization is unlocked for Glamourer. </summary>
    public bool IsUnlocked(CustomizeData data, out DateTimeOffset time)
    {
        // All other customizations are not unlockable.
        if (data.Index is not CustomizeIndex.Hairstyle and not CustomizeIndex.FacePaint)
        {
            time = DateTimeOffset.MinValue;
            return true;
        }

        if (!Unlockable.TryGetValue(data, out var pair))
        {
            time = DateTimeOffset.MinValue;
            return true;
        }

        if (_unlocked.TryGetValue(pair.Data, out var t))
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(t);
            return true;
        }

        if (!IsUnlockedGame(pair.Data))
        {
            time = DateTimeOffset.MaxValue;
            return false;
        }

        _unlocked.TryAdd(pair.Data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        time = DateTimeOffset.UtcNow;
        _event.Invoke(ObjectUnlocked.Type.Customization, pair.Data, time);
        Save();
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
        if (!_objects.Player.Valid)
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
                {
                    _event.Invoke(ObjectUnlocked.Type.Customization, id, DateTimeOffset.FromUnixTimeMilliseconds(time));
                    ++count;
                }
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

    [Signature(Sigs.SetUnlockLinkValue, DetourName = nameof(SetUnlockLinkValueDetour))]
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

                _event.Invoke(ObjectUnlocked.Type.Customization, id, DateTimeOffset.FromUnixTimeMilliseconds(time));
                Save();
                break;
            }
        }
        catch (Exception ex)
        {
            Glamourer.Log.Error($"[UnlockManager] Error in SetUnlockLinkValue Hook:\n{ex}");
        }
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.UnlockFileCustomize;

    public void Save()
        => _saveService.QueueSave(this);

    public void Save(StreamWriter writer)
        => UnlockDictionaryHelpers.Save(writer, Unlocked);

    private void Load()
        => UnlockDictionaryHelpers.Load(ToFilename(_saveService.FileNames), _unlocked, id => Unlockable.Any(c => c.Value.Data == id),
            "customization");

    /// <summary> Create a list of all unlockable hairstyles and face paints. </summary>
    private static Dictionary<CustomizeData, (uint Data, string Name)> CreateUnlockableCustomizations(CustomizeService customizations,
        IDataManager gameData)
    {
        var ret   = new Dictionary<CustomizeData, (uint Data, string Name)>();
        var sheet = gameData.GetExcelSheet<CharaMakeCustomize>(ClientLanguage.English)!;
        foreach (var (clan, gender) in CustomizeManager.AllSets())
        {
            var list = customizations.Manager.GetSet(clan, gender);
            foreach (var hair in list.HairStyles)
            {
                var x = sheet.FirstOrNull(f => f.FeatureID == hair.Value.Value);
                if (x?.IsPurchasable == true)
                {
                    var name = x.Value.FeatureID == 61
                        ? "Eternal Bond"
                        : x.Value.HintItem.ValueNullable?.Name.ExtractText().Replace("Modern Aesthetics - ", string.Empty)
                     ?? string.Empty;
                    ret.TryAdd(hair, (x.Value.Data, name));
                }
            }

            foreach (var paint in list.FacePaints)
            {
                var x = sheet.FirstOrNull(f => f.FeatureID == paint.Value.Value);
                if (x?.IsPurchasable == true)
                {
                    var name = x.Value.HintItem.ValueNullable?.Name.ExtractText().Replace("Modern Cosmetics - ", string.Empty)
                     ?? string.Empty;
                    ret.TryAdd(paint, (x.Value.Data, name));
                }
            }
        }

        return ret;
    }
}
