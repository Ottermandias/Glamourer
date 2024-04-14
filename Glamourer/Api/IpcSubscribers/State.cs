using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Enums;
using Newtonsoft.Json.Linq;
using Penumbra.Api.Helpers;

namespace Glamourer.Api.IpcSubscribers;

/// <inheritdoc cref="IGlamourerApiState.GetState"/>
public sealed class GetState(DalamudPluginInterface pi)
    : FuncSubscriber<int, uint, (int, JObject?)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(GetState)}";

    /// <inheritdoc cref="IGlamourerApiState.GetState"/>
    public new (GlamourerApiEc, JObject?) Invoke(int objectIndex, uint key = 0)
    {
        var (ec, data) = base.Invoke(objectIndex, key);
        return ((GlamourerApiEc)ec, data);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, uint, (int, JObject?)> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b) =>
        {
            var (ec, data) = api.GetState(a, b);
            return ((int)ec, data);
        });
}

/// <inheritdoc cref="IGlamourerApiState.GetStateName"/>
public sealed class GetStateName(DalamudPluginInterface pi)
    : FuncSubscriber<string, uint, (int, JObject?)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(GetStateName)}";

    /// <inheritdoc cref="IGlamourerApiState.GetStateName"/>
    public new (GlamourerApiEc, JObject?) Invoke(string objectName, uint key = 0)
    {
        var (ec, data) = base.Invoke(objectName, key);
        return ((GlamourerApiEc)ec, data);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, uint, (int, JObject?)> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (i, k) =>
        {
            var (ec, data) = api.GetStateName(i, k);
            return ((int)ec, data);
        });
}

/// <inheritdoc cref="IGlamourerApiState.ApplyState"/>
public sealed class ApplyState(DalamudPluginInterface pi)
    : FuncSubscriber<object, int, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(ApplyState)}";

    /// <inheritdoc cref="IGlamourerApiState.ApplyState"/>
    public GlamourerApiEc Invoke(JObject state, int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlagEx.StateDefault)
        => (GlamourerApiEc)Invoke(state, objectIndex, key, (ulong)flags);

    /// <inheritdoc cref="IGlamourerApiState.ApplyState"/>
    public GlamourerApiEc Invoke(string base64State, int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlagEx.StateDefault)
        => (GlamourerApiEc)Invoke(base64State, objectIndex, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<object, int, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c, d) => (int)api.ApplyState(a, b, c, (ApplyFlag)d));
}

/// <inheritdoc cref="IGlamourerApiState.ApplyStateName"/>
public sealed class ApplyStateName(DalamudPluginInterface pi)
    : FuncSubscriber<object, string, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(ApplyStateName)}";

    /// <inheritdoc cref="IGlamourerApiState.ApplyState"/>
    public GlamourerApiEc Invoke(JObject state, string objectName, uint key = 0, ApplyFlag flags = ApplyFlagEx.StateDefault)
        => (GlamourerApiEc)Invoke(state, objectName, key, (ulong)flags);

    /// <inheritdoc cref="IGlamourerApiState.ApplyState"/>
    public GlamourerApiEc Invoke(string base64State, string objectName, uint key = 0, ApplyFlag flags = ApplyFlagEx.StateDefault)
        => (GlamourerApiEc)Invoke(base64State, objectName, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<object, string, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c, d) => (int)api.ApplyStateName(a, b, c, (ApplyFlag)d));
}

/// <inheritdoc cref="IGlamourerApiState.RevertState"/>
public sealed class RevertState(DalamudPluginInterface pi)
    : FuncSubscriber<int, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(RevertState)}";

    /// <inheritdoc cref="IGlamourerApiState.RevertState"/>
    public GlamourerApiEc Invoke(int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlagEx.RevertDefault)
        => (GlamourerApiEc)Invoke(objectIndex, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c) => (int)api.RevertState(a, b, (ApplyFlag)c));
}

