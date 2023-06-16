using System.Collections.Generic;

namespace Glamourer.Gui;

public enum ColorId
{
    CustomizationDesign,
    StateDesign,
    EquipmentDesign,
}

public static class Colors
{
    public static (uint DefaultColor, string Name, string Description) Data(this ColorId color)
        => color switch
        {
            // @formatter:off
            ColorId.CustomizationDesign   => (0xFFC000C0, "Customization Design", "A design that only changes customizations on a character." ),
            ColorId.StateDesign           => (0xFF00C0C0, "State Design",         "A design that only changes meta state on a character."     ),
            ColorId.EquipmentDesign       => (0xFF00C000, "Equipment Design",     "A design that only changes equipment on a character."      ),
            _                             => (0x00000000, string.Empty,           string.Empty                                                ),
            // @formatter:on
        };

    private static IReadOnlyDictionary<ColorId, uint> _colors = new Dictionary<ColorId, uint>();

    /// <summary> Obtain the configured value for a color. </summary>
    public static uint Value(this ColorId color)
        => _colors.TryGetValue(color, out var value) ? value : color.Data().DefaultColor;

    /// <summary> Set the configurable colors dictionary to a value. </summary>
    public static void SetColors(Configuration config)
        => _colors = config.Colors;
}
