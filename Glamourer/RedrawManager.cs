using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using static Glamourer.Actor;

namespace Glamourer;

public unsafe partial class RedrawManager
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, CharacterArmor* data);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A", DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegate>? _flagSlotForUpdateHook;

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slot, CharacterArmor* data)
    {
        try
        {
            var actor      = Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
            var identifier = actor.GetIdentifier();

            if (_fixedDesigns.TryGetDesign(identifier, out var save))
                PluginLog.Information($"Loaded {slot.ToEquipSlot()} from fixed design for {identifier}.");
            else if (_currentManipulations.TryGetDesign(identifier, out save))
                PluginLog.Information($"Updated {slot.ToEquipSlot()} from current designs for {identifier}.");
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on loading new gear:\n{e}");
        }

        return _flagSlotForUpdateHook!.Original(drawObject, slot, data);
    }

    public bool ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
    {
        if (actor && actor.DrawObject != null)
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
        try
        {
            var character  = (Actor)(characterOffset - CharacterWeaponOffset);
            var identifier = character.GetIdentifier();
            if (_fixedDesigns.TryGetDesign(identifier, out var save))
                PluginLog.Information($"Loaded weapon from fixed design for {identifier}.");
            else if (unk1 == 1 && _currentManipulations.TryGetDesign(identifier, out save))
                PluginLog.Information($"Loaded weapon from current design for {identifier}.");
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on loading new weapon:\n{e}");
        }

        _loadWeaponHook!.Original(characterOffset, slot, weapon, unk1, unk2, unk3, unk4);
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(IntPtr character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 0, weapon, 0, 1, 0, 0);
                return;
            case EquipSlot.OffHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 1, weapon, 0, 1, 0, 0);
                return;
            case EquipSlot.BothHand:
                LoadWeaponDetour(character + CharacterWeaponOffset, 0, weapon,                0, 1, 0, 0);
                LoadWeaponDetour(character + CharacterWeaponOffset, 1, CharacterWeapon.Empty, 0, 1, 0, 0);
                return;
            // function can also be called with '2', but does not seem to ever be.
        }
    }

    public void LoadWeapon(Character* character, EquipSlot slot, CharacterWeapon weapon)
        => LoadWeapon((IntPtr)character, slot, weapon);

    // Load specific Main- and Offhand weapons.
    public void LoadWeapon(IntPtr character, CharacterWeapon main, CharacterWeapon off)
    {
        LoadWeaponDetour(character + CharacterWeaponOffset, 0, main, 0, 1, 0, 0);
        LoadWeaponDetour(character + CharacterWeaponOffset, 1, off,  0, 1, 0, 0);
    }

    public void LoadWeapon(Character* character, CharacterWeapon main, CharacterWeapon off)
        => LoadWeapon((IntPtr)character, main, off);
}

public unsafe partial class RedrawManager : IDisposable
{
    internal readonly CurrentManipulations CurrentManipulations = new();

    private readonly FixedDesigns         _fixedDesigns;
    private readonly CurrentManipulations _currentManipulations;

    public RedrawManager(FixedDesigns fixedDesigns, CurrentManipulations currentManipulations)
    {
        SignatureHelper.Initialise(this);
        Glamourer.Penumbra.CreatingCharacterBase += OnCharacterRedraw;
        _fixedDesigns                            =  fixedDesigns;
        _currentManipulations                    =  currentManipulations;
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
        try
        {
            var actor      = (Actor)addr;
            var identifier = actor.GetIdentifier();

            if (_currentManipulations.TryGetDesign(identifier, out var save))
                PluginLog.Information($"Loaded current design for {identifier}.");
            else if (_fixedDesigns.TryGetDesign(identifier, out save))
                PluginLog.Information($"Loaded fixed design for {identifier}.");
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on new draw object creation:\n{e}");
        }
    }
}
