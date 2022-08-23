using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.State;
using Glamourer.Structs;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.Interop;

public unsafe partial class RedrawManager
{
    private delegate void ChangeJobDelegate(IntPtr data, uint job);

    [Signature("88 51 ?? 44 3B CA", DetourName = nameof(ChangeJobDetour))]
    private readonly Hook<ChangeJobDelegate> _changeJobHook = null!;

    private void ChangeJobDetour(IntPtr data, uint job)
    {
        _changeJobHook.Original(data, job);
        JobChanged?.Invoke(data - 0x1A8, GameData.Jobs(Dalamud.GameData)[(byte)job]);
    }

    public event Action<Actor, Job>? JobChanged;
}

public unsafe partial class RedrawManager
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, CharacterArmor* data);

    // This gets called when one of the ten equip items of an existing draw object gets changed.
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A", DetourName = nameof(FlagSlotForUpdateDetour))]
    private readonly Hook<FlagSlotForUpdateDelegate> _flagSlotForUpdateHook = null!;

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slotIdx, CharacterArmor* data)
    {
        var slot = slotIdx.ToEquipSlot();
        try
        {
            var actor      = Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
            var identifier = actor.GetIdentifier();

            if (_fixedDesigns.TryGetDesign(identifier, out var save))
            {
                PluginLog.Information($"Loaded {slot} from fixed design for {identifier}.");
                (var replaced, *data) =
                    Glamourer.RestrictedGear.ResolveRestricted(save.Equipment[slot], slot, (Race)drawObject->Race, (Gender)drawObject->Sex);
            }
            else if (_currentManipulations.TryGetDesign(identifier, out var save2))
            {
                PluginLog.Information($"Updated {slot} from current designs for {identifier}.");
                (var replaced, *data) =
                    Glamourer.RestrictedGear.ResolveRestricted(*data, slot, (Race)drawObject->Race, (Gender)drawObject->Sex);
                save2.Data.Equipment[slot] = *data;
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on loading new gear:\n{e}");
        }

        return _flagSlotForUpdateHook.Original(drawObject, slotIdx, data);
    }


    public bool ChangeEquip(DrawObject drawObject, EquipSlot slot, CharacterArmor data)
    {
        if (!drawObject)
            return false;

        var slotIndex = slot.ToIndex();
        if (slotIndex > 9)
            return false;

        return FlagSlotForUpdateDetour(drawObject.Pointer, slotIndex, &data) != 0;
    }

    public bool ChangeEquip(Actor actor, EquipSlot slot, CharacterArmor data)
        => actor && ChangeEquip(actor.DrawObject, slot, data);
}

public unsafe partial class RedrawManager
{
    // The character weapon object manipulated is inside the actual character.
    public const int CharacterWeaponOffset = 0x6C0;

    public delegate void LoadWeaponDelegate(IntPtr offsetCharacter, uint slot, ulong weapon, byte redrawOnEquality, byte unk2,
        byte skipGameObject,
        byte unk4);

    // Weapons for a specific character are reloaded with this function.
    // The first argument is a pointer to the game object but shifted a bit inside.
    // slot is 0 for main hand, 1 for offhand, 2 for unknown (always called with empty data.
    // weapon argument is the new weapon data.
    // redrawOnEquality controls whether the game does anything if the new weapon is identical to the old one.
    // skipGameObject seems to control whether the new weapons are written to the game object or just influence the draw object. (1 = skip, 0 = change)
    // unk4 seemed to be the same as unk1.
    [Signature("E8 ?? ?? ?? ?? 44 8B 9F", DetourName = nameof(LoadWeaponDetour))]
    private readonly Hook<LoadWeaponDelegate> _loadWeaponHook = null!;

