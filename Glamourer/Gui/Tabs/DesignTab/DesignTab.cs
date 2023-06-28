using System;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignTab : ITab
{
    public readonly  DesignFileSystemSelector Selector;
    private readonly DesignPanel              _panel;

    public DesignTab(DesignFileSystemSelector selector, DesignPanel panel)
    {
        Selector = selector;
        _panel   = panel;
    }

    public ReadOnlySpan<byte> Label
        => "Designs"u8;

    public void DrawContent()
    {
        Selector.Draw(GetDesignSelectorSize());
        ImGui.SameLine();
        _panel.Draw();
    }

    public float GetDesignSelectorSize()
        => 200f * ImGuiHelpers.GlobalScale;
}
