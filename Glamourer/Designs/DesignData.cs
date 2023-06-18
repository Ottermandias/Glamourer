using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using Glamourer.Customization;
using Glamourer.Services;
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
    private       WeaponType    _secondaryMainhand;
    private       WeaponType    _secondaryOffhand;
    private       FullEquipType _typeMainhand;
    private       FullEquipType _typeOffhand;
    private       byte          _states;

    public DesignData()
    { }

    public readonly StainId Stain(EquipSlot slot)
    {
        var index = slot.ToIndex();
        return index > 11 ? (StainId)0 : _equipmentBytes[4 * index + 3];
    }

    public readonly EquipItem Item(EquipSlot slot)
        => slot.ToIndex() switch
        {
            // @formatter:off
            0  => new EquipItem(_nameHead,     _itemIds[ 0], _iconIds[ 0], (SetId)(_equipmentBytes[ 0] | (_equipmentBytes[ 1] << 8)), (WeaponType)0,      _equipmentBytes[ 2], FullEquipType.Head   ),
            1  => new EquipItem(_nameBody,     _itemIds[ 1], _iconIds[ 1], (SetId)(_equipmentBytes[ 4] | (_equipmentBytes[ 5] << 8)), (WeaponType)0,      _equipmentBytes[ 6], FullEquipType.Body   ),
            2  => new EquipItem(_nameHands,    _itemIds[ 2], _iconIds[ 2], (SetId)(_equipmentBytes[ 8] | (_equipmentBytes[ 9] << 8)), (WeaponType)0,      _equipmentBytes[10], FullEquipType.Hands  ),
            3  => new EquipItem(_nameLegs,     _itemIds[ 3], _iconIds[ 3], (SetId)(_equipmentBytes[12] | (_equipmentBytes[13] << 8)), (WeaponType)0,      _equipmentBytes[14], FullEquipType.Legs   ),
            4  => new EquipItem(_nameFeet,     _itemIds[ 4], _iconIds[ 4], (SetId)(_equipmentBytes[16] | (_equipmentBytes[17] << 8)), (WeaponType)0,      _equipmentBytes[18], FullEquipType.Feet   ),
            5  => new EquipItem(_nameEars,     _itemIds[ 5], _iconIds[ 5], (SetId)(_equipmentBytes[20] | (_equipmentBytes[21] << 8)), (WeaponType)0,      _equipmentBytes[22], FullEquipType.Ears   ),
            6  => new EquipItem(_nameNeck,     _itemIds[ 6], _iconIds[ 6], (SetId)(_equipmentBytes[24] | (_equipmentBytes[25] << 8)), (WeaponType)0,      _equipmentBytes[26], FullEquipType.Neck   ),
            7  => new EquipItem(_nameWrists,   _itemIds[ 7], _iconIds[ 7], (SetId)(_equipmentBytes[28] | (_equipmentBytes[29] << 8)), (WeaponType)0,      _equipmentBytes[30], FullEquipType.Wrists ),
            8  => new EquipItem(_nameRFinger,  _itemIds[ 8], _iconIds[ 8], (SetId)(_equipmentBytes[32] | (_equipmentBytes[33] << 8)), (WeaponType)0,      _equipmentBytes[34], FullEquipType.Finger ),
            9  => new EquipItem(_nameLFinger,  _itemIds[ 9], _iconIds[ 9], (SetId)(_equipmentBytes[36] | (_equipmentBytes[37] << 8)), (WeaponType)0,      _equipmentBytes[38], FullEquipType.Finger ),
            10 => new EquipItem(_nameMainhand, _itemIds[10], _iconIds[10], (SetId)(_equipmentBytes[40] | (_equipmentBytes[41] << 8)), _secondaryMainhand, _equipmentBytes[42], _typeMainhand        ),
            11 => new EquipItem(_nameOffhand,  _itemIds[11], _iconIds[11], (SetId)(_equipmentBytes[44] | (_equipmentBytes[45] << 8)), _secondaryOffhand,  _equipmentBytes[46], _typeOffhand         ),
            _  => new EquipItem(),
            // @formatter:on
        };

    public bool SetItem(EquipSlot slot, EquipItem item)
    {
        var index = slot.ToIndex();
        if (index > 11 || _itemIds[index] == item.Id)
            return false;

        _itemIds[index]                = item.Id;
        _iconIds[index]                = item.IconId;
        _equipmentBytes[4 * index + 0] = (byte)item.ModelId;
        _equipmentBytes[4 * index + 1] = (byte)(item.ModelId.Value >> 8);
        _equipmentBytes[4 * index + 2] = item.Variant;
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
            0  => SetIfDifferent(ref _equipmentBytes[3],  stain.Value),
            1  => SetIfDifferent(ref _equipmentBytes[7],  stain.Value),
            2  => SetIfDifferent(ref _equipmentBytes[11], stain.Value),
            3  => SetIfDifferent(ref _equipmentBytes[15], stain.Value),
            4  => SetIfDifferent(ref _equipmentBytes[19], stain.Value),
            5  => SetIfDifferent(ref _equipmentBytes[23], stain.Value),
            6  => SetIfDifferent(ref _equipmentBytes[27], stain.Value),
            7  => SetIfDifferent(ref _equipmentBytes[31], stain.Value),
            8  => SetIfDifferent(ref _equipmentBytes[35], stain.Value),
            9  => SetIfDifferent(ref _equipmentBytes[39], stain.Value),
            10 => SetIfDifferent(ref _equipmentBytes[43], stain.Value),
            11 => SetIfDifferent(ref _equipmentBytes[47], stain.Value),
            _  => false,
        };

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
        => (_states & 0x08) == 0x09;

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
        }

        SetItem(EquipSlot.MainHand, items.DefaultSword);
        SetStain(EquipSlot.MainHand, 0);
        SetItem(EquipSlot.OffHand, ItemManager.NothingItem(FullEquipType.Shield));
        SetStain(EquipSlot.OffHand, 0);
    }

    public void LoadNonHuman(uint modelId, Customize customize, byte* equipData)
    {
        ModelId = modelId;
        Customize.Load(customize);
        fixed (byte* ptr = _equipmentBytes)
        {
            MemoryUtility.MemCpyUnchecked(ptr, equipData, 40);
        }
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


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool SetIfDifferent<T>(ref T old, T value) where T : IEquatable<T>
    {
        if (old.Equals(value))
            return false;

        old = value;
        return true;
    }
}
