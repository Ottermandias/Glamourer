using System;
using Glamourer.Customization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using CustomizeData = Penumbra.GameData.Structs.CustomizeData;

namespace Glamourer.Saves;

public class EquipmentDesign
{
    private Data _data = default;

    // @formatter:off
    //public Slot Head     => new(ref _data, 0);
    //public Slot Body     => new(ref _data, 1);
    //public Slot Hands    => new(ref _data, 2);
    //public Slot Legs     => new(ref _data, 3);
    //public Slot Feet     => new(ref _data, 4);
    //public Slot Ears     => new(ref _data, 5);
    //public Slot Neck     => new(ref _data, 6);
    //public Slot Wrist    => new(ref _data, 7);
    //public Slot RFinger  => new(ref _data, 8);
    //public Slot LFinger  => new(ref _data, 9);
    //public Slot MainHand => new(ref _data, 10);
    //public Slot OffHand  => new(ref _data, 11);
    //// @formatter:on
    //
    //public Slot this[EquipSlot slot]
    //    => new(ref _data, (int)slot.ToIndex());
    //
    //public Slot this[int idx]
    //    => idx is >= 0 and < Data.NumEquipment ? new Slot(ref _data, idx) : throw new IndexOutOfRangeException();

    public unsafe struct Data
    {
        public const int NumEquipment = 12;

        public fixed uint   Ids[NumEquipment];
        public fixed byte   Stains[NumEquipment];
        public       ushort Flags;
        public       ushort StainFlags;
    }

    //public ref struct Slot
    //{
    //    private readonly ref Data   _data;
    //    private readonly     int    _index;
    //    private readonly     ushort _flag;
    //
    //    public Slot(ref Data data, int idx)
    //    {
    //        _data  = data;
    //        _index = idx;
    //        _flag  = (ushort)(1 << idx);
    //    }
    //
    //    public unsafe uint ItemId
    //    {
    //        get => _data.Ids[_index];
    //        set => _data.Ids[_index] = value;
    //    }
    //
    //    public unsafe StainId StainId
    //    {
    //        get => _data.Stains[_index];
    //        set => _data.Stains[_index] = value.Value;
    //    }
    //
    //    public bool ApplyItem
    //    {
    //        get => (_data.Flags & _flag) != 0;
    //        set => _data.Flags = (ushort)(value ? _data.Flags | _flag : _data.Flags & ~_flag);
    //    }
    //
    //    public bool ApplyStain
    //    {
    //        get => (_data.StainFlags & _flag) != 0;
    //        set => _data.StainFlags = (ushort)(value ? _data.StainFlags | _flag : _data.StainFlags & ~_flag);
    //    }
    //}
}

public class HumanDesign
{
    public unsafe struct Data
    {
        public CustomizeData  Values;
        public CustomizeFlag Flag;
    }

    //public ref struct Choice<T> where T : unmanaged
    //{
    //    private readonly ref Data           _data;
    //    private readonly     CustomizeFlag _flag;
    //
    //    public Choice(ref Data data, CustomizeFlag flag)
    //    {
    //        _data = data;
    //        _flag = flag;
    //    }
    //
    //    public bool ApplyChoice
    //    {
    //        get => _data.Flag.HasFlag(_flag);
    //        set => _data.Flag = value ? _data.Flag | _flag : _data.Flag & ~_flag;
    //    }
    //}
}

public class Design
{
    public string Name        { get; private set; }
    public string Description { get; private set; }

    public DateTimeOffset CreationDate   { get; }
    public DateTimeOffset LastUpdateDate { get; private set; }

    public bool ReadOnly { get; private set; }

    public EquipmentDesign? Equipment;
    public HumanDesign?     Customize;

    public string WriteJson()
    {
        return string.Empty;
    }
}

public struct DesignSaveV1
{
    public string Name;
    public string Description;

    public ulong CreationDate;
    public ulong LastUpdateDate;

    public EquipmentPiece Head;
    public EquipmentPiece Body;
    public EquipmentPiece Hands;
    public EquipmentPiece Legs;
    public EquipmentPiece Feet;
    public EquipmentPiece Ears;
    public EquipmentPiece Neck;
    public EquipmentPiece Wrists;
    public EquipmentPiece LFinger;
    public EquipmentPiece RFinger;

    public EquipmentPiece MainHand;
    public EquipmentPiece OffHand;

    public CustomizationChoice<uint>    ModelId;
    public CustomizationChoice<Race>    Race;
    public CustomizationChoice<Gender>  Gender;
    public CustomizationChoice          BodyType;
    public CustomizationChoice          Height;
    public CustomizationChoice<SubRace> Clan;
    public CustomizationChoice          Face;
    public CustomizationChoice          Hairstyle;
    public CustomizationChoice<bool>    Highlights;
    public CustomizationChoice          SkinColor;
    public CustomizationChoice          EyeColorRight;
    public CustomizationChoice          HairColor;
    public CustomizationChoice          HighlightsColor;
    public CustomizationChoice<bool>    FacialFeature1;
    public CustomizationChoice<bool>    FacialFeature2;
    public CustomizationChoice<bool>    FacialFeature3;
    public CustomizationChoice<bool>    FacialFeature4;
    public CustomizationChoice<bool>    FacialFeature5;
    public CustomizationChoice<bool>    FacialFeature6;
    public CustomizationChoice<bool>    FacialFeature7;
    public CustomizationChoice<bool>    LegacyTattoo;
    public CustomizationChoice          TattooColor;
    public CustomizationChoice          Eyebrows;
    public CustomizationChoice          EyeColorLeft;
    public CustomizationChoice          EyeShape;
    public CustomizationChoice<bool>    SmallIris;
    public CustomizationChoice          Nose;
    public CustomizationChoice          Jaw;
    public CustomizationChoice          Mouth;
    public CustomizationChoice<bool>    Lipstick;
    public CustomizationChoice          MuscleMass;
    public CustomizationChoice          TailShape;
    public CustomizationChoice          BustSize;
    public CustomizationChoice          FacePaint;
    public CustomizationChoice<bool>    FacePaintReversed;
    public CustomizationChoice          FacePaintColor;

    public bool ReadOnly;

    public override string ToString()
        => Name;
}

public struct EquipmentPiece
{
    public uint    Item;
    public bool    ApplyItem;
    public StainId Stain;
    public bool    ApplyStain;
}

public struct CustomizationChoice
{
    public CustomizeValue Value;
    public bool                   Apply;
}

public struct CustomizationChoice<T> where T : struct
{
    public T    Value;
    public bool Apply;
}
