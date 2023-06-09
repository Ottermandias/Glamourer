using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Glamourer.Gui.Tabs;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui;

public class MainWindow : Window
{
    private readonly ITab[] _tabs;

    public MainWindow(DalamudPluginInterface pi, DebugTab debugTab)
        : base(GetLabel())
    {
        pi.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(675, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        _tabs = new ITab[]
        {
            debugTab,
        };
    }

    public override void Draw()
    {
        TabBar.Draw("##tabs", ImGuiTabBarFlags.None, ReadOnlySpan<byte>.Empty, out var currentTab, () => { }, _tabs);
    }

    private static string GetLabel()
        => Item.Version.Length == 0
            ? "Glamourer###GlamourerMainWindow"
            : $"Glamourer v{Item.Version}###GlamourerMainWindow";
}
