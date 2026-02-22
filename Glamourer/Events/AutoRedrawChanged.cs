using Luna;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the auto-reload gear setting is changed in glamourer configuration.
/// </summary>
public sealed class AutoRedrawChanged(Logger log)
    : EventBase<bool, AutoRedrawChanged.Priority>(nameof(AutoRedrawChanged), log)
{
    public enum Priority
    {
        /// <seealso cref="Api.StateApi.OnGPoseChange"/>
        StateApi = int.MinValue,
    }
}
