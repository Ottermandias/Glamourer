using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using Glamourer.State;
using Glamourer.Util;
using ImGuiNET;
using OtterGui.Raii;

namespace Glamourer.Gui;

internal partial class Interface : Window, IDisposable
{
    private readonly DalamudPluginInterface _pi;

    private readonly EquipmentDrawer     _equipmentDrawer;
    private readonly CustomizationDrawer _customizationDrawer;

    private readonly ActorTab      _actorTab;
    private readonly DesignTab     _designTab;
    private readonly DebugStateTab _debugStateTab;
    private readonly DebugDataTab  _debugDataTab;

    public Interface(DalamudPluginInterface pi, ItemManager items, ActiveDesign.Manager activeDesigns, Design.Manager manager,
        DesignFileSystem fileSystem, ObjectManager objects)
        : base(GetLabel())
    {
        _pi                                                  =  pi;
        _equipmentDrawer                                     =  new EquipmentDrawer(items);
        _customizationDrawer                                 =  new CustomizationDrawer(pi);
        Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide =  true;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi       += Toggle;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(675, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        _actorTab      = new ActorTab(this, activeDesigns, objects);
        _debugStateTab = new DebugStateTab(activeDesigns);
        _debugDataTab  = new DebugDataTab(Glamourer.Customization);
        _designTab     = new DesignTab(this, manager, fileSystem);
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
