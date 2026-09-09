using System.Collections.Immutable;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using ImSharp;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;

namespace Glamourer.Interop.Material;

[JsonConverter(typeof(Converter))]
public readonly record struct MaterialValueIndex(
    MaterialValueIndex.DrawObjectType DrawObject,
    byte SlotIndex,
    byte MaterialIndex,
    byte RowIndex)
{
    public static readonly MaterialValueIndex Invalid = new(DrawObjectType.Invalid, 0, 0, 0);

    public static readonly MaterialValueIndex Hair          = Min(DrawObjectType.Human, 10);
    public static readonly MaterialValueIndex Face          = Min(DrawObjectType.Human, 11);
    public static readonly MaterialValueIndex RacialFeature = Min(DrawObjectType.Human, 12);
    public static readonly MaterialValueIndex Connector1    = Min(DrawObjectType.Human, 13);
    public static readonly MaterialValueIndex Connector2    = Min(DrawObjectType.Human, 14);
    public static readonly MaterialValueIndex Body3         = Min(DrawObjectType.Human, 15);

    public static readonly ImmutableArray<MaterialValueIndex> AllSlots =
    [
        // Show equipment/weapon/bonus slots in an order consistent with the EquipmentDrawer.
        ..EquipSlotExtensions.FullSlots.Select(FromSlot).OrderBy(static index => index.Key), ..BonusExtensions.AllFlags.Select(FromSlot),
        Hair, Face, RacialFeature, Connector1, Connector2, Body3,
    ];

    #region Prepared StringU8 slot names - see SlotName().

    private static readonly StringU8 SlotNameInvalid    = new("Invalid"u8);
    private static readonly StringU8 SlotNameHair       = BodySlot.Hair.StringU8;
    private static readonly StringU8 SlotNameFace       = BodySlot.Face.StringU8;
    private static readonly StringU8 SlotNameTailEar    = new($"{BodySlot.Tail} / {BodySlot.Ear}");
    private static readonly StringU8 SlotNameConnector1 = new("Connector 1"u8);
    private static readonly StringU8 SlotNameConnector2 = new("Connector 2"u8);
    private static readonly StringU8 SlotNameBody3      = new("Body 3"u8);

    #endregion

    public uint Key
        => ToKey(DrawObject, SlotIndex, MaterialIndex, RowIndex);

    public bool Valid
        => Validate(DrawObject) && ValidateSlot(DrawObject, SlotIndex) && ValidateMaterial(MaterialIndex) && ValidateRow(RowIndex);

    public static bool FromKey(uint key, out MaterialValueIndex index)
    {
        index = new MaterialValueIndex(key);
        return index.Valid;
    }

    public static MaterialValueIndex FromSlot(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.MainHand: return new MaterialValueIndex(DrawObjectType.Mainhand, 0, 0, 0);
            case EquipSlot.OffHand:  return new MaterialValueIndex(DrawObjectType.Offhand,  0, 0, 0);
        }

        var idx = slot.ToIndex();
        return idx < 10 ? new MaterialValueIndex(DrawObjectType.Human, (byte)idx, 0, 0) : Invalid;
    }

    public static MaterialValueIndex FromSlot(BonusItemFlag slot)
    {
        var idx = slot.ToIndex();
        return idx > 2 ? Invalid : new MaterialValueIndex(DrawObjectType.Human, (byte)(idx + 16), 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SlotEquals(MaterialValueIndex other)
        => DrawObject == other.DrawObject && SlotIndex == other.SlotIndex;

    // Should be kept in sync with its UTF-16 equivalent, SlotString().
    public StringU8 SlotName()
        => DrawObject switch
        {
            DrawObjectType.Invalid                      => SlotNameInvalid,
            DrawObjectType.Human when SlotIndex < 10    => ((uint)SlotIndex).ToEquipSlot().ToNameU8(),
            DrawObjectType.Human when SlotIndex is 10   => SlotNameHair,
            DrawObjectType.Human when SlotIndex is 11   => SlotNameFace,
            DrawObjectType.Human when SlotIndex is 12   => SlotNameTailEar,
            DrawObjectType.Human when SlotIndex is 13   => SlotNameConnector1,
            DrawObjectType.Human when SlotIndex is 14   => SlotNameConnector2,
            DrawObjectType.Human when SlotIndex is 15   => SlotNameBody3,
            DrawObjectType.Human when SlotIndex is 16   => BonusItemFlag.Glasses.ToNameU8(),
            DrawObjectType.Human when SlotIndex is 17   => BonusItemFlag.UnkSlot.ToNameU8(),
            DrawObjectType.Mainhand when SlotIndex is 0 => EquipSlot.MainHand.ToNameU8(),
            DrawObjectType.Offhand when SlotIndex is 0  => EquipSlot.OffHand.ToNameU8(),
            _                                           => new StringU8($"{DrawObject} Slot {SlotIndex}"),
        };

    public EquipSlot ToEquipSlot()
        => DrawObject switch
        {
            DrawObjectType.Human when SlotIndex < 10    => ((uint)SlotIndex).ToEquipSlot(),
            DrawObjectType.Mainhand when SlotIndex is 0 => EquipSlot.MainHand,
            DrawObjectType.Offhand when SlotIndex is 0  => EquipSlot.OffHand,
            _                                           => EquipSlot.Unknown,
        };

    public BonusItemFlag ToBonusSlot()
        => DrawObject switch
        {
            DrawObjectType.Human when SlotIndex > 15 => ((uint)SlotIndex - 16).ToBonusSlot(),
            _                                        => BonusItemFlag.Unknown,
        };

    public ModelCombinedSlots ToCombinedSlot()
        => DrawObject switch
        {
            DrawObjectType.Human when SlotIndex < 18    => (ModelCombinedSlots)((ulong)ModelCombinedSlots.Head << SlotIndex),
            DrawObjectType.Mainhand when SlotIndex is 0 => ModelCombinedSlots.Mainhand,
            DrawObjectType.Offhand when SlotIndex is 0  => ModelCombinedSlots.Offhand,
            _                                           => 0,
        };

    public unsafe bool TryGetModel(Actor actor, out Model model)
    {
        if (!actor.Valid)
        {
            model = Model.Null;
            return false;
        }

        model = DrawObject switch
        {
            DrawObjectType.Human    => actor.Model,
            DrawObjectType.Mainhand => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[0].DrawData.DrawObject : Model.Null,
            DrawObjectType.Offhand  => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[1].DrawData.DrawObject : Model.Null,
            _                       => Model.Null,
        };
        return model.IsCharacterBase;
    }

    public unsafe bool TryGetTextures(Actor actor, out ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures,
        out ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<MaterialResourceHandle>> materials)
    {
        if (!TryGetModel(actor, out var model)
         || SlotIndex >= model.AsCharacterBase->SlotCount
         || model.AsCharacterBase->ColorTableTexturesSpan.Length < (SlotIndex + 1) * MaterialService.MaterialsPerModel)
        {
            textures  = [];
            materials = [];
            return false;
        }

        var from = SlotIndex * MaterialService.MaterialsPerModel;
        textures  = model.AsCharacterBase->ColorTableTexturesSpan.Slice(from, MaterialService.MaterialsPerModel);
        materials = model.AsCharacterBase->MaterialsSpan.Slice(from, MaterialService.MaterialsPerModel);
        return true;
    }

    public unsafe bool TryGetTextures(Actor actor, out ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures)
    {
        if (!TryGetModel(actor, out var model)
         || SlotIndex >= model.AsCharacterBase->SlotCount
         || model.AsCharacterBase->ColorTableTexturesSpan.Length < (SlotIndex + 1) * MaterialService.MaterialsPerModel)
        {
            textures = [];
            return false;
        }

        var from = SlotIndex * MaterialService.MaterialsPerModel;
        textures = model.AsCharacterBase->ColorTableTexturesSpan.Slice(from, MaterialService.MaterialsPerModel);
        return true;
    }


    public unsafe bool TryGetTexture(Actor actor, out Texture** texture)
    {
        if (TryGetTextures(actor, out var textures))
            return TryGetTexture(textures, out texture);

        texture = null;
        return false;
    }

    public unsafe bool TryGetTexture(Actor actor, out Texture** texture, out ColorRow.Mode mode)
    {
        if (TryGetTextures(actor, out var textures, out var materials))
            return TryGetTexture(textures, materials, out texture, out mode);

        mode    = ColorRow.Mode.Dawntrail;
        texture = null;
        return false;
    }

    public unsafe bool TryGetTexture(ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures,
        ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<MaterialResourceHandle>> materials,
        out Texture** texture, out ColorRow.Mode mode)
    {
        mode = MaterialIndex >= materials.Length
            ? ColorRow.Mode.Dawntrail
            : PrepareColorSet.GetMode(materials[MaterialIndex].Value);


        if (MaterialIndex >= textures.Length || textures[MaterialIndex].Value == null)
        {
            texture = null;
            return false;
        }

        fixed (FFXIVClientStructs.Interop.Pointer<Texture>* ptr = textures)
        {
            texture = (Texture**)ptr + MaterialIndex;
        }

        return true;
    }

    public unsafe bool TryGetTexture(ReadOnlySpan<FFXIVClientStructs.Interop.Pointer<Texture>> textures, out Texture** texture)
    {
        if (MaterialIndex >= textures.Length || textures[MaterialIndex].Value == null)
        {
            texture = null;
            return false;
        }

        fixed (FFXIVClientStructs.Interop.Pointer<Texture>* ptr = textures)
        {
            texture = (Texture**)ptr + MaterialIndex;
        }

        return true;
    }


    public static MaterialValueIndex FromKey(uint key)
        => new(key);

    public static MaterialValueIndex Min(DrawObjectType drawObject = 0, byte slotIndex = 0, byte materialIndex = 0, byte rowIndex = 0)
        => new(drawObject, slotIndex, materialIndex, rowIndex);

    public static MaterialValueIndex Min(ModelCombinedSlots slot, byte materialIndex = 0, byte rowIndex = 0)
    {
        // The slot mask must have one and exactly one bit set.
        if (slot is 0 || slot.ExceptFirst is not 0)
            return new MaterialValueIndex(DrawObjectType.Invalid, 0, materialIndex, rowIndex);

        var index = unchecked((byte)BitOperations.TrailingZeroCount((ulong)slot));
        return index switch
        {
            < 18 => new MaterialValueIndex(DrawObjectType.Human,    index, materialIndex, rowIndex),
            32   => new MaterialValueIndex(DrawObjectType.Mainhand, 0,     materialIndex, rowIndex),
            40   => new MaterialValueIndex(DrawObjectType.Offhand,  0,     materialIndex, rowIndex),
            _    => new MaterialValueIndex(DrawObjectType.Invalid,  0,     materialIndex, rowIndex),
        };
    }

    public static MaterialValueIndex Max(DrawObjectType drawObject = (DrawObjectType)byte.MaxValue, byte slotIndex = byte.MaxValue,
        byte materialIndex = byte.MaxValue, byte rowIndex = byte.MaxValue)
        => new(drawObject, slotIndex, materialIndex, rowIndex);

    public static MaterialValueIndex Max(ModelCombinedSlots combinedItemSlot, byte materialIndex = byte.MaxValue,
        byte rowIndex = byte.MaxValue)
        => Min(combinedItemSlot, materialIndex, rowIndex);

    public enum DrawObjectType : byte
    {
        Invalid,
        Human,
        Mainhand,
        Offhand,
    };

    public static bool Validate(DrawObjectType type)
        => type is not DrawObjectType.Invalid && Enum.IsDefined(type);

    public static bool ValidateSlot(DrawObjectType type, byte slotIndex)
        => type switch
        {
            DrawObjectType.Human    => slotIndex < 18,
            DrawObjectType.Mainhand => slotIndex is 0,
            DrawObjectType.Offhand  => slotIndex is 0,
            _                       => false,
        };

    public static bool ValidateMaterial(byte materialIndex)
        => materialIndex < MaterialService.MaterialsPerModel;

    public static bool ValidateRow(byte rowIndex)
        => rowIndex < ColorTable.NumRows;

    private static uint ToKey(DrawObjectType type, byte slotIndex, byte materialIndex, byte rowIndex)
    {
        var result = (uint)rowIndex;
        result |= (uint)materialIndex << 8;
        result |= (uint)slotIndex << 16;
        result |= (uint)((byte)type << 24);
        return result;
    }

    private MaterialValueIndex(uint key)
        : this((DrawObjectType)(key >> 24), (byte)(key >> 16), (byte)(key >> 8), (byte)key)
    { }

    public override string ToString()
        => $"{SlotString()} {MaterialString()} {RowString()}";

    // Should be kept in sync with its UTF-8 equivalent, SlotName().
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string SlotString()
        => DrawObject switch
        {
            DrawObjectType.Invalid                      => "Invalid",
            DrawObjectType.Human when SlotIndex < 10    => $"{((uint)SlotIndex).ToEquipSlot().ToName()}",
            DrawObjectType.Human when SlotIndex is 10   => $"{BodySlot.Hair}",
            DrawObjectType.Human when SlotIndex is 11   => $"{BodySlot.Face}",
            DrawObjectType.Human when SlotIndex is 12   => $"{BodySlot.Tail} / {BodySlot.Ear}",
            DrawObjectType.Human when SlotIndex is 13   => "Connector 1",
            DrawObjectType.Human when SlotIndex is 14   => "Connector 2",
            DrawObjectType.Human when SlotIndex is 15   => "Body 3",
            DrawObjectType.Human when SlotIndex is 16   => $"{BonusItemFlag.Glasses.ToName()}",
            DrawObjectType.Human when SlotIndex is 17   => $"{BonusItemFlag.UnkSlot.ToName()}",
            DrawObjectType.Mainhand when SlotIndex is 0 => $"{EquipSlot.MainHand.ToName()}",
            DrawObjectType.Offhand when SlotIndex is 0  => $"{EquipSlot.OffHand.ToName()}",
            _                                           => $"{DrawObject} Slot {SlotIndex}",
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string MaterialString()
        => $"Material {(char)(MaterialIndex + 'A')}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string RowString()
        => $"Row {RowIndex / 2 + 1}{(char)(RowIndex % 2 + 'A')}";

    private class Converter : JsonConverter<MaterialValueIndex>
    {
        public override void WriteJson(JsonWriter writer, MaterialValueIndex value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Key);

        public override MaterialValueIndex ReadJson(JsonReader reader, Type objectType, MaterialValueIndex existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => FromKey(serializer.Deserialize<uint>(reader), out var value) ? value : throw new Exception($"Invalid material key {value.Key}.");
    }
}
