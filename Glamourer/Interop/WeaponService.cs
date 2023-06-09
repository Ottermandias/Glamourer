using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Interop.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class WeaponService : IDisposable
{
    public WeaponService()
    {
        SignatureHelper.Initialise(this);
        _loadWeaponHook = Hook<LoadWeaponDelegate>.FromAddress((nint) DrawDataContainer.MemberFunctionPointers.LoadWeapon, LoadWeaponDetour);
        _loadWeaponHook.Enable();
    }

    public void Dispose()
    {
        _loadWeaponHook.Dispose();
    }

    private delegate void LoadWeaponDelegate(DrawDataContainer* drawData, uint slot, ulong weapon, byte redrawOnEquality, byte unk2, byte skipGameObject, byte unk4);

    private readonly Hook<LoadWeaponDelegate> _loadWeaponHook;

    private void LoadWeaponDetour(DrawDataContainer* drawData, uint slot, ulong weapon, byte redrawOnEquality, byte unk2, byte skipGameObject,
        byte unk4)
    {
        var actor = (Actor) (nint)drawData->Unk8;
       
        // First call the regular function.
        _loadWeaponHook.Original(drawData, slot, weapon, redrawOnEquality, unk2, skipGameObject, unk4);
        Item.Log.Information($"Weapon reloaded for 0x{actor.Address:X} with attributes {slot} {weapon:X14}, {redrawOnEquality}, {unk2}, {skipGameObject}, {unk4}");
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(Actor character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                LoadWeaponDetour(&character.AsCharacter->DrawData, 0, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.OffHand:
                LoadWeaponDetour(&character.AsCharacter->DrawData, 1, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.BothHand:
                LoadWeaponDetour(&character.AsCharacter->DrawData, 0, weapon.Value,                0, 0, 1, 0);
                LoadWeaponDetour(&character.AsCharacter->DrawData, 1, CharacterWeapon.Empty.Value, 0, 0, 1, 0);
                return;
            // function can also be called with '2', but does not seem to ever be.
        }
    }

    // Load specific Main- and Offhand weapons.
    public void LoadWeapon(Actor character, CharacterWeapon main, CharacterWeapon off)
    {
        LoadWeaponDetour(&character.AsCharacter->DrawData, 0, main.Value, 1, 0, 1, 0);
        LoadWeaponDetour(&character.AsCharacter->DrawData, 1, off.Value,  1, 0, 1, 0);
    }

    public void LoadStain(Actor character, EquipSlot slot, StainId stain)
    {
        var value  = slot == EquipSlot.OffHand ? character.AsCharacter->DrawData.OffHandModel : character.AsCharacter->DrawData.MainHandModel;
        var weapon = new CharacterWeapon(value.Value) { Stain = stain.Value };
        LoadWeapon(character, slot, weapon);
    }
}
