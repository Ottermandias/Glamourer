global using StateMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueState>;
global using DesignMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueDesign>;
using Glamourer.GameData;
using Glamourer.State;
using Penumbra.GameData.Files;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Structs;


namespace Glamourer.Interop.Material;

[JsonConverter(typeof(Converter))]
public struct ColorRow(Vector3 diffuse, Vector3 specular, Vector3 emissive, float specularStrength, float glossStrength)
{
    public static readonly ColorRow Empty = new(Vector3.Zero, Vector3.Zero, Vector3.Zero, 0, 0);

    public Vector3 Diffuse          = diffuse;
    public Vector3 Specular         = specular;
    public Vector3 Emissive         = emissive;
    public float   SpecularStrength = specularStrength;
    public float   GlossStrength    = glossStrength;

    public ColorRow(in MtrlFile.ColorTable.Row row)
        : this(row.Diffuse, row.Specular, row.Emissive, row.SpecularStrength, row.GlossStrength)
    { }

    public readonly bool NearEqual(in ColorRow rhs)
        => Diffuse.NearEqual(rhs.Diffuse)
         && Specular.NearEqual(rhs.Specular)
         && Emissive.NearEqual(rhs.Emissive)
         && SpecularStrength.NearEqual(rhs.SpecularStrength)
         && GlossStrength.NearEqual(rhs.GlossStrength);

    public readonly bool Apply(ref MtrlFile.ColorTable.Row row)
    {
        var ret = false;
        if (!row.Diffuse.NearEqual(Diffuse))
        {
            row.Diffuse = Diffuse;
            ret         = true;
        }

        if (!row.Specular.NearEqual(Specular))
        {
            row.Specular = Specular;
            ret          = true;
        }

        if (!row.Emissive.NearEqual(Emissive))
        {
            row.Emissive = Emissive;
            ret          = true;
        }

        if (!row.SpecularStrength.NearEqual(SpecularStrength))
        {
            row.SpecularStrength = SpecularStrength;
            ret                  = true;
        }

        if (!row.GlossStrength.NearEqual(GlossStrength))
        {
            row.GlossStrength = GlossStrength;
            ret               = true;
        }

        return ret;
    }

    private class Converter : JsonConverter<ColorRow>
    {
        public override void WriteJson(JsonWriter writer, ColorRow value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("DiffuseR");
            writer.WriteValue(value.Diffuse.X);
            writer.WritePropertyName("DiffuseG");
            writer.WriteValue(value.Diffuse.Y);
            writer.WritePropertyName("DiffuseB");
            writer.WriteValue(value.Diffuse.Z);
            writer.WritePropertyName("SpecularR");
            writer.WriteValue(value.Specular.X);
            writer.WritePropertyName("SpecularG");
            writer.WriteValue(value.Specular.Y);
            writer.WritePropertyName("SpecularB");
            writer.WriteValue(value.Specular.Z);
            writer.WritePropertyName("SpecularA");
            writer.WriteValue(value.SpecularStrength);
            writer.WritePropertyName("EmissiveR");
            writer.WriteValue(value.Emissive.X);
            writer.WritePropertyName("EmissiveG");
            writer.WriteValue(value.Emissive.Y);
            writer.WritePropertyName("EmissiveB");
            writer.WriteValue(value.Emissive.Z);
            writer.WritePropertyName("Gloss");
            writer.WriteValue(value.GlossStrength);
            writer.WriteEndObject();
        }

        public override ColorRow ReadJson(JsonReader reader, Type objectType, ColorRow existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            Set(ref existingValue.Diffuse.X,        obj["DiffuseR"]?.Value<float>());
            Set(ref existingValue.Diffuse.Y,        obj["DiffuseG"]?.Value<float>());
            Set(ref existingValue.Diffuse.Z,        obj["DiffuseB"]?.Value<float>());
            Set(ref existingValue.Specular.X,       obj["SpecularR"]?.Value<float>());
            Set(ref existingValue.Specular.Y,       obj["SpecularG"]?.Value<float>());
            Set(ref existingValue.Specular.Z,       obj["SpecularB"]?.Value<float>());
            Set(ref existingValue.SpecularStrength, obj["SpecularA"]?.Value<float>());
            Set(ref existingValue.Emissive.X,       obj["EmissiveR"]?.Value<float>());
            Set(ref existingValue.Emissive.Y,       obj["EmissiveG"]?.Value<float>());
            Set(ref existingValue.Emissive.Z,       obj["EmissiveB"]?.Value<float>());
            Set(ref existingValue.GlossStrength,    obj["Gloss"]?.Value<float>());
            return existingValue;

            static void Set<T>(ref T target, T? value)
                where T : struct
            {
                if (value.HasValue)
                    target = value.Value;
            }
        }
    }
}

