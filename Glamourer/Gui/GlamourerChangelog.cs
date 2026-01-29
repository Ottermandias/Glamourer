using ImSharp;
using Luna;

namespace Glamourer.Gui;

public class GlamourerChangelog
{
    public const     int           LastChangelogVersion = 0;
    private readonly Configuration _config;
    public readonly  Changelog     Changelog;

    public GlamourerChangelog(Configuration config)
    {
        _config   = config;
        Changelog = new Changelog("Glamourer Changelog", ConfigData, Save);

        Add1_0_0_0(Changelog);
        Add1_0_0_1(Changelog);
        Add1_0_0_2(Changelog);
        Add1_0_0_3(Changelog);
        Add1_0_0_6(Changelog);
        Add1_0_1_1(Changelog);
        Add1_0_2_0(Changelog);
        Add1_0_3_0(Changelog);
        Add1_0_4_0(Changelog);
        Add1_0_5_0(Changelog);
        Add1_0_6_0(Changelog);
        Add1_0_7_0(Changelog);
        Add1_1_0_0(Changelog);
        Add1_1_0_2(Changelog);
        Add1_1_0_4(Changelog);
        AddDummy(Changelog);
        AddDummy(Changelog);
        Add1_2_0_0(Changelog);
        Add1_2_1_0(Changelog);
        AddDummy(Changelog);
        Add1_2_3_0(Changelog);
        Add1_3_1_0(Changelog);
        Add1_3_2_0(Changelog);
        Add1_3_3_0(Changelog);
        Add1_3_4_0(Changelog);
        Add1_3_5_0(Changelog);
        Add1_3_6_0(Changelog);
        Add1_3_7_0(Changelog);
        Add1_3_8_0(Changelog);
        Add1_4_0_0(Changelog);
        Add1_5_0_0(Changelog);
        Add1_5_1_0(Changelog);
    }

    private (int, ChangeLogDisplayType) ConfigData()
        => (_config.Ephemeral.LastSeenVersion, _config.ChangeLogDisplayType);

    private void Save(int version, ChangeLogDisplayType type)
    {
        if (_config.Ephemeral.LastSeenVersion != version)
        {
            _config.Ephemeral.LastSeenVersion = version;
            _config.Ephemeral.Save();
        }

        if (_config.ChangeLogDisplayType != type)
        {
            _config.ChangeLogDisplayType = type;
            _config.Save();
        }
    }

    private static void Add1_5_1_0(Changelog log)
        => log.NextVersion("Version 1.5.1.0"u8)
            .RegisterHighlight("Added support for Penumbras PCP functionality to add the current state of the character as a design."u8)
            .RegisterEntry("On import, a design for the PCP is created and, if possible, applied to the character."u8, 1)
            .RegisterEntry("No automation is assigned."u8,                                                             1)
            .RegisterEntry("Finer control about this can be found in the settings."u8,                                 1)
            .RegisterEntry("Fixed an issue with static visors not toggling through Glamourer (1.5.0.7)."u8)
            .RegisterEntry("The advanced dye slot combo now contains glasses (1.5.0.7)."u8)
            .RegisterEntry("Several fixes for patch-related issues (1.5.0.1 - 1.5.0.6"u8);

    private static void Add1_5_0_0(Changelog log)
        => log.NextVersion("Version 1.5.0.0"u8)
            .RegisterImportant(
                "Updated for game version 7.30 and Dalamud API13, which uses a new GUI backend. Some things may not work as expected. Please let me know any issues you encounter."u8)
            .RegisterHighlight("Added the new Viera Ears state to designs. Old designs will not apply the state."u8)
            .RegisterHighlight("Added the option to make newly created designs write-protected by default to the design defaults."u8)
            .RegisterEntry("Fixed issues with reverting state and IPC."u8)
            .RegisterEntry("Fixed an issue when using the mousewheel to scroll through designs (1.4.0.3)."u8)
            .RegisterEntry("Fixed an issue with invalid bonus items (1.4.0.3)."u8)
            .RegisterHighlight(
                "Added drag & drop of equipment pieces which will try to match the corresponding model IDs in other slots if possible (1.4.0.2)."u8)
            .RegisterEntry("Heavily optimized some issues when having many designs and creating new ones or updating them (1.4.0.2)"u8)
            .RegisterEntry("Fixed an issue with staining templates (1.4.0.1)."u8)
            .RegisterEntry("Fixed an issue with the QDB buttons not counting correctly (1.4.0.1)."u8);

    private static void Add1_4_0_0(Changelog log)
        => log.NextVersion("Version 1.4.0.0"u8)
            .RegisterHighlight(
                "The design selector width is now draggable within certain restrictions that depend on the total window width."u8)
            .RegisterEntry("The current behavior may not be final, let me know if you have any comments."u8, 1)
            .RegisterEntry("Regular customization colors can now be dragged & dropped onto other customizations."u8)
            .RegisterEntry(
                "If no identical color is available in the target slot, the most similar color available (for certain values of similar) will be chosen instead."u8,
                1)
            .RegisterEntry("Resetting advanced dyes and customizations has been split into two buttons for the quick design bar."u8)
            .RegisterEntry("Weapons now also support custom ID input in the combo search box."u8)
            .RegisterEntry("Added new IPC methods GetExtendedDesignData, AddDesign, DeleteDesign, GetDesignBase64, GetDesignJObject."u8)
            .RegisterEntry("Added the option to prevent immediate repeats for random design selection (Thanks Diorik!)."u8)
            .RegisterEntry("Optimized some multi-design changes when selecting many designs and changing them at once."u8)
            .RegisterEntry("Fixed item combos not starting from the currently selected item when scrolling them via mouse wheel."u8)
            .RegisterEntry("Fixed some issue with Glamourer not searching mods by name for mod associations in some cases."u8)
            .RegisterEntry("Fixed the IPC methods SetMetaState and SetMetaStateName not working (Thanks Caraxi!)."u8)
            .RegisterEntry("Added new IPC method GetDesignListExtended. (1.3.8.6)"u8)
            .RegisterEntry(
                "Improved the naming of NPCs for identifiers by using Haselnussbombers new naming functionality (Thanks Hasel!). (1.3.8.6)"u8)
            .RegisterEntry(
                "Added a modifier key separate from the delete modifier key that is used for less important key-checks, specifically toggling incognito mode. (1.3.8.5)"u8)
            .RegisterEntry("Used better Penumbra IPC for some things. (1.3.8.5)"u8)
            .RegisterEntry("Fixed an issue with advanced dyes for weapons. (1.3.8.5)"u8)
            .RegisterEntry("Fixed an issue with NPC automation due to missing job detection. (1.3.8.1)"u8);

