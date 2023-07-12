using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Glamourer.Interop.Structs;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using static FFXIVClientStructs.FFXIV.Client.UI.UIModule;

namespace Glamourer.Interop;

public unsafe class WeaponService : IDisposable
{
    private readonly WeaponLoading _event;

    public WeaponService(WeaponLoading @event)
    {
        _event = @event;
        SignatureHelper.Initialise(this);
        _loadWeaponHook = Hook<LoadWeaponDelegate>.FromAddress((nint)DrawDataContainer.MemberFunctionPointers.LoadWeapon, LoadWeaponDetour);
        _loadWeaponHook.Enable();
    }

    public void Dispose()
    {
        _loadWeaponHook.Dispose();
    }

    // Weapons for a specific character are reloaded with this function.
    // slot is 0 for main hand, 1 for offhand, 2 for combat effects.
    // weapon argument is the new weapon data.
    // redrawOnEquality controls whether the game does anything if the new weapon is identical to the old one.
    // skipGameObject seems to control whether the new weapons are written to the game object or just influence the draw object. (1 = skip, 0 = change)
    // unk4 seemed to be the same as unk1.
    private delegate void LoadWeaponDelegate(DrawDataContainer* drawData, uint slot, ulong weapon, byte redrawOnEquality, byte unk2,
        byte skipGameObject, byte unk4);

    private readonly Hook<LoadWeaponDelegate> _loadWeaponHook;


    private void LoadWeaponDetour(DrawDataContainer* drawData, uint slot, ulong weaponValue, byte redrawOnEquality, byte unk2, byte skipGameObject, byte unk4)
    {
        var actor  = (Actor)((nint*)drawData)[1];
        var weapon = new CharacterWeapon(weaponValue);
        var equipSlot = slot switch
        {
            0 => EquipSlot.MainHand,
            1 => EquipSlot.OffHand,
            _ => EquipSlot.Unknown,
        };

        var tmpWeapon = weapon;
        // First call the regular function.
        if (equipSlot is not EquipSlot.Unknown)
            _event.Invoke(actor, equipSlot, ref tmpWeapon);

        _loadWeaponHook.Original(drawData, slot, weapon.Value, redrawOnEquality, unk2, skipGameObject, unk4);
        if (tmpWeapon.Value != weapon.Value)
            _loadWeaponHook.Original(drawData, slot, tmpWeapon.Value, 1, unk2, 1, unk4);
        Glamourer.Log.Excessive(
            $"Weapon reloaded for 0x{actor.Address:X} ({actor.Utf8Name}) with attributes {slot} {weapon.Value:X14}, {redrawOnEquality}, {unk2}, {skipGameObject}, {unk4}");
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(Actor character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                _loadWeaponHook.Original(&character.AsCharacter->DrawData, 0, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.OffHand:
                _loadWeaponHook.Original(&character.AsCharacter->DrawData, 1, weapon.Value, 0, 0, 1, 0);
                return;
            case EquipSlot.BothHand:
                _loadWeaponHook.Original(&character.AsCharacter->DrawData, 0, weapon.Value,                0, 0, 1, 0);
                _loadWeaponHook.Original(&character.AsCharacter->DrawData, 1, CharacterWeapon.Empty.Value, 0, 0, 1, 0);
                return;
            // function can also be called with '2', but does not seem to ever be.
        }
    }

    public void LoadStain(Actor character, EquipSlot slot, StainId stain)
    {
        var mdl    = character.Model;
        var (_, _, mh, oh) = mdl.GetWeapons(character);
        var value  = slot == EquipSlot.OffHand ? oh : mh;
        var weapon = value.With(value.Set.Value == 0 ? 0 : stain);
        LoadWeapon(character, slot, weapon);
    }
}
