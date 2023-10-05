using System.Collections.Generic;

namespace Glamourer.Gui;

public enum ColorId
{
    NormalDesign,
    CustomizationDesign,
    StateDesign,
    EquipmentDesign,
    ActorAvailable,
    ActorUnavailable,
    FolderExpanded,
    FolderCollapsed,
    FolderLine,
    EnabledAutoSet,
    DisabledAutoSet,
    AutomationActorAvailable,
    AutomationActorUnavailable,
    HeaderButtons,
    FavoriteStarOn,
    FavoriteStarHovered,
    FavoriteStarOff,
}

public static class Colors
{
    public static (uint DefaultColor, string Name, string Description) Data(this ColorId color)
        => color switch
        {
            // @formatter:off
            ColorId.NormalDesign               => (0xFFFFFFFF, "Normal Design",                    "A design with no specific traits."                                                                         ),
            ColorId.CustomizationDesign        => (0xFFC000C0, "Customization Design",             "A design that only changes customizations on a character."                                                 ),
            ColorId.StateDesign                => (0xFF00C0C0, "State Design",                     "A design that does not change equipment or customizations on a character."                                 ),
            ColorId.EquipmentDesign            => (0xFF00C000, "Equipment Design",                 "A design that only changes equipment on a character."                                                      ),
            ColorId.ActorAvailable             => (0xFF18C018, "Actor Available",                  "The header in the Actor tab panel if the currently selected actor exists in the game world at least once." ),
            ColorId.ActorUnavailable           => (0xFF1818C0, "Actor Unavailable",                "The Header in the Actor tab panel if the currently selected actor does not exist in the game world."       ),
            ColorId.FolderExpanded             => (0xFFFFF0C0, "Expanded Design Folder",           "A design folder that is currently expanded."                                                               ),
            ColorId.FolderCollapsed            => (0xFFFFF0C0, "Collapsed Design Folder",          "A design folder that is currently collapsed."                                                              ),
            ColorId.FolderLine                 => (0xFFFFF0C0, "Expanded Design Folder Line",      "The line signifying which descendants belong to an expanded design folder."                                ),
            ColorId.EnabledAutoSet             => (0xFFA0F0A0, "Enabled Automation Set",           "An automation set that is currently enabled. Only one set can be enabled for each identifier at once."     ),
            ColorId.DisabledAutoSet            => (0xFF808080, "Disabled Automation Set",          "An automation set that is currently disabled."                                                             ),
            ColorId.AutomationActorAvailable   => (0xFFFFFFFF, "Automation Actor Available",       "A character associated with the given automated design set is currently visible."                          ),
            ColorId.AutomationActorUnavailable => (0xFF808080, "Automation Actor Unavailable",     "No character associated with the given automated design set is currently visible."                         ),
            ColorId.HeaderButtons              => (0xFFFFF0C0, "Header Buttons",                   "The text and border color of buttons in the header, like the Incognito toggle."                            ),
            ColorId.FavoriteStarOn             => (0xFF40D0D0, "Favored Item",                     "The color of the star for favored items and of the border in the unlock overview tab."                     ),
            ColorId.FavoriteStarHovered        => (0xFFD040D0, "Favorite Star Hovered",            "The color of the star for favored items when it is hovered."                                               ),
            ColorId.FavoriteStarOff            => (0x20808080, "Favorite Star Outline",            "The color of the star for items that are not favored when it is not hovered."                              ),
            _                                  => (0x00000000, string.Empty,                       string.Empty                                                                                                ),
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
