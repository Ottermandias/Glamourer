using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
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

    public Human* AsHuman
        => (Human*)Address;

    public static implicit operator Model(nint? pointer)
        => new(pointer ?? nint.Zero);

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

    public CharacterWeapon GetMainhand()
    {
        var weapon = AsDrawObject->Object.ChildObject;
        if (weapon == null)
            return CharacterWeapon.Empty;
        weapon
    }

    public CharacterWeapon GetOffhand()
        => *(CharacterWeapon*)&AsCharacter->DrawData.OffHandModel;
}
