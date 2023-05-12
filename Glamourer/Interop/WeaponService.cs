using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class WeaponService : IDisposable
{
    public WeaponService()
    {
        SignatureHelper.Initialise(this);
        _loadWeaponHook.Enable();
    }

    public void Dispose()
    {
        _loadWeaponHook.Dispose();
    }

    public static readonly int CharacterWeaponOffset = (int)Marshal.OffsetOf<Character>("DrawData");

    public delegate void LoadWeaponDelegate(nint offsetCharacter, uint slot, ulong weapon, byte redrawOnEquality, byte unk2,
        byte skipGameObject,
        byte unk4);

    // Weapons for a specific character are reloaded with this function.
    // The first argument is a pointer to the game object but shifted a bit inside.
    // slot is 0 for main hand, 1 for offhand, 2 for unknown (always called with empty data.
    // weapon argument is the new weapon data.
    // redrawOnEquality controls whether the game does anything if the new weapon is identical to the old one.
    // skipGameObject seems to control whether the new weapons are written to the game object or just influence the draw object. (1 = skip, 0 = change)
    // unk4 seemed to be the same as unk1.
    [Signature(Penumbra.GameData.Sigs.WeaponReload, DetourName = nameof(LoadWeaponDetour))]
    private readonly Hook<LoadWeaponDelegate> _loadWeaponHook = null!;

    private void LoadWeaponDetour(nint characterOffset, uint slot, ulong weapon, byte redrawOnEquality, byte unk2, byte skipGameObject,
        byte unk4)
    {
        //var oldWeapon = weapon;
        //var character = (Actor)(characterOffset - CharacterWeaponOffset);
        //try
        //{
        //    var identifier = character.GetIdentifier(_actors.AwaitedService);
        //    if (_fixedDesignManager.TryGetDesign(identifier, out var save))
        //    {
        //        PluginLog.Information($"Loaded weapon from fixed design for {identifier}.");
        //        weapon = slot switch
        //        {
        //            0 => save.WeaponMain.Model.Value,
        //            1 => save.WeaponOff.Model.Value,
        //            _ => weapon,
        //        };
        //    }
        //    else if (redrawOnEquality == 1 && _stateManager.TryGetValue(identifier, out var save2))
        //    {
        //        PluginLog.Information($"Loaded weapon from current design for {identifier}.");
        //        //switch (slot)
        //        //{
        //        //    case 0:
        //        //        save2.MainHand = new CharacterWeapon(weapon);
        //        //        break;
        //        //    case 1:
        //        //        save2.Data.OffHand = new CharacterWeapon(weapon);
        //        //        break;
        //        //}
        //    }
        //}
        //catch (Exception e)
        //{
        //    PluginLog.Error($"Error on loading new weapon:\n{e}");
        //}

        // First call the regular function.
        _loadWeaponHook.Original(characterOffset, slot, weapon, redrawOnEquality, unk2, skipGameObject, unk4);
        Glamourer.Log.Excessive($"Weapon reloaded for {(Actor)(characterOffset - CharacterWeaponOffset)} with attributes {slot} {weapon:X14}, {redrawOnEquality}, {unk2}, {skipGameObject}, {unk4}");
        // // If something changed the weapon, call it again with the actual change, not forcing redraws and skipping applying it to the game object.
        // if (oldWeapon != weapon)
        //     _loadWeaponHook.Original(characterOffset, slot, weapon, 0 /* redraw */, unk2, 1 /* skip */, unk4);
        // // If we're not actively changing the offhand and the game object has no offhand, redraw an empty offhand to fix animation problems.
        // else if (slot != 1 && character.OffHand.Value == 0)
        //     _loadWeaponHook.Original(characterOffset, 1, 0, 1 /* redraw */, unk2, 1 /* skip */, unk4);
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(Actor character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                LoadWeaponDetour(character.Address + CharacterWeaponOffset, 0, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.OffHand:
                LoadWeaponDetour(character.Address + CharacterWeaponOffset, 1, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.BothHand:
                LoadWeaponDetour(character.Address + CharacterWeaponOffset, 0, weapon.Value,                0, 0, 1, 0);
                LoadWeaponDetour(character.Address + CharacterWeaponOffset, 1, CharacterWeapon.Empty.Value, 0, 0, 1, 0);
                return;
            // function can also be called with '2', but does not seem to ever be.
        }
    }

    // Load specific Main- and Offhand weapons.
    public void LoadWeapon(Actor character, CharacterWeapon main, CharacterWeapon off)
    {
        LoadWeaponDetour(character.Address + CharacterWeaponOffset, 0, main.Value, 1, 0, 1, 0);
        LoadWeaponDetour(character.Address + CharacterWeaponOffset, 1, off.Value,  1, 0, 1, 0);
    }

    public void LoadStain(Actor character, EquipSlot slot, StainId stain)
    {
        var weapon = slot == EquipSlot.OffHand ? character.OffHand : character.MainHand;
        weapon.Stain = stain;
        LoadWeapon(character, slot, weapon);
    }
}