/// <inheritdoc cref="IGlamourerApiState.RevertStateName"/>
public sealed class RevertStateName(DalamudPluginInterface pi)
    : FuncSubscriber<string, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(RevertStateName)}";

    /// <inheritdoc cref="IGlamourerApiState.RevertStateName"/>
    public GlamourerApiEc Invoke(string objectName, uint key = 0, ApplyFlag flags = ApplyFlagEx.RevertDefault)
        => (GlamourerApiEc)Invoke(objectName, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c) => (int)api.RevertStateName(a, b, (ApplyFlag)c));
}

/// <inheritdoc cref="IGlamourerApiState.UnlockState"/>
public sealed class UnlockState(DalamudPluginInterface pi)
    : FuncSubscriber<int, uint, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(UnlockState)}";

    /// <inheritdoc cref="IGlamourerApiState.UnlockState"/>
    public new GlamourerApiEc Invoke(int objectIndex, uint key = 0)
        => (GlamourerApiEc)base.Invoke(objectIndex, key);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, uint, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b) => (int)api.UnlockState(a, b));
}

/// <inheritdoc cref="IGlamourerApiState.UnlockStateName"/>
public sealed class UnlockStateName(DalamudPluginInterface pi)
    : FuncSubscriber<string, uint, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(UnlockStateName)}";

    /// <inheritdoc cref="IGlamourerApiState.UnlockStateName"/>
    public new GlamourerApiEc Invoke(string objectName, uint key = 0)
        => (GlamourerApiEc)base.Invoke(objectName, key);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, uint, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b) => (int)api.UnlockStateName(a, b));
}

/// <inheritdoc cref="IGlamourerApiState.UnlockAll"/>
public sealed class UnlockAll(DalamudPluginInterface pi)
    : FuncSubscriber<uint, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(UnlockAll)}";

    /// <inheritdoc cref="IGlamourerApiState.UnlockAll"/>
    public new int Invoke(uint key)
        => base.Invoke(key);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<uint, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, api.UnlockAll);
}

/// <inheritdoc cref="IGlamourerApiState.RevertToAutomation"/>
public sealed class RevertToAutomation(DalamudPluginInterface pi)
    : FuncSubscriber<int, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(RevertToAutomation)}";

    /// <inheritdoc cref="IGlamourerApiState.RevertToAutomation"/>
    public GlamourerApiEc Invoke(int objectIndex, uint key = 0, ApplyFlag flags = ApplyFlagEx.RevertDefault)
        => (GlamourerApiEc)Invoke(objectIndex, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c) => (int)api.RevertToAutomation(a, b, (ApplyFlag)c));
}

/// <inheritdoc cref="IGlamourerApiState.RevertToAutomationName"/>
public sealed class RevertToAutomationName(DalamudPluginInterface pi)
    : FuncSubscriber<string, uint, ulong, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(RevertToAutomationName)}";

    /// <inheritdoc cref="IGlamourerApiState.RevertToAutomationName"/>
    public GlamourerApiEc Invoke(string objectName, uint key = 0, ApplyFlag flags = ApplyFlagEx.RevertDefault)
        => (GlamourerApiEc)Invoke(objectName, key, (ulong)flags);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, uint, ulong, int> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (a, b, c) => (int)api.RevertToAutomationName(a, b, (ApplyFlag)c));
}

/// <inheritdoc cref="IGlamourerApiState.StateChanged" />
public static class StateChanged
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(StateChanged)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<nint> Subscriber(DalamudPluginInterface pi, params Action<nint>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<nint> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (t => api.StateChanged += t, t => api.StateChanged -= t));
}

/// <inheritdoc cref="IGlamourerApiState.GPoseChanged" />
public static class GPoseChanged
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GPoseChanged)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<bool> Subscriber(DalamudPluginInterface pi, params Action<bool>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<bool> Provider(DalamudPluginInterface pi, IGlamourerApiState api)
        => new(pi, Label, (t => api.GPoseChanged += t, t => api.GPoseChanged -= t));
}
