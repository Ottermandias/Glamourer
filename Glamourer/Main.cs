using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Glamourer.Customization;
using Glamourer.Gui;
using ImGuiNET;
using Penumbra.Api;
using Penumbra.GameData.Structs;
using CommandManager = Glamourer.Managers.CommandManager;

namespace Glamourer
{
    public class CharacterSave
    {
        public const byte CurrentVersion    = 1;
        public const byte TotalSizeVersion1 = 1 + 1 + 2 + 56 + ActorCustomization.CustomizationBytes;

        public const byte TotalSize = TotalSizeVersion1;

        private readonly byte[] _bytes = new byte[TotalSize];

        public CharacterSave()
            => _bytes[0] = CurrentVersion;

        public byte Version
            => _bytes[0];

        public bool WriteCustomizations
        {
            get => _bytes[1] != 0;
            set => _bytes[1] = (byte) (value ? 1 : 0);
        }

        public ActorEquipMask WriteEquipment
        {
            get => (ActorEquipMask) ((ushort) _bytes[2] | ((ushort) _bytes[3] << 8));
            set
            {
                _bytes[2] = (byte) (((ushort) value) & 0xFF);
                _bytes[3] = (byte) (((ushort) value) >> 8);
            }
        }

        public void Load(ActorCustomization customization)
        {
            WriteCustomizations = true;
            customization.WriteBytes(_bytes, 4);
        }

        public void Load(ActorEquipment equipment, ActorEquipMask mask = ActorEquipMask.All)
        {
            WriteEquipment = mask;
            equipment.WriteBytes(_bytes, 4 + ActorCustomization.CustomizationBytes);
        }

        public string ToBase64()
            => System.Convert.ToBase64String(_bytes);

        public void Load(string base64)
        {
            var bytes = System.Convert.FromBase64String(base64);
            switch (bytes[0])
            {
                case 1:
                    if (bytes.Length != TotalSizeVersion1)
                        throw new Exception(
                            $"Can not parse Base64 string into CharacterSave:\n\tInvalid size {bytes.Length} instead of {TotalSizeVersion1}.");
                    if (bytes[1] != 0 && bytes[1] != 1)
                        throw new Exception(
                            $"Can not parse Base64 string into CharacterSave:\n\tInvalid value {bytes[1]} in byte 2, should be either 0 or 1.");

                    var mask = (ActorEquipMask) ((ushort) bytes[2] | ((ushort) bytes[3] << 8));
                    if (!Enum.IsDefined(typeof(ActorEquipMask), mask))
                        throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid value {mask} in byte 3 and 4.");
                    bytes.CopyTo(_bytes, 0);
                    break;
                default:
                    throw new Exception($"Can not parse Base64 string into CharacterSave:\n\tInvalid Version {bytes[0]}.");
            }
        }

        public static CharacterSave FromString(string base64)
        {
            var ret = new CharacterSave();
            ret.Load(base64);
            return ret;
        }

        public unsafe ActorCustomization Customizations
        {
            get
            {
                var ret = new ActorCustomization();
                fixed (byte* ptr = _bytes)
                {
                    ret.Read(new IntPtr(ptr) + 4);
                }

                return ret;
            }
        }

        public ActorEquipment Equipment
        {
            get
            {
                var ret = new ActorEquipment();
                ret.FromBytes(_bytes, 4 + ActorCustomization.CustomizationBytes);
                return ret;
            }
        }
    }

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
