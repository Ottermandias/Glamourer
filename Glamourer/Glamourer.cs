using Dalamud.Plugin;
using Glamourer.Api;
using Glamourer.Automation;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Services;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files;

namespace Glamourer;

public class Glamourer : IDalamudPlugin
{
    public string Name
        => "Glamourer";

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public static readonly string CommitHash =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";


    public static readonly Logger         Log = new();
    public static          MessageService Messager { get; private set; } = null!;

    private readonly ServiceManager _services;

    public Glamourer(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            _services = StaticServiceManager.CreateProvider(pluginInterface, Log, this);
            Messager  = _services.GetService<MessageService>();
            _services.EnsureRequiredServices();

            _services.GetService<VisorService>();
            _services.GetService<WeaponService>();
            _services.GetService<ScalingService>();
            _services.GetService<StateListener>();         // Initialize State Listener.
            _services.GetService<GlamourerWindowSystem>(); // initialize ui.
            _services.GetService<CommandService>();        // initialize commands.
            _services.GetService<IpcProviders>();          // initialize IPC.
            Log.Information($"Glamourer v{Version} loaded successfully.");

            //var text      = File.ReadAllBytes(@"C:\FFXIVMods\PBDTest\files\human.pbd");
            //var pbd       = new PbdFile(text);
            //var roundtrip = pbd.Write();
            //File.WriteAllBytes(@"C:\FFXIVMods\PBDTest\files\Vanilla resaved save.pbd", roundtrip);
            //var deformer = pbd.Deformers.FirstOrDefault(d => d.GenderRace is GenderRace.RoegadynFemale)!.RacialDeformer;
            //deformer.DeformMatrices["ya_fukubu_phys"] = deformer.DeformMatrices["j_kosi"];
            //var aleks = pbd.Write();
            //File.WriteAllBytes(@"C:\FFXIVMods\PBDTest\files\rue.pbd", aleks);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string GatherSupportInformation()
    {
        var sb     = new StringBuilder(10240);
        var config = _services.GetService<Configuration>();
        sb.AppendLine("**Settings**");
        sb.Append($"> **`Plugin Version:       `** {Version}\n");
        sb.Append($"> **`Commit Hash:          `** {CommitHash}\n");
        sb.Append($"> **`Enable Auto Designs:  `** {config.EnableAutoDesigns}\n");
        sb.Append($"> **`Gear Protection:      `** {config.UseRestrictedGearProtection}\n");
        sb.Append($"> **`Item Restriction:     `** {config.UnlockedItemMode}\n");
        sb.Append($"> **`Keep Manual Changes:  `** {config.RespectManualOnAutomationUpdate}\n");
        sb.Append($"> **`Auto-Reload Gear:     `** {config.AutoRedrawEquipOnChanges}\n");
        sb.Append($"> **`Revert on Zone Change:`** {config.RevertManualChangesOnZoneChange}\n");
        sb.Append($"> **`Festival Easter-Eggs: `** {config.DisableFestivals}\n");
        sb.Append($"> **`Advanced Customize:   `** {config.UseAdvancedParameters}\n");
        sb.Append($"> **`Advanced Dye:         `** {config.UseAdvancedDyes}\n");
        sb.Append($"> **`Apply Entire Weapon:  `** {config.ChangeEntireItem}\n");
        sb.Append($"> **`Apply Associated Mods:`** {config.AlwaysApplyAssociatedMods}\n");
        sb.Append($"> **`Show QDB:             `** {config.Ephemeral.ShowDesignQuickBar}\n");
        sb.Append($"> **`QDB Hotkey:           `** {config.ToggleQuickDesignBar}\n");
        sb.Append($"> **`Smaller Equip Display:`** {config.SmallEquip}\n");
        sb.Append($"> **`Debug Mode:           `** {config.DebugMode}\n");
        sb.Append($"> **`Cheat Codes:          `** {(ulong)_services.GetService<CodeService>().AllEnabled:X8}\n");
        sb.AppendLine("**Plugins**");
        GatherRelevantPlugins(sb);
        var designManager = _services.GetService<DesignManager>();
        var autoManager   = _services.GetService<AutoDesignManager>();
        var stateManager  = _services.GetService<StateManager>();
        var objectManager = _services.GetService<ObjectManager>();
        var currentPlayer = objectManager.PlayerData.Identifier;
        var states        = stateManager.Where(kvp => objectManager.ContainsKey(kvp.Key)).ToList();

        sb.AppendLine("**Statistics**");
        sb.Append($"> **`Current Player:         `** {(currentPlayer.IsValid ? currentPlayer.Incognito(null) : "None")}\n");
        sb.Append($"> **`Saved Designs:          `** {designManager.Designs.Count}\n");
        sb.Append($"> **`Automation Sets:        `** {autoManager.Count} ({autoManager.Count(set => set.Enabled)} Enabled)\n");
        sb.Append(
            $"> **`Actor States:           `** {stateManager.Count} ({states.Count} Visible, {stateManager.Values.Count(s => s.IsLocked)} Locked)\n");

        var enabledAutomation = autoManager.Where(s => s.Enabled).ToList();
        if (enabledAutomation.Count > 0)
        {
            sb.AppendLine("**Enabled Automation**");
            foreach (var set in enabledAutomation)
            {
                sb.Append(
                    $"> **`{set.Identifiers.First().Incognito(null) + ':',-24}`** {(set.Name.Length >= 2 ? $"{set.Name.AsSpan(0, 2)}..." : set.Name)} ({set.Designs.Count} {(set.Designs.Count == 1 ? "Design" : "Designs")})\n");
            }
        }

        if (states.Count > 0)
        {
            sb.AppendLine("**State**");
            foreach (var (ident, state) in states)
            {
                var sources = Enum.GetValues<StateSource>().Select(s => (0, s)).ToArray();
                foreach (var source in StateIndex.All.Select(s => state.Sources[s]))
                    ++sources[(int)source].Item1;
                foreach (var material in state.Materials.Values)
                    ++sources[(int)material.Value.Source].Item1;
                var sourcesString = string.Join(", ", sources.Where(s => s.Item1 > 0).Select(s => $"{s.s} {s.Item1}"));
                sb.Append(
                    $"> **`{ident.Incognito(null) + ':',-24}`** {(state.IsLocked ? "Locked, " : string.Empty)}Job {state.LastJob.Id}, Zone {state.LastTerritory}, Materials {state.Materials.Values.Count}, {sourcesString}\n");
            }
        }

        return sb.ToString();
    }


    private void GatherRelevantPlugins(StringBuilder sb)
    {
        ReadOnlySpan<string> relevantPlugins =
        [
            "Penumbra", "MareSynchronos", "CustomizePlus", "SimpleHeels", "VfxEditor", "heliosphere-plugin", "Ktisis", "Brio", "DynamicBridge",
            "LoporritSync", "GagSpeak", "ProjectGagSpeak", "RoleplayingVoiceDalamud",
        ];
        var plugins = _services.GetService<IDalamudPluginInterface>().InstalledPlugins
            .GroupBy(p => p.InternalName)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.OrderByDescending(p => p.IsLoaded).ThenByDescending(p => p.Version).First();
                return (item.IsLoaded, item.Version, item.Name);
            });
        foreach (var plugin in relevantPlugins)
        {
            if (plugins.TryGetValue(plugin, out var data))
                sb.Append($"> **`{data.Name + ':',-22}`** {data.Version}{(data.IsLoaded ? string.Empty : " (Disabled)")}\n");
        }
    }

    public void Dispose()
        => _services?.Dispose();
}
