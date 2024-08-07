﻿using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.Interop;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;
using CsMaterial = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Material;

namespace Glamourer.Interop.Material;

[JsonConverter(typeof(Converter))]
public readonly record struct MaterialValueIndex(
    MaterialValueIndex.DrawObjectType DrawObject,
    byte SlotIndex,
    byte MaterialIndex,
    byte RowIndex)
{
    public static readonly MaterialValueIndex Invalid = new(DrawObjectType.Invalid, 0, 0, 0);

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
        if (slot is EquipSlot.MainHand)
            return new MaterialValueIndex(DrawObjectType.Mainhand, 0, 0, 0);
        if (slot is EquipSlot.OffHand)
            return new MaterialValueIndex(DrawObjectType.Offhand, 0, 0, 0);

        var idx = slot.ToIndex();
        if (idx < 10)
            return new MaterialValueIndex(DrawObjectType.Human, (byte)idx, 0, 0);

        return Invalid;
    }

    public static MaterialValueIndex FromSlot(BonusItemFlag slot)
    {
        var idx = slot.ToIndex();
        return idx > 2 ? Invalid : new MaterialValueIndex(DrawObjectType.Human, (byte)(idx + 16), 0, 0);
    }

    public EquipSlot ToEquipSlot()
        => DrawObject switch
        {
            DrawObjectType.Human when SlotIndex < 10    => ((uint)SlotIndex).ToEquipSlot(),
            DrawObjectType.Mainhand when SlotIndex == 0 => EquipSlot.MainHand,
            DrawObjectType.Offhand when SlotIndex == 0  => EquipSlot.OffHand,
            _                                           => EquipSlot.Unknown,
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
            DrawObjectType.Mainhand => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[0].DrawObject : Model.Null,
            DrawObjectType.Offhand  => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[1].DrawObject : Model.Null,
            _                       => Model.Null,
        };
        return model.IsCharacterBase;
    }

    public unsafe bool TryGetTextures(Actor actor, out ReadOnlySpan<Pointer<Texture>> textures, out ReadOnlySpan<Pointer<CsMaterial>> materials)
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

    public unsafe bool TryGetTextures(Actor actor, out ReadOnlySpan<Pointer<Texture>> textures)
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

    public unsafe bool TryGetTexture(ReadOnlySpan<Pointer<Texture>> textures, ReadOnlySpan<Pointer<CsMaterial>> materials,
        out Texture** texture, out ColorRow.Mode mode)
    {
        mode = MaterialIndex >= materials.Length
            ? ColorRow.Mode.Dawntrail
            : PrepareColorSet.GetMode((MaterialResourceHandle*)materials[MaterialIndex].Value);


        if (MaterialIndex >= textures.Length || textures[MaterialIndex].Value == null)
        {
            texture = null;
            return false;
        }

        fixed (Pointer<Texture>* ptr = textures)
        {
            texture = (Texture**)ptr + MaterialIndex;
        }

        return true;
    }

    public unsafe bool TryGetTexture(ReadOnlySpan<Pointer<Texture>> textures, out Texture** texture)
    {
        if (MaterialIndex >= textures.Length || textures[MaterialIndex].Value == null)
        {
            texture = null;
            return false;
        }

        fixed (Pointer<Texture>* ptr = textures)
        {
            texture = (Texture**)ptr + MaterialIndex;
        }

        return true;
    }


    public static MaterialValueIndex FromKey(uint key)
        => new(key);

    public static MaterialValueIndex Min(DrawObjectType drawObject = 0, byte slotIndex = 0, byte materialIndex = 0, byte rowIndex = 0)
        => new(drawObject, slotIndex, materialIndex, rowIndex);

    public static MaterialValueIndex Max(DrawObjectType drawObject = (DrawObjectType)byte.MaxValue, byte slotIndex = byte.MaxValue,
        byte materialIndex = byte.MaxValue, byte rowIndex = byte.MaxValue)
        => new(drawObject, slotIndex, materialIndex, rowIndex);

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
            DrawObjectType.Mainhand => slotIndex == 0,
            DrawObjectType.Offhand  => slotIndex == 0,
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
        => DrawObject switch
        {
            DrawObjectType.Invalid                      => "Invalid",
            DrawObjectType.Human when SlotIndex < 10    => $"{((uint)SlotIndex).ToEquipSlot().ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 10   => $"{BodySlot.Hair} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 11   => $"{BodySlot.Face} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 12   => $"{BodySlot.Tail} / {BodySlot.Ear} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 13   => $"Connectors {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 16   => $"{BonusItemFlag.Glasses.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 17   => $"{BonusItemFlag.UnkSlot.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Mainhand when SlotIndex == 0 => $"{EquipSlot.MainHand.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Offhand when SlotIndex == 0  => $"{EquipSlot.OffHand.ToName()} {MaterialString()} {RowString()}",
            _                                           => $"{DrawObject} Slot {SlotIndex} {MaterialString()} {RowString()}",
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
