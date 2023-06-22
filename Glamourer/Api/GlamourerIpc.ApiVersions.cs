using System;
using Dalamud.Plugin;
using Penumbra.Api.Helpers;

namespace Glamourer.Api;

public partial class GlamourerIpc
{
    public const string LabelApiVersion  = "Glamourer.ApiVersion";
    public const string LabelApiVersions = "Glamourer.ApiVersions";

    private readonly FuncProvider<int>                    _apiVersionProvider;
    private readonly FuncProvider<(int Major, int Minor)> _apiVersionsProvider;

    public static FuncSubscriber<int> ApiVersionSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApiVersion);

    public static FuncSubscriber<(int Major, int Minor)> ApiVersionsSubscriber(DalamudPluginInterface pi)
        => new(pi, LabelApiVersions);

    [Obsolete($"This call is obsolete, please use {nameof(ApiVersions)} instead.")]
    public int ApiVersion()
        => CurrentApiVersionMajor;

    public (int Major, int Minor) ApiVersions()
        => (CurrentApiVersionMajor, CurrentApiVersionMinor);
}
