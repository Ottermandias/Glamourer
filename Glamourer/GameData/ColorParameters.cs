using System;
using System.Collections;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Penumbra.String.Functions;

namespace Glamourer.GameData;

public class ColorParameters : IReadOnlyList<uint>
{
    private readonly uint[] _rgbaColors;

    public ReadOnlySpan<uint> GetSlice(int offset, int count)
        => _rgbaColors.AsSpan(offset, count);

    public unsafe ColorParameters(IDataManager gameData, IPluginLog log)
    {
        try
        {
            var file = gameData.GetFile("chara/xls/charamake/human.cmp")!;
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
            _rgbaColors = Array.Empty<uint>();
        }
    }

    public IEnumerator<uint> GetEnumerator()
        => (IEnumerator<uint>)_rgbaColors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _rgbaColors.Length;

    public uint this[int index]
        => _rgbaColors[index];
}
