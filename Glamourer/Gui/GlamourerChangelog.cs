using OtterGui.Widgets;

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
    }

    private (int, ChangeLogDisplayType) ConfigData()
        => (_config.LastSeenVersion, _config.ChangeLogDisplayType);

    private void Save(int version, ChangeLogDisplayType type)
    {
        _config.LastSeenVersion      = version;
        _config.ChangeLogDisplayType = type;
        _config.Save();
    }

    private static void Add1_0_0_0(Changelog log)
        => log.NextVersion("Version 1.0.0.0 (Full Rework)")
            .RegisterHighlight(
                "Glamourer has been reworked entirely. Basically everything has been written anew from the ground up, even though some things may look the same.")
            .RegisterEntry(
                "The new version has been tested for quite a while, but there still may be bugs, unintended changes or other issues that slipped through, given the limited amount of testers.",
                1)
            .RegisterEntry(
                "Migration of configuration and existing designs should mostly work, though some fixed designs may not migrate correctly.",  1)
            .RegisterEntry("All your data should be backed up before being changed, so restauration should always be possible in some way.", 1)
            .RegisterImportant(
                "If you encounter any problems, please report them on the discord. If you encounter data loss, please do so immediately.", 1)
            .RegisterHighlight("Major Changes")
            .RegisterEntry(
                "Redrawing characters is mostly gone. All equipment changes, and all customization changes except race, gender or face, can be applied instantaneously without redrawing.",
                1)
            .RegisterEntry(
                "As a side effect, Glamourer should no longer be dangerous to use with the aesthetician, since the games data of your character is no longer manipulated, only its visualization.",
                2)
            .RegisterEntry("Things like the Lalafell/Dwarf cave in Kholusia also no longer send invalid data to the server.", 2)
            .RegisterEntry("Portraits should also be entirely safe.",                                                         2)
            .RegisterEntry(
                "As another side effect, any changes you apply in any way will be kept across zone changes or character switches until they are actively overwritten by something or you restart the entire game, even without automation.",
                2)
            .RegisterImportant(
                "Compatibility with Anamnesis is questionable. Anamnesis will not be able to detect Glamourers changes, and changes in Anamnesis may confuse Glamourer.",
                2)
            .RegisterHighlight("Mare Synchronos compatibility is retained.", 2)
            .RegisterEntry("Reverting changes made works far more dependably.", 1)
            .RegisterEntry(
                "You can enable auto-reloading of gear, which will cause your equipment to be reloaded whenever you make changes to the mod collection affecting your character. Great for immediate comparisons of mod options!",
                1)
            .RegisterEntry("Customizations can be toggled to apply or not apply individually instead of as a group for each design.", 1)
            .RegisterImportant("Replacing your weapons was slightly restricted.", 1)
            .RegisterEntry(
                "Outside of GPose, you can only replace weapons with other weapons of the same type. In GPose, you should still be able to change across types.",
                2)
            .RegisterEntry(
                "This restriction is because changing some weapon types can lead to game freezes, crashes, soft- and hard locks of your character, and can transmit invalid data to the server.",
                2)
            .RegisterEntry(
                "Designs now can carry more information, like tags, their creation or last update date, a description, and associated mods.", 1)
            .RegisterEntry(
                "Fixed Designs are now called Automated Designs and can be found in the Automation tab. This tab has a help button in the selector.",
                1)
            .RegisterEntry("Automated designs use Penumbras way of identifying characters, thus they do not apply by pure name anymore.", 2)
            .RegisterImportant(
                "Please look through them after the migration, because not all names in fixed designs could necessarily be migrated.", 2)
            .RegisterEntry(
                "Glamourer can now keep track of which items and customizations have been seen on any of your characters on this installation, so you can have a Glamour log. "
              + "This log can optionally be used to restrict your own automated designs only to things you actually have unlocked, and can otherwise be used for browsing existing items.",
                1)
            .RegisterHighlight("Notable Minor Changes")
            .RegisterEntry("Hrothgar faces should be fixed.", 1)
            .RegisterEntry("Alpha value is gone. It may be brought back later on, but generally should not be, and was not often used, so eh.",
                1)
            .RegisterEntry(
                "Glamourer now can optionally protect you from gender- or race-restricted gear not appearing when you switch, by automatically using an appropriate replacement.",
                1)
            .RegisterEntry(
                "Glamourer has some fun optional easter eggs and cheat codes, like You've got to think for yourselves! You're all individuals!",
                1)
            .RegisterEntry("You can enable game context menus so you can equip linked items via Glamourer.",         1)
            .RegisterEntry("A lot of configuration and options was learned from Penumbra and is available, like...", 1)
            .RegisterEntry("... configurable color coding for the Glamourer UI.",                                    2)
            .RegisterEntry("... sort modes for your design list.",                                                   2)
            .RegisterEntry("... an automated backup system keeping up to 10 backups of your glamourer data.",        2)
            .RegisterEntry("... this Changelog!",                                                                    2)
            .RegisterEntry("... configurable deletion modifiers for fewer misclicks!",                               2);
}
