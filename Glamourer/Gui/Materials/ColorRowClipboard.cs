using Glamourer.Interop.Material;
using Penumbra.GameData.Files.MaterialStructs;

namespace Glamourer.Gui.Materials;

public static class ColorRowClipboard
{
    public static bool IsSet { get; private set; }

    public static bool IsTableSet { get; private set; }

    public static ColorTable.Table Table
    {
        get;
        set
        {
            IsTableSet = true;
            field      = value;
        }
    }

    public static ColorRow Row
    {
        get;
        set
        {
            IsSet = true;
            field = value;
        }
    }
}
