using Glamourer.Events;
using Glamourer.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.State;

/// <remarks>Currently unused.</remarks>
public unsafe struct WeaponState
{
    private fixed ulong _weapons[FullEquipTypeExtensions.NumWeaponTypes];
    private fixed byte  _sources[FullEquipTypeExtensions.NumWeaponTypes];

    public CustomItemId? this[FullEquipType type]
    {
        get
        {
            if (!ToIndex(type, out var idx))
                return null;

            var weapon = _weapons[idx];
            if (weapon == 0)
                return null;

            return new CustomItemId(weapon);
        }
    }

    public EquipItem Get(ItemManager items, EquipItem value)
    {
        var id = this[value.Type];
        if (id == null)
            return value;

        var item = items.Resolve(value.Type, id.Value);
        return item.Type != value.Type ? value : item;
    }

    public void Set(FullEquipType type, EquipItem value, StateChanged.Source source)
    {
        if (!ToIndex(type, out var idx))
            return;

        _weapons[idx] = value.Id.Id;
        _sources[idx] = (byte)source;
    }

    public void RemoveFixedDesignSources()
    {
        for (var i = 0; i < FullEquipTypeExtensions.NumWeaponTypes; ++i)
        {
            if (_sources[i] is (byte) StateChanged.Source.Fixed)
                _sources[i] = (byte) StateChanged.Source.Manual;
        }
    }

    private static bool ToIndex(FullEquipType type, out int index)
    {
        index = ToIndex(type);
        return index is >= 0 and < FullEquipTypeExtensions.NumWeaponTypes;
    }

    private static int ToIndex(FullEquipType type)
        => (int)type - FullEquipTypeExtensions.WeaponTypesOffset;
}
