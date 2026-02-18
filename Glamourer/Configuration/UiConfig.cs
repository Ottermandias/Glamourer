using Glamourer.Services;
using Luna;
using Luna.Generators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Glamourer.Configuration;

public sealed partial class UiConfig : ConfigurationFile<FilenameService>
{
    public UiConfig(SaveService saveService, MessageService messageService)
        : base(saveService, messageService)
    {
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

    public override int CurrentVersion
        => 1;

    protected override void AddData(JsonTextWriter j)
    {
        ActorsTabScale.WriteJson(j, "ActorsTab");
        DesignsTabScale.WriteJson(j, "DesignsTab");
        AutomationTabScale.WriteJson(j, "AutomationTab");
        NpcTabScale.WriteJson(j, "NpcTab");
    }

    protected override void LoadData(JObject j)
    {
        _actorsTabScale     = TwoPanelWidth.ReadJson(j, "ActorsTab",     new TwoPanelWidth(250,  ScalingMode.Absolute));
        _designsTabScale    = TwoPanelWidth.ReadJson(j, "DesignsTab",    new TwoPanelWidth(0.3f, ScalingMode.Percentage));
        _automationTabScale = TwoPanelWidth.ReadJson(j, "AutomationTab", new TwoPanelWidth(0.3f, ScalingMode.Percentage));
        _npcTabScale        = TwoPanelWidth.ReadJson(j, "NpcTab",        new TwoPanelWidth(250,  ScalingMode.Absolute));
    }

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.UiConfiguration;
}
