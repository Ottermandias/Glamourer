using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Interop;

public unsafe partial struct DrawObject : IEquatable<DrawObject>, IDesignable
{
    public Human* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator DrawObject(IntPtr? pointer)
        => new() { Pointer = (Human*)(pointer ?? IntPtr.Zero) };

    public static implicit operator IntPtr(DrawObject drawObject)
        => drawObject.Pointer == null ? IntPtr.Zero : (IntPtr)drawObject.Pointer;

    public bool Valid
        => Pointer != null;

    public uint ModelId
        => 0;

    public bool IsWet
        => false;

    public uint Type
        => (*(delegate* unmanaged<Human*, uint>**)Pointer)[50](Pointer);

    public Customize Customize
        => new((CustomizeData*)Pointer->CustomizeData);

    public CharacterEquip Equip
        => new((CharacterArmor*)Pointer->EquipSlotData);

    public CharacterWeapon MainHand
    {
        get
        {
            var child = (byte*)Pointer->CharacterBase.DrawObject.Object.ChildObject;
            if (child == null)
                return CharacterWeapon.Empty;

            return *(CharacterWeapon*)(child + 0x8F0);
        }
    }

    public unsafe CharacterWeapon OffHand
    {
        get
        {
            var child = Pointer->CharacterBase.DrawObject.Object.ChildObject;
            if (child == null)
                return CharacterWeapon.Empty;

            var sibling = (byte*)child->NextSiblingObject;
            if (sibling == null)
                return CharacterWeapon.Empty;

            return *(CharacterWeapon*)(sibling + 0x8F0);
        }
    }

    public unsafe bool VisorEnabled
        => (*(byte*)(Address + 0x90) & 0x40) != 0;

    public unsafe bool WeaponEnabled
        => false;

    public static implicit operator bool(DrawObject actor)
        => actor.Pointer != null;

    public static bool operator true(DrawObject actor)
        => actor.Pointer != null;

    public static bool operator false(DrawObject actor)
        => actor.Pointer == null;

    public static bool operator !(DrawObject actor)
        => actor.Pointer == null;

    public bool Equals(DrawObject other)
        => Pointer == other.Pointer;

    public override bool Equals(object? obj)
        => obj is DrawObject other && Equals(other);

    public override int GetHashCode()
        => unchecked((int)(long)Pointer);

    public static bool operator ==(DrawObject lhs, DrawObject rhs)
        => lhs.Pointer == rhs.Pointer;

    public static bool operator !=(DrawObject lhs, DrawObject rhs)
        => lhs.Pointer != rhs.Pointer;
}
