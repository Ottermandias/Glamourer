using Glamourer.GameData;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;

namespace Glamourer.Designs;

public unsafe struct DesignData
{
    private       string                 _nameHead     = string.Empty;
    private       string                 _nameBody     = string.Empty;
    private       string                 _nameHands    = string.Empty;
    private       string                 _nameLegs     = string.Empty;
    private       string                 _nameFeet     = string.Empty;
    private       string                 _nameEars     = string.Empty;
    private       string                 _nameNeck     = string.Empty;
    private       string                 _nameWrists   = string.Empty;
    private       string                 _nameRFinger  = string.Empty;
    private       string                 _nameLFinger  = string.Empty;
    private       string                 _nameMainhand = string.Empty;
    private       string                 _nameOffhand  = string.Empty;
    private fixed uint                   _itemIds[12];
    private fixed ushort                 _iconIds[12];
    private fixed byte                   _equipmentBytes[48];
    public        CustomizeParameterData Parameters;
    public        CustomizeArray         Customize = CustomizeArray.Default;
    public        uint                   ModelId;
    public        CrestFlag              CrestVisibility;
    private       SecondaryId            _secondaryMainhand;
    private       SecondaryId            _secondaryOffhand;
    private       FullEquipType          _typeMainhand;
    private       FullEquipType          _typeOffhand;
    private       byte                   _states;
    public        bool                   IsHuman = true;

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

    public readonly StainIds Stain(EquipSlot slot)
    {
        var index = slot.ToIndex();
        return index > 11 ? StainIds.None : new(_equipmentBytes[4 * index + 3], _equipmentBytes[4 * index + 3]);
    }

    public readonly bool Crest(CrestFlag slot)
        => CrestVisibility.HasFlag(slot);


    public readonly FullEquipType MainhandType
        => _typeMainhand;

    public readonly FullEquipType OffhandType
        => _typeOffhand;

