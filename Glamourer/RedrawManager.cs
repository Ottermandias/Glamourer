using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer;

public class CurrentManipulations
{
    private readonly RestrictedGear                              _restrictedGear = GameData.RestrictedGear(Dalamud.GameData);
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

    public bool GetSave(Actor actor, [NotNullWhen(true)] out CharacterSave? save)
    {
        save = null;
        return actor && _characterSaves.TryGetValue(actor.GetIdentifier(), out save);
    }

    public bool GetSave(Actor.IIdentifier identifier, [NotNullWhen(true)] out CharacterSave? save)
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

public unsafe partial class RedrawManager
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, CharacterArmor* data);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A", DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegate>? _flagSlotForUpdateHook;

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slot, CharacterArmor* data)
    {
        //try
        //{
        //    var actor = Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
        //    if (actor && CurrentManipulations.GetSave(actor, out _))
        //        // TODO fixed design
        //
        //        *data = CurrentManipulations.ChangeEquip(actor, slot.ToEquipSlot(), *data) ?? *data;
        //}
        //catch
        //{
        //    // ignored
        //}

        return _flagSlotForUpdateHook!.Original(drawObject, slot, data);
    }

    public bool ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
    {
        if (actor && CurrentManipulations.ChangeEquip(actor, slot, data).HasValue && actor.DrawObject != null)
            return _flagSlotForUpdateHook?.Original(actor.DrawObject, slot.ToIndex(), &data) != 0;

        return false;
    }
}

public unsafe partial class RedrawManager
{
    // The character weapon object manipulated is inside the actual character.
    public const int CharacterWeaponOffset = 0xD8 * 8;

    public delegate void LoadWeaponDelegate(IntPtr offsetCharacter, uint slot, CharacterWeapon weapon, byte unk1, byte unk2, byte unk3,
        byte unk4);

    // Weapons for a specific character are reloaded with this function.
    // The first argument is a pointer to the game object but shifted a bit inside.
    // slot is 0 for main hand, 1 for offhand, 2 for unknown (always called with empty data.
    // weapon argument is the new weapon data.
    // unk1 seems to be 0 when re-equipping and 1 when redrawing the entire actor.
    // unk2 seemed to always be 1.
    // unk3 seemed to always be 0.
    // unk4 seemed to be the same as unk1.
    [Signature("E8 ?? ?? ?? ?? 44 8B 9F", DetourName = nameof(LoadWeaponDetour))]
    private readonly Hook<LoadWeaponDelegate>? _loadWeaponHook;

    private void LoadWeaponDetour(IntPtr characterOffset, uint slot, CharacterWeapon weapon, byte unk1, byte unk2, byte unk3, byte unk4)
    {
        _loadWeaponHook!.Original(characterOffset, slot, weapon, unk1, unk2, unk3, unk4);
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(IntPtr character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 0, weapon, 1, 1, 0, 1);
                return;
            case EquipSlot.OffHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 1, weapon, 1, 1, 0, 1);
                return;
            case EquipSlot.BothHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 0, weapon,                1, 1, 0, 1);
                LoadWeaponDetour(character + CharacterWeaponOffset, 1, CharacterWeapon.Empty, 1, 1, 0, 1);
                return;
            // function can also be called with '2', but does not seem to ever be.
        }
    }

    public void LoadWeapon(Character* character, EquipSlot slot, CharacterWeapon weapon)
        => LoadWeapon((IntPtr)character, slot, weapon);

    // Load specific Main- and Offhand weapons.
    public void LoadWeapon(IntPtr character, CharacterWeapon main, CharacterWeapon off)
    {
        LoadWeaponDetour(character + CharacterWeaponOffset, 0, main, 1, 1, 0, 1);
        LoadWeaponDetour(character + CharacterWeaponOffset, 1, off,  1, 1, 0, 1);
    }

    public void LoadWeapon(Character* character, CharacterWeapon main, CharacterWeapon off)
        => LoadWeapon((IntPtr)character, main, off);
}

public unsafe partial class RedrawManager : IDisposable
{
    internal readonly CurrentManipulations CurrentManipulations = new();

    public RedrawManager()
    {
        SignatureHelper.Initialise(this);
        Glamourer.Penumbra.CreatingCharacterBase += OnCharacterRedraw;
        //_flagSlotForUpdateHook?.Enable();
        //_loadWeaponHook?.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook?.Dispose();
        _loadWeaponHook?.Dispose();
        Glamourer.Penumbra.CreatingCharacterBase -= OnCharacterRedraw;
    }

    private void OnCharacterRedraw(IntPtr addr, IntPtr modelId, IntPtr customize, IntPtr equipData)
    {
        //if (CurrentManipulations.GetSave(addr, out var save))
        //{
        //    *(CustomizationData*)customize = *(CustomizationData*)save.Customization.Address;
        //    var equip    = (CharacterEquip)equipData;
        //    var newEquip = save.Equipment;
        //    for (var i = 0; i < 10; ++i)
        //        equip[i] = newEquip[i];
        //}
    }
}