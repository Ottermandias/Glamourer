using System;
using System.Runtime.CompilerServices;
using Glamourer.Customization;
using Glamourer.Services;
using Glamourer.Structs;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Designs;

public unsafe struct DesignData
{
    private       string        _nameHead     = string.Empty;
    private       string        _nameBody     = string.Empty;
    private       string        _nameHands    = string.Empty;
    private       string        _nameLegs     = string.Empty;
    private       string        _nameFeet     = string.Empty;
    private       string        _nameEars     = string.Empty;
    private       string        _nameNeck     = string.Empty;
    private       string        _nameWrists   = string.Empty;
    private       string        _nameRFinger  = string.Empty;
    private       string        _nameLFinger  = string.Empty;
    private       string        _nameMainhand = string.Empty;
    private       string        _nameOffhand  = string.Empty;
    private fixed uint          _itemIds[12];
    private fixed ushort        _iconIds[12];
    private fixed byte          _equipmentBytes[48];
    public        Customize     Customize = Customize.Default;
    public        uint          ModelId;
    public        CrestFlag     CrestVisibility;
    private       WeaponType    _secondaryMainhand;
    private       WeaponType    _secondaryOffhand;
    private       FullEquipType _typeMainhand;
    private       FullEquipType _typeOffhand;
    private       byte          _states;
    public        bool          IsHuman = true;

    public DesignData()
    { }

    public readonly bool ContainsName(LowerString name)
        => name.IsContained(_nameHead)
         || name.IsContained(_nameBody)
         || name.IsContained(_nameHands)
         || name.IsContained(_nameLegs)
         || name.IsContained(_nameFeet)
         || name.IsContained(_nameEars)
         || name.IsContained(_nameNeck)
         || name.IsContained(_nameWrists)
         || name.IsContained(_nameRFinger)
         || name.IsContained(_nameLFinger)
         || name.IsContained(_nameMainhand)
         || name.IsContained(_nameOffhand);

    public readonly StainId Stain(EquipSlot slot)
    {
        var index = slot.ToIndex();
        return index > 11 ? (StainId)0 : _equipmentBytes[4 * index + 3];
    }

    public readonly bool Crest(CrestFlag slot)
        => CrestVisibility.HasFlag(slot);


    public FullEquipType MainhandType
        => _typeMainhand;

    public FullEquipType OffhandType
        => _typeOffhand;

    public readonly EquipItem Item(EquipSlot slot)
        => slot.ToIndex() switch
        {
            // @formatter:off
            0  => EquipItem.FromIds(_itemIds[ 0], _iconIds[ 0], (SetId)(_equipmentBytes[ 0] | (_equipmentBytes[ 1] << 8)), (WeaponType)0,      _equipmentBytes[ 2], FullEquipType.Head,   name: _nameHead    ),
            1  => EquipItem.FromIds(_itemIds[ 1], _iconIds[ 1], (SetId)(_equipmentBytes[ 4] | (_equipmentBytes[ 5] << 8)), (WeaponType)0,      _equipmentBytes[ 6], FullEquipType.Body,   name: _nameBody    ),
            2  => EquipItem.FromIds(_itemIds[ 2], _iconIds[ 2], (SetId)(_equipmentBytes[ 8] | (_equipmentBytes[ 9] << 8)), (WeaponType)0,      _equipmentBytes[10], FullEquipType.Hands,  name: _nameHands   ),
            3  => EquipItem.FromIds(_itemIds[ 3], _iconIds[ 3], (SetId)(_equipmentBytes[12] | (_equipmentBytes[13] << 8)), (WeaponType)0,      _equipmentBytes[14], FullEquipType.Legs,   name: _nameLegs    ),
            4  => EquipItem.FromIds(_itemIds[ 4], _iconIds[ 4], (SetId)(_equipmentBytes[16] | (_equipmentBytes[17] << 8)), (WeaponType)0,      _equipmentBytes[18], FullEquipType.Feet,   name: _nameFeet    ),
            5  => EquipItem.FromIds(_itemIds[ 5], _iconIds[ 5], (SetId)(_equipmentBytes[20] | (_equipmentBytes[21] << 8)), (WeaponType)0,      _equipmentBytes[22], FullEquipType.Ears,   name: _nameEars    ),
            6  => EquipItem.FromIds(_itemIds[ 6], _iconIds[ 6], (SetId)(_equipmentBytes[24] | (_equipmentBytes[25] << 8)), (WeaponType)0,      _equipmentBytes[26], FullEquipType.Neck,   name: _nameNeck    ),
            7  => EquipItem.FromIds(_itemIds[ 7], _iconIds[ 7], (SetId)(_equipmentBytes[28] | (_equipmentBytes[29] << 8)), (WeaponType)0,      _equipmentBytes[30], FullEquipType.Wrists, name: _nameWrists  ),
            8  => EquipItem.FromIds(_itemIds[ 8], _iconIds[ 8], (SetId)(_equipmentBytes[32] | (_equipmentBytes[33] << 8)), (WeaponType)0,      _equipmentBytes[34], FullEquipType.Finger, name: _nameRFinger ),
            9  => EquipItem.FromIds(_itemIds[ 9], _iconIds[ 9], (SetId)(_equipmentBytes[36] | (_equipmentBytes[37] << 8)), (WeaponType)0,      _equipmentBytes[38], FullEquipType.Finger, name: _nameLFinger ),
            10 => EquipItem.FromIds(_itemIds[10], _iconIds[10], (SetId)(_equipmentBytes[40] | (_equipmentBytes[41] << 8)), _secondaryMainhand, _equipmentBytes[42], _typeMainhand,        name: _nameMainhand),
            11 => EquipItem.FromIds(_itemIds[11], _iconIds[11], (SetId)(_equipmentBytes[44] | (_equipmentBytes[45] << 8)), _secondaryOffhand,  _equipmentBytes[46], _typeOffhand,         name: _nameOffhand ),
            _  => new EquipItem(),
            // @formatter:on
        };

