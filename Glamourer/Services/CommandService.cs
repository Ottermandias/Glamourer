using System;
using Dalamud.Game.Command;
using Glamourer.Gui;

namespace Glamourer.Services;

public class CommandService : IDisposable
{
    private const string HelpString         = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";
    private const string MainCommandString  = "/glamourer";
    private const string ApplyCommandString = "/glamour";

    private readonly CommandManager _commands;
    private readonly Interface      _interface;

    public CommandService(CommandManager commands, Interface ui)
    {
        _commands  = commands;
        _interface = ui;

        _commands.AddHandler(MainCommandString,  new CommandInfo(OnGlamourer) { HelpMessage = "Open or close the Glamourer window." });
        _commands.AddHandler(ApplyCommandString, new CommandInfo(OnGlamour) { HelpMessage   = $"Use Glamourer Functions: {HelpString}" });
    }

    public void Dispose()
    {
        _commands.RemoveHandler(MainCommandString);
        _commands.RemoveHandler(ApplyCommandString);
    }

    private void OnGlamourer(string command, string arguments)
        => _interface.Toggle();

    private void OnGlamour(string command, string arguments)
    { }
}
