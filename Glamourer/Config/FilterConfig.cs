using Glamourer.Gui.Tabs.UnlocksTab;
using Glamourer.Services;
using ImSharp;
using Luna;
using Luna.Generators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Config;

public sealed partial class FilterConfig : ConfigurationFile<FilenameService>
{
    private readonly DictJob _jobs;

    public FilterConfig(SaveService saveService, MessageService messager, DictJob jobs)
        : base(saveService, messager, TimeSpan.FromMinutes(5))
    {
        _jobs             = jobs;
        _unlocksJobFilter = _jobs.AllAvailableJobs;
        Load();
    }

    public override int CurrentVersion
        => 1;

    protected override void AddData(JsonTextWriter j)
    {
        WriteFilter(j, ActorFilter,  "Actors");
        WriteFilter(j, DesignFilter, "Designs");
        WriteFilter(j, NpcFilter,    "Npcs");
        WriteAutomation(j);
        WriteUnlocksTab(j);
    }

    protected override void LoadData(JObject j)
    {
        _actorFilter           = j["Actors"]?["Filter"]?.Value<string>() ?? string.Empty;
        _designFilter          = j["Designs"]?["Filter"]?.Value<string>() ?? string.Empty;
        _npcFilter             = j["Npcs"]?["Filter"]?.Value<string>() ?? string.Empty;
        _automationFilter      = j["Automation"]?["Filter"]?.Value<string>() ?? string.Empty;
        _automationStateFilter = j["Automation"]?["State"]?.Value<bool>();
        LoadUnlocksTab(j);
    }

    [ConfigProperty]
    private string _designFilter = string.Empty;

    [ConfigProperty]
    private string _actorFilter = string.Empty;

    [ConfigProperty]
    private string _automationFilter = string.Empty;

    [ConfigProperty]
    private bool? _automationStateFilter;

    [ConfigProperty]
    private string _npcFilter = string.Empty;

    [ConfigProperty]
    private YesNoFlag _unlocksFavoriteFilter = YesNoFlag.Either;

    [ConfigProperty]
    private UnlockCacheItem.Modded _unlocksModdedFilter = UnlockCacheItem.ModdedAll;

    [ConfigProperty]
    private string _unlocksNameFilter = string.Empty;

    [ConfigProperty]
    private string _unlocksTypeFilter = string.Empty;

    [ConfigProperty]
    private EquipFlag _unlocksSlotFilter = UnlockCacheItem.SlotsAll;

    [ConfigProperty]
    private YesNoFlag _unlocksUnlockedFilter = YesNoFlag.Either;

    [ConfigProperty]
    private string _unlocksItemIdFilter = string.Empty;

    [ConfigProperty]
    private string _unlocksModelDataFilter = string.Empty;

    [ConfigProperty]
    private string _unlocksLevelFilter = string.Empty;

    [ConfigProperty]
    private JobFlag _unlocksJobFilter;

    [ConfigProperty]
    private UnlockCacheItem.Dyability _unlocksDyabilityFilter = UnlockCacheItem.DyableAll;

    [ConfigProperty]
    private YesNoFlag _unlocksTradableFilter = YesNoFlag.Either;

    [ConfigProperty]
    private YesNoFlag _unlocksCrestFilter = YesNoFlag.Either;

