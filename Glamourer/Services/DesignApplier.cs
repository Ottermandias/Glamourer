using Glamourer.Designs;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Interop;

namespace Glamourer.Services;

public enum DeniedApplicationReason
{
    None,
    TargetInvalid,
    TargetUnavailable,
    SourceUnavailable,
    SourceNonHuman,
    TargetNonHuman,
}

public sealed class DesignApplier(StateManager stateManager, ActorObjectManager objects, DesignConverter converter, HumanModelList humans)
    : IService
{
    public DeniedApplicationReason CanApplyTo(DesignData? source, ActorIdentifier targetIdentifier, ActorData targetData)
    {
        if (source is not { } s)
            return DeniedApplicationReason.SourceUnavailable;

        if (!s.IsHuman)
            return DeniedApplicationReason.SourceNonHuman;

        return CanApplyTo(targetIdentifier, targetData);
    }

    public DeniedApplicationReason CanApplyTo(ActorIdentifier targetIdentifier, ActorData targetData)
    {
        if (!targetIdentifier.IsValid)
            return DeniedApplicationReason.TargetInvalid;

        if (!targetData.Valid)
            return DeniedApplicationReason.TargetUnavailable;

        if (!targetData.Objects[0].IsHuman(humans))
            return DeniedApplicationReason.TargetNonHuman;

        return DeniedApplicationReason.None;
    }

    public void ApplyTo(ActorState source, ActorIdentifier targetIdentifier, ActorData targetData, ApplicationRules application)
    {
        if (!stateManager.GetOrCreate(targetIdentifier, targetData.Objects[0], out var state))
            return;

        var designBase = converter.Convert(source, application);
        stateManager.ApplyDesign(state, designBase, ApplySettings.Manual with { IsFinal = true });
    }

    public void ApplyTo(DesignData source, ActorIdentifier targetIdentifier, ActorData targetData, ApplicationRules application)
    {
        if (!stateManager.GetOrCreate(targetIdentifier, targetData.Objects[0], out var state))
            return;

        var designBase = converter.Convert(source, new StateMaterialManager(), application);
        stateManager.ApplyDesign(state, designBase, ApplySettings.Manual with { IsFinal = true });
    }

    public void ApplyTo(DesignBase source, ActorIdentifier targetIdentifier, ActorData targetData, bool restrict)
    {
        if (!stateManager.GetOrCreate(targetIdentifier, targetData.Objects[0], out var state))
            return;

        using var restriction = restrict
            ? source.TemporarilyRestrictApplication(ApplicationCollection.FromKeys())
            : DesignBase.FlagRestrictionResetter.Nothing;
        stateManager.ApplyDesign(state, source, ApplySettings.ManualWithLinks with { IsFinal = true });
    }

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