[JsonConverter(typeof(Converter))]
public struct MaterialValueDesign(ColorRow value, bool enabled)
{
    public ColorRow Value   = value;
    public bool     Enabled = enabled;

    public readonly bool Apply(ref MaterialValueState state)
    {
        if (!Enabled)
            return false;

        if (state.Model.NearEqual(Value))
            return false;

        state.Model = Value;
        return true;
    }

    private class Converter : JsonConverter<MaterialValueDesign>
    {
        public override void WriteJson(JsonWriter writer, MaterialValueDesign value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("DiffuseR");
            writer.WriteValue(value.Value.Diffuse.X);
            writer.WritePropertyName("DiffuseG");
            writer.WriteValue(value.Value.Diffuse.Y);
            writer.WritePropertyName("DiffuseB");
            writer.WriteValue(value.Value.Diffuse.Z);
            writer.WritePropertyName("SpecularR");
            writer.WriteValue(value.Value.Specular.X);
            writer.WritePropertyName("SpecularG");
            writer.WriteValue(value.Value.Specular.Y);
            writer.WritePropertyName("SpecularB");
            writer.WriteValue(value.Value.Specular.Z);
            writer.WritePropertyName("SpecularA");
            writer.WriteValue(value.Value.SpecularStrength);
            writer.WritePropertyName("EmissiveR");
            writer.WriteValue(value.Value.Emissive.X);
            writer.WritePropertyName("EmissiveG");
            writer.WriteValue(value.Value.Emissive.Y);
            writer.WritePropertyName("EmissiveB");
            writer.WriteValue(value.Value.Emissive.Z);
            writer.WritePropertyName("Gloss");
            writer.WriteValue(value.Value.GlossStrength);
            writer.WritePropertyName("Enabled");
            writer.WriteValue(value.Enabled);
            writer.WriteEndObject();
        }

