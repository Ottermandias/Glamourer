using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.GameData;
using Glamourer.Gui;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using ObjectManager = Glamourer.Interop.ObjectManager;

namespace Glamourer.Services;

public class CommandService : IDisposable, IApiService
{
    private const string RandomString       = "random";
    private const string MainCommandString  = "/glamourer";
    private const string ApplyCommandString = "/glamour";

    private readonly ICommandManager       _commands;
    private readonly MainWindow            _mainWindow;
    private readonly IChatGui              _chat;
    private readonly ActorManager          _actors;
    private readonly ObjectManager         _objects;
    private readonly StateManager          _stateManager;
    private readonly AutoDesignApplier     _autoDesignApplier;
    private readonly AutoDesignManager     _autoDesignManager;
    private readonly DesignManager         _designManager;
    private readonly DesignConverter       _converter;
    private readonly DesignFileSystem      _designFileSystem;
    private readonly Configuration         _config;
    private readonly ModSettingApplier     _modApplier;
    private readonly ItemManager           _items;
    private readonly RandomDesignGenerator _randomDesign;
    private readonly CustomizeService      _customizeService;

    public CommandService(ICommandManager commands, MainWindow mainWindow, IChatGui chat, ActorManager actors, ObjectManager objects,
        AutoDesignApplier autoDesignApplier, StateManager stateManager, DesignManager designManager, DesignConverter converter,
        DesignFileSystem designFileSystem, AutoDesignManager autoDesignManager, Configuration config, ModSettingApplier modApplier,
        ItemManager items, RandomDesignGenerator randomDesign, CustomizeService customizeService)
    {
        _commands          = commands;
        _mainWindow        = mainWindow;
        _chat              = chat;
        _actors            = actors;
        _objects           = objects;
        _autoDesignApplier = autoDesignApplier;
        _stateManager      = stateManager;
        _designManager     = designManager;
        _converter         = converter;
        _designFileSystem  = designFileSystem;
        _autoDesignManager = autoDesignManager;
        _config            = config;
        _modApplier        = modApplier;
        _items             = items;
        _randomDesign      = randomDesign;
        _customizeService  = customizeService;

        _commands.AddHandler(MainCommandString, new CommandInfo(OnGlamourer) { HelpMessage = "Open or close the Glamourer window." });
        _commands.AddHandler(ApplyCommandString,
            new CommandInfo(OnGlamour) { HelpMessage = "Use Glamourer Functions. Use with 'help' or '?' for extended help." });
    }

    public void Dispose()
    {
        _commands.RemoveHandler(MainCommandString);
        _commands.RemoveHandler(ApplyCommandString);
    }

    private void OnGlamourer(string command, string arguments)
    {
        if (arguments.Length > 0)
            switch (arguments)
            {
                case "qdb":
                case "quick":
                case "bar":
                case "designs":
                case "design":
                case "design bar":
                    _config.Ephemeral.ShowDesignQuickBar = !_config.Ephemeral.ShowDesignQuickBar;
                    _config.Ephemeral.Save();
                    return;
                case "lock":
                case "unlock":
                    _config.Ephemeral.LockMainWindow = !_config.Ephemeral.LockMainWindow;
                    _config.Ephemeral.Save();
                    return;
                default:
                    _chat.Print("Use without argument to toggle the main window.");
                    _chat.Print(new SeStringBuilder().AddText("Use ").AddPurple("/glamour").AddText(" instead of ").AddRed("/glamourer")
                        .AddText(" for application commands.").BuiltString);
                    _chat.Print(new SeStringBuilder().AddCommand("qdb",  "Toggles the quick design bar on or off.").BuiltString);
                    _chat.Print(new SeStringBuilder().AddCommand("lock", "Toggles the lock of the main window on or off.").BuiltString);
                    return;
            }

        _mainWindow.Toggle();
    }

