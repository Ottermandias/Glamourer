using Dalamud.Data;
using Dalamud.Plugin;

namespace Glamourer
{
    public class CmpFile
    {
        public readonly Lumina.Data.FileResource File;
        public readonly uint[]                   RgbaColors;

        public CmpFile(DataManager gameData)
        {
            File       = gameData.GetFile("chara/xls/charamake/human.cmp")!;
            RgbaColors = new uint[File.Data.Length >> 2];
            for (var i = 0; i < File.Data.Length; i += 4)
            {
                RgbaColors[i >> 2] = File.Data[i]
                  | (uint) (File.Data[i + 1] << 8)
                  | (uint) (File.Data[i + 2] << 16)
                  | (uint) (File.Data[i + 3] << 24);
            }
        }
    }
}