    private void WriteUnlocksTab(JsonTextWriter j)
    {
        var obj = new JObject();
        if (UnlocksFavoriteFilter is not YesNoFlagExtensions.Either)
            obj["Favorite"] = (uint)UnlocksFavoriteFilter;
        if (UnlocksCrestFilter is not YesNoFlagExtensions.Either)
            obj["Crest"] = (uint)UnlocksCrestFilter;
        if (UnlocksTradableFilter is not YesNoFlagExtensions.Either)
            obj["Tradable"] = (uint)UnlocksTradableFilter;
        if (UnlocksUnlockedFilter is not YesNoFlagExtensions.Either)
            obj["Unlocked"] = (uint)UnlocksUnlockedFilter;

        if (UnlocksModdedFilter is not UnlockCacheItem.ModdedAll)
            obj["Modded"] = (uint)UnlocksModdedFilter;

        if (UnlocksDyabilityFilter is not UnlockCacheItem.DyableAll)
            obj["Dyability"] = (uint)UnlocksDyabilityFilter;

        if (UnlocksSlotFilter is not UnlockCacheItem.SlotsAll)
            obj["Slot"] = (uint)UnlocksSlotFilter;

        if (UnlocksJobFilter != _jobs.AllAvailableJobs)
            obj["Job"] = (ulong)UnlocksJobFilter;

        if (UnlocksLevelFilter.Length > 0)
            obj["Level"] = UnlocksLevelFilter;
        if (UnlocksModelDataFilter.Length > 0)
            obj["ModelData"] = UnlocksModelDataFilter;
        if (UnlocksItemIdFilter.Length > 0)
            obj["ItemId"] = UnlocksItemIdFilter;
        if (UnlocksNameFilter.Length > 0)
            obj["Name"] = UnlocksNameFilter;
        if (UnlocksTypeFilter.Length > 0)
            obj["Type"] = UnlocksTypeFilter;

        if (obj.Count <= 0)
            return;

        j.WritePropertyName("Unlocks");
        obj.WriteTo(j);
    }

    private void LoadUnlocksTab(JObject j)
    {
        if (j["Unlocks"] is not JObject unlocks)
            return;

        _unlocksFavoriteFilter  = unlocks["Favorite"]?.Value<uint>() is { } f ? (YesNoFlag)f : YesNoFlag.Either;
        _unlocksCrestFilter     = unlocks["Crest"]?.Value<uint>() is { } c ? (YesNoFlag)c : YesNoFlag.Either;
        _unlocksTradableFilter  = unlocks["Tradable"]?.Value<uint>() is { } t ? (YesNoFlag)t : YesNoFlag.Either;
        _unlocksUnlockedFilter  = unlocks["Unlocked"]?.Value<uint>() is { } u ? (YesNoFlag)u : YesNoFlag.Either;
        _unlocksModdedFilter    = unlocks["Modded"]?.Value<uint>() is { } m ? (UnlockCacheItem.Modded)m : UnlockCacheItem.ModdedAll;
        _unlocksDyabilityFilter = unlocks["Dyability"]?.Value<uint>() is { } d ? (UnlockCacheItem.Dyability)d : UnlockCacheItem.DyableAll;
        _unlocksSlotFilter      = unlocks["Slot"]?.Value<uint>() is { } s ? (EquipFlag)s : UnlockCacheItem.SlotsAll;
        _unlocksJobFilter       = unlocks["Job"]?.Value<ulong>() is { } job ? (JobFlag)job : _jobs.AllAvailableJobs;
        _unlocksLevelFilter     = unlocks["Level"]?.Value<string>() ?? string.Empty;
        _unlocksModelDataFilter = unlocks["ModelData"]?.Value<string>() ?? string.Empty;
        _unlocksItemIdFilter    = unlocks["ItemId"]?.Value<string>() ?? string.Empty;
        _unlocksNameFilter      = unlocks["Name"]?.Value<string>() ?? string.Empty;
        _unlocksTypeFilter      = unlocks["Type"]?.Value<string>() ?? string.Empty;
    }


    public override string ToFilePath(FilenameService fileNames)
        => fileNames.FilterFile;

    private void WriteAutomation(JsonTextWriter j)
    {
        if (AutomationStateFilter is null && AutomationFilter.Length is 0)
            return;

        j.WritePropertyName("Automation");
        j.WriteStartObject();
        if (AutomationStateFilter is not null)
        {
            j.WritePropertyName("State");
            j.WriteValue(AutomationStateFilter.Value);
        }

        if (AutomationFilter.Length > 0)
        {
            j.WritePropertyName("Filter");
            j.WriteValue(AutomationFilter);
        }

        j.WriteEndObject();
    }

    private static void WriteFilter(JsonTextWriter j, string filter, string tabName)
    {
        if (filter.Length is 0)
            return;

        j.WritePropertyName(tabName);
        j.WriteStartObject();
        j.WritePropertyName("Filter");
        j.WriteValue(filter);
        j.WriteEndObject();
    }
}
