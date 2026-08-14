using Glamourer.Gui;
using Glamourer.Services;
using Luna;
using Luna.Generators;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;
using System.Text.Json;

namespace Glamourer.Config;

public sealed partial class UiConfig : ConfigurationFile<FilenameService>, IDisposable
{
    public readonly ColorCache<ColorId, ColorIdData> ColorCache;

    private readonly ActorManager _actors;

    public UiConfig(SaveService saveService, MessageService messageService, ActorManager actors)
        : base(saveService, messageService, TimeSpan.FromMinutes(5))
    {
        _actors    = actors;
        ColorCache = new ColorCache<ColorId, ColorIdData>(Colors);
        Load();
        Gui.Colors.SetCache(ColorCache);
    }

    public readonly ColorDictionary<ColorId, ColorIdData> Colors = new();

    [ConfigProperty]
    private TwoPanelWidth _actorsTabScale = new(250, ScalingMode.Absolute);

    [ConfigProperty]
    private TwoPanelWidth _designsTabScale = new(0.3f, ScalingMode.Percentage);

    [ConfigProperty]
    private TwoPanelWidth _automationTabScale = new(0.3f, ScalingMode.Percentage);

    [ConfigProperty]
    private TwoPanelWidth _npcTabScale = new(250, ScalingMode.Absolute);

    [ConfigProperty]
    private NpcId _selectedNpc = 0;

    [ConfigProperty]
    private int _selectedAutomationIndex = -1;

    [ConfigProperty]
    private ActorIdentifier _selectedActor = ActorIdentifier.Invalid;

    public override int CurrentVersion
        => 1;

    protected override void AddData(Utf8JsonWriter j)
    {
        j.WritePropertyName("Colors"u8);
        Colors.Serialize(j, false);
        ActorsTabScale.WriteJson(j, "ActorsTab"u8);
        DesignsTabScale.WriteJson(j, "DesignsTab"u8);
        AutomationTabScale.WriteJson(j, "AutomationTab"u8);
        NpcTabScale.WriteJson(j, "NpcTab"u8);
        j.WriteUnsignedIfNot("SelectedNpc"u8, _selectedNpc, NpcId.Zero);
        j.WriteSignedIfNot("SelectedAutomationIndex"u8, _selectedAutomationIndex, -1);
        if (_selectedActor.IsValid)
        {
            j.WritePropertyName("SelectedActor"u8);
            j.WriteJson("SelectedActor"u8, _selectedActor);
        }
    }

    protected override void LoadData(in JsonElement j)
    {
        _selectedNpc             = j.PropertyOrDefault("SelectedNpc"u8,             (uint)_selectedNpc);
        _selectedAutomationIndex = j.PropertyOrDefault("SelectedAutomationIndex"u8, _selectedAutomationIndex);
        _actorsTabScale          = TwoPanelWidth.ReadJson(j, "ActorsTab"u8,     _actorsTabScale);
        _designsTabScale         = TwoPanelWidth.ReadJson(j, "DesignsTab"u8,    _designsTabScale);
        _automationTabScale      = TwoPanelWidth.ReadJson(j, "AutomationTab"u8, _automationTabScale);
        _npcTabScale             = TwoPanelWidth.ReadJson(j, "NpcTab"u8,        _npcTabScale);
        if (j.TryReadObject("Colors"u8, out var colors))
        {
#pragma warning disable CA1869
            var options = new JsonSerializerOptions(JsonFunctions.SerializerOptions);
#pragma warning restore CA1869
            options.Converters.Add(new ColorDictionaryConverter<ColorId, ColorIdData>(Messager, true, true, true));
            if (colors.Deserialize<ColorDictionary<ColorId, ColorIdData>>(options) is { } dict)
                Colors.Apply(dict, true);
        }

        if (j.TryGetProperty("SelectedActor"u8, out var selectedActor))
            _selectedActor = _actors.FromJson(selectedActor);
    }

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.UiConfigurationFile;

    public void Dispose()
    {
        Gui.Colors.SetCache(null!);
        ColorCache.Dispose();
    }
}
