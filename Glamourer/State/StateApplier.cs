using Glamourer.Designs;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Interop.Material;
using Glamourer.Interop.Penumbra;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

/// <summary>
/// This class applies changes made to state to actual objects in the game.
/// It handles applying those changes as well as redrawing the actor if necessary.
/// </summary>
public class StateApplier(
    UpdateSlotService _updateSlot,
    VisorService _visor,
    WeaponService _weapon,
    ChangeCustomizeService _changeCustomize,
    ItemManager _items,
    PenumbraService _penumbra,
    MetaService _metaService,
    ObjectManager _objects,
    CrestService _crests,
    Configuration _config,
    DirectXService _directX)
{
    /// <summary> Simply force a redraw regardless of conditions. </summary>
    public void ForceRedraw(ActorData data)
    {
        foreach (var actor in data.Objects)
            _penumbra.RedrawObject(actor, RedrawType.Redraw);
    }

    /// <inheritdoc cref="ForceRedraw(ActorData)"/>
    public ActorData ForceRedraw(ActorState state, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ForceRedraw(data);

        return data;
    }

    /// <summary>
    /// Change the customization values of actors either by applying them via update or redrawing,
    /// this depends on whether the changes include changes to Race, Gender, Body Type or Face. 
    /// </summary>
    public unsafe void ChangeCustomize(ActorData data, in CustomizeArray customize, ActorState? _ = null)
    {
        foreach (var actor in data.Objects)
        {
            var mdl = actor.Model;
            if (!mdl.IsCharacterBase)
                continue;

            var flags = CustomizeArray.Compare(mdl.GetCustomize(), customize);
            if (!flags.RequiresRedraw() || !mdl.IsHuman)
            {
                _changeCustomize.UpdateCustomize(mdl, customize);
            }
            else if (data.Objects.Count > 1 && _objects.IsInGPose && !actor.IsGPoseOrCutscene)
            {
                var mdlCustomize = (CustomizeArray*)&mdl.AsHuman->Customize;
                *mdlCustomize = customize;
                _penumbra.RedrawObject(actor, RedrawType.AfterGPose);
            }
            else
            {
                _penumbra.RedrawObject(actor, RedrawType.Redraw);
            }
        }
    }

    /// <inheritdoc cref="ChangeCustomize(ActorData,in CustomizeArray,ActorState?)"/>
    public ActorData ChangeCustomize(ActorState state, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeCustomize(data, state.ModelData.Customize, state);

        return data;
    }

    /// <summary>
    /// Change a single piece of armor and/or stain depending on slot.
    /// This uses the current customization of the model to potentially prevent restricted gear types from appearing.
    /// This never requires redrawing.
    /// </summary>
    public void ChangeArmor(ActorData data, EquipSlot slot, CharacterArmor armor, bool checkRestrictions, bool isHatVisible = true)
    {
        if (slot is EquipSlot.Head && !isHatVisible)
            return;

        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl = actor.Model;
            if (!mdl.IsHuman)
                continue;

            if (checkRestrictions)
            {
                var customize = mdl.GetCustomize();
                var (_, resolvedItem) = _items.ResolveRestrictedGear(armor, slot, customize.Race, customize.Gender);
                _updateSlot.UpdateEquipSlot(actor.Model, slot, resolvedItem);
            }
            else
            {
                _updateSlot.UpdateEquipSlot(actor.Model, slot, armor);
            }
        }
    }

    /// <inheritdoc cref="ChangeArmor(ActorData,EquipSlot,CharacterArmor,bool,bool)"/>
    public ActorData ChangeArmor(ActorState state, EquipSlot slot, bool apply)
    {
        // If the source is not IPC we do not want to apply restrictions.
        var data = GetData(state);
        if (apply)
            ChangeArmor(data, slot, state.ModelData.Armor(slot), !state.Sources[slot, false].IsIpc(), state.ModelData.IsHatVisible());

        return data;
    }

    public void ChangeBonusItem(ActorData data, BonusItemFlag slot, PrimaryId id, Variant variant)
    {
        var item = new CharacterArmor(id, variant, StainIds.None);
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl = actor.Model;
            if (!mdl.IsHuman)
                continue;

            _updateSlot.UpdateBonusSlot(actor.Model, slot, item);
        }
    }

    /// <inheritdoc cref="ChangeBonusItem(ActorData,BonusItemFlag,PrimaryId,Variant)"/>
    public ActorData ChangeBonusItem(ActorState state, BonusItemFlag slot, bool apply)
    {
        // If the source is not IPC we do not want to apply restrictions.
        var data = GetData(state);
        if (apply)
        {
            var item = state.ModelData.BonusItem(slot);
            ChangeBonusItem(data, slot, item.PrimaryId, item.Variant);
        }

        return data;
    }


    /// <summary>
    /// Change the stain of a single piece of armor or weapon.
    /// If the offhand is empty, the stain will be fixed to 0 to prevent crashes.
    /// </summary>
    public void ChangeStain(ActorData data, EquipSlot slot, StainIds stains)
    {
        var idx = slot.ToIndex();
        switch (idx)
        {
            case < 10:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _updateSlot.UpdateStain(actor.Model, slot, stains);
                break;
            case 10:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _weapon.LoadStain(actor, EquipSlot.MainHand, stains);
                break;
            case 11:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _weapon.LoadStain(actor, EquipSlot.OffHand, stains);
                break;
        }
    }

    /// <inheritdoc cref="ChangeStain(ActorData,EquipSlot,StainIds)"/>
    public ActorData ChangeStain(ActorState state, EquipSlot slot, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeStain(data, slot, state.ModelData.Stain(slot));

        return data;
    }


    /// <summary> Apply a weapon to the appropriate slot. </summary>
    public void ChangeWeapon(ActorData data, EquipSlot slot, EquipItem item, StainIds stains)
    {
        if (slot is EquipSlot.MainHand)
            ChangeMainhand(data, item, stains);
        else
            ChangeOffhand(data, item, stains);
    }

    /// <inheritdoc cref="ChangeWeapon(ActorData,EquipSlot,EquipItem,StainIds)"/>
    public ActorData ChangeWeapon(ActorState state, EquipSlot slot, bool apply, bool onlyGPose)
    {
        var data = GetData(state);
        if (onlyGPose)
            data = data.OnlyGPose();

        if (apply)
            ChangeWeapon(data, slot, state.ModelData.Item(slot), state.ModelData.Stain(slot));

        return data;
    }

    /// <summary>
    /// Apply a weapon to the mainhand. If the weapon type has no associated offhand type, apply both.
    /// </summary>
    public void ChangeMainhand(ActorData data, EquipItem weapon, StainIds stains)
    {
        var slot = weapon.Type.ValidOffhand() == FullEquipType.Unknown ? EquipSlot.BothHand : EquipSlot.MainHand;
        foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
            _weapon.LoadWeapon(actor, slot, weapon.Weapon().With(stains));
    }

    /// <summary> Apply a weapon to the offhand. </summary>
    public void ChangeOffhand(ActorData data, EquipItem weapon, StainIds stains)
    {
        stains = weapon.PrimaryId.Id == 0 ? StainIds.None : stains;
        foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
            _weapon.LoadWeapon(actor, EquipSlot.OffHand, weapon.Weapon().With(stains));
    }

    /// <summary> Change a meta state. </summary>
    public void ChangeMetaState(ActorData data, MetaIndex index, bool value)
    {
        switch (index)
        {
            case MetaIndex.Wetness:
            {
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    actor.IsGPoseWet = value;
                return;
            }
            case MetaIndex.HatState:
            {
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    _metaService.SetHatState(actor, value);
                return;
            }
            case MetaIndex.WeaponState:
            {
                // Only apply to the GPose character because otherwise we get some weird incompatibility when leaving GPose.
                if (_objects.IsInGPose)
                    foreach (var actor in data.Objects.Where(a => a.IsGPoseOrCutscene))
                        _metaService.SetWeaponState(actor, value);
                else
                    foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                        _metaService.SetWeaponState(actor, value);
                return;
            }
            case MetaIndex.VisorState:
            {
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _visor.SetVisorState(actor.Model, value);
                return;
            }
        }
    }

    /// <inheritdoc cref="ChangeMetaState(ActorData, MetaIndex, bool)"/>
    public ActorData ChangeMetaState(ActorState state, MetaIndex index, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeMetaState(data, index, state.ModelData.GetMeta(index));
        return data;
    }

    /// <summary> Change the crest state on actors. </summary>
    public void ChangeCrests(ActorData data, CrestFlag flags)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            _crests.UpdateCrests(actor, flags);
    }

    /// <inheritdoc cref="ChangeCrests(ActorData, CrestFlag)"/>
    public ActorData ChangeCrests(ActorState state, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeCrests(data, state.ModelData.CrestVisibility);
        return data;
    }

    /// <summary> Change the customize parameters on models. Can change multiple at once. </summary>
    public void ChangeParameters(ActorData data, CustomizeParameterFlag flags, in CustomizeParameterData values, bool force)
    {
        if (!force && !_config.UseAdvancedParameters || flags == 0)
            return;

        foreach (var actor in data.Objects.Where(a => a is { IsCharacter: true, Model.IsHuman: true }))
            actor.Model.ApplyParameterData(flags, values);
    }

    /// <inheritdoc cref="ChangeParameters(ActorData,CustomizeParameterFlag,in CustomizeParameterData,bool)"/>
    public ActorData ChangeParameters(ActorState state, CustomizeParameterFlag flags, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeParameters(data, flags, state.ModelData.Parameters, state.IsLocked);
        return data;
    }

    public unsafe void ChangeMaterialValue(ActorData data, MaterialValueIndex index, ColorRow? value, bool force)
    {
        if (!force && !_config.UseAdvancedDyes)
            return;

        foreach (var actor in data.Objects.Where(a => a is { IsCharacter: true, Model.IsHuman: true }))
        {
            if (!index.TryGetTexture(actor, out var texture, out var mode))
                continue;

            if (!_directX.TryGetColorTable(*texture, out var table))
                continue;

            if (value.HasValue)
                value.Value.Apply(ref table[index.RowIndex], mode);
            else if (PrepareColorSet.TryGetColorTable(actor, index, out var baseTable, out _))
                table[index.RowIndex] = baseTable[index.RowIndex];
            else
                continue;

            _directX.ReplaceColorTable(texture, table);
        }
    }

    public ActorData ChangeMaterialValue(ActorState state, MaterialValueIndex index, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeMaterialValue(data, index, state.Materials.TryGetValue(index, out var v) ? v.Model : null, state.IsLocked);
        return data;
    }

    public unsafe void ChangeMaterialValues(ActorData data, in StateMaterialManager materials, bool force)
    {
        if (!force && !_config.UseAdvancedDyes)
            return;

        var groupedMaterialValues = materials.Values.Select(p => (MaterialValueIndex.FromKey(p.Key), p.Value))
            .GroupBy(p => (p.Item1.DrawObject, p.Item1.SlotIndex, p.Item1.MaterialIndex));

        foreach (var group in groupedMaterialValues)
        {
            var values  = group.ToList();
            var mainKey = values[0].Item1;
            foreach (var actor in data.Objects.Where(a => a is { IsCharacter: true, Model.IsHuman: true }))
            {
                if (!mainKey.TryGetTexture(actor, out var texture))
                    continue;

                if (!PrepareColorSet.TryGetColorTable(actor, mainKey, out var table, out var mode))
                    continue;

                foreach (var (key, value) in values)
                    value.Model.Apply(ref table[key.RowIndex], mode);

                _directX.ReplaceColorTable(texture, table);
            }
        }
    }

    /// <summary> Apply the entire state of an actor to all relevant actors, either via immediate redraw or piecewise. </summary>
    /// <param name="state"> The state to apply. </param>
    /// <param name="redraw"> Whether a redraw should be forced. </param>
    /// <param name="withLock"> Whether a temporary lock should be applied for the redraw. </param>
    /// <returns> The actor data for the actors who got changed. </returns>
    public ActorData ApplyAll(ActorState state, bool redraw, bool withLock)
    {
        var actors = ChangeMetaState(state, MetaIndex.Wetness, true);
        if (redraw)
        {
            if (withLock)
                state.TempLock();
            ForceRedraw(actors);
        }
        else
        {
            ChangeCustomize(actors, state.ModelData.Customize);
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
                ChangeArmor(actors, slot, state.ModelData.Armor(slot), !state.Sources[slot, false].IsIpc(), state.ModelData.IsHatVisible());
            foreach (var slot in BonusExtensions.AllFlags)
            {
                var item = state.ModelData.BonusItem(slot);
                ChangeBonusItem(actors, slot, item.PrimaryId, item.Variant);
            }

            var mainhandActors = state.ModelData.MainhandType != state.BaseData.MainhandType ? actors.OnlyGPose() : actors;
            ChangeMainhand(mainhandActors, state.ModelData.Item(EquipSlot.MainHand), state.ModelData.Stain(EquipSlot.MainHand));
            var offhandActors = state.ModelData.OffhandType != state.BaseData.OffhandType ? actors.OnlyGPose() : actors;
            ChangeOffhand(offhandActors, state.ModelData.Item(EquipSlot.OffHand), state.ModelData.Stain(EquipSlot.OffHand));

            if (state.ModelData.IsHuman)
            {
                ChangeMetaState(actors, MetaIndex.HatState,    state.ModelData.IsHatVisible());
                ChangeMetaState(actors, MetaIndex.WeaponState, state.ModelData.IsWeaponVisible());
                ChangeMetaState(actors, MetaIndex.VisorState,  state.ModelData.IsVisorToggled());
                ChangeCrests(actors, state.ModelData.CrestVisibility);
                ChangeParameters(actors, state.OnlyChangedParameters(), state.ModelData.Parameters, state.IsLocked);
                ChangeMaterialValues(actors, state.Materials, state.IsLocked);
            }
        }

        return actors;
    }

    private ActorData GetData(ActorState state)
    {
        _objects.Update();
        return _objects.TryGetValue(state.Identifier, out var data) ? data : ActorData.Invalid;
    }
}
