using Glamourer.Services;
using Luna;
using Luna.Generators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;

namespace Glamourer.Config;

public sealed partial class UiConfig : ConfigurationFile<FilenameService>
{
    private readonly ActorManager _actors;

    public UiConfig(SaveService saveService, MessageService messageService, ActorManager actors)
        : base(saveService, messageService, TimeSpan.FromMinutes(5))
    {
        _actors = actors;
        Load();
    }

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

    protected override void AddData(JsonTextWriter j)
    {
        ActorsTabScale.WriteJson(j, "ActorsTab");
        DesignsTabScale.WriteJson(j, "DesignsTab");
        AutomationTabScale.WriteJson(j, "AutomationTab");
        NpcTabScale.WriteJson(j, "NpcTab");
        if (_selectedNpc.Id is not 0)
        {
            j.WritePropertyName("SelectedNpc");
            j.WriteValue(_selectedNpc);
        }

        if (_selectedAutomationIndex >= 0)
        {
            j.WritePropertyName("SelectedAutomationIndex");
            j.WriteValue(_selectedAutomationIndex);
        }

        if (_selectedActor.IsValid)
        {
            j.WritePropertyName("SelectedActor");
            _selectedActor.ToJson().WriteTo(j);
        }
    }

    protected override void LoadData(JObject j)
    {
        _actorsTabScale          = TwoPanelWidth.ReadJson(j, "ActorsTab",     new TwoPanelWidth(250,  ScalingMode.Absolute));
        _designsTabScale         = TwoPanelWidth.ReadJson(j, "DesignsTab",    new TwoPanelWidth(0.3f, ScalingMode.Percentage));
        _automationTabScale      = TwoPanelWidth.ReadJson(j, "AutomationTab", new TwoPanelWidth(0.3f, ScalingMode.Percentage));
        _npcTabScale             = TwoPanelWidth.ReadJson(j, "NpcTab",        new TwoPanelWidth(250,  ScalingMode.Absolute));
        _selectedNpc             = j["SelectedNpc"]?.Value<uint>() ?? 0;
        _selectedAutomationIndex = j["SelectedAutomationIndex"]?.Value<int>() ?? -1;
        _selectedActor           = _actors.FromJson(j["SelectedActor"] as JObject);
    }

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.UiConfigurationFile;
}
