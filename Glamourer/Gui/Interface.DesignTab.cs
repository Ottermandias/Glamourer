using System;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui.Designs;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using Microsoft.VisualBasic;
using OtterGui;
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
        private readonly DesignManager            _designManager;
        private readonly ActiveDesign.Manager     _activeDesignManager;
        private readonly ObjectManager             _objects;

        public DesignTab(Interface main, DesignManager designManager, DesignFileSystem fileSystem, KeyState keyState,
            ActiveDesign.Manager activeDesignManager, ObjectManager objects)
        {
            _main                = main;
            _designManager       = designManager;
            _fileSystem          = fileSystem;
            _activeDesignManager = activeDesignManager;
            _objects             = objects;
            Selector             = new DesignFileSystemSelector(designManager, fileSystem, keyState);
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

        private void ApplySelfButton()
        {
            var self = _objects.Player;
            if (!ImGuiUtil.DrawDisabledButton("Apply to Self", Vector2.Zero, string.Empty, !self.Valid))
                return;

            var design = _activeDesignManager.GetOrCreateSave(self);
            _activeDesignManager.ApplyDesign(design, Selector.Selected!, false);
        }

        public void DrawDesignPanel()
        {
            if (Selector.Selected == null)
                return;

            using var group = ImRaii.Group();

            ApplySelfButton();

            using var child = ImRaii.Child("##DesignPanel", new Vector2(-0.001f), true, ImGuiWindowFlags.HorizontalScrollbar);
            if (!child)
                return;

            _main._customizationDrawer.Draw(Selector.Selected.ModelData.Customize, CustomizeFlagExtensions.All, true);
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
