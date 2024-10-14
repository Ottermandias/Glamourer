using Glamourer.GameData;
using Glamourer.Services;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String.Functions;

namespace Glamourer.Designs;

public unsafe struct DesignData
{
    public const int NumEquipment      = 10;
    public const int EquipmentByteSize = NumEquipment * CharacterArmor.Size;
    public const int NumBonusItems     = 1;
    public const int NumWeapons        = 2;

    private string _nameHead     = string.Empty;
    private string _nameBody     = string.Empty;
    private string _nameHands    = string.Empty;
    private string _nameLegs     = string.Empty;
    private string _nameFeet     = string.Empty;
    private string _nameEars     = string.Empty;
    private string _nameNeck     = string.Empty;
    private string _nameWrists   = string.Empty;
    private string _nameRFinger  = string.Empty;
    private string _nameLFinger  = string.Empty;
    private string _nameMainhand = string.Empty;
    private string _nameOffhand  = string.Empty;
    private string _nameGlasses  = string.Empty;

    private fixed uint                   _itemIds[NumEquipment + NumWeapons];
    private fixed uint                   _iconIds[NumEquipment + NumWeapons + NumBonusItems];
    private fixed byte                   _equipmentBytes[EquipmentByteSize + NumWeapons * CharacterWeapon.Size];
    private fixed ushort                 _bonusIds[NumBonusItems];
    private fixed ushort                 _bonusModelIds[NumBonusItems];
    private fixed byte                   _bonusVariants[NumBonusItems];
    public        CustomizeParameterData Parameters;
    public        CustomizeArray         Customize = CustomizeArray.Default;
    public        uint                   ModelId;
    public        CrestFlag              CrestVisibility;
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
         || name.IsContained(_nameOffhand)
         || name.IsContained(_nameGlasses);

    public readonly StainIds Stain(EquipSlot slot)
    {
        var index = slot.ToIndex();
        return index switch
        {
            < 10 => new StainIds(_equipmentBytes[CharacterArmor.Size * index + 3], _equipmentBytes[CharacterArmor.Size * index + 4]),
            10   => new StainIds(_equipmentBytes[EquipmentByteSize + 6],           _equipmentBytes[EquipmentByteSize + 7]),
            11   => new StainIds(_equipmentBytes[EquipmentByteSize + 14],          _equipmentBytes[EquipmentByteSize + 15]),
            _    => StainIds.None,
        };
    }

    public readonly bool Crest(CrestFlag slot)
        => CrestVisibility.HasFlag(slot);


    public readonly FullEquipType MainhandType
        => _typeMainhand;

    public readonly FullEquipType OffhandType
        => _typeOffhand;

    public readonly EquipItem Item(EquipSlot slot)
    {
        fixed (byte* ptr = _equipmentBytes)
        {
            return slot.ToIndex() switch
            {
            // @formatter:off
            0  => EquipItem.FromIds(_itemIds[ 0], _iconIds[ 0], ((CharacterArmor*)ptr)[0].Set, 0, ((CharacterArmor*)ptr)[0].Variant, FullEquipType.Head,   name: _nameHead    ),
            1  => EquipItem.FromIds(_itemIds[ 1], _iconIds[ 1], ((CharacterArmor*)ptr)[1].Set, 0, ((CharacterArmor*)ptr)[1].Variant, FullEquipType.Body,   name: _nameBody    ),
            2  => EquipItem.FromIds(_itemIds[ 2], _iconIds[ 2], ((CharacterArmor*)ptr)[2].Set, 0, ((CharacterArmor*)ptr)[2].Variant, FullEquipType.Hands,  name: _nameHands   ),
            3  => EquipItem.FromIds(_itemIds[ 3], _iconIds[ 3], ((CharacterArmor*)ptr)[3].Set, 0, ((CharacterArmor*)ptr)[3].Variant, FullEquipType.Legs,   name: _nameLegs    ),
            4  => EquipItem.FromIds(_itemIds[ 4], _iconIds[ 4], ((CharacterArmor*)ptr)[4].Set, 0, ((CharacterArmor*)ptr)[4].Variant, FullEquipType.Feet,   name: _nameFeet    ),
            5  => EquipItem.FromIds(_itemIds[ 5], _iconIds[ 5], ((CharacterArmor*)ptr)[5].Set, 0, ((CharacterArmor*)ptr)[5].Variant, FullEquipType.Ears,   name: _nameEars    ),
            6  => EquipItem.FromIds(_itemIds[ 6], _iconIds[ 6], ((CharacterArmor*)ptr)[6].Set, 0, ((CharacterArmor*)ptr)[6].Variant, FullEquipType.Neck,   name: _nameNeck    ),
            7  => EquipItem.FromIds(_itemIds[ 7], _iconIds[ 7], ((CharacterArmor*)ptr)[7].Set, 0, ((CharacterArmor*)ptr)[7].Variant, FullEquipType.Wrists, name: _nameWrists  ),
            8  => EquipItem.FromIds(_itemIds[ 8], _iconIds[ 8], ((CharacterArmor*)ptr)[8].Set, 0, ((CharacterArmor*)ptr)[8].Variant, FullEquipType.Finger, name: _nameRFinger ),
            9  => EquipItem.FromIds(_itemIds[ 9], _iconIds[ 9], ((CharacterArmor*)ptr)[9].Set, 0, ((CharacterArmor*)ptr)[9].Variant, FullEquipType.Finger, name: _nameLFinger ),
            10 => EquipItem.FromIds(_itemIds[10], _iconIds[10], *(PrimaryId*)(ptr + EquipmentByteSize + 0), *(SecondaryId*)(ptr + EquipmentByteSize + 2), *(Variant*)(ptr + EquipmentByteSize + 4), _typeMainhand, name: _nameMainhand),
            11 => EquipItem.FromIds(_itemIds[11], _iconIds[11], *(PrimaryId*)(ptr + EquipmentByteSize + 8), *(SecondaryId*)(ptr + EquipmentByteSize + 10),  *(Variant*)(ptr + EquipmentByteSize + 12), _typeOffhand,  name: _nameOffhand ),
            _  => new EquipItem(),
                // @formatter:on
            };
        }
    }

