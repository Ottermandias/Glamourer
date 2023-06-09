using System.Reflection;
using Dalamud.Plugin;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Services;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;

namespace Glamourer;

public partial class Glamourer : IDalamudPlugin
{
    public string Name
        => "Glamourer";

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public static readonly string CommitHash =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";


    public static readonly Logger          Log = new();
    public static          ChatService     ChatService { get; private set; } = null!;
    private readonly       ServiceProvider _services;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        try
        {
            EventWrapper.ChangeLogger(Log);
            _services   = ServiceManager.CreateProvider(pluginInterface, Log);
            ChatService = _services.GetRequiredService<ChatService>();
            _services.GetRequiredService<BackupService>();
            _services.GetRequiredService<GlamourerWindowSystem>();
            _services.GetRequiredService<CommandService>();
            _services.GetRequiredService<GlamourerIpc>();
            _services.GetRequiredService<ChangeCustomizeService>();
            _services.GetRequiredService<JobService>();
            _services.GetRequiredService<UpdateSlotService>();
            _services.GetRequiredService<VisorService>();
            _services.GetRequiredService<WeaponService>();
            _services.GetRequiredService<RedrawManager>();
        }
        catch
        {
            Dispose();
            throw;
        }
    }


    public void Dispose()
    {
        _services?.Dispose();
    }

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
