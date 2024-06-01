using Glamourer.Interop.Material;
using Penumbra.GameData.Files.MaterialStructs;

namespace Glamourer.Gui.Materials;

public static class ColorRowClipboard
{
    private static ColorRow         _row;
    private static LegacyColorTable _table;

    public static bool IsSet { get; private set; }

    public static bool IsTableSet { get; private set; }

    public static LegacyColorTable Table
    {
        get => _table;
        set
        {
            IsTableSet = true;
            _table     = value;
        }
    }

    public static ColorRow Row
    {
        get => _row;
        set
        {
            IsSet = true;
            _row  = value;
        }
    }
}