    private static void Add1_3_8_0(Changelog log)
        => log.NextVersion("Version 1.3.8.0"u8)
            .RegisterImportant("Updated Glamourer for update 7.20 and Dalamud API 12."u8)
            .RegisterEntry(
                "This is not thoroughly tested, but I decided to push to stable instead of testing because otherwise a lot of people would just go to testing just for early access again despite having no business doing so."u8,
                1)
            .RegisterEntry(
                "I also do not use most of the functionality of Glamourer myself, so I am unable to even encounter most issues myself."u8, 1)
            .RegisterEntry("If you encounter any issues, please report them quickly on the discord."u8,                                    1)
            .RegisterEntry("Added a chat command to clear temporary settings applied by Glamourer to Penumbra."u8)
            .RegisterEntry("Fixed small issues with customizations not applicable to your race still applying."u8);

    private static void Add1_3_7_0(Changelog log)
        => log.NextVersion("Version 1.3.7.0"u8)
            .RegisterImportant(
                "The option to disable advanced customizations or advanced dyes has been removed. The functionality can no longer be disabled entirely, you can just decide not to use it, and to hide it."u8)
            .RegisterHighlight(
                "You can now configure which panels (like Customization, Equipment, Advanced Customization etc.) are displayed at all, and which are expanded by default. This does not disable any functionality."u8)
            .RegisterHighlight(
                "The Unlocks tab now shows whether items are modded in the currently selected collection in Penumbra in Overview mode and shows and can filter and sort for it in Detailed mode."u8)
            .RegisterEntry("Added an optional button to the Quick Design Bar to reset all temporary settings applied by Glamourer."u8)
            .RegisterHighlight(
                "Any existing advanced dyes will now be highlighted on the corresponding Advanced Dye buttons in the actors panel and on the corresponding equip slot name in the design panel."u8)
            .RegisterEntry("This also affects currently inactive advanced dyes, which can now be manually removed on the inactive materials."u8,
                1)
            .RegisterHighlight(
                "In the design list of an automation set, the design indices are now highlighted if a design contains advanced dyes, mod associations, or links to other designs."u8)
            .RegisterHighlight("Some quality of life improvements:"u8)
            .RegisterEntry("Added some buttons for some application rule presets to the Application Rules panel."u8,                         1)
            .RegisterEntry("Added some buttons to enable, disable or delete all advanced dyes in a design."u8,                               1)
            .RegisterEntry("Some of those buttons are also available in multi-design selection to apply to all selected designs at once."u8, 1)
            .RegisterEntry(
                "A copied material color set from Penumbra should now be able to be imported into a advanced dye color set, as well as the other way around."u8)
            .RegisterEntry(
                "Automatically applied character updates when applying a design with mod associations and temporary settings are now skipped to prevent some issues with GPose. This should not affect anything else."u8)
            .RegisterEntry("Glamourer now differentiates between temporary settings applied through manual or automatic application."u8);


    private static void Add1_3_6_0(Changelog log)
        => log.NextVersion("Version 1.3.6.0"u8)
            .RegisterHighlight("Added some new multi design selection functionality to change design settings of many designs at once."u8)
            .RegisterEntry("Also added the number of selected designs and folders to the multi design selection display."u8, 1)
            .RegisterEntry("Glamourer will now use temporary settings when saving mod associations, if they exist in Penumbra."u8)
            .RegisterEntry(
                "Actually added the checkbox to reset all temporary settings to Automation Sets (functionality was there, just not exposed to the UI...)."u8)
            .RegisterEntry(
                "Adapted the behavior for identified copies of characters that have a different state than the character itself to deal with the associated Penumbra changes."u8)
            .RegisterEntry(
                "Added '/glamour resetdesign' as a command, that re-applies automation but resets randomly chosen designs (Thanks Diorik)."u8)
            .RegisterEntry("All existing facepaints should now be accepted in designs, including NPC facepaints."u8)
            .RegisterEntry(
                "Overwriting a design with your characters current state will now discard any prior advanced dyes and only add those from the current state."u8)
            .RegisterEntry("Fixed an issue with racial mount and accessory scaling when changing zones on a changed race."u8)
            .RegisterEntry("Fixed issues with the detection of gear set changes in certain circumstances (Thanks Cordelia)."u8)
            .RegisterEntry("Fixed an issue with the Force to Inherit checkbox in mod associations."u8)
            .RegisterEntry(
                "Added a new IPC event that fires only when Glamourer finalizes its current changes to a character (for/from Cordelia)."u8)
            .RegisterEntry("Added new IPC to set a meta flag on actors. (for/from Cordelia)."u8);

    private static void Add1_3_5_0(Changelog log)
        => log.NextVersion("Version 1.3.5.0"u8)
            .RegisterHighlight(
                "Added the usage of the new Temporary Mod Setting functionality from Penumbra to apply mod associations. This is on by default but can be turned back to permanent changes in the settings."u8)
            .RegisterEntry("Designs now have a setting to always reset all prior temporary settings made by Glamourer on application."u8, 1)
            .RegisterEntry("Automation Sets also have a setting to do this, independently of the designs contained in them."u8,           1)
            .RegisterHighlight("More NPC customization options should now be accepted as valid for designs, regardless of clan/gender."u8)
            .RegisterHighlight(
                "The 'Apply' chat command had the currently selected design and the current quick bar design added as choices."u8)
            .RegisterEntry(
                "The application buttons for designs, NPCs or actors should now stick at the top of their respective panels even when scrolling down."u8)
            .RegisterHighlight("Randomly chosen designs should now stay across loading screens or redrawing. (1.3.4.3)"u8)
            .RegisterEntry(
                "In automation, Random designs now have an option to always choose another design, including during loading screens or redrawing."u8,
                1)
            .RegisterEntry("Fixed an issue where disabling auto designs did not work as expected."u8)
            .RegisterEntry("Fixed the inversion of application flags in IPC calls."u8)
            .RegisterEntry("Fixed an issue with the scaling of the Advanced Dye popup with increased font sizes."u8)
            .RegisterEntry("Fixed a bug when editing gear set conditions in the automation tab."u8)
            .RegisterEntry("Fixed some ImGui issues."u8);

