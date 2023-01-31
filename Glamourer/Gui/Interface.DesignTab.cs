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
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class DesignTab : IDisposable
    {
        public readonly  DesignFileSystemSelector Selector;
        private readonly DesignFileSystem         _fileSystem;
        private readonly Design.Manager           _manager;

        public DesignTab(Design.Manager manager, DesignFileSystem fileSystem)
        {
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
            var weapon = Selector.Selected.WeaponMain;
            var mw = new CharacterWeapon(weapon.ModelBase, weapon.WeaponBase, weapon.Variant, weapon.Stain);
            weapon = Selector.Selected.WeaponOff;
            var              ow = new CharacterWeapon(weapon.ModelBase, weapon.WeaponBase, weapon.Variant, weapon.Stain);
            ApplicationFlags f  = 0;
            EquipmentDrawer.Draw(Selector.Selected.Customize(), Selector.Selected.Equipment(), ref mw, ref ow, ref f, Array.Empty<Actor>(), true);
        }
    }
}
