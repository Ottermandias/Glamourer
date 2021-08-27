using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Glamourer
{
    public class Dalamud
    {
        public static void Initialize(DalamudPluginInterface pluginInterface)
            => pluginInterface.Create<Dalamud>();

        // @formatter:off
        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static CommandManager         Commands        { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static SigScanner             SigScanner      { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static DataManager            GameData        { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ClientState            ClientState     { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ChatGui                Chat            { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static SeStringManager        SeStrings       { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static ChatHandlers           ChatHandlers    { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static Framework              Framework       { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static GameNetwork            Network         { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static Condition              Conditions      { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static KeyState               Keys            { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static GameGui                GameGui         { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static FlyTextGui             FlyTexts        { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static ToastGui               Toasts          { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static JobGauges              Gauges          { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static PartyFinderGui         PartyFinder     { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static BuddyList              Buddies         { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static PartyList              Party           { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static TargetManager          Targets         { get; private set; } = null!;
        [PluginService][RequiredVersion("1.0")] public static ObjectTable            Objects         { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static FateTable              Fates           { get; private set; } = null!;
        //[PluginService][RequiredVersion("1.0")] public static LibcFunction           LibC            { get; private set; } = null!;
        // @formatter:on
    }
}