    private static void Add1_3_4_0(Changelog log)
        => log.NextVersion("Version 1.3.4.0"u8)
            .RegisterEntry("Glamourer has been updated for Dalamud API 11 and patch 7.1."u8)
            .RegisterEntry("Maybe fixed issues with shared weapon types and reset designs."u8)
            .RegisterEntry("Fixed issues with resetting advanced dyes and certain weapon types."u8);

    private static void Add1_3_3_0(Changelog log)
        => log.NextVersion("Version 1.3.3.0"u8)
            .RegisterHighlight("Added the option to create automations for owned human NPCs (like trust avatars)."u8)
            .RegisterEntry("Added some special filters to the Actors tab selector, hover over it to see the options."u8)
            .RegisterEntry("Added an option for designs to always reset all previously applied advanced dyes."u8)
            .RegisterEntry("Added some new NPC-only customizations to the valid customizations."u8)
            .RegisterEntry("Reworked quite a bit of things around face wear / bonus items. Please let me know if anything broke."u8);

    private static void Add1_3_2_0(Changelog log)
        => log.NextVersion("Version 1.3.2.0"u8)
            .RegisterEntry("Fixed an issue with weapon hiding when leaving GPose or changing zones."u8)
            .RegisterEntry("Added support for unnamed items to be previewed from Penumbra."u8)
            .RegisterEntry(
                "Item combos filters now check if the model string starts with the current filter, instead of checking if the primary ID contains the current filter."u8)
            .RegisterEntry("Improved the handling of bonus items (glasses) in designs."u8)
            .RegisterEntry("Imported .chara files now import bonus items."u8)
            .RegisterEntry(
                "Added a Debug Data rider in the Actors tab that is visible if Debug Mode is enabled and (currently) contains some IDs."u8)
            .RegisterEntry("Fixed bonus items not reverting correctly in some cases."u8)
            .RegisterEntry("Fixed an issue with the RNG in cheat codes and events skipping some possible entries."u8)
            .RegisterEntry("Fixed the chat log context menu for glamourer Try-On."u8)
            .RegisterEntry("Fixed some issues with cheat code sets."u8)
            .RegisterEntry(
                "Made the popped out Advanced Dye Window and Unlocks Window non-docking as that caused issues when docked to the main Glamourer window."u8)
            .RegisterEntry("Refreshed NPC name associations."u8)
            .RegisterEntry("Removed a now useless cheat code."u8)
            .RegisterEntry("Added API for Bonus Items. (1.3.1.1)"u8);

    private static void Add1_3_1_0(Changelog log)
        => log.NextVersion("Version 1.3.1.0"u8)
            .RegisterHighlight("Glamourer is now released for Dawntrail!"u8)
            .RegisterEntry("Added support for female Hrothgar."u8,  1)
            .RegisterEntry("Added support for the Glasses slot."u8, 1)
            .RegisterEntry("Added support for two dye slots."u8,    1)
            .RegisterImportant(
                "There were some issues with Advanced Dyes stored in Designs. When launching this update, Glamourer will try to migrate all your old designs into the new form."u8)
            .RegisterEntry("Unfortunately, this is slightly based on guesswork and may cause false-positive migrations."u8,            1)
            .RegisterEntry("In general, the values for Gloss and Specular Strength were swapped, so the migration swaps them back."u8, 1)
            .RegisterEntry(
                "In some cases this may not be correct, or the values stored were problematic to begin with and will now cause further issues."u8,
                1)
            .RegisterImportant(
                "If your designs lose their specular color, you need to verify that the Specular Strength is non-zero (usually in 0-100%)."u8,
                1)
            .RegisterImportant(
                "If your designs are extremely glossy and reflective, you need to verify that the Gloss value is greater than zero (usually a power of 2 >= 1, it should never be 0)."u8,
                1)
            .RegisterEntry(
                "I am very sorry for the inconvenience but there is no way to salvage this sanely in all cases, especially with user-input values."u8,
                1)
            .RegisterImportant(
                "Any materials already using Dawntrails shaders will currently not be able to edit the Gloss or Specular Strength Values in Advanced Dyes."u8)
            .RegisterImportant(
                "Skin and Hair Shine from advanced customizations are not supported by the game any longer, so they are not displayed for the moment."u8)
            .RegisterHighlight("All eyes now support Limbal rings (which use the Feature Color for their color)."u8)
            .RegisterHighlight("Dyes can now be dragged and dropped onto other dyes to replicate them."u8)
            .RegisterEntry("The job filter in the Unlocks tab has been improved."u8)
            .RegisterHighlight(
                "Editing designs or actors now has a history and you can undo up to 16 of the last changes you made, separately per design or actor."u8)
            .RegisterEntry(
                "Some changes (like when a weapon applies its offhand) may count as multiple and have to be stepped back separately."u8, 1)
            .RegisterEntry("You can now change the priority or enabled state of associated mods directly."u8)
            .RegisterEntry("Glamourer now has a Support Info button akin to Penumbra's."u8)
            .RegisterEntry("Glamourer now respects write protection on designs better."u8)
            .RegisterEntry("The advanced dye window popup should now get focused when it is opening even in detached state."u8)
            .RegisterEntry("Added API and IPC for bonus items, i.e. the Glasses slot."u8)
            .RegisterHighlight("You can now display your characters height in Corgis or Olympic Swimming Pools."u8)
            .RegisterEntry("Fixed some issues with advanced customizations and dyes applied via IPC. (1.2.3.2)"u8)
            .RegisterEntry(
                "Glamourer now uses the last matching game object for advanced dyes instead of the first (mainly relevant for GPose). (1.2.3.1)"u8);

