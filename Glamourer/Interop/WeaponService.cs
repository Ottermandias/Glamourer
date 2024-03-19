using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Glamourer.Events;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public unsafe class WeaponService : IDisposable
{
    private readonly WeaponLoading     _event;
    private readonly ThreadLocal<bool> _inUpdate = new(() => false);


    private readonly delegate* unmanaged[Stdcall]<DrawDataContainer*, uint, ulong, byte, byte, byte, byte, void>
        _original;

    public WeaponService(WeaponLoading @event, IGameInteropProvider interop)
    {
        _event = @event;
        _loadWeaponHook =
            interop.HookFromAddress<LoadWeaponDelegate>((nint)DrawDataContainer.MemberFunctionPointers.LoadWeapon, LoadWeaponDetour);
        _original =
            (delegate* unmanaged[Stdcall] < DrawDataContainer*, uint, ulong, byte, byte, byte, byte, void >)
            DrawDataContainer.MemberFunctionPointers.LoadWeapon;
        _loadWeaponHook.Enable();
    }

    public void Dispose()
        => _loadWeaponHook.Dispose();

    // Weapons for a specific character are reloaded with this function.
    // slot is 0 for main hand, 1 for offhand, 2 for combat effects.
    // weapon argument is the new weapon data.
    // redrawOnEquality controls whether the game does anything if the new weapon is identical to the old one.
    // skipGameObject seems to control whether the new weapons are written to the game object or just influence the draw object. (1 = skip, 0 = change)
    // unk4 seemed to be the same as unk1.
    private delegate void LoadWeaponDelegate(DrawDataContainer* drawData, uint slot, ulong weapon, byte redrawOnEquality, byte unk2,
        byte skipGameObject, byte unk4);

    private readonly Hook<LoadWeaponDelegate> _loadWeaponHook;

    private void LoadWeaponDetour(DrawDataContainer* drawData, uint slot, ulong weaponValue, byte redrawOnEquality, byte unk2,
        byte skipGameObject, byte unk4)
    {
        if (!_inUpdate.Value)
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
            // Sage hack for weapons appearing in animations?
            // Check for weapon value 0 for certain cases (e.g. carbuncles transforming to humans) because that breaks some stuff (weapon hiding?) otherwise.
            else if (weaponValue == actor.GetMainhand().Value && weaponValue != 0)
                _event.Invoke(actor, EquipSlot.MainHand, ref tmpWeapon);

            _loadWeaponHook.Original(drawData, slot, weapon.Value, redrawOnEquality, unk2, skipGameObject, unk4);

            if (tmpWeapon.Value != weapon.Value)
            {
                if (tmpWeapon.Skeleton.Id == 0)
                    tmpWeapon.Stain = 0;
                _loadWeaponHook.Original(drawData, slot, tmpWeapon.Value, 1, unk2, 1, unk4);
            }

            Glamourer.Log.Excessive(
                $"Weapon reloaded for 0x{actor.Address:X} ({actor.Utf8Name}) with attributes {slot} {weapon.Value:X14}, {redrawOnEquality}, {unk2}, {skipGameObject}, {unk4}");
        }
        else
        {
            _loadWeaponHook.Original(drawData, slot, weaponValue, redrawOnEquality, unk2, skipGameObject, unk4);
        }
    }

    // Load a specific weapon for a character by its data and slot.
    public void LoadWeapon(Actor character, EquipSlot slot, CharacterWeapon weapon)
    {
        switch (slot)
        {
            case EquipSlot.MainHand:
                _inUpdate.Value = true;
                _original(&character.AsCharacter->DrawData, 0, weapon.Value, 1, 0, 1, 0);
                _inUpdate.Value = false;
                return;
            case EquipSlot.OffHand:
                _inUpdate.Value = true;
                _original(&character.AsCharacter->DrawData, 1, weapon.Value, 1, 0, 1, 0);
                _inUpdate.Value = false;
                return;
            case EquipSlot.BothHand:
                _inUpdate.Value = true;
                _original(&character.AsCharacter->DrawData, 0, weapon.Value,                1, 0, 1, 0);
                _original(&character.AsCharacter->DrawData, 1, CharacterWeapon.Empty.Value, 1, 0, 1, 0);
                _inUpdate.Value = false;
                return;
        }
    }

    public void LoadStain(Actor character, EquipSlot slot, StainId stain)
    {
        var mdl = character.Model;
        var (_, _, mh, oh) = mdl.GetWeapons(character);
        var value  = slot == EquipSlot.OffHand ? oh : mh;
        var weapon = value.With(value.Skeleton.Id == 0 ? 0 : stain);
        LoadWeapon(character, slot, weapon);
    }
}