    public readonly EquipItem Item(EquipSlot slot)
        => slot.ToIndex() switch
        {
            // @formatter:off
            0  => EquipItem.FromIds((ItemId)_itemIds[ 0], (IconId)_iconIds[ 0], (PrimaryId)(_equipmentBytes[ 0] | (_equipmentBytes[ 1] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[ 2], FullEquipType.Head,   name: _nameHead    ),
            1  => EquipItem.FromIds((ItemId)_itemIds[ 1], (IconId)_iconIds[ 1], (PrimaryId)(_equipmentBytes[ 4] | (_equipmentBytes[ 5] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[ 6], FullEquipType.Body,   name: _nameBody    ),
            2  => EquipItem.FromIds((ItemId)_itemIds[ 2], (IconId)_iconIds[ 2], (PrimaryId)(_equipmentBytes[ 8] | (_equipmentBytes[ 9] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[10], FullEquipType.Hands,  name: _nameHands   ),
            3  => EquipItem.FromIds((ItemId)_itemIds[ 3], (IconId)_iconIds[ 3], (PrimaryId)(_equipmentBytes[12] | (_equipmentBytes[13] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[14], FullEquipType.Legs,   name: _nameLegs    ),
            4  => EquipItem.FromIds((ItemId)_itemIds[ 4], (IconId)_iconIds[ 4], (PrimaryId)(_equipmentBytes[16] | (_equipmentBytes[17] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[18], FullEquipType.Feet,   name: _nameFeet    ),
            5  => EquipItem.FromIds((ItemId)_itemIds[ 5], (IconId)_iconIds[ 5], (PrimaryId)(_equipmentBytes[20] | (_equipmentBytes[21] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[22], FullEquipType.Ears,   name: _nameEars    ),
            6  => EquipItem.FromIds((ItemId)_itemIds[ 6], (IconId)_iconIds[ 6], (PrimaryId)(_equipmentBytes[24] | (_equipmentBytes[25] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[26], FullEquipType.Neck,   name: _nameNeck    ),
            7  => EquipItem.FromIds((ItemId)_itemIds[ 7], (IconId)_iconIds[ 7], (PrimaryId)(_equipmentBytes[28] | (_equipmentBytes[29] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[30], FullEquipType.Wrists, name: _nameWrists  ),
            8  => EquipItem.FromIds((ItemId)_itemIds[ 8], (IconId)_iconIds[ 8], (PrimaryId)(_equipmentBytes[32] | (_equipmentBytes[33] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[34], FullEquipType.Finger, name: _nameRFinger ),
            9  => EquipItem.FromIds((ItemId)_itemIds[ 9], (IconId)_iconIds[ 9], (PrimaryId)(_equipmentBytes[36] | (_equipmentBytes[37] << 8)), (SecondaryId)0,     (Variant)_equipmentBytes[38], FullEquipType.Finger, name: _nameLFinger ),
            10 => EquipItem.FromIds((ItemId)_itemIds[10], (IconId)_iconIds[10], (PrimaryId)(_equipmentBytes[40] | (_equipmentBytes[41] << 8)), _secondaryMainhand, (Variant)_equipmentBytes[42], _typeMainhand,        name: _nameMainhand),
            11 => EquipItem.FromIds((ItemId)_itemIds[11], (IconId)_iconIds[11], (PrimaryId)(_equipmentBytes[44] | (_equipmentBytes[45] << 8)), _secondaryOffhand,  (Variant)_equipmentBytes[46], _typeOffhand,         name: _nameOffhand ),
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
        _iconIds[index]                = (ushort)item.IconId.Id;
        _equipmentBytes[4 * index + 0] = (byte)item.PrimaryId.Id;
        _equipmentBytes[4 * index + 1] = (byte)(item.PrimaryId.Id >> 8);
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
                _secondaryMainhand = item.SecondaryId;
                _typeMainhand      = item.Type;
                return true;
            case 11:
                _nameOffhand      = item.Name;
                _secondaryOffhand = item.SecondaryId;
                _typeOffhand      = item.Type;
                return true;
        }

        return true;
    }

    public bool SetStain(EquipSlot slot, StainIds stains)
        => slot.ToIndex() switch
        {
            // Those need to be changed

            0  => SetIfDifferent(ref _equipmentBytes[3],  stains),
            1  => SetIfDifferent(ref _equipmentBytes[7],  stains),
            2  => SetIfDifferent(ref _equipmentBytes[11], stains),
            3  => SetIfDifferent(ref _equipmentBytes[15], stains),
            4  => SetIfDifferent(ref _equipmentBytes[19], stains),
            5  => SetIfDifferent(ref _equipmentBytes[23], stains),
            6  => SetIfDifferent(ref _equipmentBytes[27], stains),
            7  => SetIfDifferent(ref _equipmentBytes[31], stains),
            8  => SetIfDifferent(ref _equipmentBytes[35], stains),
            9  => SetIfDifferent(ref _equipmentBytes[39], stains),
            10 => SetIfDifferent(ref _equipmentBytes[43], stains),
            11 => SetIfDifferent(ref _equipmentBytes[47], stains),
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

    public readonly bool GetMeta(MetaIndex index)
        => index switch
        {
            MetaIndex.Wetness     => IsWet(),
            MetaIndex.HatState    => IsHatVisible(),
            MetaIndex.VisorState  => IsVisorToggled(),
            MetaIndex.WeaponState => IsWeaponVisible(),
            _                     => false,
        };

    public bool SetMeta(MetaIndex index, bool value)
        => index switch
        {
            MetaIndex.Wetness     => SetIsWet(value),
            MetaIndex.HatState    => SetHatVisible(value),
            MetaIndex.VisorState  => SetVisor(value),
            MetaIndex.WeaponState => SetWeaponVisible(value),
            _                     => false,
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
            SetStain(slot, StainIds.None);
            SetCrest(slot.ToCrestFlag(), false);
        }

        SetItem(EquipSlot.MainHand, items.DefaultSword);
        SetStain(EquipSlot.MainHand, StainIds.None);
        SetCrest(CrestFlag.MainHand, false);
        SetItem(EquipSlot.OffHand, ItemManager.NothingItem(FullEquipType.Shield));
        SetStain(EquipSlot.OffHand, StainIds.None);
        SetCrest(CrestFlag.OffHand, false);
    }


    public bool LoadNonHuman(uint modelId, CustomizeArray customize, nint equipData)
    {
        ModelId = modelId;
        IsHuman = false;
        Customize.Read(customize.Data);
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
        var ret = new byte[CustomizeArray.Size];
        fixed (byte* retPtr = ret, inPtr = Customize.Data)
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
