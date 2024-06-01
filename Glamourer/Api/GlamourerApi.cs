using Glamourer.Api.Api;
using OtterGui.Services;

namespace Glamourer.Api;

public class GlamourerApi(DesignsApi designs, StateApi state, ItemsApi items) : IGlamourerApi, IApiService
{
    public const int CurrentApiVersionMajor = 1;
    public const int CurrentApiVersionMinor = 2;

    public (int Major, int Minor) ApiVersion
        => (CurrentApiVersionMajor, CurrentApiVersionMinor);

    public IGlamourerApiDesigns Designs
        => designs;

    public IGlamourerApiItems Items
        => items;

    public IGlamourerApiState State
        => state;
}
