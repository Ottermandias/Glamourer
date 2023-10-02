using System;
using System.Collections.Concurrent;
using Dalamud.Plugin.Services;
using OtterGui.Classes;

namespace Glamourer.Events;

public class GPoseService : EventWrapper<Action<bool>, GPoseService.Priority>
{
    private readonly IFramework   _framework;
    private readonly IClientState _state;

    private readonly ConcurrentQueue<Action> _onLeave = new();
    private readonly ConcurrentQueue<Action> _onEnter = new();

    public enum Priority
    {
        /// <seealso cref="Api.GlamourerIpc.OnGPoseChanged"/>
        GlamourerIpc = int.MinValue,
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

    public new void Dispose()
    {
        _framework.Update -= OnFramework;
        base.Dispose();
    }

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
        Invoke(this, InGPose);
        var actions = InGPose ? _onEnter : _onLeave;
        foreach (var action in actions)
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