    private void LoadWeaponDetour(IntPtr characterOffset, uint slot, ulong weapon, byte redrawOnEquality, byte unk2, byte skipGameObject,
        byte unk4)
    {
        var oldWeapon = weapon;
        var character = (Actor)(characterOffset - CharacterWeaponOffset);
        try
        {
            var identifier = character.GetIdentifier();
            if (_fixedDesigns.TryGetDesign(identifier, out var save))
            {
                PluginLog.Information($"Loaded weapon from fixed design for {identifier}.");
                weapon = slot switch
                {
                    0 => save.MainHand.Value,
                    1 => save.OffHand.Value,
                    _ => weapon,
                };
            }
            else if (redrawOnEquality == 1 && _currentManipulations.TryGetDesign(identifier, out var save2))
            {
                PluginLog.Information($"Loaded weapon from current design for {identifier}.");
                switch (slot)
                {
                    //case 0:
                    //    save2.Data.MainHand = new CharacterWeapon(weapon);
                    //    break;
                    //case 1:
                    //    save.OffHand = new CharacterWeapon(weapon);
                    //    break;
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on loading new weapon:\n{e}");
        }

        // First call the regular function.
        _loadWeaponHook.Original(characterOffset, slot, oldWeapon, redrawOnEquality, unk2, skipGameObject, unk4);
        // If something changed the weapon, call it again with the actual change, not forcing redraws and skipping applying it to the game object.
        if (oldWeapon != weapon)
            _loadWeaponHook.Original(characterOffset, slot, weapon, 0 /* redraw */, unk2, 1 /* skip */, unk4);
        // If we're not actively changing the offhand and the game object has no offhand, redraw an empty offhand to fix animation problems.
        else if (slot != 1 && character.OffHand.Value == 0)
            _loadWeaponHook.Original(characterOffset, 1, 0, 1 /* redraw */, unk2, 1 /* skip */, unk4);
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
        LoadWeaponDetour(character.Address + CharacterWeaponOffset, 0, main.Value, 0, 0, 1, 0);
        LoadWeaponDetour(character.Address + CharacterWeaponOffset, 1, off.Value,  0, 0, 1, 0);
    }
}

public unsafe partial class RedrawManager : IDisposable
{
    private readonly FixedDesigns         _fixedDesigns;
    private readonly CurrentManipulations _currentManipulations;

    public RedrawManager(FixedDesigns fixedDesigns, CurrentManipulations currentManipulations)
    {
        SignatureHelper.Initialise(this);
        Glamourer.Penumbra.CreatingCharacterBase += OnCharacterRedraw;
        Glamourer.Penumbra.CreatedCharacterBase  += OnCharacterRedrawFinished;
        _fixedDesigns                            =  fixedDesigns;
        _currentManipulations                    =  currentManipulations;
        _flagSlotForUpdateHook.Enable();
        _loadWeaponHook.Enable();
        _changeJobHook.Enable();
    }

    public void Dispose()
    {
        _flagSlotForUpdateHook.Dispose();
        _loadWeaponHook.Dispose();
        _changeJobHook.Dispose();
        Glamourer.Penumbra.CreatingCharacterBase -= OnCharacterRedraw;
        Glamourer.Penumbra.CreatedCharacterBase  -= OnCharacterRedrawFinished;
    }

    private void OnCharacterRedraw(Actor actor, uint* modelId, Customize customize, CharacterEquip equip)
    {
        // Do not apply anything if the game object model id does not correspond to the draw object model id.
        // This is the case if the actor is transformed to a different creature.
        if (actor.ModelId != *modelId)
            return;

        // Check if we have a current design in use, or if not if the actor has a fixed design.
        var identifier = actor.GetIdentifier();
        if (!(_currentManipulations.TryGetDesign(identifier, out var save) || _fixedDesigns.TryGetDesign(identifier, out var save2)))
            return;


        // Compare game object customize data against draw object customize data for transformations.
        // Apply customization if they correspond and there is customization to apply.
        //var gameObjectCustomize = new Customize((CustomizeData*)actor.Pointer->CustomizeData);
        //if (gameObjectCustomize.Equals(customize))
        //    customize.Load(save.Customize);
        //
        //// Compare game object equip data against draw object equip data for transformations.
        //// Apply each piece of equip that should be applied if they correspond.
        //var gameObjectEquip = new CharacterEquip((CharacterArmor*)actor.Pointer->EquipSlotData);
        //if (gameObjectEquip.Equals(equip))
        //{
        //    var saveEquip = save.Equipment;
        //    foreach (var slot in EquipSlotExtensions.EqdpSlots)
        //    {
        //        (var _, equip[slot]) =
        //            Glamourer.RestrictedGear.ResolveRestricted(true ? equip[slot] : saveEquip[slot], slot, customize.Race, customize.Gender);
        //    }
        //}
    }

    private void OnCharacterRedraw(IntPtr gameObject, IntPtr modelId, IntPtr customize, IntPtr equipData)
    {
        try
        {
            OnCharacterRedraw(gameObject, (uint*)modelId, new Customize((CustomizeData*)customize),
                new CharacterEquip((CharacterArmor*)equipData));
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error on new draw object creation:\n{e}");
        }
    }

    private static void OnCharacterRedrawFinished(IntPtr gameObject, IntPtr drawObject)
    {
        //SetVisor((Human*)drawObject, true);
        if (Glamourer.Models.FromCharacterBase((CharacterBase*)drawObject, out var data))
            PluginLog.Information($"Name: {data.FirstName} ({data.Id})");
        else
            PluginLog.Information($"Key: {Glamourer.Models.KeyFromCharacterBase((CharacterBase*)drawObject):X16}");
    }

    // Update 
    public delegate bool ChangeCustomizeDelegate(Human* human, byte* data, byte skipEquipment);

    [Signature("E8 ?? ?? ?? ?? 41 0F B6 C5 66 41 89 86")]
    private readonly ChangeCustomizeDelegate _changeCustomize = null!;

    public bool UpdateCustomize(DrawObject drawObject, Customize customize)
    {
        if (!drawObject.Valid)
            return false;

        return _changeCustomize(drawObject.Pointer, (byte*)customize.Data, 1);
    }


    public static void SetVisor(Human* data, bool on)
    {
        if (data == null)
            return;

        var flags = &data->CharacterBase.UnkFlags_01;
        var state = (*flags & 0x40) != 0;
        if (state == on)
            return;

        *flags = (byte)((on ? *flags | 0x40 : *flags & 0xBF) | 0x80);
    }
}
