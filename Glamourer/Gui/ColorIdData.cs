using ImSharp;
using Luna;
using static Glamourer.Gui.ColorId;

namespace Glamourer.Gui;

public readonly struct ColorIdData : IColorData<ColorId>
{
    private static readonly ColorData<ColorId>[] ColorData = CreateData();

    public static ColorData<ColorId> Data(in ColorId id)
    {
        if ((int)id < 0 || (int)id >= ColorData.Length)
            return ColorData<ColorId>.Invalid;

        return ColorData[(int)id];
    }

    public static StringU8 Parent { get; } = new("Glamourer"u8);

    private static ColorData<ColorId>[] CreateData()
    {
        var designs    = "Design Selector"u8;
        var metadata   = "Metadata"u8;
        var automation = "Automation"u8;
        var actors     = "Actors & NPCs"u8;
        var qdb        = "Quick Design Bar"u8;

        var ret = new ColorData<ColorId>[ColorId.Values.Count];

        ret[(int)NormalDesign] = new ColorData<ColorId>(ImGuiColor.Text, "Normal Design"u8, "A design with no specific traits."u8, designs);
        ret[(int)CustomizationDesign] = new ColorData<ColorId>(0xFFC000C0, "Customization Design"u8,
            "A design that only changes customizations on a character."u8, designs);
        ret[(int)StateDesign] = new ColorData<ColorId>(0xFF00C0C0, "State Design"u8,
            "A design that does not change equipment or customizations on a character."u8, designs);
        ret[(int)EquipmentDesign] =
            new ColorData<ColorId>(0xFF00C000, "Equipment Design"u8, "A design that only changes equipment on a character."u8, designs);
        ret[(int)ActorAvailable] = new ColorData<ColorId>(DalamudColor.SuccessForeground, "Actor Available"u8,
            "The header in the Actor tab panel if the currently selected actor exists in the game world at least once."u8, actors);
        ret[(int)ActorUnavailable] = new ColorData<ColorId>(DalamudColor.ErrorForeground, "Actor Unavailable"u8,
            "The Header in the Actor tab panel if the currently selected actor does not exist in the game world."u8, actors);
        ret[(int)FolderExpanded] =
            new ColorData<ColorId>(FolderLine, "Expanded Design Folder"u8, "A design folder that is currently expanded."u8, designs);
        ret[(int)FolderCollapsed] =
            new ColorData<ColorId>(FolderLine, "Collapsed Design Folder"u8, "A design folder that is currently collapsed."u8, designs);
        ret[(int)FolderLine] = new ColorData<ColorId>(0xFFFFF0C0, "Expanded Design Folder Line"u8,
            "The line signifying which descendants belong to an expanded design folder."u8, designs);
        ret[(int)AlternatingFolderLine] = new ColorData<ColorId>(FolderLine, "Expanded Mod Folder Line (Alternating)"u8,
            "The line signifying which descendants belong to an expanded mod folder for even folder lines."u8, designs);
        ret[(int)EnabledAutoSet] = new ColorData<ColorId>(0xFFA0F0A0, "Enabled Automation Set"u8,
            "An automation set that is currently enabled. Only one set can be enabled for each identifier at once."u8, automation);
        ret[(int)DisabledAutoSet] =
            new ColorData<ColorId>(ImGuiColor.TextDisabled, "Disabled Automation Set"u8, "An automation set that is currently disabled."u8, automation);
        ret[(int)AutomationActorAvailable] = new ColorData<ColorId>(ImGuiColor.Text, "Automation Actor Available"u8,
            "A character associated with the given automated design set is currently visible."u8, automation);
        ret[(int)AutomationActorUnavailable] = new ColorData<ColorId>(ImGuiColor.TextDisabled, "Automation Actor Unavailable"u8,
            "No character associated with the given automated design set is currently visible."u8, automation);
        ret[(int)HeaderButtons] = new ColorData<ColorId>(0xFFFFF0C0, "Header Buttons"u8,
            "The text and border color of buttons in the header, like the Incognito toggle."u8, metadata);
        ret[(int)FavoriteStarOn] = new ColorData<ColorId>(0xFF40D0D0, "Favored Item"u8,
            "The color of the star for favored items and of the border in the unlock overview tab."u8, metadata);
        ret[(int)FavoriteStarHovered] = new ColorData<ColorId>(0xFFD040D0, "Favorite Star Hovered"u8,
            "The color of the star for favored items when it is hovered."u8, metadata);
        ret[(int)FavoriteStarOff] = new ColorData<ColorId>(0x20808080, "Favorite Star Outline"u8,
            "The color of the star for items that are not favored when it is not hovered."u8, metadata);
        ret[(int)PredefinedTagAdd] = new ColorData<ColorId>(DalamudColor.SuccessBackground, "Predefined Tags: Add Tag"u8,
            "A predefined tag that is not present on the current design and can be added."u8, metadata);
        ret[(int)PredefinedTagRemove] = new ColorData<ColorId>(DalamudColor.ErrorBackground, "Predefined Tags: Remove Tag"u8,
            "A predefined tag that is already present on the current design and can be removed."u8, metadata);
        ret[(int)QuickDesignButton] = new ColorData<ColorId>(0x900A0A0A, "Quick Design Bar Button Background"u8,
            "The color of button frames in the quick design bar."u8, qdb);
        ret[(int)QuickDesignFrame] = new ColorData<ColorId>(0x90383838, "Quick Design Bar Combo Background"u8,
            "The color of the combo background in the quick design bar."u8, qdb);
        ret[(int)QuickDesignBg] = new ColorData<ColorId>(0x00F0F0F0, "Quick Design Bar Window Background"u8,
            "The color of the window background in the quick design bar."u8, qdb);
        ret[(int)TriStateCheck] = new ColorData<ColorId>(0xFF00D000, "Checkmark in Tri-State Checkboxes"u8,
            "The color of the checkmark indicating positive change in tri-state checkboxes."u8, metadata);
        ret[(int)TriStateCross] = new ColorData<ColorId>(0xFF0000D0, "Cross in Tri-State Checkboxes"u8,
            "The color of the cross indicating negative change in tri-state checkboxes."u8, metadata);
        ret[(int)TriStateNeutral] = new ColorData<ColorId>(0xFFD0D0D0, "Dot in Tri-State Checkboxes"u8,
            "The color of the dot indicating no change in tri-state checkboxes."u8, metadata);
        ret[(int)BattleNpc] = new ColorData<ColorId>(ImGuiColor.Text, "Battle NPC in NPC Tab"u8,
            "The color of the names of battle NPCs in the NPC tab that do not have a more specific color assigned."u8, actors);
        ret[(int)EventNpc] = new ColorData<ColorId>(ImGuiColor.Text, "Event NPC in NPC Tab"u8,
            "The color of the names of event NPCs in the NPC tab that do not have a more specific color assigned."u8, actors);
        ret[(int)ModdedItemMarker] = new ColorData<ColorId>(0xFFFF20FF, "Modded Item Marker"u8,
            "The color of dot in the unlocks overview tab signaling that the item is modded in the currently selected Penumbra collection."u8,
            metadata);
        ret[(int)ContainsItemsEnabled] = new ColorData<ColorId>(0xFFA0F0A0, "Enabled Mod Contains Design Items"u8,
            "The color of enabled mods in the associated mod dropdown menu when they contain items used in this design."u8, metadata);
        ret[(int)ContainsItemsDisabled] = new ColorData<ColorId>(0x80A0F0A0, "Disabled Mod Contains Design Items"u8,
            "The color of disabled mods in the associated mod dropdown menu when they contain items used in this design."u8, metadata);
        ret[(int)AdvancedDyeActive] = new ColorData<ColorId>(0xFF58DDFF, "Advanced Dyes Active"u8,
            "The highlight color for the advanced dye button and marker if any advanced dyes are active for this slot."u8, metadata);

        foreach (var data in ret)
        {
            if (data.Default.Value is 0)
                throw new SystemException("A color ID has no data assigned.");
        }

        return ret;
    }

    /// <summary> The old hardcoded default values used for migration. </summary>
    internal static Rgba32 OldDefault(ColorId id)
        => id switch
        {
            NormalDesign               => 0xFFFFFFFF,
            CustomizationDesign        => 0xFFC000C0,
            StateDesign                => 0xFF00C0C0,
            EquipmentDesign            => 0xFF00C000,
            ActorAvailable             => 0xFF18C018,
            ActorUnavailable           => 0xFF1818C0,
            FolderExpanded             => 0xFFFFF0C0,
            FolderCollapsed            => 0xFFFFF0C0,
            FolderLine                 => 0xFFFFF0C0,
            EnabledAutoSet             => 0xFFA0F0A0,
            DisabledAutoSet            => 0xFF808080,
            AutomationActorAvailable   => 0xFFFFFFFF,
            AutomationActorUnavailable => 0xFF808080,
            HeaderButtons              => 0xFFFFF0C0,
            FavoriteStarOn             => 0xFF40D0D0,
            FavoriteStarHovered        => 0xFFD040D0,
            FavoriteStarOff            => 0x20808080,
            QuickDesignButton          => 0x900A0A0A,
            QuickDesignFrame           => 0x90383838,
            QuickDesignBg              => 0x00F0F0F0,
            TriStateCheck              => 0xFF00D000,
            TriStateCross              => 0xFF0000D0,
            TriStateNeutral            => 0xFFD0D0D0,
            BattleNpc                  => 0xFFFFFFFF,
            EventNpc                   => 0xFFFFFFFF,
            ModdedItemMarker           => 0xFFFF20FF,
            ContainsItemsEnabled       => 0xFFA0F0A0,
            ContainsItemsDisabled      => 0x80A0F0A0,
            AdvancedDyeActive          => 0xFF58DDFF,
            _                          => 0,
        };
}
