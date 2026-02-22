global using StateMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueState>;
global using DesignMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueDesign>;
using Glamourer.GameData;
using Glamourer.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Structs;


namespace Glamourer.Interop.Material;

/// <summary> Values are not squared. </summary>
public struct ColorRow(Vector3 diffuse, Vector3 specular, Vector3 emissive, float specularStrength, float glossStrength, float roughness, float metalness, float sheen, float sheenTint, float sheenAperture)
{
    public enum Mode
    {
        Legacy,
        Dawntrail,
    }

    public const float DefaultSpecularStrength = 1f;
    public const float DefaultGlossStrength    = 20f;
    public const float DefaultRoughness        = 0.5f;
    public const float DefaultMetalness        = 0f;
    public const float DefaultSheen            = 0.1f;
    public const float DefaultSheenTint        = 0.2f;
    public const float DefaultSheenAperture    = 5f;

    public static readonly ColorRow Empty = new(Vector3.Zero, Vector3.Zero, Vector3.Zero, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN,
        float.NaN, float.NaN);

    public Vector3 Diffuse          = diffuse;
    public Vector3 Specular         = specular;
    public Vector3 Emissive         = emissive;
    public float   SpecularStrength = specularStrength;
    public float   GlossStrength    = glossStrength;
    public float   Roughness        = roughness;
    public float   Metalness        = metalness;
    public float   Sheen            = sheen;
    public float   SheenTint        = sheenTint;
    public float   SheenAperture    = sheenAperture;

    public static ColorRow From(in ColorTableRow row, Mode mode)
        => mode switch
        {
            Mode.Legacy => new(Root((Vector3)row.DiffuseColor), Root((Vector3)row.SpecularColor), Root((Vector3)row.EmissiveColor),
                (float)row.LegacySpecularStrength(), (float)row.LegacyGloss(), float.NaN, float.NaN, float.NaN, float.NaN, float.NaN),
            Mode.Dawntrail => new(Root((Vector3)row.DiffuseColor), Root((Vector3)row.SpecularColor), Root((Vector3)row.EmissiveColor),
                float.NaN, float.NaN, (float)row.DawntrailRoughness(), (float)row.DawntrailMetalness(), (float)row.DawntrailSheen(),
                (float)row.DawntrailSheenTint(), (float)row.DawntrailSheenAperture()),
            _ => throw new NotImplementedException(),
        };

    public readonly bool NearEqual(in ColorRow rhs, bool skipEmpty = false)
        => Diffuse.NearEqual(rhs.Diffuse)
         && Specular.NearEqual(rhs.Specular)
         && Emissive.NearEqual(rhs.Emissive)
         && (float.IsNaN(SpecularStrength) ? skipEmpty || float.IsNaN(rhs.SpecularStrength) : SpecularStrength.NearEqual(rhs.SpecularStrength))
         && (float.IsNaN(GlossStrength) ? skipEmpty || float.IsNaN(rhs.GlossStrength) : GlossStrength.NearEqual(rhs.GlossStrength))
         && (float.IsNaN(Roughness) ? skipEmpty || float.IsNaN(rhs.Roughness) : Roughness.NearEqual(rhs.Roughness))
         && (float.IsNaN(Metalness) ? skipEmpty || float.IsNaN(rhs.Metalness) : Metalness.NearEqual(rhs.Metalness))
         && (float.IsNaN(Sheen) ? skipEmpty || float.IsNaN(rhs.Sheen) : Sheen.NearEqual(rhs.Sheen))
         && (float.IsNaN(SheenAperture) ? skipEmpty || float.IsNaN(rhs.SheenAperture) : SheenAperture.NearEqual(rhs.SheenAperture))
         && (float.IsNaN(SheenTint) ? skipEmpty || float.IsNaN(rhs.SheenTint) : SheenTint.NearEqual(rhs.SheenTint));

    private static Vector3 Square(Vector3 value)
        => new(Square(value.X), Square(value.Y), Square(value.Z));

    private static float Square(float value)
        => value < 0 ? -value * value : value * value;

    private static Vector3 Root(Vector3 value)
        => new(Root(value.X), Root(value.Y), Root(value.Z));

    private static float Root(float value)
        => value < 0 ? MathF.Sqrt(-value) : MathF.Sqrt(value);

