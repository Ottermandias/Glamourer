using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Glamourer.Gui.Tabs;
using Glamourer.Gui.Tabs.ActorTab;
using Glamourer.Gui.Tabs.DesignTab;
using ImGuiNET;
using OtterGui.Custom;
using OtterGui.Widgets;

namespace Glamourer.Gui;

public class MainWindow : Window
{
    public enum TabType
    {
        None     = -1,
        Settings = 0,
        Debug    = 1,
        Actors   = 2,
        Designs  = 3,
    }

    private readonly Configuration _config;
    private readonly ITab[]        _tabs;

    public readonly SettingsTab Settings;
    public readonly ActorTab    Actors;
    public readonly DebugTab    Debug;
    public readonly DesignTab   Designs;

    public TabType SelectTab = TabType.None;

    public MainWindow(DalamudPluginInterface pi, Configuration config, SettingsTab settings, ActorTab actors, DesignTab designs,
        DebugTab debugTab)
        : base(GetLabel())
    {
        pi.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(675, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        Settings = settings;
        Debug    = debugTab;
        Designs  = designs;
        Actors   = actors;
        _config  = config;
        _tabs = new ITab[]
        {
            settings,
            actors,
            designs,
            debugTab,
        };

        IsOpen = _config.DebugMode;
    }

    public override void Draw()
    {
        if (!TabBar.Draw("##tabs", ImGuiTabBarFlags.None, ToLabel(SelectTab), out var currentTab, () => { }, _tabs))
            return;

        SelectTab           = TabType.None;
        _config.SelectedTab = FromLabel(currentTab);
        _config.Save();
    }

    private ReadOnlySpan<byte> ToLabel(TabType type)
        => type switch
        {
            TabType.Settings => Settings.Label,
            TabType.Debug    => Debug.Label,
            TabType.Actors   => Actors.Label,
            TabType.Designs  => Designs.Label,
            _                => ReadOnlySpan<byte>.Empty,
        };

    private TabType FromLabel(ReadOnlySpan<byte> label)
    {
        // @formatter:off
        if (label == Actors.Label) return TabType.Actors;
        if (label == Designs.Label) return TabType.Designs;
        if (label == Settings.Label) return TabType.Settings;
        if (label == Debug.Label)    return TabType.Debug;
        // @formatter:on
        return TabType.None;
    }

    private static string GetLabel()
        => Glamourer.Version.Length == 0
            ? "Glamourer###GlamourerMainWindow"
            : $"Glamourer v{Glamourer.Version}###GlamourerMainWindow";


    /// <summary> Draw the support button group on the right-hand side of the window. </summary>
    public static void DrawSupportButtons()
    {
        var width = ImGui.CalcTextSize("Join Discord for Support").X + ImGui.GetStyle().FramePadding.X * 2;
        var xPos  = ImGui.GetWindowWidth() - width;
        // Respect the scroll bar width.
        if (ImGui.GetScrollMaxY() > 0)
            xPos -= ImGui.GetStyle().ScrollbarSize + ImGui.GetStyle().FramePadding.X;

        ImGui.SetCursorPos(new Vector2(xPos, 0));
        CustomGui.DrawDiscordButton(Glamourer.Chat, width);

        ImGui.SetCursorPos(new Vector2(xPos, ImGui.GetFrameHeightWithSpacing()));
        CustomGui.DrawGuideButton(Glamourer.Chat, width);
    }
}
