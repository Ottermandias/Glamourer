using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.Api;
using CommandManager = Glamourer.Managers.CommandManager;

namespace Glamourer
{
    internal class Glamourer
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly CommandManager         _commands;

        public Glamourer(DalamudPluginInterface pi)
        {
            _pluginInterface = pi;
            _commands        = new CommandManager(_pluginInterface);
        }
    }

    public class GlamourerPlugin : IDalamudPlugin
    {
        public const int RequiredPenumbraShareVersion = 1;

        public string Name
            => "Glamourer";

        public static DalamudPluginInterface PluginInterface = null!;
        private       Glamourer              _glamourer      = null!;
        private       Interface              _interface      = null!;
        public static ICustomizationManager  Customization   = null!;

        public static string Version = string.Empty;

        public static IPenumbraApi? Penumbra;

        private Dalamud.Dalamud                                                                                                _dalamud = null!;
        private List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)> _plugins = null!;

        private void SetDalamud(DalamudPluginInterface pi)
        {
            var dalamud = (Dalamud.Dalamud?) pi.GetType()
                ?.GetField("dalamud", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(pi);

            _dalamud = dalamud ?? throw new Exception("Could not obtain Dalamud.");
        }

        private void PenumbraTooltip(object? it)
        {
            if (it is Lumina.Excel.GeneratedSheets.Item)
                ImGui.Text("Right click to apply to current Glamourer Set. [Glamourer]");
        }

        private void PenumbraRightClick(MouseButton button, object? it)
        {
            if (button == MouseButton.Right && it is Lumina.Excel.GeneratedSheets.Item item)
            {
                var actors = PluginInterface.ClientState.Actors;
                var player = actors[Interface.GPoseActorId] ?? actors[0];
                if (player != null)
                {
                    var writeItem = new Item(item, string.Empty);
                    writeItem.Write(player.Address);
                    _interface.UpdateActors(player);
                }
            }
        }

        private void RegisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip += PenumbraTooltip;
            Penumbra!.ChangedItemClicked += PenumbraRightClick;
        }

        private void UnregisterFunctions()
        {
            if (Penumbra == null || !Penumbra.Valid)
                return;

            Penumbra!.ChangedItemTooltip -= PenumbraTooltip;
            Penumbra!.ChangedItemClicked -= PenumbraRightClick;
        }

        private void SetPlugins(DalamudPluginInterface pi)
        {
            var pluginManager = _dalamud?.GetType()
                ?.GetProperty("PluginManager", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(_dalamud);

            if (pluginManager == null)
                throw new Exception("Could not obtain plugin manager.");

            var pluginsList =
                (List<(IDalamudPlugin Plugin, PluginDefinition Definition, DalamudPluginInterface PluginInterface, bool IsRaw)>?) pluginManager
                    ?.GetType()
                    ?.GetProperty("Plugins", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(pluginManager);

            _plugins = pluginsList ?? throw new Exception("Could not obtain Dalamud.");
        }

        private bool GetPenumbra()
        {
            if (Penumbra?.Valid ?? false)
                return true;

            var plugin = _plugins.Find(p
                => p.Definition.InternalName == "Penumbra"
             && string.Compare(p.Definition.AssemblyVersion, "0.4.0.3", StringComparison.Ordinal) >= 0).Plugin;

            var penumbra = (IPenumbraApiBase?) plugin?.GetType().GetProperty("Api", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(plugin);
            if (penumbra != null && penumbra.Valid && penumbra.ApiVersion >= RequiredPenumbraShareVersion)
            {
                Penumbra = (IPenumbraApi) penumbra!;
                RegisterFunctions();
            }
            else
            {
                Penumbra = null;
            }

            return Penumbra != null;
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Version         = Assembly.GetExecutingAssembly()?.GetName().Version.ToString() ?? "";
            PluginInterface = pluginInterface;
            Customization   = CustomizationManager.Create(PluginInterface);
            SetDalamud(PluginInterface);
            SetPlugins(PluginInterface);
            GetPenumbra();

            PluginInterface.CommandManager.AddHandler("/glamour", new CommandInfo(OnCommand)
            {
                HelpMessage = "/penumbra - toggle ui\n/penumbra reload - reload mod file lists & discover any new mods",
            });

            _glamourer = new Glamourer(PluginInterface);
            _interface = new Interface();
        }

        public void OnCommand(string command, string arguments)
        {
            if (GetPenumbra())
                Penumbra!.RedrawAll(RedrawType.WithSettings);
            else
                PluginLog.Information("Could not get Penumbra.");
        }


        public void Dispose()
        {
            UnregisterFunctions();
            _interface?.Dispose();
            PluginInterface.CommandManager.RemoveHandler("/glamour");
            PluginInterface.Dispose();
        }
    }
}
