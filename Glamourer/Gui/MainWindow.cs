using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Gui.Tabs;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.Gui.Tabs.AutomationTab;
using Glamourer.Gui.Tabs.DesignTab;
using Glamourer.Gui.Tabs.UnlocksTab;
using ImGuiNET;
using OtterGui.Custom;
using OtterGui.Widgets;

namespace Glamourer.Gui;

public class MainWindow : Window, IDisposable
{
    public enum TabType
    {
        None       = -1,
        Settings   = 0,
        Debug      = 1,
        Actors     = 2,
        Designs    = 3,
        Automation = 4,
        Unlocks    = 5,
        Messages   = 6,
    }

    private readonly Configuration  _config;
    private readonly DesignQuickBar _quickBar;
    private readonly TabSelected    _event;
    private readonly ITab[]         _tabs;

    public readonly SettingsTab   Settings;
    public readonly ActorTab      Actors;
    public readonly DebugTab      Debug;
    public readonly DesignTab     Designs;
    public readonly AutomationTab Automation;
    public readonly UnlocksTab    Unlocks;
    public readonly MessagesTab   Messages;

    public TabType SelectTab = TabType.None;

    public MainWindow(DalamudPluginInterface pi, Configuration config, SettingsTab settings, ActorTab actors, DesignTab designs,
        DebugTab debugTab, AutomationTab automation, UnlocksTab unlocks, TabSelected @event, MessagesTab messages, DesignQuickBar quickBar)
        : base(GetLabel())
    {
        pi.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        Settings   = settings;
        Actors     = actors;
        Designs    = designs;
        Automation = automation;
        Debug      = debugTab;
        Unlocks    = unlocks;
        _event     = @event;
        Messages   = messages;
        _quickBar  = quickBar;
        _config    = config;
        _tabs = new ITab[]
        {
            settings,
            actors,
            designs,
            automation,
            unlocks,
            messages,
            debugTab,
        };
        _event.Subscribe(OnTabSelected, TabSelected.Priority.MainWindow);
        IsOpen = _config.DebugMode;
    }

    public override void PreDraw()
    {
        Flags = _config.LockMainWindow
            ? Flags | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
            : Flags & ~(ImGuiWindowFlags.NoMove |ImGuiWindowFlags.NoResize);
    }

    public void Dispose()
        => _event.Unsubscribe(OnTabSelected);

    public override void Draw()
    {
        var yPos = ImGui.GetCursorPosY();
        if (TabBar.Draw("##tabs", ImGuiTabBarFlags.None, ToLabel(SelectTab), out var currentTab, () => { }, _tabs))
        {
            SelectTab           = TabType.None;
            _config.SelectedTab = FromLabel(currentTab);
            _config.Save();
        }

        if (_config.ShowQuickBarInTabs)
        {
            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - 10 * ImGui.GetFrameHeight(), yPos - ImGuiHelpers.GlobalScale));
            _quickBar.Draw();
        }
    }

    private ReadOnlySpan<byte> ToLabel(TabType type)
        => type switch
        {
            TabType.Settings   => Settings.Label,
            TabType.Debug      => Debug.Label,
            TabType.Actors     => Actors.Label,
            TabType.Designs    => Designs.Label,
            TabType.Automation => Automation.Label,
            TabType.Unlocks    => Unlocks.Label,
            TabType.Messages   => Messages.Label,
            _                  => ReadOnlySpan<byte>.Empty,
        };

    private TabType FromLabel(ReadOnlySpan<byte> label)
    {
        // @formatter:off
        if (label == Actors.Label)     return TabType.Actors;
        if (label == Designs.Label)    return TabType.Designs;
        if (label == Settings.Label)   return TabType.Settings;
        if (label == Automation.Label) return TabType.Automation;
        if (label == Unlocks.Label)    return TabType.Unlocks;
        if (label == Messages.Label)   return TabType.Messages;
        if (label == Debug.Label)      return TabType.Debug;
        // @formatter:on
        return TabType.None;
    }

    /// <summary> Draw the support button group on the right-hand side of the window. </summary>
    public static void DrawSupportButtons(Changelog changelog)
    {
        var width = ImGui.CalcTextSize("Join Discord for Support").X + ImGui.GetStyle().FramePadding.X * 2;
        var xPos  = ImGui.GetWindowWidth() - width;
        // Respect the scroll bar width.
        if (ImGui.GetScrollMaxY() > 0)
            xPos -= ImGui.GetStyle().ScrollbarSize + ImGui.GetStyle().FramePadding.X;

        ImGui.SetCursorPos(new Vector2(xPos, 0));
        CustomGui.DrawDiscordButton(Glamourer.Messager, width);

        ImGui.SetCursorPos(new Vector2(xPos, ImGui.GetFrameHeightWithSpacing()));
        CustomGui.DrawGuideButton(Glamourer.Messager, width);

        ImGui.SetCursorPos(new Vector2(xPos, 2 * ImGui.GetFrameHeightWithSpacing()));
        if (ImGui.Button("Show Changelogs", new Vector2(width, 0)))
            changelog.ForceOpen = true;
    }

    private void OnTabSelected(TabType type, Design? _)
        => SelectTab = type;

    private static string GetLabel()
        => Glamourer.Version.Length == 0
            ? "Glamourer###GlamourerMainWindow"
            : $"Glamourer v{Glamourer.Version}###GlamourerMainWindow";
}