    public readonly bool Apply(ref ColorTableRow row, Mode mode)
    {
        var ret = false;
        var d   = Square(Diffuse);
        if (!((Vector3)row.DiffuseColor).NearEqual(d))
        {
            row.DiffuseColor = (HalfColor)d;
            ret              = true;
        }

        var s = Square(Specular);
        if (!((Vector3)row.SpecularColor).NearEqual(s))
        {
            row.SpecularColor = (HalfColor)s;
            ret               = true;
        }

        var e = Square(Emissive);
        if (!((Vector3)row.EmissiveColor).NearEqual(e))
        {
            row.EmissiveColor = (HalfColor)e;
            ret               = true;
        }

        switch (mode)
        {
            case Mode.Legacy:
                if (!float.IsNaN(SpecularStrength) && !((float)row.LegacySpecularStrength()).NearEqual(SpecularStrength))
                {
                    row.LegacySpecularStrengthWrite() = (Half)SpecularStrength;
                    ret                               = true;
                }

                if (!float.IsNaN(GlossStrength) && !((float)row.LegacyGloss()).NearEqual(GlossStrength))
                {
                    row.LegacyGlossWrite() = (Half)GlossStrength;
                    ret                    = true;
                }

                break;
            case Mode.Dawntrail:
                if (!float.IsNaN(Roughness) && !((float)row.DawntrailRoughness()).NearEqual(Roughness))
                {
                    row.DawntrailRoughnessWrite() = (Half)Roughness;
                    ret                           = true;
                }

                if (!float.IsNaN(Metalness) && !((float)row.DawntrailMetalness()).NearEqual(Metalness))
                {
                    row.DawntrailMetalnessWrite() = (Half)Metalness;
                    ret                           = true;
                }

                if (!float.IsNaN(Sheen) && !((float)row.DawntrailSheen()).NearEqual(Sheen))
                {
                    row.DawntrailSheenWrite() = (Half)Sheen;
                    ret                       = true;
                }

                if (!float.IsNaN(SheenAperture) && !((float)row.DawntrailSheenAperture()).NearEqual(SheenAperture))
                {
                    row.DawntrailSheenApertureWrite() = (Half)SheenAperture;
                    ret                               = true;
                }

                if (!float.IsNaN(SheenTint) && !((float)row.DawntrailSheenTint()).NearEqual(SheenTint))
                {
                    row.DawntrailSheenTintWrite() = (Half)SheenTint;
                    ret                           = true;
                }

                break;
            default: throw new NotImplementedException();
        }

        return ret;
    }

    public readonly ColorRow MergeOnto(ColorRow previous)
        => new(Diffuse, Specular, Emissive, float.IsNaN(SpecularStrength) ? previous.SpecularStrength : SpecularStrength,
            float.IsNaN(GlossStrength) ? previous.GlossStrength : GlossStrength, float.IsNaN(Roughness) ? previous.Roughness : Roughness,
            float.IsNaN(Metalness) ? previous.Metalness : Metalness, float.IsNaN(Sheen) ? previous.Sheen : Sheen,
            float.IsNaN(SheenTint) ? previous.SheenTint : SheenTint, float.IsNaN(SheenAperture) ? previous.SheenAperture : SheenAperture);

    public readonly bool IsPartial(Mode mode)
        => mode switch
        {
            Mode.Legacy => float.IsNaN(SpecularStrength) || float.IsNaN(GlossStrength),
            Mode.Dawntrail => float.IsNaN(Roughness)
             || float.IsNaN(Metalness)
             || float.IsNaN(Sheen)
             || float.IsNaN(SheenTint)
             || float.IsNaN(SheenAperture),
            _ => throw new NotImplementedException(),
        };

    public readonly Mode GuessMode()
        => float.IsNaN(Roughness) && float.IsNaN(Metalness) && float.IsNaN(Sheen) && float.IsNaN(SheenTint) && float.IsNaN(SheenAperture)
            ? Mode.Legacy
            : Mode.Dawntrail;

    public override readonly string ToString()
        => $"[ColorRow Diffuse={Diffuse} Specular={Specular} Emissive={Emissive} SpecularStrength={SpecularStrength} GlossStrength={GlossStrength} Roughness={Roughness} Metalness={Metalness} Sheen={Sheen} SheenTint={SheenTint} SheenAperture={SheenAperture}]";
}

internal static class ColorTableRowExtensions
{
    internal static Half LegacySpecularStrength(this in ColorTableRow row)
        => row[7];

    internal static Half LegacyGloss(this in ColorTableRow row)
        => row[3];

    internal static Half DawntrailSheen(this in ColorTableRow row)
        => row[12];

    internal static Half DawntrailSheenTint(this in ColorTableRow row)
        => row[13];

