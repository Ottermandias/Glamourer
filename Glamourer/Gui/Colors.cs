using Glamourer.Config;
using ImSharp;
using Luna;

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
    AlternatingFolderLine,
    EnabledAutoSet,
    DisabledAutoSet,
    AutomationActorAvailable,
    AutomationActorUnavailable,
    HeaderButtons,
    FavoriteStarOn,
    FavoriteStarHovered,
    FavoriteStarOff,
    QuickDesignButton,
    QuickDesignFrame,
    QuickDesignBg,
    TriStateCheck,
    TriStateCross,
    TriStateNeutral,
    BattleNpc,
    EventNpc,
    ModdedItemMarker,
    ContainsItemsEnabled,
    ContainsItemsDisabled,
    AdvancedDyeActive,
}

public static class Colors
{
    public const   uint                             SelectedRed = 0xFF2020D0;
    private static ColorCache<ColorId, ColorIdData> _colors     = null!;

    extension(ColorId color)
    {
        public Rgba32 Value
            => _colors[color];

        public Vector4 Vector
            => _colors[color, true];
    }

    extension(ImGuiColor color)
    {
        public Rgba32 Value
            => _colors[color];

        public Vector4 Vector
            => _colors[color, true];
    }

    internal static void SetCache(ColorCache<ColorId, ColorIdData> cache)
        => _colors = cache;
}
