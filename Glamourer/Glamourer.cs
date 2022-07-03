using System;
using System.Linq;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Api;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.FileSystem;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.GameData.ByteString;
using Penumbra.GameData.Enums;
using Penumbra.PlayerWatch;

namespace Glamourer;

public unsafe class FixedDesignManager : IDisposable
{
    public delegate ulong FlagSlotForUpdateDelegate(Human* drawObject, uint slot, uint* data);

    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A",
        DetourName = nameof(FlagSlotForUpdateDetour))]
    public Hook<FlagSlotForUpdateDelegate>? FlagSlotForUpdateHook;

    public readonly FixedDesigns FixedDesigns;

    public FixedDesignManager(DesignManager designs)
    {
        SignatureHelper.Initialise(this);
        FixedDesigns = new FixedDesigns(designs);


        if (Glamourer.Config.ApplyFixedDesigns)
            Enable();
    }

    public void Enable()
    {
        FlagSlotForUpdateHook?.Enable();
        Glamourer.Penumbra.CreatingCharacterBase += ApplyFixedDesign;
    }

    public void Disable()
    {
        FlagSlotForUpdateHook?.Disable();
        Glamourer.Penumbra.CreatingCharacterBase -= ApplyFixedDesign;
    }

    public void Dispose()
    {
        FlagSlotForUpdateHook?.Dispose();
    }

    private void ApplyFixedDesign(IntPtr addr, IntPtr customize, IntPtr equipData)
    {
        var human = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)addr;
        if (human->GameObject.ObjectKind is (byte)ObjectKind.EventNpc or (byte)ObjectKind.BattleNpc or (byte)ObjectKind.Player
         && human->ModelCharaId == 0)
        {
            var name = new Utf8String(human->GameObject.Name).ToString();
            if (FixedDesigns.EnabledDesigns.TryGetValue(name, out var designs))
            {
                var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(human->ClassJob));
                if (design != null)
                {
                    if (design.Design.Data.WriteCustomizations)
                        *(CharacterCustomization*)customize = design.Design.Data.Customizations;

                    var data = (uint*)equipData;
                    for (var i = 0u; i < 10; ++i)
                    {
                        var slot = i.ToEquipSlot();
                        if (design.Design.Data.WriteEquipment.Fits(slot))
                            data[i] = slot switch
                            {
                                EquipSlot.Head    => design.Design.Data.Equipment.Head.Value,
                                EquipSlot.Body    => design.Design.Data.Equipment.Body.Value,
                                EquipSlot.Hands   => design.Design.Data.Equipment.Hands.Value,
                                EquipSlot.Legs    => design.Design.Data.Equipment.Legs.Value,
                                EquipSlot.Feet    => design.Design.Data.Equipment.Feet.Value,
                                EquipSlot.Ears    => design.Design.Data.Equipment.Ears.Value,
                                EquipSlot.Neck    => design.Design.Data.Equipment.Neck.Value,
                                EquipSlot.Wrists  => design.Design.Data.Equipment.Wrists.Value,
                                EquipSlot.RFinger => design.Design.Data.Equipment.RFinger.Value,
                                EquipSlot.LFinger => design.Design.Data.Equipment.LFinger.Value,
                                _                 => 0,
                            };
                    }
                }
            }
        }
    }

    private ulong FlagSlotForUpdateDetour(Human* drawObject, uint slotIdx, uint* data)
    {
        ulong ret;
        var   slot = slotIdx.ToEquipSlot();
        try
        {
            if (slot != EquipSlot.Unknown)
            {
                var gameObject =
                    (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)Glamourer.Penumbra.GameObjectFromDrawObject((IntPtr)drawObject);
                if (gameObject != null)
                {
                    var name = new Utf8String(gameObject->GameObject.Name).ToString();
                    if (FixedDesigns.EnabledDesigns.TryGetValue(name, out var designs))
                    {
                        var design = designs.OrderBy(d => d.Jobs.Count).FirstOrDefault(d => d.Jobs.Fits(gameObject->ClassJob));
                        if (design != null && design.Design.Data.WriteEquipment.Fits(slot))
                            *data = slot switch
                            {
                                EquipSlot.Head    => design.Design.Data.Equipment.Head.Value,
                                EquipSlot.Body    => design.Design.Data.Equipment.Body.Value,
                                EquipSlot.Hands   => design.Design.Data.Equipment.Hands.Value,
                                EquipSlot.Legs    => design.Design.Data.Equipment.Legs.Value,
                                EquipSlot.Feet    => design.Design.Data.Equipment.Feet.Value,
                                EquipSlot.Ears    => design.Design.Data.Equipment.Ears.Value,
                                EquipSlot.Neck    => design.Design.Data.Equipment.Neck.Value,
                                EquipSlot.Wrists  => design.Design.Data.Equipment.Wrists.Value,
                                EquipSlot.RFinger => design.Design.Data.Equipment.RFinger.Value,
                                EquipSlot.LFinger => design.Design.Data.Equipment.LFinger.Value,
                                _                 => 0,
                            };
                    }
                }
            }
        }
        finally
        {
            ret = FlagSlotForUpdateHook!.Original(drawObject, slotIdx, data);
        }

        return ret;
    }
}

public class Glamourer : IDalamudPlugin
{
    private const string HelpString = "[Copy|Apply|Save],[Name or PlaceHolder],<Name for Save>";

    public string Name
        => "Glamourer";

    public static    GlamourerConfig       Config             = null!;
    public static    IPlayerWatcher        PlayerWatcher      = null!;
    public static    ICustomizationManager Customization      = null!;
    public static    FixedDesignManager    FixedDesignManager = null!;
    private readonly Interface             _interface;
    public readonly  DesignManager         Designs;