    internal static Half DawntrailSheenAperture(this in ColorTableRow row)
        => row[14];

    internal static Half DawntrailRoughness(this in ColorTableRow row)
        => row[16];

    internal static Half DawntrailMetalness(this in ColorTableRow row)
        => row[18];

    internal static ref Half LegacySpecularStrengthWrite(this ref ColorTableRow row)
        => ref row[7];

    internal static ref Half LegacyGlossWrite(this ref ColorTableRow row)
        => ref row[3];

    internal static ref Half DawntrailSheenWrite(this ref ColorTableRow row)
        => ref row[12];

    internal static ref Half DawntrailSheenTintWrite(this ref ColorTableRow row)
        => ref row[13];

    internal static ref Half DawntrailSheenApertureWrite(this ref ColorTableRow row)
        => ref row[14];

    internal static ref Half DawntrailRoughnessWrite(this ref ColorTableRow row)
        => ref row[16];

    internal static ref Half DawntrailMetalnessWrite(this ref ColorTableRow row)
        => ref row[18];
}

[JsonConverter(typeof(Converter))]
public struct MaterialValueDesign(ColorRow value, bool enabled, bool revert, ColorRow.Mode mode)
{
    public ColorRow      Value   = value;
    public bool          Enabled = enabled;
    public bool          Revert  = revert;
    public ColorRow.Mode Mode    = mode;

    public readonly bool Apply(ref MaterialValueState state)
    {
        if (!Enabled)
            return false;

        if (Revert)
        {
            if (state.Model.NearEqual(state.Game))
                return false;

            state.Model = state.Game;
            return true;
        }

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
            writer.WritePropertyName("Revert");
            writer.WriteValue(value.Revert);
            writer.WritePropertyName("Mode");
            writer.WriteValue(value.Mode.ToString());
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
            if (!float.IsNaN(value.Value.SpecularStrength))
            {
                writer.WritePropertyName("SpecularA");
                writer.WriteValue(value.Value.SpecularStrength);
            }

            writer.WritePropertyName("EmissiveR");
            writer.WriteValue(value.Value.Emissive.X);
            writer.WritePropertyName("EmissiveG");
            writer.WriteValue(value.Value.Emissive.Y);
            writer.WritePropertyName("EmissiveB");
            writer.WriteValue(value.Value.Emissive.Z);
            if (!float.IsNaN(value.Value.GlossStrength))
            {
                writer.WritePropertyName("Gloss");
                writer.WriteValue(value.Value.GlossStrength);
            }

            if (!float.IsNaN(value.Value.Roughness))
            {
                writer.WritePropertyName("Roughness");
                writer.WriteValue(value.Value.Roughness);
            }

            if (!float.IsNaN(value.Value.Metalness))
            {
                writer.WritePropertyName("Metalness");
                writer.WriteValue(value.Value.Metalness);
            }

            if (!float.IsNaN(value.Value.Sheen))
            {
                writer.WritePropertyName("Sheen");
                writer.WriteValue(value.Value.Sheen);
            }

            if (!float.IsNaN(value.Value.SheenTint))
            {
                writer.WritePropertyName("SheenTint");
                writer.WriteValue(value.Value.SheenTint);
            }

            if (!float.IsNaN(value.Value.SheenAperture))
            {
                writer.WritePropertyName("SheenAperture");
                writer.WriteValue(value.Value.SheenAperture);
            }

            writer.WritePropertyName("Enabled");
            writer.WriteValue(value.Enabled);
            writer.WriteEndObject();
        }