    private void OnGlamour(string command, string arguments)
    {
        var argumentList = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (argumentList.Length < 1)
        {
            PrintHelp("?");
            return;
        }

        var argument = argumentList.Length == 2 ? argumentList[1] : string.Empty;
        var _ = argumentList[0].ToLowerInvariant() switch
        {
            "apply"              => Apply(argument),
            "reapply"            => ReapplyState(argument),
            "revert"             => Revert(argument),
            "reapplyautomation"  => ReapplyAutomation(argument, "reapplyautomation",  false),
            "reverttoautomation" => ReapplyAutomation(argument, "reverttoautomation", true),
            "automation"         => SetAutomation(argument),
            "copy"               => CopyState(argument),
            "save"               => SaveState(argument),
            "delete"             => Delete(argument),
            "applyitem"          => ApplyItem(argument),
            "applycustomization" => ApplyCustomization(argument),
            _                    => PrintHelp(argumentList[0]),
        };
    }

    private bool PrintHelp(string argument)
    {
        if (!string.Equals(argument, "help", StringComparison.OrdinalIgnoreCase) && argument != "?")
            _chat.Print(new SeStringBuilder().AddText("The given argument ").AddRed(argument, true)
                .AddText(" is not valid. Valid arguments are:").BuiltString);
        else
            _chat.Print("Valid arguments for /glamour are:");

        _chat.Print(new SeStringBuilder().AddCommand("apply", "Applies a given design to a given character. Use without arguments for help.")
            .BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("reapply", "Re-applies the current supposed state of a given character. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("revert", "Reverts a given character to its game state. Use without arguments for help.")
            .BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("reapplyautomation",
            "Reapplies the current automation state on top of the characters current state.. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder().AddCommand("reverttoautomation",
            "Reverts a given character to its supposed state using automated designs. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("copy", "Copy the current state of a character to clipboard. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("save", "Save the current state of a character to a named design. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("automation", "Change the state of automated design sets. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("applyitem", "Apply a specific item to a character. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("applycustomization", "Apply a specific customization value to a character. Use without arguments for help.")
            .BuiltString);
        return true;
    }

    private bool SetAutomation(string arguments)
    {
        var argumentList = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (argumentList.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour automation ").AddBlue("enable, disable or application", true)
                .AddText(" ")
                .AddRed("Automated Design Set Index or Name", true).AddText(" | ").AddYellow("<Design Index>").AddText(" ")
                .AddPurple("<Application Flags>")
                .BuiltString);
            _chat.Print(
                "    》 If the design set name is a valid natural number it will be used as a index. Design names that are such numbers can not be dealt with.");
            _chat.Print("    》 If multiple design sets have the same name, the first one will be changed.");
            _chat.Print("    》 The name is case-insensitive.");
            _chat.Print(new SeStringBuilder().AddText("    》 If the command is ").AddBlue("application")
                .AddText(" the ").AddYellow("design index").AddText(" and ").AddPurple("flags").AddText(" are required.").BuiltString);
            _chat.Print(new SeStringBuilder().AddText("    》 The ").AddYellow("design index")
                .AddText(" is the number in front of the relevant design in the automated design set.").BuiltString);
            _chat.Print(new SeStringBuilder().AddText("    》 The ").AddPurple("Application Flags").AddText(" are a combination of the letters ")
                .AddInitialPurple("Customizations, ")
                .AddInitialPurple("Equipment, ")
                .AddInitialPurple("Accessories, ")
                .AddInitialPurple("Dyes & Crests and ")
                .AddInitialPurple("Weapons, where ").AddPurple("CEADW")
                .AddText(" means everything should be toggled on, and no value means nothing should be toggled on.")
                .BuiltString);
            return false;
        }

        bool? state = null;
        switch (argumentList[0].ToLowerInvariant())
        {
            case "enabled":
            case "enable":
            case "on":
            case "true":
                state = true;
                break;
            case "disabled":
            case "disable":
            case "off":
            case "false":
                state = false;
                break;
            case "toggle":
            case "switch":
                break;
            case "application": return HandleApplication(argumentList[1]);
            default:
                _chat.Print(new SeStringBuilder().AddText("The command ")
                    .AddBlue(argumentList[0], true).AddText(" is unknown. Currently only ").AddBlue("enable").AddText(", ").AddBlue("disable")
                    .AddText(" or ").AddBlue("application")
                    .AddText(" are supported.").BuiltString);
                return false;
        }

        if (!GetAutoDesignSetIndex(argumentList[1], out var designIdx))
            return false;

        _autoDesignManager.SetState(designIdx, state ?? !_autoDesignManager[designIdx].Enabled);
        return true;
    }

    private bool GetAutoDesignSetIndex(string name, out int idx)
    {
        var lowerName = name.ToLowerInvariant();

        idx = int.TryParse(lowerName, out var designIdx) && designIdx > 0 && designIdx <= _autoDesignManager.Count
            ? designIdx - 1
            : _autoDesignManager.IndexOf(d => d.Name.ToLowerInvariant() == lowerName);
        if (idx >= 0)
            return true;

        _chat.Print(new SeStringBuilder().AddText("Could not change state of automated design set ")
            .AddRed(name, true).AddText(" No automated design set of that name or index exists.").BuiltString);
        return false;
    }

    private bool HandleApplication(string argument)
    {
        var split = argument.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("The command ").AddBlue("automation")
                .AddText(" requires a design index and application flags.").BuiltString);
            return false;
        }

        var setName = split[0];
        if (!GetAutoDesignSetIndex(setName, out var setIdx))
            return false;

        var set = _autoDesignManager[setIdx];

        var split2 = split[1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!int.TryParse(split2[0], out var designIdx) || designIdx <= 0)
        {
            _chat.Print(new SeStringBuilder().AddText("The value ").AddYellow(split2[0], true)
                .AddText(" is not a valid design index.").BuiltString);
            return false;
        }

        if (designIdx > set.Designs.Count)
        {
            _chat.Print(new SeStringBuilder().AddText($"The set {setIdx} does not have {designIdx} designs.").BuiltString);
            return false;
        }

        --designIdx;
        ApplicationType applicationFlags = 0;
        if (split2.Length == 2)
            foreach (var character in split2[1])
            {
                switch (char.ToLowerInvariant(character))
                {
                    case 'c':
                        applicationFlags |= ApplicationType.Customizations;
                        break;
                    case 'e':
                        applicationFlags |= ApplicationType.Armor;
                        break;
                    case 'a':
                        applicationFlags |= ApplicationType.Accessories;
                        break;
                    case 'd':
                        applicationFlags |= ApplicationType.GearCustomization;
                        break;
                    case 'w':
                        applicationFlags |= ApplicationType.Weapons;
                        break;
                    default:
                        _chat.Print(new SeStringBuilder().AddText("The value ").AddPurple(split2[1], true)
                            .AddText(" is not a valid set of application flags.").BuiltString);
                        return false;
                }
            }

        _autoDesignManager.ChangeApplicationType(set, designIdx, applicationFlags);
        return true;
    }

    private bool ReapplyAutomation(string argument, string command, bool revert)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText($"Use with /glamour {command} ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var data))
                return true;

            foreach (var actor in data.Objects)
            {
                if (_stateManager.GetOrCreate(identifier, actor, out var state))
                {
                    _autoDesignApplier.ReapplyAutomation(actor, identifier, state, revert, out var forcedRedraw);
                    _stateManager.ReapplyState(actor, forcedRedraw, StateSource.Manual);
                }
            }
        }

        return true;
    }

