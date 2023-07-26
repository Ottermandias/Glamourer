using System;
using System.Collections.Concurrent;
using Dalamud.Game;
using OtterGui.Classes;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Glamourer.Events;

public class GPoseService : EventWrapper<Action<bool>, GPoseService.Priority>
{
    private readonly Framework _framework;

    private readonly ConcurrentQueue<Action> _onLeave = new();
    private readonly ConcurrentQueue<Action> _onEnter = new();

    public enum Priority
    { }

    public bool InGPose { get; private set; } = false;

    public GPoseService(Framework framework)
        : base(nameof(GPoseService))
    {
        _framework        =  framework;
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

    private void OnFramework(Framework _)
    {
        var inGPose = GameMain.IsInGPose();
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
