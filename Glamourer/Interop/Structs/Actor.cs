using Penumbra.GameData.Actors;
using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Glamourer.Interop.Structs;

public readonly unsafe struct Actor : IEquatable<Actor>
{
    private Actor(nint address)
        => Address = address;

    public static readonly Actor Null = new(nint.Zero);

    public readonly nint Address;

    public GameObject* AsObject
        => (GameObject*)Address;

    public Character* AsCharacter
        => (Character*)Address;

    public bool Valid
        => Address != nint.Zero;

    public bool IsCharacter
        => Valid && AsObject->IsCharacter();

    public static implicit operator Actor(nint? pointer)
        => new(pointer ?? nint.Zero);

    public static implicit operator Actor(GameObject* pointer)
        => new((nint)pointer);

    public static implicit operator Actor(Character* pointer)
        => new((nint)pointer);

    public static implicit operator nint(Actor actor)
        => actor.Address;

    public bool IsGPoseOrCutscene
        => Index.Index is >= (int)ScreenActor.CutsceneStart and < (int)ScreenActor.CutsceneEnd;

    public bool IsTransformed
        => AsCharacter->CharacterData.TransformationId != 0;

    public ActorIdentifier GetIdentifier(ActorManager actors)
        => actors.FromObject(AsObject, out _, true, true, false);

    public ByteString Utf8Name
        => Valid ? new ByteString(AsObject->Name) : ByteString.Empty;

    public bool Identifier(ActorManager actors, out ActorIdentifier ident)
    {
        if (Valid)
        {
            ident = GetIdentifier(actors);
            return ident.IsValid;
        }

        ident = ActorIdentifier.Invalid;
        return false;
    }

    public ObjectIndex Index
        => Valid ? AsObject->ObjectIndex : ObjectIndex.AnyIndex;

    public Model Model
        => Valid ? AsObject->DrawObject : null;

    public byte Job
        => IsCharacter ? AsCharacter->CharacterData.ClassJob : (byte)0;

    public static implicit operator bool(Actor actor)
        => actor.Address != nint.Zero;

    public static bool operator true(Actor actor)
        => actor.Address != nint.Zero;

    public static bool operator false(Actor actor)
        => actor.Address == nint.Zero;

    public static bool operator !(Actor actor)
        => actor.Address == nint.Zero;

    public bool Equals(Actor other)
        => Address == other.Address;

    public override bool Equals(object? obj)
        => obj is Actor other && Equals(other);

    public override int GetHashCode()
        => Address.GetHashCode();

    public static bool operator ==(Actor lhs, Actor rhs)
        => lhs.Address == rhs.Address;

    public static bool operator !=(Actor lhs, Actor rhs)
        => lhs.Address != rhs.Address;

    /// <summary> Only valid for characters. </summary>
    public CharacterArmor GetArmor(EquipSlot slot)
        => ((CharacterArmor*)&AsCharacter->DrawData.Head)[slot.ToIndex()];

    public bool GetCrest(EquipSlot slot)
        => (GetFreeCompanyCrestBitfield() & CrestMask(slot)) != 0;

    public CharacterWeapon GetMainhand()
        => new(AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId.Value);

    public CharacterWeapon GetOffhand()
        => new(AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId.Value);

    public Customize GetCustomize()
        => *(Customize*)&AsCharacter->DrawData.CustomizeData;

    // TODO remove this when available in ClientStructs
    private byte GetFreeCompanyCrestBitfield()
        => ((byte*)Address)[0x1BBB];

    private static byte CrestMask(EquipSlot slot)
        => slot switch
        {
            EquipSlot.OffHand => 0x01,
            EquipSlot.Head    => 0x02,
            EquipSlot.Body    => 0x04,
            EquipSlot.Hands   => 0x08,
            EquipSlot.Legs    => 0x10,
            EquipSlot.Feet    => 0x20,
            _                 => 0x00,
        };

    public override string ToString()
        => $"0x{Address:X}";
}
