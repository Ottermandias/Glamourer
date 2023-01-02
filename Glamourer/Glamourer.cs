using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.State;
using OtterGui.Log;
using Penumbra.GameData;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;

namespace Glamourer;

public class Glamourer : IDalamudPlugin
{
    private const string HelpString         = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";
    private const string MainCommandString  = "/glamourer";
    private const string ApplyCommandString = "/glamour";

    public string Name
        => "Glamourer";

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public static readonly string CommitHash =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";


    public static GlamourerConfig Config = null!;
    public static Logger          Log    = null!;

    public static   IObjectIdentifier     Identifier     = null!;
    public static   ActorManager          Actors         = null!;
    public static   PenumbraAttach        Penumbra       = null!;
    public static   ICustomizationManager Customization  = null!;
    public static   RestrictedGear        RestrictedGear = null!;
    public static   RedrawManager         RedrawManager  = null!;
    public readonly FixedDesigns          FixedDesigns;
    public readonly CurrentManipulations  CurrentManipulations;

    private readonly WindowSystem _windowSystem = new("Glamourer");
    private readonly Interface    _interface;

    //public readonly  DesignManager         Designs;

    //public static   RevertableDesigns RevertableDesigns = new();
    //public readonly GlamourerIpc      GlamourerIpc;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        try
        {
            Dalamud.Initialize(pluginInterface);
            Log = new Logger();

            Customization  = CustomizationManager.Create(Dalamud.PluginInterface, Dalamud.GameData);

            Config = GlamourerConfig.Load();

            Identifier           = global::Penumbra.GameData.GameData.GetIdentifier(Dalamud.PluginInterface, Dalamud.GameData);
            Penumbra             = new PenumbraAttach(Config.AttachToPenumbra);
            Actors = new ActorManager(Dalamud.PluginInterface, Dalamud.Objects, Dalamud.ClientState, Dalamud.GameData, Dalamud.GameGui,
                i => (short)Penumbra.CutsceneParent(i));
            FixedDesigns         = new FixedDesigns();
            CurrentManipulations = new CurrentManipulations();
            //Designs            = new DesignManager();

            //GlamourerIpc       = new GlamourerIpc(Dalamud.ClientState, Dalamud.Objects, Dalamud.PluginInterface);
            RedrawManager = new RedrawManager(FixedDesigns, CurrentManipulations);

            Dalamud.Commands.AddHandler(MainCommandString, new CommandInfo(OnGlamourer)
            {
                HelpMessage = "Open or close the Glamourer window.",
            });
            Dalamud.Commands.AddHandler(ApplyCommandString, new CommandInfo(OnGlamour)
            {
                HelpMessage = $"Use Glamourer Functions: {HelpString}",
            });

            _interface = new Interface(this);
            _windowSystem.AddWindow(_interface);
            Dalamud.PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
            //FixedDesignManager.Flag((Human*)((Actor)Dalamud.ClientState.LocalPlayer?.Address).Pointer->GameObject.DrawObject, 0, &x);
        }
        catch
        {
            Dispose();
            throw;
        }
    }


    public void Dispose()
    {

        Dalamud.PluginInterface.RelinquishData("test1");
        RedrawManager?.Dispose();
        Penumbra?.Dispose();
        if (_windowSystem != null)
            Dalamud.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _interface?.Dispose();
        //GlamourerIpc.Dispose();
        Dalamud.Commands.RemoveHandler(ApplyCommandString);
        Dalamud.Commands.RemoveHandler(MainCommandString);
    }

    public void OnGlamourer(string command, string arguments)
        => _interface.Toggle();

    //private static GameObject? GetPlayer(string name)
    //{
    //    var lowerName = name.ToLowerInvariant();
    //    return lowerName switch
    //    {
    //        ""          => null,
    //        "<me>"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
    //        "self"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
    //        "<t>"       => Dalamud.Targets.Target,
    //        "target"    => Dalamud.Targets.Target,
    //        "<f>"       => Dalamud.Targets.FocusTarget,
    //        "focus"     => Dalamud.Targets.FocusTarget,
    //        "<mo>"      => Dalamud.Targets.MouseOverTarget,
    //        "mouseover" => Dalamud.Targets.MouseOverTarget,
    //        _ => Dalamud.Objects.LastOrDefault(
    //            a => string.Equals(a.Name.ToString(), lowerName, StringComparison.InvariantCultureIgnoreCase)),
    //    };
    //}
    //
    //public void CopyToClipboard(Character player)
    //{
    //    var save = new CharacterSave();
    //    save.LoadCharacter(player);
    //    ImGui.SetClipboardText(save.ToBase64());
    //}
    //
    //public void ApplyCommand(Character player, string target)
    //{
    //    CharacterSave? save = null;
    //    if (target.ToLowerInvariant() == "clipboard")
    //        try
    //        {
    //            save = CharacterSave.FromString(ImGui.GetClipboardText());
    //        }
    //        catch (Exception)
    //        {
    //            Dalamud.Chat.PrintError("Clipboard does not contain a valid customization string.");
    //        }
    //    else if (!Designs.FileSystem.Find(target, out var child) || child is not Design d)
    //        Dalamud.Chat.PrintError("The given path to a saved design does not exist or does not point to a design.");
    //    else
    //        save = d.Data;
    //
    //    save?.Apply(player);
    //    Penumbra.UpdateCharacters(player);
    //}
    //
    //public void SaveCommand(Character player, string path)
    //{
    //    var save = new CharacterSave();
    //    save.LoadCharacter(player);
    //    try
    //    {
    //        var (folder, name) = Designs.FileSystem.CreateAllFolders(path);
    //        var design = new Design(folder, name) { Data = save };
    //        folder.FindOrAddChild(design);
    //        Designs.Designs.Add(design.FullName(), design.Data);
    //        Designs.SaveToFile();
    //    }
    //    catch (Exception e)
    //    {
    //        Dalamud.Chat.PrintError("Could not save file:");
    //        Dalamud.Chat.PrintError($"    {e.Message}");
    //    }
    //}
    //
    public void OnGlamour(string command, string arguments)
    {
        //static void PrintHelp()
        //{
        //    Dalamud.Chat.Print("Usage:");
        //    Dalamud.Chat.Print($"    {HelpString}");
        //}

        //arguments = arguments.Trim();
        //if (!arguments.Any())
        //{
        //    PrintHelp();
        //    return;
        //}
        //
        //var split = arguments.Split(new[]
        //{
        //    ',',
        //}, 3, StringSplitOptions.RemoveEmptyEntries);
        //
        //if (split.Length < 2)
        //{
        //    PrintHelp();
        //    return;
        //}
        //
        //var player = GetPlayer(split[1]) as Character;
        //if (player == null)
        //{
        //    Dalamud.Chat.Print($"Could not find object for {split[1]} or it was not a Character.");
        //    return;
        //}
        //
        //switch (split[0].ToLowerInvariant())
        //{
        //    case "copy":
        //        CopyToClipboard(player);
        //        return;
        //    case "apply":
        //    {
        //        if (split.Length < 3)
        //        {
        //            Dalamud.Chat.Print("Applying requires a name for the save to be applied or 'clipboard'.");
        //            return;
        //        }
        //
        //        ApplyCommand(player, split[2]);
        //
        //        return;
        //    }
        //    case "save":
        //    {
        //        if (split.Length < 3)
        //        {
        //            Dalamud.Chat.Print("Saving requires a name for the save.");
        //            return;
        //        }
        //
        //        SaveCommand(player, split[2]);
        //        return;
        //    }
        //    default:
        //        PrintHelp();
        //        return;
        //}
    }
}
