using Glamourer.Api.Enums;
using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Designs.Links;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop.Material;
using Glamourer.Interop.Penumbra;
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
    DesignMerger merger,
    ModSettingApplier modApplier,
    GPoseService gPose) : IDesignEditor
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

        var actors = Applier.ForceRedraw(state, source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set model id in state {state.Identifier.Incognito(null)} from {old} to {modelId}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Model, source, state, actors, null);
    }

    /// <inheritdoc/>
    public void ChangeCustomize(object data, CustomizeIndex idx, CustomizeValue value, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeCustomize(state, idx, value, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeCustomize(state, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {idx.ToDefaultName()} customizations in state {state.Identifier.Incognito(null)} from {old.Value} to {value.Value}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Customize, settings.Source, state, actors, new CustomizeTransaction(idx, old, value));
    }

    /// <inheritdoc/>
    public void ChangeEntireCustomize(object data, in CustomizeArray customizeInput, CustomizeFlag apply, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeHumanCustomize(state, customizeInput, apply, _ => settings.Source, out var old, out var applied, settings.Key))
            return;

        var actors = Applier.ChangeCustomize(state, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {applied} customizations in state {state.Identifier.Incognito(null)} from {old} to {customizeInput}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.EntireCustomize, settings.Source, state, actors,
            new EntireCustomizeTransaction(applied, old, customizeInput));
    }

    /// <inheritdoc/>
    public void ChangeItem(object data, EquipSlot slot, EquipItem item, ApplySettings settings = default)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeItem(state, slot, item, settings.Source, out var old, settings.Key))
            return;

        var type = slot.ToIndex() < 10 ? StateChangeType.Equip : StateChangeType.Weapon;
        var actors = type is StateChangeType.Equip
            ? Applier.ChangeArmor(state, slot, settings.Source.RequiresChange())
            : Applier.ChangeWeapon(state, slot, settings.Source.RequiresChange(),
                item.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));

        if (slot is EquipSlot.MainHand)
            ApplyMainhandPeriphery(state, item, null, settings);

        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item.Name} ({item.ItemId}). [Affecting {actors.ToLazyString("nothing")}.]");

        if (type is StateChangeType.Equip)
        {
            StateChanged.Invoke(type, settings.Source, state, actors, new EquipTransaction(slot, old, item));
        }
        else if (slot is EquipSlot.MainHand)
        {
            var oldOff       = state.ModelData.Item(EquipSlot.OffHand);
            var oldGauntlets = state.ModelData.Item(EquipSlot.Hands);
            StateChanged.Invoke(type, settings.Source, state, actors,
                new WeaponTransaction(old, oldOff, oldGauntlets, item, oldOff, oldGauntlets));
        }
        else
        {
            var oldMain      = state.ModelData.Item(EquipSlot.MainHand);
            var oldGauntlets = state.ModelData.Item(EquipSlot.Hands);
            StateChanged.Invoke(type, settings.Source, state, actors,
                new WeaponTransaction(oldMain, old, oldGauntlets, oldMain, item, oldGauntlets));
        }
    }

    public void ChangeBonusItem(object data, BonusItemFlag slot, BonusItem item, ApplySettings settings = default)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeBonusItem(state, slot, item, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeBonusItem(state, slot, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.Id}) to {item.Name} ({item.Id}). [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.BonusItem, settings.Source, state, actors, new BonusItemTransaction(slot, old, item));
    }

    /// <inheritdoc/>
    public void ChangeEquip(object data, EquipSlot slot, EquipItem? item, StainIds? stains, ApplySettings settings)
    {
        switch (item.HasValue, stains.HasValue)
        {
            case (false, false): return;
            case (true, false):
                ChangeItem(data, slot, item!.Value, settings);
                return;
            case (false, true):
                ChangeStains(data, slot, stains!.Value, settings);
                return;
        }

        var state = (ActorState)data;
        if (!Editor.ChangeEquip(state, slot, item ?? state.ModelData.Item(slot), stains ?? state.ModelData.Stain(slot), settings.Source,
                out var old, out var oldStains, settings.Key))
            return;

        var type = slot.ToIndex() < 10 ? StateChangeType.Equip : StateChangeType.Weapon;
        var actors = type is StateChangeType.Equip
            ? Applier.ChangeArmor(state, slot, settings.Source.RequiresChange())
            : Applier.ChangeWeapon(state, slot, settings.Source.RequiresChange(),
                item!.Value.Type != (slot is EquipSlot.MainHand ? state.BaseData.MainhandType : state.BaseData.OffhandType));

        if (slot is EquipSlot.MainHand)
            ApplyMainhandPeriphery(state, item, stains, settings);

        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} in state {state.Identifier.Incognito(null)} from {old.Name} ({old.ItemId}) to {item!.Value.Name} ({item.Value.ItemId}) and its stain from {oldStains} to {stains!.Value}. [Affecting {actors.ToLazyString("nothing")}.]");
        if (type is StateChangeType.Equip)
        {
            StateChanged.Invoke(type, settings.Source, state, actors, new EquipTransaction(slot, old, item!.Value));
        }
        else if (slot is EquipSlot.MainHand)
        {
            var oldOff       = state.ModelData.Item(EquipSlot.OffHand);
            var oldGauntlets = state.ModelData.Item(EquipSlot.Hands);
            StateChanged.Invoke(type, settings.Source, state, actors,
                new WeaponTransaction(old, oldOff, oldGauntlets, item!.Value, oldOff, oldGauntlets));
        }
        else
        {
            var oldMain      = state.ModelData.Item(EquipSlot.MainHand);
            var oldGauntlets = state.ModelData.Item(EquipSlot.Hands);
            StateChanged.Invoke(type, settings.Source, state, actors,
                new WeaponTransaction(oldMain, old, oldGauntlets, oldMain, item!.Value, oldGauntlets));
        }

        StateChanged.Invoke(StateChangeType.Stains, settings.Source, state, actors, new StainTransaction(slot, oldStains, stains!.Value));
    }

    /// <inheritdoc/>
    public void ChangeStains(object data, EquipSlot slot, StainIds stains, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeStains(state, slot, stains, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeStain(state, slot, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {slot.ToName()} stain in state {state.Identifier.Incognito(null)} from {old} to {stains}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Stains, settings.Source, state, actors, new StainTransaction(slot, old, stains));
    }

    /// <inheritdoc/>
    public void ChangeCrest(object data, CrestFlag slot, bool crest, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeCrest(state, slot, crest, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeCrests(state, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {slot.ToLabel()} crest in state {state.Identifier.Incognito(null)} from {old} to {crest}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Crest, settings.Source, state, actors, new CrestTransaction(slot, old, crest));
    }

    /// <inheritdoc/>
    public void ChangeCustomizeParameter(object data, CustomizeParameterFlag flag, CustomizeParameterValue value, ApplySettings settings)
    {
        var state = (ActorState)data;
        // Also apply main color to highlights when highlights is off.
        if (!state.ModelData.Customize.Highlights && flag is CustomizeParameterFlag.HairDiffuse)
            ChangeCustomizeParameter(state, CustomizeParameterFlag.HairHighlight, value, settings);

        if (!Editor.ChangeParameter(state, flag, value, settings.Source, out var old, settings.Key))
            return;

        var @new   = state.ModelData.Parameters[flag];
        var actors = Applier.ChangeParameters(state, flag, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set {flag} in state {state.Identifier.Incognito(null)} from {old} to {@new}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Parameter, settings.Source, state, actors, new ParameterTransaction(flag, old, @new));
    }

    public void ChangeMaterialValue(object data, MaterialValueIndex index, in MaterialValueState newValue, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeMaterialValue(state, index, newValue, settings.Source, out var oldValue, settings.Key))
            return;

        var actors = Applier.ChangeMaterialValue(state, index, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set material value in state {state.Identifier.Incognito(null)} from {oldValue} to {newValue.Game}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.MaterialValue, settings.Source, state, actors,
            new MaterialTransaction(index, oldValue, newValue.Game));
    }

    public void ResetMaterialValue(object data, MaterialValueIndex index, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ResetMaterialValue(state, index, settings.Key))
            return;

        var actors = Applier.ChangeMaterialValue(state, index, true);
        Glamourer.Log.Verbose(
            $"Reset material value in state {state.Identifier.Incognito(null)} to game value. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.MaterialValue, settings.Source, state, actors, new MaterialTransaction(index, null, null));
    }

    /// <inheritdoc/>
    public void ChangeMetaState(object data, MetaIndex index, bool value, ApplySettings settings)
    {
        var state = (ActorState)data;
        if (!Editor.ChangeMetaState(state, index, value, settings.Source, out var old, settings.Key))
            return;

        var actors = Applier.ChangeMetaState(state, index, settings.Source.RequiresChange());
        Glamourer.Log.Verbose(
            $"Set Head Gear Visibility in state {state.Identifier.Incognito(null)} from {old} to {value}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Other, settings.Source, state, actors, new MetaTransaction(index, old, value));
    }

    /// <inheritdoc/>
    public void ApplyDesign(object data, MergedDesign mergedDesign, ApplySettings settings)
    {
        var state = (ActorState)data;
        modApplier.HandleStateApplication(state, mergedDesign);
        if (!Editor.ChangeModelId(state, mergedDesign.Design.DesignData.ModelId, mergedDesign.Design.DesignData.Customize,
                mergedDesign.Design.GetDesignDataRef().GetEquipmentPtr(), settings.Source, out var oldModelId, settings.Key))
            return;

        var requiresRedraw = mergedDesign.ForcedRedraw
         || oldModelId != mergedDesign.Design.DesignData.ModelId
         || !mergedDesign.Design.DesignData.IsHuman;

        if (state.ModelData.IsHuman)
        {
            foreach (var slot in CrestExtensions.AllRelevantSet.Where(mergedDesign.Design.DoApplyCrest))
            {
                if (!settings.RespectManual || !state.Sources[slot].IsManual())
                    Editor.ChangeCrest(state, slot, mergedDesign.Design.DesignData.Crest(slot), Source(slot),
                        out _, settings.Key);
            }

            var customizeFlags = mergedDesign.Design.Application.Customize;
            if (mergedDesign.Design.DoApplyCustomize(CustomizeIndex.Clan))
                customizeFlags |= CustomizeFlag.Race;

            Func<CustomizeIndex, bool> applyWhich = settings.RespectManual
                ? i => customizeFlags.HasFlag(i.ToFlag()) && !state.Sources[i].IsManual()
                : i => customizeFlags.HasFlag(i.ToFlag());

            if (Editor.ChangeHumanCustomize(state, mergedDesign.Design.DesignData.Customize, applyWhich, i => Source(i), out _, out var changed,
                    settings.Key))
                requiresRedraw |= changed.RequiresRedraw();

            if (settings.ResetMaterials)
            {
                state.ModelData.Parameters = state.BaseData.Parameters;
                foreach (var parameter in CustomizeParameterExtensions.AllFlags)
                    state.Sources[parameter] = StateSource.Game;
            }

            foreach (var parameter in mergedDesign.Design.Application.Parameters.Iterate())
            {
                if (settings.RespectManual && state.Sources[parameter].IsManual())
                    continue;

                var source = Source(parameter).SetPending();
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
                    if (!settings.RespectManual || !state.Sources[slot, false].IsManual())
                        Editor.ChangeItem(state, slot, mergedDesign.Design.DesignData.Item(slot),
                            Source(slot.ToState()), out _, settings.Key);

                if (mergedDesign.Design.DoApplyStain(slot))
                    if (!settings.RespectManual || !state.Sources[slot, true].IsManual())
                        Editor.ChangeStains(state, slot, mergedDesign.Design.DesignData.Stain(slot),
                            Source(slot.ToState(true)), out _, settings.Key);
            }

            foreach (var slot in BonusExtensions.AllFlags)
            {
                if (mergedDesign.Design.DoApplyBonusItem(slot))
                    if (!settings.RespectManual || !state.Sources[slot].IsManual())
                        Editor.ChangeBonusItem(state, slot, mergedDesign.Design.DesignData.BonusItem(slot), Source(slot), out _, settings.Key);
            }

            foreach (var weaponSlot in EquipSlotExtensions.WeaponSlots)
            {
                if (mergedDesign.Design.DoApplyStain(weaponSlot))
                    if (!settings.RespectManual || !state.Sources[weaponSlot, true].IsManual())
                        Editor.ChangeStains(state, weaponSlot, mergedDesign.Design.DesignData.Stain(weaponSlot),
                            Source(weaponSlot.ToState(true)), out _, settings.Key);

                if (!mergedDesign.Design.DoApplyEquip(weaponSlot))
                    continue;

                if (settings.RespectManual && state.Sources[weaponSlot, false].IsManual())
                    continue;

                if (!settings.FromJobChange)
                {
                    if (gPose.InGPose)
                    {
                        Editor.ChangeItem(state, weaponSlot, mergedDesign.Design.DesignData.Item(weaponSlot),
                            settings.UseSingleSource ? settings.Source : mergedDesign.Sources[weaponSlot, false], out var old, settings.Key);
                        var oldSource = state.Sources[weaponSlot, false];
                        gPose.AddActionOnLeave(() =>
                        {
                            if (old.Type == state.BaseData.Item(weaponSlot).Type)
                                Editor.ChangeItem(state, weaponSlot, old, oldSource, out _, settings.Key);
                        });
                    }

                    var currentType = state.BaseData.Item(weaponSlot).Type;
                    if (mergedDesign.Weapons.TryGet(currentType, state.LastJob, out var weapon))
                    {
                        var source = settings.UseSingleSource ? settings.Source :
                            weapon.Item2 is StateSource.Game  ? StateSource.Game : settings.Source;
                        Editor.ChangeItem(state, weaponSlot, weapon.Item1, source, out _,
                            settings.Key);
                    }
                }
            }

            if (settings.FromJobChange)
                jobChange.Set(state, mergedDesign.Weapons.Values.Select(m =>
                    (m.Item1, settings.UseSingleSource ? settings.Source :
                        m.Item2 is StateSource.Game    ? StateSource.Game : settings.Source, m.Item3)));

            foreach (var meta in MetaExtensions.AllRelevant.Where(mergedDesign.Design.DoApplyMeta))
            {
                if (!settings.RespectManual || !state.Sources[meta].IsManual())
                    Editor.ChangeMetaState(state, meta, mergedDesign.Design.DesignData.GetMeta(meta), Source(meta), out _, settings.Key);
            }

            if (settings.ResetMaterials)
                state.Materials.Clear();

            foreach (var (key, value) in mergedDesign.Design.Materials)
            {
                if (!value.Enabled)
                    continue;

                var idx    = MaterialValueIndex.FromKey(key);
                var source = settings.Source.SetPending();
                if (state.Materials.TryGetValue(idx, out var materialState))
                {
                    if (settings.RespectManual && materialState.Source.IsManual())
                        continue;

                    if (value.Revert)
                        Editor.ChangeMaterialValue(state, idx, default, StateSource.Game, out _, settings.Key);
                    else
                        Editor.ChangeMaterialValue(state, idx,
                            new MaterialValueState(materialState.Game, value.Value, materialState.DrawData, source), settings.Source, out _,
                            settings.Key);
                }
                else if (!value.Revert)
                {
                    Editor.ChangeMaterialValue(state, idx, new MaterialValueState(ColorRow.Empty, value.Value, CharacterWeapon.Empty, source),
                        settings.Source, out _, settings.Key);
                }
            }
        }

        var actors = settings.Source.RequiresChange()
            ? Applier.ApplyAll(state, requiresRedraw, false)
            : ActorData.Invalid;

        Glamourer.Log.Verbose(
            $"Applied design to {state.Identifier.Incognito(null)}. [Affecting {actors.ToLazyString("nothing")}.]");
        StateChanged.Invoke(StateChangeType.Design, state.Sources[MetaIndex.Wetness], state, actors, null); // FIXME: maybe later

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
        var          state = (ActorState)data;
        MergedDesign merged;
        if (!settings.MergeLinks || design is not Design d)
            merged = new MergedDesign(design);
        else
            merged = merger.Merge(d.AllLinks, state.ModelData.IsHuman ? state.ModelData.Customize : CustomizeArray.Default, state.BaseData,
                false, Config.AlwaysApplyAssociatedMods);

        ApplyDesign(data, merged, settings with
        {
            FromJobChange = false,
            RespectManual = false,
            UseSingleSource = true,
        });
    }


    /// <summary> Apply offhand item and potentially gauntlets if configured. </summary>
    private void ApplyMainhandPeriphery(ActorState state, EquipItem? newMainhand, StainIds? newStains, ApplySettings settings)
    {
        if (!Config.ChangeEntireItem || !settings.Source.IsManual())
            return;

        var mh      = newMainhand ?? state.ModelData.Item(EquipSlot.MainHand);
        // Do not change Shields to nothing.
        if (mh.Type is FullEquipType.Sword)
            return;

        var offhand = newMainhand != null ? Items.GetDefaultOffhand(mh) : state.ModelData.Item(EquipSlot.OffHand);
        var stains  = newStains ?? state.ModelData.Stain(EquipSlot.MainHand);
        if (offhand.Valid)
            ChangeEquip(state, EquipSlot.OffHand, offhand, stains, settings);

        if (mh is { Type: FullEquipType.Fists } && Items.ItemData.Tertiary.TryGetValue(mh.ItemId, out var gauntlets))
            ChangeEquip(state, EquipSlot.Hands, newMainhand != null ? gauntlets : state.ModelData.Item(EquipSlot.Hands),
                stains,        settings);
    }
}