        public override MaterialValueDesign ReadJson(JsonReader reader, Type objectType, MaterialValueDesign existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            if (obj["Mode"]?.Value<string>() is { } mode)
                Enum.TryParse(mode, true, out existingValue.Mode);
            Set(ref existingValue.Revert,           obj["Revert"]?.Value<bool>());
            Set(ref existingValue.Value.Diffuse.X,  obj["DiffuseR"]?.Value<float>());
            Set(ref existingValue.Value.Diffuse.Y,  obj["DiffuseG"]?.Value<float>());
            Set(ref existingValue.Value.Diffuse.Z,  obj["DiffuseB"]?.Value<float>());
            Set(ref existingValue.Value.Specular.X, obj["SpecularR"]?.Value<float>());
            Set(ref existingValue.Value.Specular.Y, obj["SpecularG"]?.Value<float>());
            Set(ref existingValue.Value.Specular.Z, obj["SpecularB"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.X, obj["EmissiveR"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.Y, obj["EmissiveG"]?.Value<float>());
            Set(ref existingValue.Value.Emissive.Z, obj["EmissiveB"]?.Value<float>());
            existingValue.Value.SpecularStrength = obj["SpecularA"]?.Value<float>() ?? float.NaN;
            existingValue.Value.GlossStrength    = obj["Gloss"]?.Value<float>() ?? float.NaN;
            existingValue.Value.Roughness        = obj["Roughness"]?.Value<float>() ?? float.NaN;
            existingValue.Value.Metalness        = obj["Metalness"]?.Value<float>() ?? float.NaN;
            existingValue.Value.Sheen            = obj["Sheen"]?.Value<float>() ?? float.NaN;
            existingValue.Value.SheenTint        = obj["SheenTint"]?.Value<float>() ?? float.NaN;
            existingValue.Value.SheenAperture    = obj["SheenAperture"]?.Value<float>() ?? float.NaN;
            existingValue.Enabled                = obj["Enabled"]?.Value<bool>() ?? false;
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

public struct MaterialValueState(
    in ColorRow game,
    in ColorRow model,
    CharacterWeapon drawData,
    StateSource source)
{
    public MaterialValueState(in ColorRow gameRow, in ColorRow modelRow, CharacterArmor armor, StateSource source)
        : this(gameRow, modelRow, armor.ToWeapon(0), source)
    { }

    public          ColorRow        Game     = game;
    public          ColorRow        Model    = model;
    public readonly CharacterWeapon DrawData = drawData;
    public readonly StateSource     Source   = source;

    public readonly bool EqualGame(in ColorRow rhsRow, CharacterWeapon rhsData)
        => DrawData.Skeleton == rhsData.Skeleton
         && DrawData.Weapon == rhsData.Weapon
         && DrawData.Variant == rhsData.Variant
         && DrawData.Stains == rhsData.Stains
         && rhsRow.NearEqual(Game, true);

    public readonly MaterialValueDesign Convert()
        => new(Model, true, false, Model.GuessMode());

    public readonly MaterialValueState MergeOnto(in ColorRow previous)
        => new(Game, Model.MergeOnto(previous), DrawData, Source);
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
        => TryGetValue(index.Key, out value);

    public bool TryGetValue(uint key, out T value)
    {
        if (_values.Count == 0)
        {
            value = default!;
            return false;
        }

        var idx = Search(key);
        if (idx >= 0)
        {
            value = _values[idx].Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool TryAddValue(MaterialValueIndex index, in T value)
        => TryAddValue(index.Key, value);

    public bool TryAddValue(uint key, in T value)
    {
        var idx = Search(key);
        if (idx >= 0)
            return false;

        _values.Insert(~idx, (key, value));
        return true;
    }

    public bool CheckExistenceSlot(MaterialValueIndex index)
    {
        var key = CheckExistence(index);
        return key.Valid && key.DrawObject == index.DrawObject && key.SlotIndex == index.SlotIndex;
    }

    public bool CheckExistenceMaterial(MaterialValueIndex index)
    {
        var key = CheckExistence(index);
        return key.Valid && key.DrawObject == index.DrawObject && key.SlotIndex == index.SlotIndex && key.MaterialIndex == index.MaterialIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MaterialValueIndex CheckExistence(MaterialValueIndex index)
    {
        if (_values.Count == 0)
            return MaterialValueIndex.Invalid;

        var key = index.Key;
        var idx = Search(key);
        if (idx >= 0)
            return index;

        idx = ~idx;
        if (idx >= _values.Count)
            return MaterialValueIndex.Invalid;

        return MaterialValueIndex.FromKey(_values[idx].Key);
    }

    public bool RemoveValue(MaterialValueIndex index)
        => RemoveValue(index.Key);

    public bool RemoveValue(uint key)
    {
        if (_values.Count == 0)
            return false;

        var idx = Search(key);
        if (idx < 0)
            return false;

        _values.RemoveAt(idx);
        return true;
    }

    public void AddOrUpdateValue(MaterialValueIndex index, in T value)
        => AddOrUpdateValue(index.Key, value);

    public void AddOrUpdateValue(uint key, in T value)
    {
        var idx = Search(key);
        if (idx < 0)
            _values.Insert(~idx, (key, value));
        else
            _values[idx] = (key, value);
    }

    public bool UpdateValue(MaterialValueIndex index, in T value, out T oldValue)
        => UpdateValue(index.Key, value, out oldValue);

    public bool UpdateValue(uint key, in T value, out T oldValue)
    {
        if (_values.Count == 0)
        {
            oldValue = default!;
            return false;
        }

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
