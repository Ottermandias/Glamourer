global using StateMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueState>;
global using DesignMaterialManager = Glamourer.Interop.Material.MaterialValueManager<Glamourer.Interop.Material.MaterialValueDesign>;
using Glamourer.State;


namespace Glamourer.Interop.Material;

public record struct MaterialValueDesign(Vector3 Value, bool Enabled);
public record struct MaterialValueState(Vector3 Game, Vector3 Model, StateSource Source);

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

    public ReadOnlySpan<(uint key, T Value)> GetValues(MaterialValueIndex min, MaterialValueIndex max)
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
