using Glamourer.Designs;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace Glamourer.Services;

public sealed class DesignApplier(StateManager stateManager, ActorObjectManager objects) : IService
{
    public void ApplyToPlayer(DesignBase design)
    {
        var (player, data) = objects.PlayerData;
        if (!data.Valid)
            return;

        if (!stateManager.GetOrCreate(player, data.Objects[0], out var state))
            return;

        stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks);
    }

    public void ApplyToTarget(DesignBase design)
    {
        var (player, data) = objects.TargetData;
        if (!data.Valid)
            return;

        if (!stateManager.GetOrCreate(player, data.Objects[0], out var state))
            return;

        stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks);
    }

    public void Apply(ActorIdentifier actor, DesignBase design)
        => Apply(actor, objects.TryGetValue(actor, out var d) ? d : ActorData.Invalid, design, ApplySettings.ManualWithLinks);

    public void Apply(ActorIdentifier actor, DesignBase design, ApplySettings settings)
        => Apply(actor, objects.TryGetValue(actor, out var d) ? d : ActorData.Invalid, design, settings);

    public void Apply(ActorIdentifier actor, ActorData data, DesignBase design)
        => Apply(actor, data, design, ApplySettings.ManualWithLinks);

    public void Apply(ActorIdentifier actor, ActorData data, DesignBase design, ApplySettings settings)
    {
        if (!actor.IsValid || !data.Valid)
            return;

        if (!stateManager.GetOrCreate(actor, data.Objects[0], out var state))
            return;

        stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks);
    }
}