    public static   RevertableDesigns RevertableDesigns = new();
    public readonly GlamourerIpc      GlamourerIpc;


    public static string         Version  = string.Empty;
    public static PenumbraAttach Penumbra = null!;

    public Glamourer(DalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);
        Version            = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
        Config             = GlamourerConfig.Load();
        Customization      = CustomizationManager.Create(Dalamud.PluginInterface, Dalamud.GameData, Dalamud.ClientState.ClientLanguage);
        Designs            = new DesignManager();
        Penumbra           = new PenumbraAttach(Config.AttachToPenumbra);
        PlayerWatcher      = PlayerWatchFactory.Create(Dalamud.Framework, Dalamud.ClientState, Dalamud.Objects);
        GlamourerIpc       = new GlamourerIpc(Dalamud.ClientState, Dalamud.Objects, Dalamud.PluginInterface);
        FixedDesignManager = new FixedDesignManager(Designs);
        if (!Config.ApplyFixedDesigns)
            PlayerWatcher.Disable();

        Dalamud.Commands.AddHandler("/glamourer", new CommandInfo(OnGlamourer)
        {
            HelpMessage = "Open or close the Glamourer window.",
        });
        Dalamud.Commands.AddHandler("/glamour", new CommandInfo(OnGlamour)
        {
            HelpMessage = $"Use Glamourer Functions: {HelpString}",
        });

        _interface = new Interface(this);
    }

    public void OnGlamourer(string command, string arguments)
        => _interface.ToggleVisibility();

    private static GameObject? GetPlayer(string name)
    {
        var lowerName = name.ToLowerInvariant();
        return lowerName switch
        {
            ""          => null,
            "<me>"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
            "self"      => Dalamud.Objects[Interface.GPoseObjectId] ?? Dalamud.ClientState.LocalPlayer,
            "<t>"       => Dalamud.Targets.Target,
            "target"    => Dalamud.Targets.Target,
            "<f>"       => Dalamud.Targets.FocusTarget,
            "focus"     => Dalamud.Targets.FocusTarget,
            "<mo>"      => Dalamud.Targets.MouseOverTarget,
            "mouseover" => Dalamud.Targets.MouseOverTarget,
            _ => Dalamud.Objects.LastOrDefault(
                a => string.Equals(a.Name.ToString(), lowerName, StringComparison.InvariantCultureIgnoreCase)),
        };
    }

    public void CopyToClipboard(Character player)
    {
        var save = new CharacterSave();
        save.LoadCharacter(player);
        ImGui.SetClipboardText(save.ToBase64());
    }

    public void ApplyCommand(Character player, string target)
    {
        CharacterSave? save = null;
        if (target.ToLowerInvariant() == "clipboard")
            try
            {
                save = CharacterSave.FromString(ImGui.GetClipboardText());
            }
            catch (Exception)
            {
                Dalamud.Chat.PrintError("Clipboard does not contain a valid customization string.");
            }
        else if (!Designs.FileSystem.Find(target, out var child) || child is not Design d)
            Dalamud.Chat.PrintError("The given path to a saved design does not exist or does not point to a design.");
        else
            save = d.Data;

        save?.Apply(player);
        Penumbra.UpdateCharacters(player);
    }

    public void SaveCommand(Character player, string path)
    {
        var save = new CharacterSave();
        save.LoadCharacter(player);
        try
        {
            var (folder, name) = Designs.FileSystem.CreateAllFolders(path);
            var design = new Design(folder, name) { Data = save };
            folder.FindOrAddChild(design);
            Designs.Designs.Add(design.FullName(), design.Data);
            Designs.SaveToFile();
        }
        catch (Exception e)
        {
            Dalamud.Chat.PrintError("Could not save file:");
            Dalamud.Chat.PrintError($"    {e.Message}");
        }
    }

    public void OnGlamour(string command, string arguments)
    {
        static void PrintHelp()
        {
            Dalamud.Chat.Print("Usage:");
            Dalamud.Chat.Print($"    {HelpString}");
        }

        arguments = arguments.Trim();
        if (!arguments.Any())
        {
            PrintHelp();
            return;
        }

        var split = arguments.Split(new[]
        {
            ',',
        }, 3, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length < 2)
        {
            PrintHelp();
            return;
        }

        var player = GetPlayer(split[1]) as Character;
        if (player == null)
        {
            Dalamud.Chat.Print($"Could not find object for {split[1]} or it was not a Character.");
            return;
        }

        switch (split[0].ToLowerInvariant())
        {
            case "copy":
                CopyToClipboard(player);
                return;
            case "apply":
            {
                if (split.Length < 3)
                {
                    Dalamud.Chat.Print("Applying requires a name for the save to be applied or 'clipboard'.");
                    return;
                }

                ApplyCommand(player, split[2]);

                return;
            }
            case "save":
            {
                if (split.Length < 3)
                {
                    Dalamud.Chat.Print("Saving requires a name for the save.");
                    return;
                }

                SaveCommand(player, split[2]);
                return;
            }
            default:
                PrintHelp();
                return;
        }
    }

    public void Dispose()
    {
        FixedDesignManager.Dispose();
        Penumbra.Dispose();
        PlayerWatcher.Dispose();
        _interface.Dispose();
        GlamourerIpc.Dispose();
        Dalamud.Commands.RemoveHandler("/glamour");
        Dalamud.Commands.RemoveHandler("/glamourer");
    }
}
