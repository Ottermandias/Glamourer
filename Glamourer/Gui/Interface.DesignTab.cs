using System;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Gui.Designs;
using Glamourer.Gui.Equipment;
using Glamourer.Interop;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class DesignTab : IDisposable
    {
        public readonly  DesignFileSystemSelector Selector;
        private readonly Interface                _main;
        private readonly DesignFileSystem         _fileSystem;
        private readonly Design.Manager           _manager;

        public DesignTab(Interface main, Design.Manager manager, DesignFileSystem fileSystem)
        {
            _main       = main;
            _manager    = manager;
            _fileSystem = fileSystem;
            Selector    = new DesignFileSystemSelector(manager, fileSystem);
        }

        public void Dispose()
            => Selector.Dispose();

        public void Draw()
        {
            using var tab = ImRaii.TabItem("Designs");
            if (!tab)
            {
                return;
            }

            Selector.Draw(GetDesignSelectorSize());
            ImGui.SameLine();
            DrawDesignPanel();
        }

        public float GetDesignSelectorSize()
            => 200f * ImGuiHelpers.GlobalScale;

        public void DrawDesignPanel()
        {
            using var child = ImRaii.Child("##DesignPanel", new Vector2(-0.001f), true, ImGuiWindowFlags.HorizontalScrollbar);
            if (!child || Selector.Selected == null)
                return;

            CustomizationDrawer.Draw(Selector.Selected.Customize(), Selector.Selected.Equipment(), true);
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                _main._equipmentDrawer.DrawStain(Selector.Selected, slot, out var stain);
                ImGui.SameLine();
                _main._equipmentDrawer.DrawArmor(Selector.Selected, slot, out var armor);
            }
        }
    }
}
