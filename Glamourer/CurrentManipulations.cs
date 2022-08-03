using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer;

public class CurrentManipulations
{
    private readonly RestrictedGear                               _restrictedGear = GameData.RestrictedGear(Dalamud.GameData);
    private readonly Dictionary<Actor.IIdentifier, CharacterSave> _characterSaves = new();

    public CharacterSave CreateSave(Actor actor)
    {
        var id = actor.GetIdentifier();
        if (_characterSaves.TryGetValue(id, out var save))
            return save;

        save = new CharacterSave(actor);
        _characterSaves.Add(id.CreatePermanent(), save);
        return save;
    }

    public bool TryGetDesign(Actor.IIdentifier identifier, [NotNullWhen(true)] out CharacterSave? save)
        => _characterSaves.TryGetValue(identifier, out save);

    public CharacterArmor? ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
    {
        var save = CreateSave(actor);
        (_, data) = _restrictedGear.ResolveRestricted(data, slot, save.Customize.Race, save.Customize.Gender);
        if (save.Equipment[slot] == data)
            return null;

        save.Equipment[slot] = data;
        return data;
    }

    public bool ChangeWeapon(Actor actor, CharacterWeapon main)
    {
        var save = CreateSave(actor);
        if (save.MainHand == main)
            return false;

        save.MainHand = main;
        return true;
    }

    public bool ChangeWeapon(Actor actor, CharacterWeapon main, CharacterWeapon off)
    {
        var save = CreateSave(actor);
        if (main == save.MainHand && off == save.OffHand)
            return false;

        save.MainHand = main;
        save.OffHand  = off;
        return true;
    }

    public void ChangeCustomization(Actor actor, Customize customize)
    {
        var save = CreateSave(actor);
        FixRestrictedGear(save, customize.Gender, customize.Race);
        save.Customize.Load(customize);
    }

    public bool ChangeCustomization(Actor actor, CustomizationId id, byte value)
    {
        if (id == CustomizationId.Race)
            return ChangeRace(actor, (SubRace)value);
        if (id == CustomizationId.Gender)
            return ChangeGender(actor, (Gender)value);

        var save      = CreateSave(actor);
        var customize = save.Customize;
        if (customize[id] != value)
            return false;

        customize[id] = value;
        return true;
    }

    // Change a gender and fix up all required customizations afterwards.
    public bool ChangeGender(Actor actor, Gender gender)
    {
        var save = CreateSave(actor);
        if (save.Customize.Gender == gender)
            return false;

        var customize = save.Customize;
        FixRestrictedGear(save, gender, customize.Race);
        FixUpAttributes(customize);
        return true;
    }

    // Change a race and fix up all required customizations afterwards.
    public bool ChangeRace(Actor actor, SubRace clan)
    {
        var save = CreateSave(actor);
        if (save.Customize.Clan == clan)
            return false;

        var customize = save.Customize;
        var race      = clan.ToRace();
        var gender    = race == Race.Hrothgar ? Gender.Male : customize.Gender; // TODO Female Hrothgar
        FixRestrictedGear(save, gender, race);
        customize.Gender = gender;
        customize.Race   = race;
        customize.Clan   = clan;

        FixUpAttributes(customize);
        return true;
    }

    // Go through a whole customization struct and fix up all settings that need fixing.
    private void FixUpAttributes(Customize customize)
    {
        var set = Glamourer.Customization.GetList(customize.Clan, customize.Gender);
        foreach (CustomizationId id in Enum.GetValues(typeof(CustomizationId)))
        {
            switch (id)
            {
                case CustomizationId.Race:                  break;
                case CustomizationId.Clan:                  break;
                case CustomizationId.BodyType:              break;
                case CustomizationId.Gender:                break;
                case CustomizationId.FacialFeaturesTattoos: break;
                case CustomizationId.HighlightsOnFlag:      break;
                case CustomizationId.Face:                  break;
                default:
                    var count = set.Count(id);
                    if (set.DataByValue(id, customize[id], out _) < 0)
                        customize[id] = count == 0 ? (byte)0 : set.Data(id, 0).Value;
                    break;
            }
        }
    }

    private void FixRestrictedGear(CharacterSave save, Gender gender, Race race)
    {
        if (race == save.Customize.Race && gender == save.Customize.Gender)
            return;

        var equip = save.Equipment;
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
            (_, equip[slot]) = _restrictedGear.ResolveRestricted(equip[slot], slot, race, gender);
    }
}
