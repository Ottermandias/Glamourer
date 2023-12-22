using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;

namespace Glamourer.Services;

public class CommandService : IDisposable
{
    private const string MainCommandString  = "/glamourer";
    private const string ApplyCommandString = "/glamour";

    private readonly ICommandManager   _commands;
    private readonly MainWindow        _mainWindow;
    private readonly IChatGui          _chat;
    private readonly ActorManager      _actors;
    private readonly ObjectManager     _objects;
    private readonly StateManager      _stateManager;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly AutoDesignManager _autoDesignManager;
    private readonly DesignManager     _designManager;
    private readonly DesignConverter   _converter;
    private readonly DesignFileSystem  _designFileSystem;
    private readonly Configuration     _config;

    public CommandService(ICommandManager commands, MainWindow mainWindow, IChatGui chat, ActorManager actors, ObjectManager objects,
        AutoDesignApplier autoDesignApplier, StateManager stateManager, DesignManager designManager, DesignConverter converter,
        DesignFileSystem designFileSystem, AutoDesignManager autoDesignManager, Configuration config)
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
                    _chat.Print(new SeStringBuilder().AddText("Use ").AddPurple("/glamour").AddText(" instead of ").AddRed("/glamourer").AddText(" for application commands.").BuiltString);
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
            "apply"             => Apply(argument),
            "reapply"           => ReapplyState(argument),
            "revert"            => Revert(argument),
            "reapplyautomation" => ReapplyAutomation(argument),
            "automation"        => SetAutomation(argument),
            "copy"              => CopyState(argument),
            "save"              => SaveState(argument),
            _                   => PrintHelp(argumentList[0]),
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
            "Reverts a given character to its supposed state using automated designs. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("copy", "Copy the current state of a character to clipboard. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("save", "Save the current state of a character to a named design. Use without arguments for help.").BuiltString);
        _chat.Print(new SeStringBuilder()
            .AddCommand("automation", "Change the state of automated design sets. Use without arguments for help.").BuiltString);
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
        AutoDesign.Type applicationFlags = 0;
        if (split2.Length == 2)
            foreach (var character in split2[1])
            {
                switch (char.ToLowerInvariant(character))
                {
                    case 'c':
                        applicationFlags |= AutoDesign.Type.Customizations;
                        break;
                    case 'e':
                        applicationFlags |= AutoDesign.Type.Armor;
                        break;
                    case 'a':
                        applicationFlags |= AutoDesign.Type.Accessories;
                        break;
                    case 'd':
                        applicationFlags |= AutoDesign.Type.GearCustomization;
                        break;
                    case 'w':
                        applicationFlags |= AutoDesign.Type.Weapons;
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

    private bool ReapplyAutomation(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour reapplyautomation ").AddGreen("[Character Identifier]").BuiltString);
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
                    _autoDesignApplier.ReapplyAutomation(actor, identifier, state);
                    _stateManager.ReapplyState(actor);
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
                _stateManager.ResetState(state, StateChanged.Source.Manual);
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
                _stateManager.ReapplyState(actor);
        }


        return true;
    }

    private bool Apply(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour apply ").AddYellow("[Design Name, Path or Identifier, or Clipboard]")
                .AddText(" | ")
                .AddGreen("[Character Identifier]").BuiltString);
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
            PlayerIdentifierHelp(false, true);
        }

        if (!GetDesign(split[0], out var design, true) || !IdentifierHandling(split[1], out var identifiers, false, true))
            return false;

        _objects.Update();
        foreach (var identifier in identifiers)
        {
            if (!_objects.TryGetValue(identifier, out var actors))
            {
                if (_stateManager.TryGetValue(identifier, out var state))
                    _stateManager.ApplyDesign(design, state, StateChanged.Source.Manual);
            }
            else
            {
                foreach (var actor in actors.Objects)
                {
                    if (_stateManager.GetOrCreate(actor.GetIdentifier(_actors), actor, out var state))
                        _stateManager.ApplyDesign(design, state, StateChanged.Source.Manual);
                }
            }
        }

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
                var text = _converter.ShareBase64(state);
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

            var design = _converter.Convert(state, EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant, CrestExtensions.All);
            _designManager.CreateClone(design, split[0], true);
            return true;
        }

        _chat.Print(new SeStringBuilder().AddText("Could not save state to design ").AddYellow(split[0], true)
            .AddText(": No identified object is available or has stored state.").BuiltString);
        return false;
    }

    private bool GetDesign(string argument, [NotNullWhen(true)] out DesignBase? design, bool allowClipboard)
    {
        design = null;
        if (argument.Length == 0)
            return false;

        if (allowClipboard && string.Equals("clipboard", argument, StringComparison.OrdinalIgnoreCase))
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

        if (Guid.TryParse(argument, out var guid))
        {
            design = _designManager.Designs.FirstOrDefault(d => d.Identifier == guid);
        }
        else
        {
            var lower = argument.ToLowerInvariant();
            design = _designManager.Designs.FirstOrDefault(d
                => d.Name.Lower == lower || lower.Length > 3 && d.Identifier.ToString().StartsWith(lower));
            if (design == null && _designFileSystem.Find(lower, out var child) && child is DesignFileSystem.Leaf leaf)
                design = leaf.Value;
        }

        if (design == null)
        {
            _chat.Print(new SeStringBuilder().AddText("The token ").AddYellow(argument, true).AddText(" did not resolve to an existing design.")
                .BuiltString);
            return false;
        }

        return true;
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
                identifiers = new[]
                {
                    identifier,
                };
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
        catch (ActorManager.IdentifierParseError e)
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
