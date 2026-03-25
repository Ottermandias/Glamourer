using System.Text.Json;
using Glamourer.Gui.Tabs.UnlocksTab;
using Glamourer.Services;
using ImSharp;
using Luna;
using Luna.Generators;
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

    protected override void AddData(Utf8JsonWriter j)
    {
        WriteFilter(j, ActorFilter,  "Actors"u8);
        WriteFilter(j, DesignFilter, "Designs"u8);
        WriteFilter(j, NpcFilter,    "Npcs"u8);
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

    private void WriteUnlocksTab(Utf8JsonWriter j)
    {
        using var tmp = j.TemporaryObject("Unlocks"u8);
        tmp.WriteUnsignedIfNot("Favorite"u8,  UnlocksFavoriteFilter,  YesNoFlagExtensions.Either);
        tmp.WriteUnsignedIfNot("Crest"u8,     UnlocksCrestFilter,     YesNoFlagExtensions.Either);
        tmp.WriteUnsignedIfNot("Tradable"u8,  UnlocksTradableFilter,  YesNoFlagExtensions.Either);
        tmp.WriteUnsignedIfNot("Unlocked"u8,  UnlocksUnlockedFilter,  YesNoFlagExtensions.Either);
        tmp.WriteUnsignedIfNot("Modded"u8,    UnlocksModdedFilter,    UnlockCacheItem.ModdedAll);
        tmp.WriteUnsignedIfNot("Dyability"u8, UnlocksDyabilityFilter, UnlockCacheItem.DyableAll);
        tmp.WriteUnsignedIfNot("Slot"u8,      UnlocksSlotFilter,      UnlockCacheItem.SlotsAll);
        tmp.WriteUnsignedIfNot("Job"u8,       UnlocksJobFilter,       _jobs.AllAvailableJobs);
        tmp.WriteNonEmptyString("Level"u8,     UnlocksLevelFilter);
        tmp.WriteNonEmptyString("ModelData"u8, UnlocksModelDataFilter);
        tmp.WriteNonEmptyString("ItemId"u8,    UnlocksItemIdFilter);
        tmp.WriteNonEmptyString("Name"u8,      UnlocksNameFilter);
        tmp.WriteNonEmptyString("Type"u8,      UnlocksTypeFilter);
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

    private void WriteAutomation(Utf8JsonWriter j)
    {
        using var tmp = j.TemporaryObject("Automation"u8);
        if (AutomationStateFilter is not null)
            j.WriteBoolean("State"u8, AutomationStateFilter.Value);
        tmp.WriteNonEmptyString("Filter"u8, AutomationFilter);
    }

    private static void WriteFilter(Utf8JsonWriter j, string filter, ReadOnlySpan<byte> tabName)
    {
        if (filter.Length is 0)
            return;

        j.WritePropertyName(tabName);
        j.WriteStartObject();
        j.WriteString("Filter"u8, filter);
        j.WriteEndObject();
    }
}