    private static void Add1_2_3_0(Changelog log)
        => log.NextVersion("Version 1.2.3.0"u8)
            .RegisterHighlight(
                "Added a field to rename designs directly from the mod selector context menu, instead of moving them in the filesystem."u8)
            .RegisterEntry("You can choose which rename field (none, either one or both) to display in the settings."u8, 1)
            .RegisterEntry("Automatically applied offhand weapons due to mainhand settings now also apply the mainhands dye."u8)
            .RegisterHighlight("Added a height display in real-world units next to the height-selector."u8)
            .RegisterEntry("This can be configured to use your favourite unit of measurement, even wrong ones, or not display at all."u8, 1)
            .RegisterHighlight(
                "Added a chat command '/glamour applycustomization' that can apply single customization values to actors. Use without arguments for help."u8)
            .RegisterHighlight(
                "Added an option for designs to always force a redraw when applied to a character, regardless of whether it is necessary or not."u8)
            .RegisterHighlight("Added a button to overwrite the selected design with the current player state."u8)
            .RegisterEntry("Added some copy/paste functionality for mod associations."u8)
            .RegisterEntry("Reworked the API and IPC structure heavily."u8)
            .RegisterEntry("Added warnings if Glamourer can not attach successfully to Penumbra or if Penumbras IPC version is not correct."u8)
            .RegisterEntry("Added hints for all of the available cheat codes and improved the cheat code display somewhat."u8)
            .RegisterEntry("Fixed weapon selectors not having a favourite star available."u8)
            .RegisterEntry("Fixed issues with items with custom names."u8)
            .RegisterEntry("Fixed the labels for eye colors."u8)
            .RegisterEntry("Fixed the tooltip for Apply Dye checkboxes."u8)
            .RegisterEntry("Fixed an issue when hovering over assigned mod settings."u8)
            .RegisterEntry("Made conformant to Dalamud guidelines by adding a button to open the main UI."u8)
            .RegisterEntry("Fixed an issue with visor states. (1.2.1.3)"u8)
            .RegisterEntry("Fixed an issue with identical weapon types and multiple restricted designs. (1.2.1.3)"u8);

    private static void Add1_2_1_0(Changelog log)
        => log.NextVersion("Version 1.2.1.0"u8)
            .RegisterEntry("Updated for .net 8 and FFXIV 6.58, using some new framework options to improve performance and stability."u8)
            .RegisterEntry("Previewing changed items in Penumbra now works with all weapons in GPose. (1.2.0.8)"u8)
            .RegisterEntry(
                "Added a design type selectable for automation that applies the design currently selected in the quick design bar. (1.2.0.4)"u8)
            .RegisterEntry("Added an option to respect manual changes when changing automation settings. (1.2.0.3)"u8)
            .RegisterEntry(
                "You can now apply designs to the player character with a double click on them (can be turned off in settings). (1.2.0.1)"u8)
            .RegisterEntry("The last selected design and tab are now stored and applied on startup. (1.2.0.1)"u8)
            .RegisterEntry("Fixed behavior of revert to automation to actually revert and not just reapply. (1.2.0.8)"u8)
            .RegisterEntry("Added Reapply Automation buttons and chat commands with prior behaviour."u8, 1)
            .RegisterEntry("Fixed random design never applying the last design in the set. (1.2.0.7)"u8)
            .RegisterEntry("Fixed colors of special designs. (1.2.0.7)"u8)
            .RegisterEntry("Fixed issues with weapon tracking. (1.2.0.5, 1.2.0.6)"u8)
            .RegisterEntry("Fixed issues with moved items and gearset changes not being listened to. (1.2.0.4)"u8)
            .RegisterEntry("Fixed issues with applying advanced dyes in fixed states. (1.2.0.2)"u8)
            .RegisterEntry("Fixed issues turning non-humans human. (1.2.0.1)"u8)
            .RegisterEntry("Fixed issues with body type application. (1.2.0.1, 1.2.0.2)"u8)
            .RegisterEntry("Fixed issue with design link application rule checkboxes. (1.2.0.1)"u8);

    private static void Add1_2_0_0(Changelog log)
        => log.NextVersion("Version 1.2.0.0"u8)
            .RegisterHighlight("Added the option to link to other designs in a design, causing all of them to be applied at once."u8)
            .RegisterEntry("This required reworking the handling for applying multiple designs at once (i.e.merging them)."u8, 1)
            .RegisterEntry(
                "This was a considerable backend change on both automation sets and design application. I may have messed up and introduced bugs. "u8
              + "The new version was on Testing for multiple weeks, but not many people use it. "u8
              + "Please let me know if something does not work right anymore."u8, 1)
            .RegisterHighlight("Added advanced dye options for equipment. You can now live-edit the color sets of your gear."u8)
            .RegisterEntry(
                "The logic for this is very complicated and may interfere with other options or not update correctly, it will need a lot of testing."u8,
                1)
            .RegisterEntry("Like Advanced Customization options, this can be turned off in the behaviour settings."u8, 1)
            .RegisterEntry(
                "To access the options, click the palette buttons in the Equipment Panel - the popup can also be detached from the main window in the settings."u8,
                1)
            .RegisterEntry("In designs, only actually changed rows will be stored. You can manually add rows, too."u8, 1)
            .RegisterHighlight(
                "Added an option so that manual application of a mainhand weapon will also automatically apply its associated offhand (and gloves, for certain fist weapons). This is off by default."u8)
            .RegisterHighlight(
                "Added an option that always tries to apply associated mod settings for designs to the Penumbra collection associated with the character the design is applied to."u8)
            .RegisterEntry(
                "This is off by default and I strongly recommend AGAINST using it, since Glamourer has no way to revert such changes. You are responsible for keeping your collection in order."u8,
                1)
            .RegisterHighlight(
                "Added mouse wheel scrolling to many selectors, e.g. for equipment, dyes or customizations. You need to hold Control while scrolling in most places."u8)
            .RegisterEntry("Improved handling for highlights with advanced customization colors and normal customization settings."u8)
            .RegisterHighlight(
                "Changed Item Customizations in Penumbra can now be right-clicked to preview them on your character, if you have the correct Gender/Race combo on them."u8)
            .RegisterHighlight(
                "Add the option to override associated collections for characters, so that automatically applied mod associations affect the overriden collection."u8)
            .RegisterHighlight(
                "Added the option to apply random designs (with optional restrictions) to characters via slash commands and automation."u8)
            .RegisterEntry("Added copy/paste buttons for advanced customization colors."u8)
            .RegisterEntry("Added alpha preview to advanced customization colors."u8)
            .RegisterEntry("Added a button to update the settings for an associated mod from their current settings."u8)
            .RegisterHighlight("Added 'Revert Equipment' and 'Revert Customizations' buttons to the Quick Design Bar."u8)
            .RegisterEntry("You can now toggle every functionality of the Quick Design Bar on or off separately."u8)
            .RegisterEntry("Updated a few fun module things. Now there are Pink elephants on parade!"u8)
            .RegisterEntry("Split up the IPC source state so IPC consumers can apply designs without them sticking around."u8)
            .RegisterEntry("Fixed an issue with gearset changes not registering in Glamourer for Automation."u8)
            .RegisterEntry("Fixed an issue with weapon loading being dependant on the order of loading Penumbra and Glamourer."u8)
            .RegisterEntry(
                "Fixed an issue with buttons sharing state and switching from design duplication to creating new ones caused errors."u8)
            .RegisterEntry("Fixed an issue where actors leaving during cutscenes or GPose caused Glamourer to throw a fit."u8)
            .RegisterEntry("Fixed an issue with NPC designs applying advanced customizations to targets and coloring them entirely black."u8);

