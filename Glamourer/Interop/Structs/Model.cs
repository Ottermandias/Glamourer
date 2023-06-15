using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;
using ObjectType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType;

namespace Glamourer.Interop.Structs;

public readonly unsafe struct Model : IEquatable<Model>
{
    private Model(nint address)
        => Address = address;

    public readonly nint Address;

    public static readonly Model Null = new(0);

    public DrawObject* AsDrawObject
        => (DrawObject*)Address;

    public CharacterBase* AsCharacterBase
        => (CharacterBase*)Address;

    public Weapon* AsWeapon
        => (Weapon*)Address;

    public Human* AsHuman
        => (Human*)Address;

    public static implicit operator Model(nint? pointer)
        => new(pointer ?? nint.Zero);

    public static implicit operator Model(Object* pointer)
        => new((nint)pointer);

    public static implicit operator Model(DrawObject* pointer)
        => new((nint)pointer);

    public static implicit operator Model(Human* pointer)
        => new((nint)pointer);

    public static implicit operator Model(CharacterBase* pointer)
        => new((nint)pointer);

    public static implicit operator nint(Model model)
        => model.Address;

    public bool Valid
        => Address != nint.Zero;

    public bool IsCharacterBase
        => Valid && AsDrawObject->Object.GetObjectType() == ObjectType.CharacterBase;

    public bool IsHuman
        => IsCharacterBase && AsCharacterBase->GetModelType() == CharacterBase.ModelType.Human;

    public bool IsWeapon
        => IsCharacterBase && AsCharacterBase->GetModelType() == CharacterBase.ModelType.Weapon;

    public static implicit operator bool(Model actor)
        => actor.Address != nint.Zero;

    public static bool operator true(Model actor)
        => actor.Address != nint.Zero;

    public static bool operator false(Model actor)
        => actor.Address == nint.Zero;

    public static bool operator !(Model actor)
        => actor.Address == nint.Zero;

    public bool Equals(Model other)
        => Address == other.Address;

    public override bool Equals(object? obj)
        => obj is Model other && Equals(other);

    public override int GetHashCode()
        => Address.GetHashCode();

    public static bool operator ==(Model lhs, Model rhs)
        => lhs.Address == rhs.Address;

    public static bool operator !=(Model lhs, Model rhs)
        => lhs.Address != rhs.Address;

    /// <summary> Only valid for humans. </summary>
    public CharacterArmor GetArmor(EquipSlot slot)
        => ((CharacterArmor*)AsHuman->EquipSlotData)[slot.ToIndex()];

    public (Model Address, CharacterWeapon Data) GetMainhand()
    {
        Model weapon = AsDrawObject->Object.ChildObject;
        return !weapon.IsWeapon
            ? (Null, CharacterWeapon.Empty)
            : (weapon, new CharacterWeapon(weapon.AsWeapon->ModelSetId, weapon.AsWeapon->SecondaryId, weapon.AsWeapon->Variant,
                (StainId)weapon.AsWeapon->ModelUnknown));
    }

    public (Model Address, CharacterWeapon Data) GetOffhand()
    {
        var mainhand = AsDrawObject->Object.ChildObject;
        if (mainhand == null)
            return (Null, CharacterWeapon.Empty);

        Model offhand = mainhand->NextSiblingObject;
        if (offhand == mainhand || !offhand.IsWeapon)
            return (Null, CharacterWeapon.Empty);

        return (offhand, new CharacterWeapon(offhand.AsWeapon->ModelSetId, offhand.AsWeapon->SecondaryId, offhand.AsWeapon->Variant,
            (StainId)offhand.AsWeapon->ModelUnknown));
    }

    /// <summary> Obtain the mainhand and offhand and their data by guesstimating which child object is which. </summary>
    public (Model Mainhand, Model Offhand, CharacterWeapon MainData, CharacterWeapon OffData) GetWeapons()
    {
        var (first, second, count) = GetChildrenWeapons();
        switch (count)
        {
            case 0: return (Null, Null, CharacterWeapon.Empty, CharacterWeapon.Empty);
            case 1:
                return (first, Null, new CharacterWeapon(first.AsWeapon->ModelSetId, first.AsWeapon->SecondaryId, first.AsWeapon->Variant,
                    (StainId)first.AsWeapon->ModelUnknown), CharacterWeapon.Empty);
            default:
                var (main, off) = DetermineMainhand(first, second);
                var mainData = new CharacterWeapon(main.AsWeapon->ModelSetId, main.AsWeapon->SecondaryId, main.AsWeapon->Variant,
                    (StainId)main.AsWeapon->ModelUnknown);
                var offData = new CharacterWeapon(off.AsWeapon->ModelSetId, off.AsWeapon->SecondaryId, off.AsWeapon->Variant,
                    (StainId)off.AsWeapon->ModelUnknown);
                return (main, off, mainData, offData);
        }
    }

    /// <summary> Obtain the mainhand and offhand and their data by using the drawdata container from the corresponding actor. </summary>
    public (Model Mainhand, Model Offhand, CharacterWeapon MainData, CharacterWeapon OffData) GetWeapons(Actor actor)
    {
        if (!Valid || !actor.IsCharacter || actor.Model.Address != Address)
            return (Null, Null, CharacterWeapon.Empty, CharacterWeapon.Empty);

        Model main     = *((nint*)&actor.AsCharacter->DrawData.MainHand + 1);
        var   mainData = CharacterWeapon.Empty;
        if (main.IsWeapon)
            mainData = new CharacterWeapon(main.AsWeapon->ModelSetId, main.AsWeapon->SecondaryId, main.AsWeapon->Variant,
                (StainId)main.AsWeapon->ModelUnknown);
        else
            main = Null;
        Model off     = *((nint*)&actor.AsCharacter->DrawData.OffHand + 1);
        var   offData = CharacterWeapon.Empty;
        if (off.IsWeapon)
            offData = new CharacterWeapon(off.AsWeapon->ModelSetId, off.AsWeapon->SecondaryId, off.AsWeapon->Variant,
                (StainId)off.AsWeapon->ModelUnknown);
        else
            off = Null;
        return (main, off, mainData, offData);
    }

    private (Model, Model, int) GetChildrenWeapons()
    {
        Span<Model> weapons = stackalloc Model[2];
        weapons[0] = Null;
        weapons[1] = Null;
        var count = 0;

        if (!Valid || AsDrawObject->Object.ChildObject == null)
            return (weapons[0], weapons[1], count);

        Model starter  = AsDrawObject->Object.ChildObject;
        var   iterator = starter;
        do
        {
            if (iterator.IsWeapon)
                weapons[count++] = iterator;
            if (count == 2)
                return (weapons[0], weapons[1], count);

            iterator = iterator.AsDrawObject->Object.NextSiblingObject;
        } while (iterator.Address != starter.Address);

        return (weapons[0], weapons[1], count);
    }

    /// <summary> I don't know a safe way to do this but in experiments this worked.
    /// The first uint at +0x8 was set to non-zero for the mainhand and zero for the offhand. </summary>
    private static (Model Mainhand, Model Offhand) DetermineMainhand(Model first, Model second)
    {
        var discriminator1 = *(ulong*)(first.Address + 0x10);
        var discriminator2 = *(ulong*)(second.Address + 0x10);
        return discriminator1 == 0 && discriminator2 != 0 ? (second, first) : (first, second);
    }

    public override string ToString()
        => $"0x{Address:X}";
}
