using System.Linq;
using Glamourer.Customization;
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
public class StateEditor
{
    private readonly PenumbraService        _penumbra;
    private readonly UpdateSlotService      _updateSlot;
    private readonly VisorService           _visor;
    private readonly WeaponService          _weapon;
    private readonly ChangeCustomizeService _changeCustomize;
    private readonly ItemManager            _items;

    public StateEditor(UpdateSlotService updateSlot, VisorService visor, WeaponService weapon, ChangeCustomizeService changeCustomize,
        ItemManager items, PenumbraService penumbra)
    {
        _updateSlot      = updateSlot;
        _visor           = visor;
        _weapon          = weapon;
        _changeCustomize = changeCustomize;
        _items           = items;
        _penumbra        = penumbra;
    }

    /// <summary> Changing the model ID simply requires guaranteed redrawing. </summary>
    public void ChangeModelId(ActorData data, uint modelId)
    {
        foreach (var actor in data.Objects)
            _penumbra.RedrawObject(actor, RedrawType.Redraw);
    }

    /// <summary>
    /// Change the customization values of actors either by applying them via update or redrawing,
    /// this depends on whether the changes include changes to Race, Gender, Body Type or Face. 
    /// </summary>
    public void ChangeCustomize(ActorData data, Customize customize)
    {
        foreach (var actor in data.Objects)
        {
            var mdl = actor.Model;
            if (!mdl.IsHuman)
                continue;

            var flags = Customize.Compare(mdl.GetCustomize(), customize);
            if (!flags.RequiresRedraw())
                _changeCustomize.UpdateCustomize(mdl, customize.Data);
            else
                _penumbra.RedrawObject(actor, RedrawType.Redraw);
        }
    }

    /// <summary>
    /// Change a single piece of armor and/or stain depending on slot.
    /// This uses the current customization of the model to potentially prevent restricted gear types from appearing.
    /// This never requires redrawing.
    /// </summary>
    public void ChangeArmor(ActorData data, EquipSlot slot, CharacterArmor armor)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl       = actor.Model;
            var customize = mdl.IsHuman ? mdl.GetCustomize() : actor.GetCustomize();
            var (_, resolvedItem) = _items.ResolveRestrictedGear(armor, slot, customize.Race, customize.Gender);
            _updateSlot.UpdateSlot(actor.Model, slot, resolvedItem);
        }
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
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    _updateSlot.UpdateStain(actor.Model, slot, stain);
                break;
            case 10:
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    _weapon.LoadStain(actor, EquipSlot.MainHand, stain);
                break;
            case 11:
                foreach (var actor in data.Objects.Where(a => a.IsCharacter))
                    _weapon.LoadStain(actor, EquipSlot.OffHand, stain);
                break;
        }
    }

    /// <summary> Apply a weapon to the appropriate slot. </summary>
    public void ChangeWeapon(ActorData data, EquipSlot slot, EquipItem item, StainId stain)
    {
        if (slot is EquipSlot.MainHand)
            ChangeMainhand(data, item, stain);
        else
            ChangeOffhand(data, item, stain);
    }

    /// <summary>
    /// Apply a weapon to the mainhand. If the weapon type has no associated offhand type, apply both.
    /// </summary>
    public void ChangeMainhand(ActorData data, EquipItem weapon, StainId stain)
    {
        var slot = weapon.Type.Offhand() == FullEquipType.Unknown ? EquipSlot.BothHand : EquipSlot.MainHand;
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            _weapon.LoadWeapon(actor, slot, weapon.Weapon().With(stain));
    }

    /// <summary> Apply a weapon to the offhand. </summary>
    public void ChangeOffhand(ActorData data, EquipItem weapon, StainId stain)
    {
        stain = weapon.ModelId.Value == 0 ? 0 : stain;
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            _weapon.LoadWeapon(actor, EquipSlot.OffHand, weapon.Weapon().With(stain));
    }

    /// <summary> Change the visor state of actors only on the draw object. </summary>
    public void ChangeVisor(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl = actor.Model;
            if (!mdl.IsHuman)
                continue;

            _visor.SetVisorState(mdl, value);
        }
    }

    /// <summary> Change the forced wetness state on actors. </summary>
    public unsafe void ChangeWetness(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->IsGPoseWet = value;
    }

    /// <summary> Change the hat-visibility state on actors. </summary>
    public unsafe void ChangeHatState(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->DrawData.HideHeadgear(0, !value);
    }

    /// <summary> Change the weapon-visibility state on actors. </summary>
    public unsafe void ChangeWeaponState(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->DrawData.HideWeapons(!value);
    }
}