    public readonly EquipItem BonusItem(BonusItemFlag slot)
        => slot switch
        {
            // @formatter:off
            BonusItemFlag.Glasses => EquipItem.FromBonusIds(_bonusIds[0], _iconIds[12], _bonusModelIds[0], _bonusVariants[0], BonusItemFlag.Glasses, _nameGlasses),
            _                     => EquipItem.BonusItemNothing(slot),
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
            var weaponPtr = (CharacterWeapon*)(ptr + EquipmentByteSize);
            return weaponPtr[slot is EquipSlot.MainHand ? 0 : 1];
        }
    }

    public bool SetItem(EquipSlot slot, EquipItem item)
    {
        var index = slot.ToIndex();
        if (index > NumEquipment + NumWeapons)
            return false;

        _itemIds[index]                                  = item.ItemId.Id;
        _iconIds[index]                                  = item.IconId.Id;
        _equipmentBytes[CharacterArmor.Size * index + 0] = (byte)item.PrimaryId.Id;
        _equipmentBytes[CharacterArmor.Size * index + 1] = (byte)(item.PrimaryId.Id >> 8);
        _equipmentBytes[CharacterArmor.Size * index + 2] = item.Variant.Id;
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
                _nameMainhand                          = item.Name;
                _equipmentBytes[EquipmentByteSize + 2] = (byte)item.SecondaryId.Id;
                _equipmentBytes[EquipmentByteSize + 3] = (byte)(item.SecondaryId.Id >> 8);
                _equipmentBytes[EquipmentByteSize + 4] = item.Variant.Id;
                _typeMainhand                          = item.Type;
                return true;
            case 11:
                _nameOffhand                            = item.Name;
                _equipmentBytes[EquipmentByteSize + 10] = (byte)item.SecondaryId.Id;
                _equipmentBytes[EquipmentByteSize + 11] = (byte)(item.SecondaryId.Id >> 8);
                _equipmentBytes[EquipmentByteSize + 12] = item.Variant.Id;
                _typeOffhand                            = item.Type;
                return true;
        }

        return true;
    }

    public bool SetBonusItem(BonusItemFlag slot, EquipItem item)
    {
        var index = slot.ToIndex();
        if (index > NumBonusItems)
            return false;

        _iconIds[NumEquipment + NumWeapons + index] = item.IconId.Id;
        _bonusIds[index]                            = item.Id.BonusItem.Id;
        _bonusModelIds[index]                       = item.PrimaryId.Id;
        _bonusVariants[index]                       = item.Variant.Id;
        switch (index)
        {
            case 0:
                _nameGlasses = item.Name;
                return true;
            default: return false;
        }
    }

    public bool SetStain(EquipSlot slot, StainIds stains)
        => slot.ToIndex() switch
        {
            // @formatter:off
             0 => SetIfDifferent(ref _equipmentBytes[0 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[0 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             1 => SetIfDifferent(ref _equipmentBytes[1 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[1 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             2 => SetIfDifferent(ref _equipmentBytes[2 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[2 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             3 => SetIfDifferent(ref _equipmentBytes[3 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[3 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             4 => SetIfDifferent(ref _equipmentBytes[4 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[4 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             5 => SetIfDifferent(ref _equipmentBytes[5 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[5 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             6 => SetIfDifferent(ref _equipmentBytes[6 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[6 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             7 => SetIfDifferent(ref _equipmentBytes[7 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[7 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             8 => SetIfDifferent(ref _equipmentBytes[8 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[8 * CharacterArmor.Size + 4],  stains.Stain2.Id),
             9 => SetIfDifferent(ref _equipmentBytes[9 * CharacterArmor.Size + 3], stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[9 * CharacterArmor.Size + 4],  stains.Stain2.Id),
            10 => SetIfDifferent(ref _equipmentBytes[EquipmentByteSize + 6],      stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[EquipmentByteSize + 7],        stains.Stain2.Id),
            11 => SetIfDifferent(ref _equipmentBytes[EquipmentByteSize + 14],     stains.Stain1.Id) | SetIfDifferent(ref _equipmentBytes[EquipmentByteSize + 15],       stains.Stain2.Id),
            _ => false,
            // @formatter:on
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
        SetDefaultBonusItems();
    }

    public void SetDefaultBonusItems()
    {
        foreach (var slot in BonusExtensions.AllFlags)
            SetBonusItem(slot, EquipItem.BonusItemNothing(slot));
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

        fixed (uint* ptr = _iconIds)
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
        _nameGlasses = string.Empty;
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
        var ret = new byte[80];
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
            var data = new Span<byte>(dataPtr, 80);
            return Convert.TryFromBase64String(base64, data, out var written) && written == 80;
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
