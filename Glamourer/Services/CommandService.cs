using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui.Classes;
using Penumbra.GameData.Actors;

namespace Glamourer.Services;

public class CommandService : IDisposable
{
    private const string MainCommandString  = "/glamourer";
    private const string ApplyCommandString = "/glamour";

    private readonly CommandManager    _commands;
    private readonly MainWindow        _mainWindow;
    private readonly ChatGui           _chat;
    private readonly ActorService      _actors;
    private readonly ObjectManager     _objects;
    private readonly StateManager      _stateManager;
    private readonly AutoDesignApplier _autoDesignApplier;
    private readonly DesignManager     _designManager;
    private readonly DesignConverter   _converter;

    public CommandService(CommandManager commands, MainWindow mainWindow, ChatGui chat, ActorService actors, ObjectManager objects,
        AutoDesignApplier autoDesignApplier, StateManager stateManager, DesignManager designManager, DesignConverter converter)
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
        => _mainWindow.Toggle();

    private void OnGlamour(string command, string arguments)
    {
        var argumentList = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (argumentList.Length < 1)
            return;

        var argument = argumentList.Length == 2 ? argumentList[1] : string.Empty;
        var _ = argumentList[0].ToLowerInvariant() switch
        {
            "apply"             => Apply(argument),
            "reapply"           => ReapplyState(argument),
            "revert"            => Revert(argument),
            "reapplyautomation" => ReapplyAutomation(argument),
            //"automation"        => SetAutomation(argument),
            "copy" => CopyState(argument),
            "save" => SaveState(argument),
            _      => PrintHelp(argumentList[0]),
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
        //_chat.Print(new SeStringBuilder()
        //    .AddCommand("automation", "Change the configuration for automated designs. Use without arguments for help.").BuiltString);
        return true;
    }

    // TODO: implement automation changes via chat.
    private bool SetAutomation(string arguments)
        => true;

    private bool ReapplyAutomation(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour reapplyautomation ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifier, false))
            return false;

        _objects.Update();
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

