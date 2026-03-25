using Luna.Generators;

namespace Glamourer.GameData;

[NamedEnum(Utf16: false)]
public enum MenuType
{
    ListSelector      = 0,
    IconSelector      = 1,
    ColorPicker       = 2,
    DoubleColorPicker = 3,
    IconCheckmark     = 4,
    Percentage        = 5,
    Checkmark         = 6, // custom
    Nothing           = 7, // custom
    List1Selector     = 8, // custom, 1-indexed lists
}
