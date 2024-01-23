using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Interop;
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
    Configuration _config)
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

    /// <inheritdoc cref="ChangeCustomize(ActorData, in CustomizeArray, ActorState?)"/>
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
                _updateSlot.UpdateSlot(actor.Model, slot, resolvedItem);
            }
            else
            {
                _updateSlot.UpdateSlot(actor.Model, slot, armor);
            }
        }
    }

    /// <inheritdoc cref="ChangeArmor(ActorData,EquipSlot,CharacterArmor,bool,bool)"/>
    public ActorData ChangeArmor(ActorState state, EquipSlot slot, bool apply)
    {
        // If the source is not IPC we do not want to apply restrictions.
        var data = GetData(state);
        if (apply)
            ChangeArmor(data, slot, state.ModelData.Armor(slot), state.Sources[slot, false] is not StateSource.Ipc,
                state.ModelData.IsHatVisible());

        return data;
    }


    /// <summary>
    /// Change the stain of a single piece of armor or weapon.
    /// If the offhand is empty, the stain will be fixed to 0 to prevent crashes.
    /// </summary>
    public void ChangeStain(ActorData data, EquipSlot slot, StainId stain)
    {
        var idx = slot.ToIndex();
        switch (idx)
        {
            case < 10:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _updateSlot.UpdateStain(actor.Model, slot, stain);
                break;
            case 10:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _weapon.LoadStain(actor, EquipSlot.MainHand, stain);
                break;
            case 11:
                foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
                    _weapon.LoadStain(actor, EquipSlot.OffHand, stain);
                break;
        }
    }

    /// <inheritdoc cref="ChangeStain(ActorData,EquipSlot,StainId)"/>
    public ActorData ChangeStain(ActorState state, EquipSlot slot, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeStain(data, slot, state.ModelData.Stain(slot));

        return data;
    }


    /// <summary> Apply a weapon to the appropriate slot. </summary>
    public void ChangeWeapon(ActorData data, EquipSlot slot, EquipItem item, StainId stain)
    {
        if (slot is EquipSlot.MainHand)
            ChangeMainhand(data, item, stain);
        else
            ChangeOffhand(data, item, stain);
    }

    /// <inheritdoc cref="ChangeWeapon(ActorData,EquipSlot,EquipItem,StainId)"/>
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
    public void ChangeMainhand(ActorData data, EquipItem weapon, StainId stain)
    {
        var slot = weapon.Type.ValidOffhand() == FullEquipType.Unknown ? EquipSlot.BothHand : EquipSlot.MainHand;
        foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
            _weapon.LoadWeapon(actor, slot, weapon.Weapon().With(stain));
    }

    /// <summary> Apply a weapon to the offhand. </summary>
    public void ChangeOffhand(ActorData data, EquipItem weapon, StainId stain)
    {
        stain = weapon.PrimaryId.Id == 0 ? 0 : stain;
        foreach (var actor in data.Objects.Where(a => a.Model.IsHuman))
            _weapon.LoadWeapon(actor, EquipSlot.OffHand, weapon.Weapon().With(stain));
    }

    /// <summary> Change a meta state. </summary>
    public unsafe void ChangeMetaState(ActorData data, MetaIndex index, bool value)
    {
        switch (index)
        {
            case MetaIndex.Wetness:
            {
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    actor.AsCharacter->IsGPoseWet = value;
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

    /// <inheritdoc cref="ChangeParameters(ActorData,CustomizeParameterFlag,in CustomizeParameterData)"/>
    public ActorData ChangeParameters(ActorState state, CustomizeParameterFlag flags, bool apply)
    {
        var data = GetData(state);
        if (apply)
            ChangeParameters(data, flags, state.ModelData.Parameters, state.IsLocked);
        return data;
    }

    private ActorData GetData(ActorState state)
    {
        _objects.Update();
        return _objects.TryGetValue(state.Identifier, out var data) ? data : ActorData.Invalid;
    }
}
