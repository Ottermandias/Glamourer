using System;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui.Widgets;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignTab : ITab
{
    public readonly  DesignFileSystemSelector Selector;
    private readonly DesignFileSystem         _fileSystem;
    private readonly DesignManager            _designManager;
    private readonly DesignPanel              _panel;
    private readonly ObjectManager            _objects;

    public DesignTab(DesignFileSystemSelector selector, DesignFileSystem fileSystem, DesignManager designManager, ObjectManager objects, DesignPanel panel)
    {
        Selector       = selector;
        _fileSystem    = fileSystem;
        _designManager = designManager;
        _objects       = objects;
        _panel    = panel;
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
