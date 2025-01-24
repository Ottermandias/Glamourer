using Dalamud.Plugin.Services;
using OtterGui.Classes;

namespace Glamourer.Events;

public sealed class GPoseService : EventWrapper<bool, GPoseService.Priority>
{
    private readonly IFramework   _framework;
    private readonly IClientState _state;

    private readonly ConcurrentQueue<Action> _onLeave = new();
    private readonly ConcurrentQueue<Action> _onEnter = new();

    public enum Priority
    {
        /// <seealso cref="Api.StateApi.OnGPoseChange"/>
        StateApi = int.MinValue,
    }

    public bool InGPose { get; private set; }

    public GPoseService(IFramework framework, IClientState state)
        : base(nameof(GPoseService))
    {
        _framework        =  framework;
        _state            =  state;
        InGPose           =  state.IsGPosing;
        _framework.Update += OnFramework;
    }

    protected override void Dispose(bool _)
        => _framework.Update -= OnFramework;

    public void AddActionOnLeave(Action onLeave)
    {
        if (InGPose)
            _onLeave.Enqueue(onLeave);
        else
            onLeave();
    }

    public void AddActionOnEnter(Action onEnter)
    {
        if (InGPose)
            onEnter();
        else
            _onEnter.Enqueue(onEnter);
    }

    private void OnFramework(IFramework _)
    {
        var inGPose = _state.IsGPosing;
        if (InGPose == inGPose)
            return;

        InGPose = inGPose;
        Invoke(InGPose);
        var actions = InGPose ? _onEnter : _onLeave;
        while (actions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Glamourer.Log.Error($"Error executing GPose action:\n{ex}");
            }
        }
    }
}
