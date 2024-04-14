using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Penumbra.Api.Helpers;

namespace Glamourer.Api.IpcSubscribers;

/// <inheritdoc cref="IGlamourerApiDesigns.GetDesignList"/>
public sealed class GetDesignList(DalamudPluginInterface pi)
    : FuncSubscriber<Dictionary<Guid, string>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(GetDesignList)}";

    /// <inheritdoc cref="IGlamourerApiDesigns.GetDesignList"/>
    public new Dictionary<Guid, string> Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Dictionary<Guid, string>> Provider(DalamudPluginInterface pi, IGlamourerApiDesigns api)
        => new(pi, Label, api.GetDesignList);
}

/// <inheritdoc cref="IGlamourerApiDesigns.ApplyDesign"/>
public sealed class ApplyDesign(DalamudPluginInterface pi) : FuncSubscriber<Guid, int, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(ApplyDesign)}";

    /// <inheritdoc cref="IGlamourerApiDesigns.ApplyDesign"/>
    public GlamourerApiEc Invoke(Guid designId, int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlagEx.DesignDefault)
        => (GlamourerApiEc)Invoke(designId, objectIndex, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, int, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiDesigns api)
        => new(pi, Label, (a, b, c, d) => (int)api.ApplyDesign(a, b, c, (ApplyFlag)d));
}

/// <inheritdoc cref="IGlamourerApiDesigns.ApplyDesignName"/>
public sealed class ApplyDesignName(DalamudPluginInterface pi) : FuncSubscriber<Guid, string, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(ApplyDesignName)}";

    /// <inheritdoc cref="IGlamourerApiDesigns.ApplyDesignName"/>
    public GlamourerApiEc Invoke(Guid designId, string objectName, uint key = 0, ApplyFlag flags = ApplyFlagEx.DesignDefault)
        => (GlamourerApiEc)Invoke(designId, objectName, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiDesigns api)
        => new(pi, Label, (a, b, c, d) => (int)api.ApplyDesignName(a, b, c, (ApplyFlag)d));
}
