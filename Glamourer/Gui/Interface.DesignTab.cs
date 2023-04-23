using System;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui.Designs;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public partial class Interface
{
    private class DesignTab : IDisposable
    {
        public readonly  DesignFileSystemSelector Selector;
        private readonly Interface                _main;
        private readonly DesignFileSystem         _fileSystem;
        private readonly Design.Manager            _manager;

        public DesignTab(Interface main, Design.Manager manager, DesignFileSystem fileSystem, KeyState keyState)
        {
            _main       = main;
            _manager    = manager;
            _fileSystem = fileSystem;
            Selector    = new DesignFileSystemSelector(manager, fileSystem, keyState);
        }

        public void Dispose()
            => Selector.Dispose();

        public void Draw()
        {
            using var tab = ImRaii.TabItem("Designs");
            if (!tab)
                return;

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

            _main._customizationDrawer.Draw(Selector.Selected.Customize(), CustomizeFlagExtensions.All, true);
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                var current = Selector.Selected.Armor(slot);
                _main._equipmentDrawer.DrawStain(current.Stain, slot, out var stain);
                ImGui.SameLine();
                _main._equipmentDrawer.DrawArmor(current, slot, out var armor);
            }

            var currentMain = Selector.Selected.WeaponMain;
            _main._equipmentDrawer.DrawStain(currentMain.Stain, EquipSlot.MainHand, out var stainMain);
            ImGui.SameLine();
            _main._equipmentDrawer.DrawMainhand(currentMain, true, out var main);
            if (currentMain.Type.Offhand() != FullEquipType.Unknown)
            {
                var currentOff = Selector.Selected.WeaponOff;
                _main._equipmentDrawer.DrawStain(currentOff.Stain, EquipSlot.OffHand, out var stainOff);
                ImGui.SameLine();
                _main._equipmentDrawer.DrawOffhand(currentOff, main.Type, out var off);
            }
        }
    }
}
