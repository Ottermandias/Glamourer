using System.Linq;
using Glamourer.Customization;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

public class StateEditor
{
    private readonly UpdateSlotService      _updateSlot;
    private readonly VisorService           _visor;
    private readonly WeaponService          _weapon;
    private readonly ChangeCustomizeService _changeCustomize;
    private readonly ItemManager            _items;

    public StateEditor(UpdateSlotService updateSlot, VisorService visor, WeaponService weapon, ChangeCustomizeService changeCustomize,
        ItemManager items)
    {
        _updateSlot      = updateSlot;
        _visor           = visor;
        _weapon          = weapon;
        _changeCustomize = changeCustomize;
        _items           = items;
    }


    public void ChangeCustomize(ActorData data, Customize customize)
    {
        foreach (var actor in data.Objects)
            _changeCustomize.UpdateCustomize(actor, customize.Data);
    }

    public void ChangeCustomize(ActorData data, CustomizeIndex idx, CustomizeValue value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl       = actor.Model;
            var customize = mdl.GetCustomize();
            customize[idx] = value;
            _changeCustomize.UpdateCustomize(mdl, customize.Data);
        }
    }

    public void ChangeArmor(ActorState state, ActorData data, EquipSlot slot)
    {
        var armor = state.ModelData.Armor(slot);
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
        {
            var mdl       = actor.Model;
            var customize = mdl.IsHuman ? mdl.GetCustomize() : actor.GetCustomize();
            var (_, resolvedItem) = _items.RestrictedGear.ResolveRestricted(armor, slot, customize.Race, customize.Gender);
            _updateSlot.UpdateSlot(actor.Model, slot, resolvedItem);
        }
    }

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

    public void ChangeMainhand(ActorData data, EquipItem weapon)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            _weapon.LoadWeapon(actor, EquipSlot.MainHand, weapon.Weapon());
    }

    public void ChangeOffhand(ActorData data, EquipItem weapon)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            _weapon.LoadWeapon(actor, EquipSlot.OffHand, weapon.Weapon());
    }

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

    public unsafe void ChangeWetness(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->IsGPoseWet = value;
    }

    public unsafe void ChangeHatState(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->DrawData.HideHeadgear(0, !value);
    }

    public unsafe void ChangeWeaponState(ActorData data, bool value)
    {
        foreach (var actor in data.Objects.Where(a => a.IsCharacter))
            actor.AsCharacter->DrawData.HideWeapons(!value);
    }
}
