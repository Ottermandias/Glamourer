using Glamourer.Designs;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateEditor(
    InternalStateEditor editor,
    StateApplier applier,
    StateChanged stateChanged,
    JobChangeState jobChange,
    Configuration config,
    ItemManager items,
    DesignMerger merger) : IDesignEditor
{
    protected readonly InternalStateEditor Editor       = editor;
    protected readonly StateApplier        Applier      = applier;
    protected readonly StateChanged        StateChanged = stateChanged;
    protected readonly Configuration       Config       = config;
    protected readonly ItemManager         Items        = items;

    /// <summary> Turn an actor to. </summary>
    public void ChangeModelId(ActorState state, uint modelId, CustomizeArray customize, nint equipData, StateSource source,
        uint key = 0)
    {
        if (!Editor.ChangeModelId(state, modelId, customize, equipData, source, out var old, key))
            return;

        var actors = Applier.ForceRedraw(state, source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set model id in state {state.Identifier.Incognito(null)} from {old} to {modelId}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Model, source, state, actors, (old, modelId));
    }

    /// <inheritdoc/>
    public void ChangeCustomize(object data, CustomizeIndex idx, CustomizeValue value, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeCustomize(state, idx, value, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeCustomize(state, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set {idx.ToDefaultName()} customizations in state {state.Identifier.Incognito(null)} from {old.Value} to {value.Value}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Customize, settings.Source, state, actors, (old, value, idx));
    }

    /// <inheritdoc/>
    public void ChangeEntireCustomize(object data, in CustomizeArray customizeInput, CustomizeFlag apply, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeHumanCustomize(state, customizeInput, apply, _ => settings.Source, out var old, out var applied, settings.Key))
            return;

        var actors = Applier.ChangeCustomize(state, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set {applied} customizations in state {state.Identifier.Incognito(null)} from {old} to {customizeInput}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.EntireCustomize, settings.Source, state, actors, (old, applied));
    }

    /// <inheritdoc/>
    public void ChangeItem(object data, EquipSlot slot, EquipItem item, ApplySettings settings = default)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeItem(state, slot, item, settings.Source, out var old, settings.Key))
            return;

        var type = slot.ToIndex() < 10 ? StateChanged.Type.Equip : StateChanged.Type.Weapon;
        var actors = type is StateChanged.Type.Equip
            ? Applier.ChangeArmor(state, slot, settings.Source is StateSource.Manual or StateSource.Ipc)
            : Applier.ChangeWeapon(state, slot, settings.Source is StateSource.Manual or StateSource.Ipc,
                item.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));

        if (slot is EquipSlot.MainHand)
            ApplyMainhandPeriphery(state, item, settings);

        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}). [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(type, settings.Source, state, actors, (old, item, slot));
    }

    /// <inheritdoc/>
    public void ChangeEquip(object data, EquipSlot slot, EquipItem? item, StainId? stain, ApplySettings settings)
    {
        switch (item.HasValue, stain.HasValue)
        {
            case (false, false): return;
            case (true, false):
                ChangeItem(data, slot, item!.Value, settings);
                return;
            case (false, true):
                ChangeStain(data, slot, stain!.Value, settings);
                return;
        }

        var state = (ActorState)data;
        if (!Editor.ChangeEquip(state, slot, item ?? state.ModelData.Item(slot), stain ?? state.ModelData.Stain(slot), settings.Source,
                out var old, out var oldStain, settings.Key))
            return;

        var type = slot.ToIndex() < 10 ? StateChanged.Type.Equip : StateChanged.Type.Weapon;
        var actors = type is StateChanged.Type.Equip
            ? Applier.ChangeArmor(state, slot, settings.Source is StateSource.Manual or StateSource.Ipc)
            : Applier.ChangeWeapon(state, slot, settings.Source is StateSource.Manual or StateSource.Ipc,
                item!.Value.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));

        if (slot is EquipSlot.MainHand)
            ApplyMainhandPeriphery(state, item, settings);

        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item!.Value.Name} ({item.Value.ItemId}) and its stain from {oldStain.Id} to {stain!.Value.Id}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(type,                    settings.Source, state, actors, (old, item!.Value, slot));
        StateChanged.Invoke(StateChanged.Type.Stain, settings.Source, state, actors, (oldStain, stain!.Value, slot));
    }

    /// <inheritdoc/>
    public void ChangeStain(object data, EquipSlot slot, StainId stain, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeStain(state, slot, stain, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeStain(state, slot, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} stain in state {state.Identifier.Incognito(null)} from {old.Id} to {stain.Id}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Stain, settings.Source, state, actors, (old, stain, slot));
    }

    /// <inheritdoc/>
    public void ChangeCrest(object data, CrestFlag slot, bool crest, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeCrest(state, slot, crest, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeCrests(state, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set {slot.ToLabel()} crest in state {state.Identifier.Incognito(null)} from {old} to {crest}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Crest, settings.Source, state, actors, (old, crest, slot));
    }

    /// <inheritdoc/>
    public void ChangeCustomizeParameter(object data, CustomizeParameterFlag flag, CustomizeParameterValue value, ApplySettings settings)
    {
        if (data is not ActorState state)
            return;

        // Also apply main color to highlights when highlights is off.
        if (!state.ModelData.Customize.Highlights && flag is CustomizeParameterFlag.HairDiffuse)
            ChangeCustomizeParameter(state, CustomizeParameterFlag.HairHighlight, value, settings);

        if (!Editor.ChangeParameter(state, flag, value, settings.Source, out var old, settings.Key))
            return;

        var @new   = state.ModelData.Parameters[flag];
        var actors = Applier.ChangeParameters(state, flag, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set {flag} crest in state {state.Identifier.Incognito(null)} from {old} to {@new}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Parameter, settings.Source, state, actors, (old, @new, flag));
    }

    /// <inheritdoc/>
    public void ChangeMetaState(object data, MetaIndex index, bool value, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeMetaState(state, index, value, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeMetaState(state, index, settings.Source is StateSource.Manual or StateSource.Ipc);
        Glamourer.Log.Verbose(
            $"Set Head Gear Visibility in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Other, settings.Source, state, actors, (old, value, MetaIndex.HatState));
    }

    /// <inheritdoc/>
    public void ApplyDesign(object data, MergedDesign mergedDesign, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeModelId(state, mergedDesign.Design.DesignData.ModelId, mergedDesign.Design.DesignData.Customize,
                mergedDesign.Design.GetDesignDataRef().GetEquipmentPtr(), settings.Source, out var oldModelId, settings.Key))
            return;

        var requiresRedraw = oldModelId != mergedDesign.Design.DesignData.ModelId || !mergedDesign.Design.DesignData.IsHuman;

        if (state.ModelData.IsHuman)
        {
            foreach (var slot in CrestExtensions.AllRelevantSet.Where(mergedDesign.Design.DoApplyCrest))
            {
                if (!settings.RespectManual || state.Sources[slot] is not StateSource.Manual)
                    Editor.ChangeCrest(state, slot, mergedDesign.Design.DesignData.Crest(slot), Source(slot),
                        out _, settings.Key);
            }

            var customizeFlags = mergedDesign.Design.ApplyCustomizeRaw;
            if (mergedDesign.Design.DoApplyCustomize(CustomizeIndex.Clan))
                customizeFlags |= CustomizeFlag.Race;

            Func<CustomizeIndex, bool> applyWhich = settings.RespectManual
                ? i => customizeFlags.HasFlag(i.ToFlag()) && state.Sources[i] is not StateSource.Manual
                : i => customizeFlags.HasFlag(i.ToFlag());

            if (Editor.ChangeHumanCustomize(state, mergedDesign.Design.DesignData.Customize, applyWhich, i => Source(i), out _, out var changed,
                    settings.Key))
                requiresRedraw |= changed.RequiresRedraw();

            foreach (var parameter in mergedDesign.Design.ApplyParameters.Iterate())
            {
                if (settings.RespectManual && state.Sources[parameter] is StateSource.Manual or StateSource.Pending)
                    continue;

                var source = Source(parameter);
                if (source is StateSource.Manual)
                    source = StateSource.Pending;
                Editor.ChangeParameter(state, parameter, mergedDesign.Design.DesignData.Parameters[parameter], source, out _, settings.Key);
            }

            // Do not apply highlights from a design if highlights is unchecked.
            if (!state.ModelData.Customize.Highlights)
                Editor.ChangeParameter(state, CustomizeParameterFlag.HairHighlight,
                    state.ModelData.Parameters[CustomizeParameterFlag.HairDiffuse],
                    state.Sources[CustomizeParameterFlag.HairDiffuse], out _, settings.Key);

            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                if (mergedDesign.Design.DoApplyEquip(slot))
                    if (!settings.RespectManual || state.Sources[slot, false] is not StateSource.Manual)
                        Editor.ChangeItem(state, slot, mergedDesign.Design.DesignData.Item(slot),
                            Source(slot.ToState()), out _, settings.Key);

                if (mergedDesign.Design.DoApplyStain(slot))
                    if (!settings.RespectManual || state.Sources[slot, true] is not StateSource.Manual)
                        Editor.ChangeStain(state, slot, mergedDesign.Design.DesignData.Stain(slot),
                            Source(slot.ToState(true)), out _, settings.Key);
            }

            foreach (var weaponSlot in EquipSlotExtensions.WeaponSlots)
            {
                if (mergedDesign.Design.DoApplyStain(weaponSlot))
                    if (!settings.RespectManual || state.Sources[weaponSlot, true] is not StateSource.Manual)
                        Editor.ChangeStain(state, weaponSlot, mergedDesign.Design.DesignData.Stain(weaponSlot),
                            Source(weaponSlot.ToState(true)), out _, settings.Key);

                if (!mergedDesign.Design.DoApplyEquip(weaponSlot))
                    continue;

                if (settings.RespectManual && state.Sources[weaponSlot, false] is StateSource.Manual)
                    continue;

                var currentType = state.ModelData.Item(weaponSlot).Type;
                if (!settings.FromJobChange && mergedDesign.Weapons.TryGetValue(currentType, out var weapon))
                {
                    var source = settings.UseSingleSource ? settings.Source :
                        weapon.Item2 is StateSource.Game  ? StateSource.Game : weapon.Item2;
                    Editor.ChangeItem(state, weaponSlot, weapon.Item1, source, out _,
                        settings.Key);
                }
            }

            if (settings.FromJobChange)
                jobChange.Set(state, mergedDesign.Weapons.Values.Select(m =>
                    (m.Item1, settings.UseSingleSource ? settings.Source :
                        m.Item2 is StateSource.Game    ? StateSource.Game : m.Item2)));

            foreach (var meta in MetaExtensions.AllRelevant)
            {
                if (!settings.RespectManual || state.Sources[meta] is not StateSource.Manual)
                    Editor.ChangeMetaState(state, meta, mergedDesign.Design.DesignData.GetMeta(meta), Source(meta), out _, settings.Key);
            }
        }

        var actors = settings.Source is StateSource.Manual or StateSource.Ipc
            ? Applier.ApplyAll(state, requiresRedraw, false)
            : ActorData.Invalid;

        Glamourer.Log.Verbose(
            $"Applied design to {state.Identifier.Incognito(null)}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChanged.Type.Design, state.Sources[MetaIndex.Wetness], state, actors, mergedDesign.Design);

        return;

        StateSource Source(StateIndex index)
        {
            if (settings.UseSingleSource)
                return settings.Source;

            var source = mergedDesign.Sources[index];
            return source is StateSource.Game ? StateSource.Game : settings.Source;
        }
    }

    public void ApplyDesign(object data, DesignBase design, ApplySettings settings)
    {
        var merged = settings.MergeLinks && design is Design d
            ? merger.Merge(d.AllLinks, ((ActorState)data).ModelData, false, false)
            : new MergedDesign(design);

        ApplyDesign(data, merged, settings with
        {
            FromJobChange = false,
            RespectManual = false,
            UseSingleSource = true,
        });
    }


    /// <summary> Apply offhand item and potentially gauntlets if configured. </summary>
    private void ApplyMainhandPeriphery(ActorState state, EquipItem? newMainhand, ApplySettings settings)
    {
        if (!Config.ChangeEntireItem || settings.Source is not StateSource.Manual)
            return;

        var mh      = newMainhand ?? state.ModelData.Item(EquipSlot.MainHand);
        var offhand = newMainhand != null ? Items.GetDefaultOffhand(mh) : state.ModelData.Item(EquipSlot.OffHand);
        if (offhand.Valid)
            ChangeEquip(state, EquipSlot.OffHand, offhand, state.ModelData.Stain(EquipSlot.OffHand), settings);

        if (mh is { Type: FullEquipType.Fists } && Items.ItemData.Tertiary.TryGetValue(mh.ItemId, out var gauntlets))
            ChangeEquip(state, EquipSlot.Hands, newMainhand != null ? gauntlets : state.ModelData.Item(EquipSlot.Hands),
                state.ModelData.Stain(EquipSlot.Hands), settings);
    }
}
