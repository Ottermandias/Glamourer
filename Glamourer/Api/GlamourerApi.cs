using Glamourer.Api.Api;
using Glamourer.Config;
using Luna;

namespace Glamourer.Api;

public class GlamourerApi(Configuration config, DesignsApi designs, StateApi state, ItemsApi items, PluginApi plugin) : IGlamourerApi, IApiService
{
    public const int CurrentApiVersionMajor = 1;
    public const int CurrentApiVersionMinor = 8;

    public (int Major, int Minor) ApiVersion
        => (CurrentApiVersionMajor, CurrentApiVersionMinor);

    public bool AutoReloadGearEnabled
        => config.AutoRedrawEquipOnChanges;

    public IGlamourerApiDesigns Designs
        => designs;

    public IGlamourerApiItems Items
        => items;

    public IGlamourerApiState State
        => state;

    public IGlamourerApiPlugin Plugin
        => plugin;
}