    private static void AddDummy(Changelog log)
        => log.NextVersion(StringU8.Empty);

    private static void Add1_1_0_4(Changelog log)
        => log.NextVersion("Version 1.1.0.4"u8)
            .RegisterEntry("Added a check and warning for a lingering Palette+ installation."u8)
            .RegisterHighlight(
                "Added a button to only revert advanced customizations to game state to the quick design bar. This can be toggled off in the interface settings."u8)
            .RegisterEntry("Added visible configuration options for color display for the advanced customizations."u8)
            .RegisterEntry("Updated Battle NPC data from Gubal for 6.55."u8)
            .RegisterEntry("Fixed issues with advanced customizations not resetting correctly with Use Game State as Base."u8)
            .RegisterEntry("Fixed an issues with non-standard body type customizations not transmitting through Mare."u8)
            .RegisterEntry("Fixed issues with application rule checkboxes not working for advanced parameters."u8)
            .RegisterEntry("Fixed an issue with fist weapons, again again."u8)
            .RegisterEntry("Fixed multiple issues with advanced parameters not applying after certain other changes."u8)
            .RegisterEntry("Fixed another wrong restricted item."u8);

    private static void Add1_1_0_2(Changelog log)
        => log.NextVersion("Version 1.1.0.2"u8)
            .RegisterEntry("Added design colors in the preview of combos (in the quick bar and the automation panel)."u8)
            .RegisterHighlight("Improved Palette+ import options: Instead of entering a name, you can now select from available palettes."u8)
            .RegisterHighlight("In the settings tab, there is also a button to import ALL palettes from Palette+ as separate designs."u8, 1)
            .RegisterEntry(
                "Added a tooltip that you can enter numeric values to drag sliders by control-clicking for the muscle slider, also used slightly more useful caps."u8)
            .RegisterEntry("Fixed issues with monk weapons, again."u8)
            .RegisterEntry("Fixed an issue with the favourites file not loading."u8)
            .RegisterEntry("Fixed the name of the advanced parameters in the application panel."u8)
            .RegisterEntry("Fixed design clones not respecting advanced parameter application rules."u8);


    private static void Add1_1_0_0(Changelog log)
        => log.NextVersion("Version 1.1.0.0"u8)
            .RegisterHighlight("Added a new tab to browse, apply or copy (human) NPC appearances."u8)
            .RegisterHighlight("A characters body type can now be changed when copying state or saving designs from certain NPCs."u8)
            .RegisterHighlight("Added support for picking advanced colors for your characters customizations."u8)
            .RegisterEntry("The display and application of those can be toggled off in Glamourers behaviour settings."u8, 1)
            .RegisterEntry(
                "This provides the same functionality as Palette+, and Palette+ will probably be discontinued soonish (in accordance with Chirp)."u8,
                1)
            .RegisterEntry(
                "An option to import existing palettes from Palette+ by name is provided for designs, and can be toggled off in the settings."u8,
                1)
            .RegisterHighlight(
                "Advanced colors, equipment and dyes can now be reset to their game state separately by Control-Rightclicking them."u8)
            .RegisterHighlight("Hairstyles and face paints can now be made favourites."u8)
            .RegisterEntry("Added a new command '/glamour delete' to delete saved designs by name or identifier."u8)
            .RegisterEntry(
                "Added an optional parameter to the '/glamour apply' command that makes it apply the associated mod settings for a design to the collection associated with the identified character."u8)
            .RegisterEntry("Fixed changing weapons in Designs not working correctly."u8)
            .RegisterEntry("Fixed restricted gear protection breaking outfits for Mare pairs."u8)
            .RegisterEntry("Improved the handling of some cheat codes and added new ones."u8)
            .RegisterEntry("Added IPC to set single items or stains on characters."u8)
            .RegisterEntry("Added IPC to apply designs by GUID, and obtain a list of designs."u8);

    private static void Add1_0_7_0(Changelog log)
        => log.NextVersion("Version 1.0.7.0"u8)
            .RegisterHighlight("Glamourer now can set the free company crests on body slots, head slots and shields."u8)
            .RegisterEntry("Fixed an issue with tooltips in certain combo selectors."u8)
            .RegisterEntry("Fixed some issues with Hide Hat Gear and monsters turned into humans."u8)
            .RegisterEntry(
                "Hopefully fixed issues with icons used by Glamourer that are modified through Penumbra preventing Glamourer to even start in some cases."u8)
            .RegisterEntry("Those icons might still not appear if they fail to load, but Glamourer should at least still work."u8, 1)
            .RegisterEntry("Pre-emptively fixed a potential issue for the holidays."u8);

