using Dalamud.Utility;
using Penumbra.GameData.Structs;

namespace Glamourer.Structs;

// A wrapper for the clothing dyes the game provides with their RGBA color value, game ID, unmodified color value and name.
public readonly struct Stain
{
    // An empty stain with transparent color.
    public static readonly Stain  None = new("None");

    public readonly        string Name;
    public readonly        uint   RgbaColor;

    // Combine the Id byte with the 3 bytes of color values.
    private readonly uint _seColorId;

    public byte R
        => (byte)(RgbaColor & 0xFF);

    public byte G
        => (byte)((RgbaColor >> 8) & 0xFF);

    public byte B
        => (byte)((RgbaColor >> 16) & 0xFF);

    public byte Intensity
        => (byte)((1 + R + G + B) / 3);

    public uint SeColor
        => _seColorId & 0x00FFFFFF;

    public StainId RowIndex
        => (StainId)(_seColorId >> 24);

    // R and B need to be shuffled and Alpha set to max.
    public static uint SeColorToRgba(uint color)
        => ((color & 0xFF) << 16) | ((color >> 16) & 0xFF) | (color & 0xFF00) | 0xFF000000;

    public Stain(byte index, Lumina.Excel.GeneratedSheets.Stain stain)
    {
        Name       = stain.Name.ToDalamudString().ToString();
        _seColorId = stain.Color | ((uint)index << 24);
        RgbaColor  = SeColorToRgba(stain.Color);
    }

    // Only used by None.
    private Stain(string name)
    {
        Name       = name;
        _seColorId = 0;
        RgbaColor  = 0;
    }
}
