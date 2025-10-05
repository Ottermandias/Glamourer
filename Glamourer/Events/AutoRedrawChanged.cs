using OtterGui.Classes;

namespace Glamourer.Events;

/// <summary>
/// Triggered when the auto-reload gear setting is changed in glamourer configuration.
/// </summary>
public sealed class AutoRedrawChanged()
    : EventWrapper<bool, AutoRedrawChanged.Priority>(nameof(AutoRedrawChanged))
{
    public enum Priority
    {
        /// <seealso cref="Api.StateApi.OnGPoseChange"/>
        StateApi = int.MinValue,
    }
}