    private static void Add1_0_6_0(Changelog log)
        => log.NextVersion("Version 1.0.6.0"u8)
            .RegisterHighlight("Added the option to define custom color groups and associate designs with them."u8)
            .RegisterEntry("You can create and name design colors in Settings -> Colors -> Custom Design Colors."u8, 1)
            .RegisterEntry(
                "By default, all designs have an automatic coloring corresponding to the current system, that chooses a color dynamically based on application rules."u8,
                1)
            .RegisterEntry(
                "Example: You create a custom color named 'Test' and make it bright blue. Now you assign 'Test' to some design in its Design Details, and it will always display bright blue in the design list."u8,
                1)
            .RegisterEntry(
                "Design colors are stored by name. If a color can not be found, the design will display the Missing Color instead."u8, 1)
            .RegisterEntry("You can filter for designs using specific colors via c:"u8,                                                1)
            .RegisterHighlight(
                "You can now filter for the special case 'None' for filters where that makes sense (like Tags or Mod Associations)."u8)
            .RegisterHighlight(
                "When selecting multiple designs, you can now add or remove tags from them at once, and set their colors at once."u8)
            .RegisterEntry("Improved tri-state checkboxes. The colors of the new symbols can be changed in Color Settings."u8)
            .RegisterEntry("Removed half-baked localization of customization names and fixed some names in application rules."u8)
            .RegisterEntry("Improved Brio compatibility"u8)
            .RegisterEntry("Fixed some display issues with text color on locked designs."u8)
            .RegisterEntry("Fixed issues with automatic design color display for customization-only designs."u8)
            .RegisterEntry("Removed borders from the quick design window regardless of custom styling."u8)
            .RegisterEntry("Improved handling of (un)available customization options."u8)
            .RegisterEntry(
                "Some configuration like the currently selected tab states are now stored in a separate file that is not backed up and saved less often."u8)
            .RegisterEntry("Added option to open the Glamourer main window at game start independently of Debug Mode."u8);

    private static void Add1_0_5_0(Changelog log)
        => log.NextVersion("Version 1.0.5.0"u8)
            .RegisterHighlight("Dyes are can now be favorited the same way equipment pieces can."u8)
            .RegisterHighlight(
                "The quick design bar combo can now be scrolled through via mousewheel when hovering over the combo without opening it."u8)
            .RegisterEntry(
                "Control-Rightclicking the quick design bar now not only jumps to the corresponding design, but also opens the main window if it is not currently open."u8)
            .RegisterHighlight("You can now filter for designs containing specific items by using \"i:partial item name\"."u8)
            .RegisterEntry(
                "When overwriting a saved designs data entirely from clipboard, you can now undo this change and restore the prior design data once via a button top-left."u8)
            .RegisterEntry("Removed the \"Enabled\" checkbox in the settings since it was barely doing anything but breaking Glamourer."u8)
            .RegisterEntry(
                "If you want to disable Glamourers state-tracking and hooking, you will need to disable the entire Plugin via Dalamud now."u8,
                1)
            .RegisterEntry("Added a reference to \"/glamour\" in the \"/glamourer help\" section."u8)
            .RegisterEntry("Updated BNPC Data with new crowd-sourced data from the gubal library."u8)
            .RegisterEntry("Fixed an issue with the quick design bar when no designs are saved."u8)
            .RegisterEntry("Fixed a problem with characters not redrawing after leaving GPose even if necessary."u8);

    private static void Add1_0_4_0(Changelog log)
        => log.NextVersion("Version 1.0.4.0"u8)
            .RegisterEntry("The GPose target is now used for target-dependent functionality in GPose."u8)
            .RegisterEntry("Fixed a few issues with transformations, especially their weapons and head gear."u8)
            .RegisterEntry(
                "Previewing Offhand Models for both-handed weapons via right click is now possible (may need to wait for a not-yet released Penumbra update)."u8)
            .RegisterEntry("Updated the known list of Battle NPCs."u8)
            .RegisterEntry("Removed another technically unrestricted item from restricted item list."u8)
            .RegisterEntry("Use local time for discerning the current day on start-up instead of UTC-time."u8)
            .RegisterEntry("Improved the Unlocks Table with additional info. (1.0.3.1)"u8)
            .RegisterEntry("Added position locking option and more color options. (1.0.3.1)"u8)
            .RegisterEntry("Removed the default key combination for toggling the quick bar. (1.0.3.1)"u8);

    private static void Add1_0_3_0(Changelog log)
        => log.NextVersion("Version 1.0.3.0"u8)
            .RegisterEntry("Hopefully improved Palette+ compatibility."u8)
            .RegisterHighlight(
                "Added a Quick Design Bar, which is a small bar in which you can select your designs and apply them to yourself or your target, or revert them."u8)
            .RegisterEntry("You can toggle visibility of this bar via keybinds, which you can set up in the settings tab."u8,     1)
            .RegisterEntry("You can also lock the bar, and enable or disable an additional, identical bar in the main window."u8, 1)
            .RegisterEntry("Disabled a sound that played on startup when a certain Dalamud setting was enabled."u8)
            .RegisterEntry("Fixed an issue with reading state for Who Am I!?!. (1.0.2.2)"u8)
            .RegisterEntry("Fixed an issue where applying gear sets would not always update your dyes. (1.0.2.2)"u8)
            .RegisterEntry("Fixed an issue where some errors due to missing null-checks wound up in the log. (1.0.2.2)"u8)
            .RegisterEntry("Fixed an issue with hat visibility. (1.0.2.1 and 1.0.2.2)"u8)
            .RegisterEntry("Improved some logging. (1.0.2.1)"u8)
            .RegisterEntry("Improved notifications when encountering errors while loading automation sets. (1.0.2.1)"u8)
            .RegisterEntry("Fixed another issue with monk fist weapons. (1.0.2.1)"u8)
            .RegisterEntry("Added missing dot to changelog entry."u8);

