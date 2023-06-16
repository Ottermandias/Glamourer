using System;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui;

public partial class Interface : Window, IDisposable
{
    private readonly DalamudPluginInterface _pi;

    private readonly EquipmentDrawer     _equipmentDrawer;
    private readonly CustomizationDrawer _customizationDrawer;
    private readonly ConfigurationOld       _config;
    private readonly ActorTab            _actorTab;
    private readonly DesignTab           _designTab;
    private readonly DebugStateTab       _debugStateTab;
    private readonly DebugDataTab        _debugDataTab;

    public Interface(DalamudPluginInterface pi, ItemManager items, ActiveDesign.Manager activeDesigns, DesignManager designManager,
        DesignFileSystem fileSystem, ObjectManager objects, CustomizationService customization, ConfigurationOld config, DataManager gameData, TargetManager targets, ActorService actors, KeyState keyState)
        : base(GetLabel())
    {
        _pi                             = pi;
        _config                         = config;
        _equipmentDrawer                = new EquipmentDrawer(gameData, items);
        _customizationDrawer            = new CustomizationDrawer(pi, customization, items);
        pi.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(675, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        _actorTab      = new ActorTab(this, activeDesigns, objects, targets, actors, items);
        _debugStateTab = new DebugStateTab(activeDesigns);
        _debugDataTab  = new DebugDataTab(customization);
        _designTab     = new DesignTab(this, designManager, fileSystem, keyState, activeDesigns, objects);
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##Tabs");
        if (!tabBar)
            return;

        try
        {
            UpdateState();

            _actorTab.Draw();
            _designTab.Draw();
            DrawSettingsTab();
            _debugStateTab.Draw();
            _debugDataTab.Draw();
            //        DrawSaves();
            //        DrawFixedDesignsTab();
            //        DrawRevertablesTab();
        }
        catch (Exception e)
        {
            PluginLog.Error($"Unexpected Error during Draw:\n{e}");
        }
    }

    public void Dispose()
    {
        _pi.UiBuilder.OpenConfigUi -= Toggle;
        _customizationDrawer.Dispose();
        _designTab.Dispose();
    }

    private static string GetLabel()
        => Glamourer.Version.Length == 0
            ? "Glamourer###GlamourerConfigWindow"
            : $"Glamourer v{Glamourer.Version}###GlamourerConfigWindow";
}