        public override MaterialValueDesign ReadJson(JsonReader reader, Type objectType, MaterialValueDesign existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            Set(ref existingValue.Value.Diffuse.X,        obj["DiffuseR"]?.Value<float>());
            Set(ref existingValue.Value.Diffuse.Y,        obj["DiffuseG"]?.Value<float>());
            Set(ref existingValue.Value.Diffuse.Z,        obj["DiffuseB"]?.Value<float>());
            Set(ref existingValue.Value.Specular.X,       obj["SpecularR"]?.Value<float>());
            Set(ref existingValue.Value.Specular.Y,       obj["SpecularG"]?.Value<float>());
            Set(ref existingValue.Value.Specular.Z,       obj["SpecularB"]?.Value<float>());
            Set(ref existingValue.Value.SpecularStrength, obj["SpecularA"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.X,       obj["EmissiveR"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.Y,       obj["EmissiveG"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.Z,       obj["EmissiveB"]?.Value<float>());
            Set(ref existingValue.Value.GlossStrength,    obj["Gloss"]?.Value<float>());
            existingValue.Enabled = obj["Enabled"]?.Value<bool>() ?? false;
            return existingValue;

            static void Set<T>(ref T target, T? value)
                where T : struct
            {
                if (value.HasValue)
                    target = value.Value;
            }
        }
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct MaterialValueState(
    in ColorRow game,
    in ColorRow model,
    CharacterWeapon drawData,
    StateSource source)
{
    public MaterialValueState(in ColorRow gameRow, in ColorRow modelRow, CharacterArmor armor, StateSource source)
        : this(gameRow, modelRow, armor.ToWeapon(0), source)
    { }

    [FieldOffset(0)]
    public ColorRow Game = game;

    [FieldOffset(44)]
    public ColorRow Model = model;

    [FieldOffset(88)]
    public readonly CharacterWeapon DrawData = drawData;

    [FieldOffset(95)]
    public readonly StateSource Source = source;

    public readonly bool EqualGame(in ColorRow rhsRow, CharacterWeapon rhsData)
        => DrawData.Skeleton == rhsData.Skeleton
         && DrawData.Weapon == rhsData.Weapon
         && DrawData.Variant == rhsData.Variant
         && DrawData.Stain == rhsData.Stain
         && Game.NearEqual(rhsRow);

    public readonly MaterialValueDesign Convert()
        => new(Model, true);
}

public readonly struct MaterialValueManager<T>
{
    private readonly List<(uint Key, T Value)> _values = [];

    public MaterialValueManager()
    { }

    public void Clear()
        => _values.Clear();

    public MaterialValueManager<T> Clone()
    {
        var ret = new MaterialValueManager<T>();
        ret._values.AddRange(_values);
        return ret;
    }

    public bool TryGetValue(MaterialValueIndex index, out T value)
    {
        if (_values.Count == 0)
        {
            value = default!;
            return false;
        }

        var idx = Search(index.Key);
        if (idx >= 0)
        {
            value = _values[idx].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool TryAddValue(MaterialValueIndex index, in T value)
    {
        var key = index.Key;
        var idx = Search(key);
        if (idx >= 0)
            return false;

        _values.Insert(~idx, (key, value));
        return true;
    }

    public bool RemoveValue(MaterialValueIndex index)
    {
        if (_values.Count == 0)
            return false;

        var idx = Search(index.Key);
        if (idx < 0)
            return false;

        _values.RemoveAt(idx);
        return true;
    }

    public void AddOrUpdateValue(MaterialValueIndex index, in T value)
    {
        var key = index.Key;
        var idx = Search(key);
        if (idx < 0)
            _values.Insert(~idx, (key, value));
        else
            _values[idx] = (key, value);
    }

    public bool UpdateValue(MaterialValueIndex index, in T value, out T oldValue)
    {
        if (_values.Count == 0)
        {
            oldValue = default!;
            return false;
        }

        var key = index.Key;
        var idx = Search(key);
        if (idx < 0)
        {
            oldValue = default!;
            return false;
        }

        oldValue     = _values[idx].Value;
        _values[idx] = (key, value);
        return true;
    }

    public IReadOnlyList<(uint Key, T Value)> Values
        => _values;

    public int RemoveValues(MaterialValueIndex min, MaterialValueIndex max)
    {
        var (minIdx, maxIdx) = MaterialValueManager.GetMinMax<T>(CollectionsMarshal.AsSpan(_values), min.Key, max.Key);
        if (minIdx < 0)
            return 0;

        var count = maxIdx - minIdx;
        _values.RemoveRange(minIdx, count);
        return count;
    }

    public ReadOnlySpan<(uint Key, T Value)> GetValues(MaterialValueIndex min, MaterialValueIndex max)
        => MaterialValueManager.Filter<T>(CollectionsMarshal.AsSpan(_values), min, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int Search(uint key)
        => _values.BinarySearch((key, default!), MaterialValueManager.Comparer<T>.Instance);
}

public static class MaterialValueManager
{
    internal class Comparer<T> : IComparer<(uint Key, T Value)>
    {
        public static readonly Comparer<T> Instance = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        int IComparer<(uint Key, T Value)>.Compare((uint Key, T Value) x, (uint Key, T Value) y)
            => x.Key.CompareTo(y.Key);
    }

    public static bool GetSpecific<T>(ReadOnlySpan<(uint Key, T Value)> values, MaterialValueIndex index, out T ret)
    {
        var idx = values.BinarySearch((index.Key, default!), Comparer<T>.Instance);
        if (idx < 0)
        {
            ret = default!;
            return false;
        }

        ret = values[idx].Value;
        return true;
    }

    public static ReadOnlySpan<(uint Key, T Value)> Filter<T>(ReadOnlySpan<(uint Key, T Value)> values, MaterialValueIndex min,
        MaterialValueIndex max)
    {
        var (minIdx, maxIdx) = GetMinMax(values, min.Key, max.Key);
        return minIdx < 0 ? [] : values[minIdx..(maxIdx + 1)];
    }

    /// <summary> Obtain the minimum index and maximum index for a minimum and maximum key. </summary>
    internal static (int MinIdx, int MaxIdx) GetMinMax<T>(ReadOnlySpan<(uint Key, T Value)> values, uint minKey, uint maxKey)
    {
        // Find the minimum index by binary search.
        var idx    = values.BinarySearch((minKey, default!), Comparer<T>.Instance);
        var minIdx = idx;

        // If the key does not exist, check if it is an invalid range or set it correctly.
        if (minIdx < 0)
        {
            minIdx = ~minIdx;
            if (minIdx == values.Length || values[minIdx].Key > maxKey)
                return (-1, -1);

            idx = minIdx;
        }
        else
        {
            // If it does exist, go upwards until the first key is reached that is actually smaller.
            while (minIdx > 0 && values[minIdx - 1].Key >= minKey)
                --minIdx;
        }

        // Check if the range can be valid.
        if (values[minIdx].Key < minKey || values[minIdx].Key > maxKey)
            return (-1, -1);


        // Do pretty much the same but in the other direction with the maximum key.
        var maxIdx = values[idx..].BinarySearch((maxKey, default!), Comparer<T>.Instance);
        if (maxIdx < 0)
        {
            maxIdx = ~maxIdx + idx;
            return maxIdx > minIdx ? (minIdx, maxIdx - 1) : (-1, -1);
        }

        maxIdx += idx;

        while (maxIdx < values.Length - 1 && values[maxIdx + 1].Key <= maxKey)
            ++maxIdx;

        if (values[maxIdx].Key < minKey || values[maxIdx].Key > maxKey)
            return (-1, -1);

        return (minIdx, maxIdx);
    }
}