    private static void Add1_0_2_0(Changelog log)
        => log.NextVersion("Version 1.0.2.0"u8)
            .RegisterHighlight("Added option to favorite items so they appear first in the item selection combos."u8)
            .RegisterEntry(
                "The reordering in the combo only happens after closing and opening it again so items do not vanish from view when you (un)favor them."u8,
                1)
            .RegisterEntry(
                "Favored items also get a highlighting border in the overview panels of the unlocks tab, but do not reorder those."u8, 1)
            .RegisterEntry("In the details panel of the unlocks tab items can be sorted and filtered on favorites."u8,                 1)
            .RegisterEntry("Added drag & drop support to drag an automated design into a different automated design set."u8)
            .RegisterEntry("This will remove said design from your current set and add it to the dragged-on set at the end."u8, 1)
            .RegisterEntry("Fixed ONE issue with hat visibility state. There are probably more. This is weird."u8)
            .RegisterEntry("Fixed minion placement for transformed Lalafell again."u8)
            .RegisterEntry("Fixed job flag filtering in detailed unlocks."u8)
            .RegisterEntry("Worked around a bug in the game's code breaking certain Monk Fist Weapons again... thanks SE."u8);

    private static void Add1_0_1_1(Changelog log)
        => log.NextVersion("Version 1.0.1.1"u8)
            .RegisterImportant(
                "Updated for 6.5 - Square Enix shuffled around a lot of things this update, so some things still might not work but have not been noticed yet. Please report any issues."u8)
            .RegisterHighlight(
                "Added additional item data like Job Restrictions, Required Level and Dyability to Items to search or filter for in the Detailed Unlocks tab."u8)
            .RegisterEntry("Improved support for non-item Weapons like NPC-Weapons saved to designs."u8)
            .RegisterEntry(
                "Improved messaging: many warnings or errors appearing will stay a little longer and can now be looked at in a Messages tab (visible only if there have been any)."u8)
            .RegisterEntry("Fixed an issue where moving automation sets caused editing them to not work correctly afterwards."u8)
            .RegisterEntry("Omega items are no longer restricted."u8)
            .RegisterEntry("Fixed reverting to game state not removing forced wetness."u8)
            .RegisterEntry(
                "Added some new cheat codes. You can now use the code 'Look at me, I'm your character now.' to add buttons that copy the actual state of yourself or your target into a clipboard-design, in case the randomizers gave you something cool!"u8)
            .RegisterEntry("Other new codes will be published in other ways."u8, 1);

    private static void Add1_0_0_6(Changelog log)
        => log.NextVersion("Version 1.0.0.6"u8)
            .RegisterHighlight(
                "Added two buttons to the top-right of the Glamourer window that allow you to revert your own player characters state to Game or to Automation state from any tab."u8)
            .RegisterEntry("Added the Shift/Control modifiers to the Apply buttons in the Designs tab, too."u8)
            .RegisterEntry("Fixed some issues with weapon types applying wrongly in automated designs."u8)
            .RegisterEntry(
                "Glamourer now removes designs you delete from all automation sets instead of screaming about them missing the next time you launch."u8)
            .RegisterEntry("Improved fixed design migration from pre 1.0 versions if anyone updates later."u8)
            .RegisterEntry("Added a line to warning messages for invalid entries that those entries are not saved with the warning."u8)
            .RegisterEntry("Added and improved some IPC for Mare."u8)
            .RegisterEntry("Broke and fixed some application rules for not-always available options."u8)
            .RegisterHighlight("This should fix tails and ears not being shared via Mare!"u8, 1);

    private static void Add1_0_0_3(Changelog log)
        => log.NextVersion("Version 1.0.0.3"u8)
            .RegisterHighlight("Reintroduced holding Control or Shift to apply only gear or customization changes."u8)
            .RegisterEntry("Deletion of multiple selected designs at once is now supported."u8)
            .RegisterEntry(
                "Added an 'Apply Mod Associations' button at the top row for designs. Hovering it tells you which collection it would edit."u8)
            .RegisterHighlight(
                "Turned 'Use Replacement Gear for Gear Unavailable to Your Race or Gender' OFF by default. If this setting confused you or you use mods that make those pieces available, please disable the setting."u8)
            .RegisterHighlight(
                "Added an option that a characters state reverts all manual changes after a zone change to simulate pre-rework behavior. This is OFF by default."u8)
            .RegisterEntry("Fixed some issues with chat command parsing and applying for NPCs."u8)
            .RegisterEntry("Turning into a Lalafell should now correctly cause minions to sit on your head instead your shoulders."u8)
            .RegisterEntry("Another, better, possibly working fix for Lalafell and Elezen ear shapes."u8)
            .RegisterEntry("Fixed a big issue with a memory leak concerning owned NPCs."u8)
            .RegisterEntry("Fixed some issues with non-zero model-ID but human characters, like Zero."u8)
            .RegisterEntry("Fixed an issue with weapons not respecting disabled automated designs."u8)
            .RegisterEntry("Maybe fixed an issue where unavailable customizations set to Apply still applied their invisible values."u8)
            .RegisterEntry(
                "Fixed display of automated design rows when unobtained item warnings were disabled but full checkmarks enabled, and the window was not wide enough for single row."u8)
            .RegisterEntry("Apply Forced Wetness state after creation of draw objects, maybe fixing it turning off on zone changes."u8);

    private static void Add1_0_0_2(Changelog log)
        => log.NextVersion("Version 1.0.0.2"u8)
            .RegisterHighlight("Added support for 'Clipboard' as a design source for /glamour apply."u8)
            .RegisterEntry("Improved tooltips for tri-state toggles."u8)
            .RegisterEntry("Improved some labels of settings to clarify what they do."u8)
            .RegisterEntry("Improved vertical space for automated design sets."u8)
            .RegisterEntry(
                "Improved tooltips for renaming/moving designs via right-click context to make it clear that this does not rename the design itself."u8)
            .RegisterHighlight("Added new configuration to hide advanced application rule settings in automated design lines."u8)
            .RegisterHighlight("Added new configuration to hide unobtained item warnings in automated design lines."u8)
            .RegisterEntry("Removed some warning popups for temporary designs when sharing non-human actors via Mare (I guess?)"u8)
            .RegisterEntry(
                "Fixed an issue with unnecessary redrawing in GPose when having applied a change that required redrawing after entering GPose."u8)
            .RegisterEntry("Fixed chat commands parsing concerning NPC identifiers."u8)
            .RegisterEntry("Fixed restricted racial gear applying to accessories by mistake."u8)
            .RegisterEntry("Maybe fixed Mare syncing having issues with restricted gear protection."u8)
            .RegisterEntry("Fixed the icon for disabled mods in associated mods."u8)
            .RegisterEntry("Fixed inability to remove associated mods except for the last one."u8)
            .RegisterEntry(
                "Fixed treating certain gloves as restricted because one restricted items sharing the model with identical, unrestricted gloves exists (wtf SE?)."u8)
            .RegisterEntry(
                "Hopefully fixed ear shape numbering for Elezen and Lalafell (yes SE, just put a 1-indexed option in a sea of 0-indexed options, sure. Fuck off-by-one error)."u8);

