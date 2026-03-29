using Glamourer.Designs;
using Glamourer.Interop.Material;
using Glamourer.State;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

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
    public DeniedApplicationReason CanApplyToPlayer()
    {
        var (identifier, data) = objects.PlayerData;
        return CanApplyTo(identifier, data);
    }

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

    public void ApplyToPlayer(DesignBase source, EquipSlot slot, bool applyItem, bool applyColors)
    {
        var (identifier, data) = objects.PlayerData;
        ApplyTo(source, slot, identifier, data, applyItem, applyColors);
    }

    public void ApplyToPlayer(DesignBase source, BonusItemFlag slot, bool applyItem, bool applyColors)
    {
        var (identifier, data) = objects.PlayerData;
        ApplyTo(source, slot, identifier, data, applyItem, applyColors);
    }

    public void ApplyTo(DesignBase source, EquipSlot slot, ActorIdentifier targetIdentifier, ActorData targetData, bool applyItem,
        bool applyColors)
    {
        if (!stateManager.GetOrCreate(targetIdentifier, targetData.Objects[0], out var state))
            return;

        if (!EquipSlotExtensions.FullSlots.Contains(slot))
            return;

        var applyBoth = applyItem && applyColors;
        // Only apply the item if we either specifically apply the item (applyItem true, applyColors false), or if we apply both and the source design applies the item for this slot. Vice versa for colors.
        EquipItem? item   = applyItem && !applyColors || applyBoth && source.DoApplyEquip(slot) ? source.DesignData.Item(slot) : null;
        StainIds?  stains = applyColors && !applyItem || applyBoth && source.DoApplyStain(slot) ? source.DesignData.Stain(slot) : null;
        stateManager.ChangeEquip(state, slot, item, stains, ApplySettings.Manual);

        if (!applyColors)
            return;

        var material = source.GetMaterialDataRef();
        var (type, humanSlot) = slot switch
        {
            EquipSlot.MainHand => (MaterialValueIndex.DrawObjectType.Mainhand, (byte)0),
            EquipSlot.OffHand  => (MaterialValueIndex.DrawObjectType.Offhand, (byte)0),
            _                  => (MaterialValueIndex.DrawObjectType.Human, (byte)slot.ToHumanSlot()),
        };
        var values = material.GetValues(MaterialValueIndex.Min(type, humanSlot), MaterialValueIndex.Max(type, humanSlot));
        ApplyMaterials(state, values, applyBoth);
    }

    public void ApplyTo(DesignBase source, BonusItemFlag slot, ActorIdentifier targetIdentifier, ActorData targetData, bool applyItem,
        bool applyColors)
    {
        if (!stateManager.GetOrCreate(targetIdentifier, targetData.Objects[0], out var state))
            return;

        if (!BonusExtensions.AllFlags.Contains(slot))
            return;

        var applyBoth = applyItem && applyColors;
        // Only apply the item if we either specifically apply the item (applyItem true, applyColors false), or if we apply both and the source design applies the item for this slot.
        if (applyItem && !applyColors || applyBoth && source.DoApplyBonusItem(slot))
            stateManager.ChangeBonusItem(state, slot, source.DesignData.BonusItem(slot), ApplySettings.Manual);

        if (!applyColors)
            return;

        var material  = source.GetMaterialDataRef();
        var humanSlot = (byte)slot.ToModelIndex();
        var values = material.GetValues(MaterialValueIndex.Min(MaterialValueIndex.DrawObjectType.Human, humanSlot),
            MaterialValueIndex.Max(MaterialValueIndex.DrawObjectType.Human, humanSlot));
        ApplyMaterials(state, values, applyBoth);
    }

    private void ApplyMaterials(ActorState state, ReadOnlySpan<(uint, MaterialValueDesign)> values, bool applyBoth)
    {
        foreach (var (key, value) in values)
        {
            // Again, apply the material colors if only applyColors is true, or if both are true and the design has it enabled.
            if (applyBoth && !value.Enabled)
                continue;

            var idx = MaterialValueIndex.FromKey(key);
            if (state.Materials.TryGetValue(idx, out var materialState))
            {
                if (value.Revert)
                    stateManager.ResetMaterialValue(state, idx, ApplySettings.Manual);
                else
                    stateManager.ChangeMaterialValue(state, idx,
                        new MaterialValueState(materialState.Game, value.Value, materialState.DrawData, StateSource.Pending),
                        ApplySettings.Manual);
            }
            else if (!value.Revert)
            {
                stateManager.ChangeMaterialValue(state, idx,
                    new MaterialValueState(ColorRow.Empty, value.Value, CharacterWeapon.Empty, StateSource.Pending), ApplySettings.Manual);
            }
        }
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
