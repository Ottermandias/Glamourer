using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Plugin.Services;

namespace Glamourer.Customization;

// Convert the Human.Cmp file into color sets.
// If the file can not be read due to TexTools corruption, create a 0-array of size MinSize.
internal class CmpFile
{
    private readonly Lumina.Data.FileResource? _file;
    private readonly uint[]                    _rgbaColors;

    // No error checking since only called internally.
    public IEnumerable<uint> GetSlice(int offset, int count)
        => _rgbaColors.Length >= offset + count ? _rgbaColors.Skip(offset).Take(count) : Enumerable.Repeat(0u, count);

    public bool Valid
        => _file != null;

    public CmpFile(IDataManager gameData)
    {
        try
        {
            _file       = gameData.GetFile("chara/xls/charamake/human.cmp")!;
            _rgbaColors = new uint[_file.Data.Length >> 2];
            for (var i = 0; i < _file.Data.Length; i += 4)
            {
                _rgbaColors[i >> 2] = _file.Data[i]
                  | (uint)(_file.Data[i + 1] << 8)
                  | (uint)(_file.Data[i + 2] << 16)
                  | (uint)(_file.Data[i + 3] << 24);
            }
        }
        catch (Exception e)
        {
            PluginLog.Error("READ THIS\n======== Could not obtain the human.cmp file which is necessary for color sets.\n"
              + "======== This usually indicates an error with your index files caused by TexTools modifications.\n"
              + "======== If you have used TexTools before, you will probably need to start over in it to use Glamourer.", e);
            _file       = null;
            _rgbaColors = Array.Empty<uint>();
        }
    }
}