    public readonly CharacterArmor Armor(EquipSlot slot)
    {
        fixed (byte* ptr = _equipmentBytes)
        {
            var armorPtr = (CharacterArmor*)ptr;
            return armorPtr[slot.ToIndex()];
        }
    }

    public readonly CharacterArmor ArmorWithState(EquipSlot slot)
    {
        if (slot is EquipSlot.Head && !IsHatVisible())
            return CharacterArmor.Empty;

        fixed (byte* ptr = _equipmentBytes)
        {
            var armorPtr = (CharacterArmor*)ptr;
            return armorPtr[slot.ToIndex()];
        }
    }

    public readonly CharacterWeapon Weapon(EquipSlot slot)
    {
        fixed (byte* ptr = _equipmentBytes)
        {
            var armorPtr = (CharacterArmor*)ptr;
            return slot is EquipSlot.MainHand ? armorPtr[10].ToWeapon(_secondaryMainhand) : armorPtr[11].ToWeapon(_secondaryOffhand);
        }
    }

    public bool SetItem(EquipSlot slot, EquipItem item)
    {
        var index = slot.ToIndex();
        if (index > 11)
            return false;

        _itemIds[index]                = item.ItemId.Id;
        _iconIds[index]                = item.IconId.Id;
        _equipmentBytes[4 * index + 0] = (byte)item.ModelId.Id;
        _equipmentBytes[4 * index + 1] = (byte)(item.ModelId.Id >> 8);
        _equipmentBytes[4 * index + 2] = item.Variant.Id;
        switch (index)
        {
            // @formatter:off
            case 0:  _nameHead     = item.Name; return true;
            case 1:  _nameBody     = item.Name; return true;
            case 2:  _nameHands    = item.Name; return true;
            case 3:  _nameLegs     = item.Name; return true;
            case 4:  _nameFeet     = item.Name; return true;
            case 5:  _nameEars     = item.Name; return true;
            case 6:  _nameNeck     = item.Name; return true;
            case 7:  _nameWrists   = item.Name; return true;
            case 8:  _nameRFinger  = item.Name; return true;
            case 9:  _nameLFinger  = item.Name; return true;
            // @formatter:on
            case 10:
                _nameMainhand      = item.Name;
                _secondaryMainhand = item.WeaponType;
                _typeMainhand      = item.Type;
                return true;
            case 11:
                _nameOffhand      = item.Name;
                _secondaryOffhand = item.WeaponType;
                _typeOffhand      = item.Type;
                return true;
        }

        return true;
    }

    public bool SetStain(EquipSlot slot, StainId stain)
        => slot.ToIndex() switch
        {
            0  => SetIfDifferent(ref _equipmentBytes[3],  stain.Id),
            1  => SetIfDifferent(ref _equipmentBytes[7],  stain.Id),
            2  => SetIfDifferent(ref _equipmentBytes[11], stain.Id),
            3  => SetIfDifferent(ref _equipmentBytes[15], stain.Id),
            4  => SetIfDifferent(ref _equipmentBytes[19], stain.Id),
            5  => SetIfDifferent(ref _equipmentBytes[23], stain.Id),
            6  => SetIfDifferent(ref _equipmentBytes[27], stain.Id),
            7  => SetIfDifferent(ref _equipmentBytes[31], stain.Id),
            8  => SetIfDifferent(ref _equipmentBytes[35], stain.Id),
            9  => SetIfDifferent(ref _equipmentBytes[39], stain.Id),
            10 => SetIfDifferent(ref _equipmentBytes[43], stain.Id),
            11 => SetIfDifferent(ref _equipmentBytes[47], stain.Id),
            _  => false,
        };

