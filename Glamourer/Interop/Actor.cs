using System;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Penumbra.GameData.ByteString;
using Penumbra.GameData.Structs;

namespace Glamourer.Interop;

public interface IDesignable
{
    public bool            Valid         { get; }
    public uint            ModelId       { get; }
    public Customize       Customize     { get; }
    public CharacterEquip  Equip         { get; }
    public CharacterWeapon MainHand      { get; }
    public CharacterWeapon OffHand       { get; }
    public bool            VisorEnabled  { get; }
    public bool            WeaponEnabled { get; }
}

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

    public uint Type
        => (*(delegate* unmanaged<Human*, uint>**)Pointer)[50](Pointer);

    public Customize Customize
        => new((CustomizeData*)Pointer->CustomizeData);

    public CharacterEquip Equip
        => new((CharacterArmor*)Pointer->EquipSlotData);

    public unsafe CharacterWeapon MainHand
        => CharacterWeapon.Empty;

    public unsafe CharacterWeapon OffHand
        => CharacterWeapon.Empty;

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

public unsafe partial struct Actor : IEquatable<Actor>, IDesignable
{
    public static readonly Actor Null = new() { Pointer = null };

    public FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Pointer;

    public IntPtr Address
        => (IntPtr)Pointer;

    public static implicit operator Actor(IntPtr? pointer)
        => new() { Pointer = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)(pointer ?? IntPtr.Zero) };

    public static implicit operator IntPtr(Actor actor)
        => actor.Pointer == null ? IntPtr.Zero : (IntPtr)actor.Pointer;

    public IIdentifier GetIdentifier()
        => CreateIdentifier(this);

    public bool Identifier(out IIdentifier ident)
    {
        if (Valid)
        {
            ident = GetIdentifier();
            return true;
        }
        ident = IIdentifier.Invalid;
        return false;
    }

    public Character? Character
        => Pointer == null ? null : Dalamud.Objects[Pointer->GameObject.ObjectIndex] as Character;

    public bool IsAvailable
        => Pointer->GameObject.GetIsTargetable();

    public bool IsHuman
        => Pointer != null && Pointer->ModelCharaId == 0;

    public ObjectKind ObjectKind
    {
        get => (ObjectKind)Pointer->GameObject.ObjectKind;
        set => Pointer->GameObject.ObjectKind = (byte)value;
    }

    public Utf8String Utf8Name
        => new(Pointer->GameObject.Name);

    public byte Job
        => Pointer->ClassJob;

    public DrawObject DrawObject
        => (IntPtr)Pointer->GameObject.DrawObject;

    public bool Valid
        => Pointer != null;

    public uint ModelId
    {
        get => (uint)Pointer->ModelCharaId;
        set => Pointer->ModelCharaId = (int)value;
    }

    public Customize Customize
        => new((CustomizeData*)Pointer->CustomizeData);

    public CharacterEquip Equip
        => new((CharacterArmor*)Pointer->EquipSlotData);

    public unsafe CharacterWeapon MainHand
    {
        get => *(CharacterWeapon*)(Address + 0x06C0 + 0x10);
        set => *(CharacterWeapon*)(Address + 0x06C0 + 0x10) = value;
    }

    public unsafe CharacterWeapon OffHand
    {
        get => *(CharacterWeapon*)(Address + 0x06C0 + 0x10 + 0x68);
        set => *(CharacterWeapon*)(Address + 0x06C0 + 0x10 + 0x68) = value;
    }

    public unsafe bool VisorEnabled
    {
        get => (*(byte*)(Address + Offsets.Character.VisorToggled) & Offsets.Character.Flags.IsVisorToggled) != 0;
        set => *(byte*)(Address + Offsets.Character.VisorToggled) = (byte)(value
            ? *(byte*)(Address + Offsets.Character.VisorToggled) | Offsets.Character.Flags.IsVisorToggled
            : *(byte*)(Address + Offsets.Character.VisorToggled) & ~Offsets.Character.Flags.IsVisorToggled);
    }

    public unsafe bool WeaponEnabled
    {
        get => (*(byte*)(Address + Offsets.Character.WeaponHidden1) & Offsets.Character.Flags.IsWeaponHidden1) == 0;
        set
        {
            ref var w1 = ref *(byte*)(Address + Offsets.Character.WeaponHidden1);
            ref var w2 = ref *(byte*)(Address + Offsets.Character.WeaponHidden2);
            if (value)
            {
                w1 = (byte)(w1 & ~Offsets.Character.Flags.IsWeaponHidden1);
                w2 = (byte)(w2 & ~Offsets.Character.Flags.IsWeaponHidden2);
            }
            else
            {
                w1 = (byte)(w1 | Offsets.Character.Flags.IsWeaponHidden1);
                w2 = (byte)(w2 | Offsets.Character.Flags.IsWeaponHidden2);
            }
        }
    }


    public void SetModelId(int value)
    {
        if (Pointer != null)
            Pointer->ModelCharaId = value;
    }

    public static implicit operator bool(Actor actor)
        => actor.Pointer != null;

    public static bool operator true(Actor actor)
        => actor.Pointer != null;

    public static bool operator false(Actor actor)
        => actor.Pointer == null;

    public static bool operator !(Actor actor)
        => actor.Pointer == null;

    public bool Equals(Actor other)
        => Pointer == other.Pointer;

    public override bool Equals(object? obj)
        => obj is Actor other && Equals(other);

    public override int GetHashCode()
        => ((ulong)Pointer).GetHashCode();

    public static bool operator ==(Actor lhs, Actor rhs)
        => lhs.Pointer == rhs.Pointer;

    public static bool operator !=(Actor lhs, Actor rhs)
        => lhs.Pointer != rhs.Pointer;
}