        return true;
    }

    private bool Revert(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour revert ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifier, false))
            return false;

        if (_stateManager.TryGetValue(identifier, out var state))
            _stateManager.ResetState(state);

        return true;
    }

    private bool ReapplyState(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour revert ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false);
            return true;
        }

        if (!IdentifierHandling(argument, out var identifier, false))
            return false;

        _objects.Update();
        if (!_objects.TryGetValue(identifier, out var data))
            return true;

        foreach (var actor in data.Objects)
            _stateManager.ReapplyState(actor);

        return true;
    }

    private bool Apply(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour apply ").AddYellow("[Design Name or Identifier]").AddText(" | ")
                .AddGreen("[Character Identifier]").BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText("    》 The design name must match up to case. If multiple designs of that name exist, the first one is chosen.")
                .BuiltString);
            _chat.Print(new SeStringBuilder()
                .AddText(
                    "    》 If using the design identifier, you need to specify at least 4 characters for it, and the first one starting with the provided characters is chosen.")
                .BuiltString);
            PlayerIdentifierHelp(false);
        }

        if (!GetDesign(split[0], out var design) || !IdentifierHandling(split[1], out var identifier, false))
            return false;

        _objects.Update();
        if (!_objects.TryGetValue(identifier, out var actors))
        {
            if (_stateManager.TryGetValue(identifier, out var state))
                _stateManager.ApplyDesign(design, state, StateChanged.Source.Manual);
        }
        else
        {
            foreach (var actor in actors.Objects)
            {
                if (_stateManager.GetOrCreate(identifier, actor, out var state))
                    _stateManager.ApplyDesign(design, state, StateChanged.Source.Manual);
            }
        }

        return true;
    }

    private bool CopyState(string argument)
    {
        if (argument.Length == 0)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour copy ").AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false);
        }

        if (!IdentifierHandling(argument, out var identifier, false))
            return false;

        string text;
        if (_stateManager.TryGetValue(identifier, out var state))
        {
            text = _converter.ShareBase64(state);
        }
        else
        {
            if (!_objects.TryGetValue(identifier, out var data)
             || !data.Valid
             || !_stateManager.GetOrCreate(identifier, data.Objects[0], out state))
            {
                _chat.Print(new SeStringBuilder().AddText("Could not copy state to clipboard: The identified object ")
                    .AddGreen(identifier.ToString(), true).AddText(" is not available and has no stored state.").BuiltString);
                return false;
            }

            text = _converter.ShareBase64(state);
        }

        try
        {
            ImGui.SetClipboardText(text);
        }
        catch
        {
            _chat.Print("Could not copy state to clipboard: Failure to write to clipboard.");
            return false;
        }

        return true;
    }

    private bool SaveState(string arguments)
    {
        var split = arguments.Split('|', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2)
        {
            _chat.Print(new SeStringBuilder().AddText("Use with /glamour save ").AddYellow("[New Design Name]").AddText(" | ")
                .AddGreen("[Character Identifier]").BuiltString);
            PlayerIdentifierHelp(false);
        }

        if (!IdentifierHandling(split[1], out var identifier, false))
            return false;

        if (!_stateManager.TryGetValue(identifier, out var state))
        {
            _objects.Update();
            if (!_objects.TryGetValue(identifier, out var data)
             || !data.Valid
             || !_stateManager.GetOrCreate(identifier, data.Objects[0], out state))
            {
                _chat.Print(new SeStringBuilder().AddText("Could not save state to design ").AddYellow(split[0], true)
                    .AddText(": The identified object ")
                    .AddGreen(identifier.ToString(), true).AddText(" is not available and has no stored state.").BuiltString);
                return false;
            }
        }

        var design = _converter.Convert(state, EquipFlagExtensions.All, CustomizeFlagExtensions.AllRelevant);
        _designManager.CreateClone(design, split[0]);
        return true;
    }

    private bool GetDesign(string argument, [NotNullWhen(true)] out Design? design)
    {
        design = null;
        if (argument.Length == 0)
            return false;

        if (Guid.TryParse(argument, out var guid))
        {
            design = _designManager.Designs.FirstOrDefault(d => d.Identifier == guid);
        }
        else
        {
            var lower = argument.ToLowerInvariant();
            design = _designManager.Designs.FirstOrDefault(d
                => d.Name.Lower == lower || lower.Length > 3 && d.Identifier.ToString().StartsWith(lower));
        }

        if (design == null)
        {
            _chat.Print(new SeStringBuilder().AddText("The token ").AddYellow(argument, true).AddText(" did not resolve to an existing design.")
                .BuiltString);
            return false;
        }

        return true;
    }

    private unsafe bool IdentifierHandling(string argument, out ActorIdentifier identifier, bool allowAnyWorld)
    {
        try
        {
            if (_objects.GetName(argument.ToLowerInvariant(), out var obj))
            {
                identifier = _actors.AwaitedService.FromObject(obj.AsObject, out _, true, true, true);
                if (!identifier.IsValid)
                {
                    _chat.Print(new SeStringBuilder().AddText("The placeholder ").AddGreen(argument)
                        .AddText(" did not resolve to a game object with a valid identifier.").BuiltString);
                    return false;
                }
            }
            else
            {
                identifier = _actors.AwaitedService.FromUserString(argument);
                if (!allowAnyWorld
                 && identifier.Type is IdentifierType.Player or IdentifierType.Owned
                 && identifier.HomeWorld == ushort.MaxValue)
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
            identifier = ActorIdentifier.Invalid;
            return false;
        }
    }

    private void PlayerIdentifierHelp(bool allowAnyWorld)
    {
        _chat.Print(new SeStringBuilder().AddText("    》 Valid Character Identifiers have the form:").BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("<me>").AddText(" or ").AddGreen("<t>").AddText(" or ").AddGreen("<mo>")
            .AddText(" or ").AddGreen("<f>")
            .AddText(" as placeholders for your character, your target, your mouseover or your focus, if they exist.").BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("p").AddText(" | ").AddWhite("[Player Name]@[World Name]")
            .AddText(allowAnyWorld ? ", if no @ is provided, Any World is used." : ".")
            .BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("r").AddText(" | ").AddWhite("[Retainer Name]").AddText(".").BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》").AddGreen("n").AddText(" | ").AddPurple("[NPC Type]").AddText(" : ")
            .AddRed("[NPC Name]").AddText(", where NPC Type can be ").AddInitialPurple("Mount").AddInitialPurple("Companion")
            .AddInitialPurple("Accessory").AddInitialPurple("Event NPC").AddText("or ").AddInitialPurple("Battle NPC", false).AddText(".")
            .BuiltString);
        _chat.Print(new SeStringBuilder().AddText("    》》》 ").AddGreen("o").AddText(" | ").AddPurple("[NPC Type]")
            .AddText(" : ")
            .AddRed("[NPC Name]").AddText(" | ").AddWhite("[Player Name]@<World Name>").AddText(".").BuiltString);
    }
}
