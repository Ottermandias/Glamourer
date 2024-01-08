using Dalamud.Plugin.Services;
using Penumbra.String.Functions;

namespace Glamourer.GameData;

/// <summary> Parse the Human.cmp file as a list of 4-byte integer values to obtain colors. </summary>
public class ColorParameters : IReadOnlyList<uint>
{
    private readonly uint[] _rgbaColors;

    /// <summary> Get a slice of the colors starting at <paramref name="offset"/> and containing <paramref name="count"/> colors. </summary>
    public ReadOnlySpan<uint> GetSlice(int offset, int count)
        => _rgbaColors.AsSpan(offset, count);

    public unsafe ColorParameters(IDataManager gameData, IPluginLog log)
    {
        try
        {
            var file = gameData.GetFile("chara/xls/charamake/human.cmp")!;
            // Just copy all the data into an uint array.
            _rgbaColors = new uint[file.Data.Length >> 2];
            fixed (byte* ptr1 = file.Data)
            {
                fixed (uint* ptr2 = _rgbaColors)
                {
                    MemoryUtility.MemCpyUnchecked(ptr2, ptr1, file.Data.Length);
                }
            }
        }
        catch (Exception e)
        {
            log.Error("READ THIS\n======== Could not obtain the human.cmp file which is necessary for color sets.\n"
              + "======== This usually indicates an error with your index files caused by TexTools modifications.\n"
              + "======== If you have used TexTools before, you will probably need to start over in it to use Glamourer.", e);
            _rgbaColors = [];
        }
    }

    /// <inheritdoc/>
    public IEnumerator<uint> GetEnumerator()
        => (IEnumerator<uint>)_rgbaColors.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => _rgbaColors.Length;

    /// <inheritdoc/>
    public uint this[int index]
        => _rgbaColors[index];
}