    public bool SetCrest(CrestFlag slot, bool visible)
    {
        var newValue = visible ? CrestVisibility | slot : CrestVisibility & ~slot;
        if (newValue == CrestVisibility)
            return false;

        CrestVisibility = newValue;
        return true;
    }

    public readonly bool IsWet()
        => (_states & 0x01) == 0x01;

    public bool SetIsWet(bool value)
    {
        if (value == IsWet())
            return false;

        _states = (byte)(value ? _states | 0x01 : _states & ~0x01);
        return true;
    }


    public readonly bool IsVisorToggled()
        => (_states & 0x02) == 0x02;

    public bool SetVisor(bool value)
    {
        if (value == IsVisorToggled())
            return false;

        _states = (byte)(value ? _states | 0x02 : _states & ~0x02);
        return true;
    }

    public readonly bool IsHatVisible()
        => (_states & 0x04) == 0x04;

    public bool SetHatVisible(bool value)
    {
        if (value == IsHatVisible())
            return false;

        _states = (byte)(value ? _states | 0x04 : _states & ~0x04);
        return true;
    }

    public readonly bool IsWeaponVisible()
        => (_states & 0x08) == 0x08;

    public bool SetWeaponVisible(bool value)
    {
        if (value == IsWeaponVisible())
            return false;

        _states = (byte)(value ? _states | 0x08 : _states & ~0x08);
        return true;
    }

    public void SetDefaultEquipment(ItemManager items)
    {
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            SetItem(slot, ItemManager.NothingItem(slot));
            SetStain(slot, 0);
            SetCrest(slot.ToCrestFlag(), false);
        }

        SetItem(EquipSlot.MainHand, items.DefaultSword);
        SetStain(EquipSlot.MainHand, 0);
        SetCrest(CrestFlag.MainHand, false);
        SetItem(EquipSlot.OffHand, ItemManager.NothingItem(FullEquipType.Shield));
        SetStain(EquipSlot.OffHand, 0);
        SetCrest(CrestFlag.OffHand, false);
    }


    public bool LoadNonHuman(uint modelId, Customize customize, nint equipData)
    {
        ModelId = modelId;
        IsHuman = false;
        Customize.Load(customize);
        fixed (byte* ptr = _equipmentBytes)
        {
            MemoryUtility.MemCpyUnchecked(ptr, (byte*)equipData, 40);
        }

        SetHatVisible(true);
        SetWeaponVisible(true);
        SetVisor(false);
        fixed (uint* ptr = _itemIds)
        {
            MemoryUtility.MemSet(ptr, 0, 10 * 4);
        }

        fixed (ushort* ptr = _iconIds)
        {
            MemoryUtility.MemSet(ptr, 0, 10 * 2);
        }

        _nameHead    = string.Empty;
        _nameBody    = string.Empty;
        _nameHands   = string.Empty;
        _nameLegs    = string.Empty;
        _nameFeet    = string.Empty;
        _nameEars    = string.Empty;
        _nameNeck    = string.Empty;
        _nameWrists  = string.Empty;
        _nameRFinger = string.Empty;
        _nameLFinger = string.Empty;
        return true;
    }

    public readonly byte[] GetCustomizeBytes()
    {
        var ret = new byte[CustomizeData.Size];
        fixed (byte* retPtr = ret, inPtr = Customize.Data.Data)
        {
            MemoryUtility.MemCpyUnchecked(retPtr, inPtr, ret.Length);
        }

        return ret;
    }

    public readonly byte[] GetEquipmentBytes()
    {
        var ret = new byte[40];
        fixed (byte* retPtr = ret, inPtr = _equipmentBytes)
        {
            MemoryUtility.MemCpyUnchecked(retPtr, inPtr, ret.Length);
        }

        return ret;
    }

    public nint GetEquipmentPtr()
    {
        fixed (byte* ptr = _equipmentBytes)
        {
            return (nint)ptr;
        }
    }

    public bool SetEquipmentBytesFromBase64(string base64)
    {
        fixed (byte* dataPtr = _equipmentBytes)
        {
            var data = new Span<byte>(dataPtr, 40);
            return Convert.TryFromBase64String(base64, data, out var written) && written == 40;
        }
    }

    public string WriteEquipmentBytesBase64()
    {
        fixed (byte* dataPtr = _equipmentBytes)
        {
            var data = new ReadOnlySpan<byte>(dataPtr, 40);
            return Convert.ToBase64String(data);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferent<T>(ref T old, T value) where T : IEquatable<T>
    {
        if (old.Equals(value))
            return false;

        old = value;
        return true;
    }
}
