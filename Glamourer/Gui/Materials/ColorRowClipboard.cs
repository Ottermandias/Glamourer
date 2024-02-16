using Glamourer.Interop.Material;

namespace Glamourer.Gui.Materials;

public static class ColorRowClipboard
{
    private static ColorRow _row;

    public static bool IsSet { get; private set; }

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
