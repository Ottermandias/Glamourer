using Penumbra.GameData.Structs;

namespace Glamourer
{
    public readonly struct Stain
    {
        public readonly string Name;
        public readonly uint   RgbaColor;

        private readonly uint _seColorId;

        public byte R
            => (byte) (RgbaColor & 0xFF);

        public byte G
            => (byte) ((RgbaColor >> 8) & 0xFF);

        public byte B
            => (byte) ((RgbaColor >> 16) & 0xFF);

        public byte Intensity
            => (byte) ((1 + R + G + B) / 3);

        public uint SeColor
            => _seColorId & 0x00FFFFFF;

        public StainId RowIndex
            => (StainId) (_seColorId >> 24);


        public static uint SeColorToRgba(uint color)
            => ((color & 0xFF) << 16) | ((color >> 16) & 0xFF) | (color & 0xFF00) | 0xFF000000;

        public Stain(byte index, Lumina.Excel.GeneratedSheets.Stain stain)
        {
            Name       = stain.Name.ToString();
            _seColorId = stain.Color | ((uint) index << 24);
            RgbaColor  = SeColorToRgba(stain.Color);
        }

        public static readonly Stain None = new("None");

        private Stain(string name)
        {
            Name       = name;
            _seColorId = 0;
            RgbaColor  = 0;
        }
    }
}
