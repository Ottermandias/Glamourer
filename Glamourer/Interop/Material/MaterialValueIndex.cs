using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.Interop;
using Glamourer.Interop.Structs;
using Newtonsoft.Json;
using Penumbra.GameData.Files;

namespace Glamourer.Interop.Material;

[JsonConverter(typeof(Converter))]
public readonly record struct MaterialValueIndex(
    MaterialValueIndex.DrawObjectType DrawObject,
    byte SlotIndex,
    byte MaterialIndex,
    byte RowIndex,
    MaterialValueIndex.ColorTableIndex DataIndex)
{
    public uint Key
        => ToKey(DrawObject, SlotIndex, MaterialIndex, RowIndex, DataIndex);

    public bool Valid
        => Validate(DrawObject) && ValidateSlot(SlotIndex) && ValidateMaterial(MaterialIndex) && ValidateRow(RowIndex) && Validate(DataIndex);

    public static bool FromKey(uint key, out MaterialValueIndex index)
    {
        index = new MaterialValueIndex(key);
        return index.Valid;
    }

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
            DrawObjectType.Mainhand => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponDataSpan[0].DrawObject : Model.Null,
            DrawObjectType.Offhand  => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponDataSpan[1].DrawObject : Model.Null,
            _                       => Model.Null,
        };
        return model.IsCharacterBase;
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

        textures = model.AsCharacterBase->ColorTableTexturesSpan.Slice(SlotIndex * MaterialService.MaterialsPerModel,
            MaterialService.MaterialsPerModel);
        return true;
    }

    public unsafe bool TryGetTexture(Actor actor, out Texture** texture)
    {
        if (TryGetTextures(actor, out var textures))
            return TryGetTexture(textures, out texture);

        texture = null;
        return false;
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

    public unsafe bool TryGetColorTable(Actor actor, out MtrlFile.ColorTable table)
    {
        if (TryGetTexture(actor, out var texture))
            return TryGetColorTable(texture, out table);

        table = default;
        return false;
    }

    public unsafe bool TryGetColorTable(Texture** texture, out MtrlFile.ColorTable table)
        => DirectXTextureHelper.TryGetColorTable(*texture, out table);

    public unsafe bool TryGetColorRow(Actor actor, out MtrlFile.ColorTable.Row row)
    {
        if (!TryGetColorTable(actor, out var table))
        {
            row = default;
            return false;
        }

        row = table[RowIndex];
        return true;
    }

    public unsafe bool TryGetValue(Actor actor, out Vector3 value)
    {
        if (!TryGetColorRow(actor, out var row))
        {
            value = Vector3.Zero;
            return false;
        }

        value = DataIndex switch
        {
            ColorTableIndex.Diffuse          => row.Diffuse,
            ColorTableIndex.Specular         => row.Specular,
            ColorTableIndex.SpecularStrength => new Vector3(row.SpecularStrength, 0, 0),
            ColorTableIndex.Emissive         => row.Emissive,
            ColorTableIndex.GlossStrength    => new Vector3(row.GlossStrength, 0, 0),
            ColorTableIndex.TileSet          => new Vector3(row.TileSet),
            ColorTableIndex.MaterialRepeat   => new Vector3(row.MaterialRepeat, 0),
            ColorTableIndex.MaterialSkew     => new Vector3(row.MaterialSkew,   0),
            _                                => new Vector3(float.NaN),
        };
        return !float.IsNaN(value.X);
    }

    public static MaterialValueIndex FromKey(uint key)
        => new(key);

    public static MaterialValueIndex Min(DrawObjectType drawObject = 0, byte slotIndex = 0, byte materialIndex = 0, byte rowIndex = 0,
        ColorTableIndex dataIndex = 0)
        => new(drawObject, slotIndex, materialIndex, rowIndex, dataIndex);

    public static MaterialValueIndex Max(DrawObjectType drawObject = (DrawObjectType)byte.MaxValue, byte slotIndex = byte.MaxValue,
        byte materialIndex = byte.MaxValue, byte rowIndex = byte.MaxValue,
        ColorTableIndex dataIndex = (ColorTableIndex)byte.MaxValue)
        => new(drawObject, slotIndex, materialIndex, rowIndex, dataIndex);

    public enum DrawObjectType : byte
    {
        Human,
        Mainhand,
        Offhand,
    };

    public enum ColorTableIndex : byte
    {
        Diffuse,
        Specular,
        SpecularStrength,
        Emissive,
        GlossStrength,
        TileSet,
        MaterialRepeat,
        MaterialSkew,
    }

    public static bool Validate(DrawObjectType type)
        => Enum.IsDefined(type);

    public static bool ValidateSlot(byte slotIndex)
        => slotIndex < 10;

    public static bool ValidateMaterial(byte materialIndex)
        => materialIndex < MaterialService.MaterialsPerModel;

    public static bool ValidateRow(byte rowIndex)
        => rowIndex < MtrlFile.ColorTable.NumRows;

    public static bool Validate(ColorTableIndex dataIndex)
        => Enum.IsDefined(dataIndex);

    private static uint ToKey(DrawObjectType type, byte slotIndex, byte materialIndex, byte rowIndex, ColorTableIndex index)
    {
        var result = (uint)index & 0xFF;
        result |= (uint)(rowIndex & 0xFF) << 8;
        result |= (uint)(materialIndex & 0xF) << 16;
        result |= (uint)(slotIndex & 0xFF) << 20;
        result |= (uint)((byte)type & 0xF) << 28;
        return result;
    }

    private MaterialValueIndex(uint key)
        : this((DrawObjectType)((key >> 28) & 0xF), (byte)(key >> 20), (byte)((key >> 16) & 0xF), (byte)(key >> 8),
            (ColorTableIndex)(key & 0xFF))
    { }

    private class Converter : JsonConverter<MaterialValueIndex>
    {
        public override void WriteJson(JsonWriter writer, MaterialValueIndex value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Key);

        public override MaterialValueIndex ReadJson(JsonReader reader, Type objectType, MaterialValueIndex existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => FromKey(serializer.Deserialize<uint>(reader), out var value) ? value : throw new Exception($"Invalid material key {value.Key}.");
    }
}

public static class MaterialExtensions
{
    public static bool TryGetValue(this MaterialValueIndex.ColorTableIndex index, in MtrlFile.ColorTable.Row row, out Vector3 value)
    {
        value = index switch
        {
            MaterialValueIndex.ColorTableIndex.Diffuse          => row.Diffuse,
            MaterialValueIndex.ColorTableIndex.Specular         => row.Specular,
            MaterialValueIndex.ColorTableIndex.SpecularStrength => new Vector3(row.SpecularStrength, 0, 0),
            MaterialValueIndex.ColorTableIndex.Emissive         => row.Emissive,
            MaterialValueIndex.ColorTableIndex.GlossStrength    => new Vector3(row.GlossStrength, 0, 0),
            MaterialValueIndex.ColorTableIndex.TileSet          => new Vector3(row.TileSet),
            MaterialValueIndex.ColorTableIndex.MaterialRepeat   => new Vector3(row.MaterialRepeat, 0),
            MaterialValueIndex.ColorTableIndex.MaterialSkew     => new Vector3(row.MaterialSkew,   0),
            _                                                   => new Vector3(float.NaN),
        };
        return !float.IsNaN(value.X);
    }

    public static bool SetValue(this MaterialValueIndex.ColorTableIndex index, ref MtrlFile.ColorTable.Row row, in Vector3 value)
    {
        switch (index)
        {
            case MaterialValueIndex.ColorTableIndex.Diffuse:
                if (value == row.Diffuse)
                    return false;

                row.Diffuse = value;
                return true;

            case MaterialValueIndex.ColorTableIndex.Specular:
                if (value == row.Specular)
                    return false;

                row.Specular = value;
                return true;
            case MaterialValueIndex.ColorTableIndex.SpecularStrength:
                if (value.X == row.SpecularStrength)
                    return false;

                row.SpecularStrength = value.X;
                return true;
            case MaterialValueIndex.ColorTableIndex.Emissive:
                if (value == row.Emissive)
                    return false;

                row.Emissive = value;
                return true;
            case MaterialValueIndex.ColorTableIndex.GlossStrength:
                if (value.X == row.GlossStrength)
                    return false;

                row.GlossStrength = value.X;
                return true;
            case MaterialValueIndex.ColorTableIndex.TileSet:
                var @ushort = (ushort)(value.X + 0.5f);
                if (@ushort == row.TileSet)
                    return false;

                row.TileSet = @ushort;
                return true;
            case MaterialValueIndex.ColorTableIndex.MaterialRepeat:
                if (value.X == row.MaterialRepeat.X && value.Y == row.MaterialRepeat.Y)
                    return false;

                row.MaterialRepeat = new Vector2(value.X, value.Y);
                return true;
            case MaterialValueIndex.ColorTableIndex.MaterialSkew:
                if (value.X == row.MaterialSkew.X && value.Y == row.MaterialSkew.Y)
                    return false;

                row.MaterialSkew = new Vector2(value.X, value.Y);
                return true;
            default: return false;
        }
    }
}