    private bool Revert(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour revert ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifiers, false, true))
            return false;

        foreach (var identifier in identifiers)
        {
            if (_stateManager.TryGetValue(identifier, out var state))
                _stateManager.ResetState(state, StateSource.Manual);
        }


        return true;
    }

    private bool ReapplyState(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour revert ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var data))
                return true;

            foreach (var actor in data.Objects)
                _stateManager.ReapplyState(actor, false, StateSource.Manual);
        }


        return true;
    }

    private bool ApplyItem(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length is not 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour applyitem ").AddYellow("[Item ID or Item Name]")
                .AddText(" | ")
                .AddGreen("[Character Identifier]")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 The item name is case-insensitive. Numeric IDs are preferred before item names.")
                .BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }

        var items = new EquipItem[3];
        if (uint.TryParse(split[0], out var id))
        {
            if (_items.ItemData.Primary.TryGetValue(id, out var main))
                items[0] = main;
        }
        else if (_items.ItemData.Primary.FindFirst(pair => string.Equals(pair.Value.Name, split[0], StringComparison.OrdinalIgnoreCase),
                     out var i))
        {
            items[0] = i.Value;
        }

        if (!items[0].Valid)
        {
            _chat.Print(new SeStringBuilder().AddText("The item ").AddYellow(split[0], true)
                .AddText(" could not be identified as a valid item.").BuiltString);
            return false;
        }

        if (_items.ItemData.Secondary.TryGetValue(items[0].ItemId, out var off))
        {
            items[1] = off;
            if (_items.ItemData.Tertiary.TryGetValue(items[0].ItemId, out var gauntlet))
                items[2] = gauntlet;
        }

        if (!IdentifierHandling(split[1], out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var actors))
            {
                if (!_stateManager.TryGetValue(identifier, out var state))
                    continue;

                foreach (var item in items.Where(i => i.Valid))
                    _stateManager.ChangeItem(state, item.Type.ToSlot(), item, ApplySettings.Manual);
            }
            else
            {
                foreach (var actor in actors.Objects)
                {
                    if (!_stateManager.GetOrCreate(actor.GetIdentifier(_actors), actor, out var state))
                        continue;

                    foreach (var item in items.Where(i => i.Valid))
                        _stateManager.ChangeItem(state, item.Type.ToSlot(), item, ApplySettings.Manual);
                }
            }
        }

        return true;
    }

    private bool ApplyCustomization(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length is not 2)
            return PrintCustomizationHelp();

        var customizationSplit = split[0].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (customizationSplit.Length < 2)
            return PrintCustomizationHelp();

        if (!Enum.TryParse(customizationSplit[0], true, out CustomizeIndex customizeIndex)
         || !CustomizationExtensions.AllBasic.Contains(customizeIndex))
        {
            if (!int.TryParse(customizationSplit[0], out var customizeInt)
             || customizeInt < 0
             || customizeInt >= CustomizationExtensions.AllBasic.Length)
            {
                _chat.Print(new SeStringBuilder().AddText("The customization type ").AddYellow(customizationSplit[0], true)
                    .AddText(" could not be identified as a valid type.").BuiltString);
                return false;
            }

            customizeIndex = CustomizationExtensions.AllBasic[customizeInt];
        }

        var valueString = customizationSplit[1].ToLowerInvariant();
        var (wrapAround, offset) = valueString switch
        {
            "next"     => (true, (sbyte)1),
            "previous" => (true, (sbyte)-1),
            "plus"     => (false, (sbyte)1),
            "minus"    => (false, (sbyte)-1),
            _          => (false, (sbyte)0),
        };
        byte? baseValue = null;
        if (offset == 0)
        {
            if (byte.TryParse(valueString, out var b))
            {
                baseValue = b;
            }
            else
            {
                _chat.Print(new SeStringBuilder().AddText("The customization value ").AddPurple(valueString, true)
                    .AddText(" could not be parsed.")
                    .BuiltString);
                return false;
            }
        }

        if (customizationSplit.Length < 3 || !byte.TryParse(customizationSplit[2], out var multiplier))
            multiplier = 1;

        if (!IdentifierHandling(split[1], out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var actors))
            {
                if (_stateManager.TryGetValue(identifier, out var state))
                    ApplyToState(state);
            }
            else
            {
                foreach (var actor in actors.Objects)
                {
                    if (_stateManager.GetOrCreate(actor.GetIdentifier(_actors), actor, out var state))
                        ApplyToState(state);
                }
            }
        }

        return true;

        void ApplyToState(ActorState state)
        {
            var customize = state.ModelData.Customize;
            if (!state.ModelData.IsHuman)
                return;

            var set = _customizeService.Manager.GetSet(customize.Clan, customize.Gender);
            if (!set.IsAvailable(customizeIndex))
                return;

            if (baseValue != null)
            {
                var v = baseValue.Value;
                if (set.Type(customizeIndex) is MenuType.ListSelector)
                    --v;
                set.DataByValue(customizeIndex, new CustomizeValue(v), out var data, customize.Face);
                if (data != null)
                    _stateManager.ChangeCustomize(state, customizeIndex, data.Value.Value, ApplySettings.Manual);
            }
            else
            {
                var idx   = set.DataByValue(customizeIndex, customize[customizeIndex], out var data, customize.Face);
                var count = set.Count(customizeIndex, customize.Face);
                var m     = multiplier % count;
                var newIdx = offset is 1
                    ? idx >= count - m
                        ? wrapAround
                            ? m + idx - count
                            : count - 1
                        : idx + m
                    : idx < m
                        ? wrapAround
                            ? count - m + idx
                            : 0
                        : idx - m;
                data = set.Data(customizeIndex, newIdx, customize.Face);
                _stateManager.ChangeCustomize(state, customizeIndex, data.Value.Value, ApplySettings.Manual);
            }
        }

        bool PrintCustomizationHelp()
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour applycustomization ").AddYellow("[Customization Type]")
                .AddPurple(" [Value, Next, Previous, Minus, or Plus] ")
                .AddBlue("<Amount>")
                .AddText(" | ")
                .AddGreen("[Character Identifier]")
                .BuiltString);
            _chat.Print(new SeStringBuilder().AddText("    》 Valid ").AddPurple("values")
                .AddText(" depend on the the character's gender, clan, and the customization type.").BuiltString);
            _chat.Print(new SeStringBuilder().AddText("    》 ").AddPurple("Plus").AddText(" and ").AddPurple("Minus")
                .AddText(" are the same as pressing the + and - buttons in the UI, times the optional ").AddBlue(" amount").AddText(".")
                .BuiltString);
            _chat.Print(new SeStringBuilder().AddText("    》 ").AddPurple("Next").AddText(" and ").AddPurple("Previous")
                .AddText(" is similar to Plus and Minus, but with wrap-around on reaching the end.").BuiltString);
            var builder = new SeStringBuilder().AddText("    》 Available ").AddYellow("Customization Types")
                .AddText(" are either a number in ")
                .AddYellow($"[0, {CustomizationExtensions.AllBasic.Length}]")
                .AddText(" or one of ");
            foreach (var index in CustomizationExtensions.AllBasic.SkipLast(1))
                builder.AddYellow(index.ToString()).AddText(", ");
            _chat.Print(builder.AddYellow(CustomizationExtensions.AllBasic[^1].ToString()).AddText(".").BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 The item name is case-insensitive. Numeric IDs are preferred before item names.")
                .BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }
    }

    private bool Apply(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length is not 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour apply ")
                .AddYellow("[Design Name, Path or Identifier, Random, or Clipboard]")
                .AddText(" | ")
                .AddGreen("[Character Identifier]")
                .AddText("; ")
                .AddBlue("<Apply Mods>")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 The design name is case-insensitive. If multiple designs of that name up to case exist, the first one is chosen.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 If using the design identifier, you need to specify at least 4 characters for it, and the first one starting with the provided characters is chosen.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 The design path is the folder path in the selector, with '/' as separators. It is also case-insensitive.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 Clipboard as a single word will try to apply a design string currently in your clipboard.").BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 ").AddYellow("Random")
                .AddText(
                    " supports many restrictions, see the Restriction Builder when adding a Random design to Automations for valid strings.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 ").AddBlue("<Enable Mods>").AddText(" is optional and can be omitted (together with the ;), ").AddBlue("true")
                .AddText(" or ").AddBlue("false").AddText(".").BuiltString);
            _chat.Print(new SeStringBuilder().AddText("If ").AddBlue("true")
                .AddText(", it will try to apply mod associations to the collection assigned to the identified character.").BuiltString);
            PlayerIdentifierHelp(false, true);
            return true;
        }

        var split2 = split[1].Split(';', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var applyMods = split2.Length == 2
         && split2[1].ToLowerInvariant() switch
            {
                "true" => true,
                "1"    => true,
                "t"    => true,
                "yes"  => true,
                "y"    => true,
                _      => false,
            };
        if (!GetDesign(split[0], out var design, true) || !IdentifierHandling(split2[0], out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var actors))
            {
                if (_stateManager.TryGetValue(identifier, out var state))
                    _stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks);
            }
            else
            {
                foreach (var actor in actors.Objects)
                {
                    if (_stateManager.GetOrCreate(actor.GetIdentifier(_actors), actor, out var state))
                    {
                        ApplyModSettings(design, actor, applyMods);
                        _stateManager.ApplyDesign(state, design, ApplySettings.ManualWithLinks);
                    }
                }
            }
        }

        return true;
    }

    private void ApplyModSettings(DesignBase design, Actor actor, bool applyMods)
    {
        if (!applyMods || design is not Design d)
            return;

        var (messages, appliedMods, collection, name, overridden) = _modApplier.ApplyModSettings(d.AssociatedMods, actor);

        foreach (var message in messages)
            Glamourer.Messager.Chat.Print($"Error applying mod settings: {message}");

        if (appliedMods > 0)
            Glamourer.Messager.Chat.Print(
                $"Applied {appliedMods} mod settings to {name}{(overridden ? " (overridden by settings)" : string.Empty)}.");
    }

    private bool Delete(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour delete ").AddYellow("[Design Name, Path or Identifier]").BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 The design name is case-insensitive. If multiple designs of that name up to case exist, the first one is chosen.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 If using the design identifier, you need to specify at least 4 characters for it, and the first one starting with the provided characters is chosen.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 The design path is the folder path in the selector, with '/' as separators. It is also case-insensitive.")
                .BuiltString);
            return false;
        }

        if (!GetDesign(argument, out var designBase, false) || designBase is not Design d)
            return false;

        _designManager.Delete(d);

        return true;
    }

    private bool CopyState(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour copy ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false, true);
        }

        if (!IdentifierHandling(argument, out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_stateManager.TryGetValue(identifier, out var state)
             && !(_objects.TryGetValue(identifier, out var data)
                 && data.Valid
                 && _stateManager.GetOrCreate(identifier, data.Objects[0], out state)))
                continue;

            try
            {
                var text = _converter.ShareBase64(state, ApplicationRules.AllButParameters(state));
                ImGui.SetClipboardText(text);
                return true;
            }
            catch
            {
                _chat.Print("Could not copy state to clipboard: Failure to write to clipboard.");
                return false;
            }
        }

        _chat.Print(new SeStringBuilder().AddText("Could not copy state to clipboard: No identified object is available or has stored state.")
            .BuiltString);

        return false;
    }

    private bool SaveState(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour save ").AddYellow("[New Design Name]").AddText(" | ")
                .AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false, true);
        }

        if (!IdentifierHandling(split[1], out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_stateManager.TryGetValue(identifier, out var state)
             && !(_objects.TryGetValue(identifier, out var data)
                 && data.Valid
                 && _stateManager.GetOrCreate(identifier, data.Objects[0], out state)))
                continue;

            var design = _converter.Convert(state, ApplicationRules.FromModifiers(state));
            _designManager.CreateClone(design, split[0], true);
            return true;
        }

        _chat.Print(new SeStringBuilder().AddText("Could not save state to design ").AddYellow(split[0], true)
            .AddText(": No identified object is available or has stored state.").BuiltString);
        return false;
    }

    private bool GetDesign(string argument, [NotNullWhen(true)] out DesignBase? design, bool allowSpecial)
    {
        design = null;
        if (argument.Length == 0)
            return false;

        if (allowSpecial)
        {
            if (string.Equals("clipboard", argument, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var clipboardText = ImGui.GetClipboardText();
                    if (clipboardText.Length > 0)
                        design = _converter.FromBase64(clipboardText, true, true, out _);
                }
                catch
                {
                    // ignored
                }

                if (design != null)
                    return true;

                _chat.Print(new SeStringBuilder().AddText("Your current clipboard did not contain a valid design string.").BuiltString);
                return false;
            }

            if (argument.StartsWith(RandomString, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (argument.Length == RandomString.Length)
                        design = _randomDesign.Design();
                    else if (argument[RandomString.Length] == ':')
                        design = _randomDesign.Design(argument[(RandomString.Length + 1)..]);
                    if (design == null)
                    {
                        _chat.Print(new SeStringBuilder().AddText("No design matched your restrictions.").BuiltString);
                        return false;
                    }

                    _chat.Print($"Chose random design {((Design)design).Name}.");
                }
                catch (Exception ex)
                {
                    _chat.Print(new SeStringBuilder().AddText($"Error in the restriction string: {ex.Message}").BuiltString);
                    return false;
                }

                return true;
            }
        }

        if (Guid.TryParse(argument, out var guid))
        {
            design = _designManager.Designs.ByIdentifier(guid);
        }
        else
        {
            var lower = argument.ToLowerInvariant();
            design = _designManager.Designs.FirstOrDefault(d
                => d.Name.Lower == lower || lower.Length > 3 && d.Identifier.ToString().StartsWith(lower));
            if (design == null && _designFileSystem.Find(lower, out var child) && child is DesignFileSystem.Leaf leaf)
                design = leaf.Value;
        }

        if (design != null)
            return true;

        _chat.Print(new SeStringBuilder().AddText("The token ").AddYellow(argument, true).AddText(" did not resolve to an existing design.")
            .BuiltString);
        return false;
    }

    private unsafe bool IdentifierHandling(string argument, out ActorIdentifier[] identifiers, bool allowAnyWorld, bool allowIndex)
    {
        try
        {
            if (_objects.GetName(argument.ToLowerInvariant(), out var obj))
            {
                var identifier = _actors.FromObject(obj.AsObject, out _, true, true, true);
                if (!identifier.IsValid)
                {
                    _chat.Print(new SeStringBuilder().AddText("The placeholder ").AddGreen(argument)
                        .AddText(" did not resolve to a game object with a valid identifier.").BuiltString);
                    identifiers = Array.Empty<ActorIdentifier>();
                    return false;
                }

                if (allowIndex && identifier.Type is IdentifierType.Npc)
                    identifier = _actors.CreateNpc(identifier.Kind, identifier.DataId, obj.Index);
                identifiers =
                [
                    identifier,
                ];
            }
            else
            {
                identifiers = _actors.FromUserString(argument, allowIndex);
                if (!allowAnyWorld
                 && identifiers[0].Type is IdentifierType.Player or IdentifierType.Owned
                 && identifiers[0].HomeWorld == ushort.MaxValue)
                {
                    _chat.Print(new SeStringBuilder().AddText("The argument ").AddRed(argument, true)
                        .AddText(" did not specify a world.").BuiltString);
                    return false;
                }
            }

            return true;
        }
        catch (ActorIdentifierFactory.IdentifierParseError e)
        {
            _chat.Print(new SeStringBuilder().AddText("The argument ").AddRed(argument, true)
                .AddText($" could not be converted to an identifier. {e.Message}")
                .BuiltString);
            identifiers = Array.Empty<ActorIdentifier>();
            return false;
        }
    }

    private void PlayerIdentifierHelp(bool allowAnyWorld, bool allowIndex)
    {
        var npcGuide = new SeStringBuilder().AddText("    》》》").AddGreen("n").AddText(" | ").AddPurple("[NPC Type]").AddText(" : ")
            .AddRed("[NPC Name]").AddBlue(allowIndex ? "@<Object Index>" : string.Empty).AddText(", where NPC Type can be ")
            .AddInitialPurple("Mount")
            .AddInitialPurple("Companion")
            .AddInitialPurple("Accessory").AddInitialPurple("Event NPC").AddText("or ").AddInitialPurple("Battle NPC", false);
        if (allowIndex)
            npcGuide = npcGuide.AddText(", and the ").AddBlue("index").AddText(" is an optional non-negative number in the object table.");
        else
            npcGuide = npcGuide.AddText(".");

        _chat.Print(new SeStringBuilder().AddText("    》 Valid Character Identifiers have the form:").BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("<me>").AddText(" or ").AddGreen("<t>").AddText(" or ").AddGreen("<mo>")
            .AddText(" or ").AddGreen("<f>")
            .AddText(" as placeholders for your character, your target, your mouseover or your focus, if they exist.").BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("p").AddText(" | ").AddWhite("[Player Name]@[World Name]")
            .AddText(allowAnyWorld ? ", if no @ is provided, Any World is used." : ".")
            .BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("r").AddText(" | ").AddWhite("[Retainer Name]").AddText(".").BuiltString);
        _chat.Print(npcGuide.BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》 ").AddGreen("o").AddText(" | ").AddPurple("[NPC Type]")
            .AddText(" : ")
            .AddRed("[NPC Name]").AddText(" | ").AddWhite("[Player Name]@<World Name>").AddText(".").BuiltString);
    }
}