    private static void Add1_0_0_1(Changelog log)
        => log.NextVersion("Version 1.0.0.1"u8)
            .RegisterImportant("Fixed Issue with Migration of identically named designs. "u8
              + "If you lost any designs during migration, try going to \"%appdata%\\XIVLauncher\\pluginConfigs\\Glamourer\\\" "u8
              + "and renaming the file \"Designs.json.bak\" to \"Designs.json\"u8, then restarting."u8)
            .RegisterEntry("This may cause some duplicated entries"u8, 1)
            .RegisterEntry("Added a highlight border around the Enable/Disable All toggle for Automated Designs."u8)
            .RegisterEntry("Fixed newly created designs not being moved to folders when using paths like 'path/to/design' anymore."u8)
            .RegisterEntry("Added a tooltip to clarify the intent of the Mod Associations tab."u8)
            .RegisterEntry("Fixed an issue with some weapons not being recognized as offhands correctly."u8);

    private static void Add1_0_0_0(Changelog log)
        => log.NextVersion("Version 1.0.0.0 (Full Rework)"u8)
            .RegisterHighlight(
                "Glamourer has been reworked entirely. Basically everything has been written anew from the ground up, even though some things may look the same."u8)
            .RegisterEntry(
                "The new version has been tested for quite a while, but there still may be bugs, unintended changes or other issues that slipped through, given the limited amount of testers."u8,
                1)
            .RegisterEntry(
                "Migration of configuration and existing designs should mostly work, though some fixed designs may not migrate correctly."u8, 1)
            .RegisterEntry("All your data should be backed up before being changed, so restauration should always be possible in some way."u8,
                1)
            .RegisterImportant(
                "If you encounter any problems, please report them on the discord. If you encounter data loss, please do so immediately."u8, 1)
            .RegisterHighlight("Major Changes"u8)
            .RegisterEntry(
                "Redrawing characters is mostly gone. All equipment changes, and all customization changes except race, gender or face, can be applied instantaneously without redrawing."u8,
                1)
            .RegisterEntry(
                "As a side effect, Glamourer should no longer be dangerous to use with the aesthetician, since the games data of your character is no longer manipulated, only its visualization."u8,
                2)
            .RegisterEntry("Things like the Lalafell/Dwarf cave in Kholusia also no longer send invalid data to the server."u8, 2)
            .RegisterEntry("Portraits should also be entirely safe."u8,                                                         2)
            .RegisterEntry(
                "As another side effect, any changes you apply in any way will be kept across zone changes or character switches until they are actively overwritten by something or you restart the entire game, even without automation."u8,
                2)
            .RegisterImportant(
                "Compatibility with Anamnesis is questionable. Anamnesis will not be able to detect Glamourers changes, and changes in Anamnesis may confuse Glamourer."u8,
                2)
            .RegisterHighlight("Mare Synchronos compatibility is retained."u8, 2)
            .RegisterEntry("Reverting changes made works far more dependably."u8, 1)
            .RegisterEntry(
                "You can enable auto-reloading of gear, which will cause your equipment to be reloaded whenever you make changes to the mod collection affecting your character. Great for immediate comparisons of mod options!"u8,
                1)
            .RegisterEntry("Customizations can be toggled to apply or not apply individually instead of as a group for each design."u8, 1)
            .RegisterImportant("Replacing your weapons was slightly restricted."u8, 1)
            .RegisterEntry(
                "Outside of GPose, you can only replace weapons with other weapons of the same type. In GPose, you should still be able to change across types."u8,
                2)
            .RegisterEntry(
                "This restriction is because changing some weapon types can lead to game freezes, crashes, soft- and hard locks of your character, and can transmit invalid data to the server."u8,
                2)
            .RegisterEntry(
                "Designs now can carry more information, like tags, their creation or last update date, a description, and associated mods."u8,
                1)
            .RegisterEntry(
                "Fixed Designs are now called Automated Designs and can be found in the Automation tab. This tab has a help button in the selector."u8,
                1)
            .RegisterEntry("Automated designs use Penumbras way of identifying characters, thus they do not apply by pure name anymore."u8, 2)
            .RegisterImportant(
                "Please look through them after the migration, because not all names in fixed designs could necessarily be migrated."u8, 2)
            .RegisterEntry(
                "Glamourer can now keep track of which items and customizations have been seen on any of your characters on this installation, so you can have a Glamour log. "u8
              + "This log can optionally be used to restrict your own automated designs only to things you actually have unlocked, and can otherwise be used for browsing existing items."u8,
                1)
            .RegisterHighlight("Notable Minor Changes"u8)
            .RegisterEntry("Hrothgar faces should be fixed."u8, 1)
            .RegisterEntry(
                "Alpha value is gone. It may be brought back later on, but generally should not be, and was not often used, so eh."u8,
                1)
            .RegisterEntry(
                "Glamourer now can optionally protect you from gender- or race-restricted gear not appearing when you switch, by automatically using an appropriate replacement."u8,
                1)
            .RegisterEntry(
                "Glamourer has some fun optional easter eggs and cheat codes, like You've got to think for yourselves! You're all individuals!"u8,
                1)
            .RegisterEntry("You can enable game context menus so you can equip linked items via Glamourer."u8,         1)
            .RegisterEntry("A lot of configuration and options was learned from Penumbra and is available, like..."u8, 1)
            .RegisterEntry("... configurable color coding for the Glamourer UI."u8,                                    2)
            .RegisterEntry("... sort modes for your design list."u8,                                                   2)
            .RegisterEntry("... an automated backup system keeping up to 10 backups of your glamourer data."u8,        2)
            .RegisterEntry("... this Changelog!"u8,                                                                    2)
            .RegisterEntry("... configurable deletion modifiers for fewer misclicks!"u8,                               2);
}
