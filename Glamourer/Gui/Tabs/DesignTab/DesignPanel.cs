using System.Numerics;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Gui.Customization;
using Glamourer.Interop;
using Glamourer.Interop.Penumbra;
using Glamourer.State;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui.Raii;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui.Tabs.DesignTab;

public class DesignPanel
{
    private readonly ObjectManager            _objects;
    private readonly DesignFileSystemSelector _selector;
    private readonly DesignManager            _manager;
    private readonly CustomizationDrawer      _customizationDrawer;
    private readonly StateManager             _state;
    private readonly PenumbraService          _penumbra;
    private readonly UpdateSlotService        _updateSlot;
    private readonly WeaponService            _weaponService;
    private readonly ChangeCustomizeService   _changeCustomizeService;

    public DesignPanel(DesignFileSystemSelector selector, CustomizationDrawer customizationDrawer, DesignManager manager, ObjectManager objects,
        StateManager state, PenumbraService penumbra, ChangeCustomizeService changeCustomizeService, WeaponService weaponService,
        UpdateSlotService updateSlot)
    {
        _selector               = selector;
        _customizationDrawer    = customizationDrawer;
        _manager                = manager;
        _objects                = objects;
        _state                  = state;
        _penumbra               = penumbra;
        _changeCustomizeService = changeCustomizeService;
        _weaponService          = weaponService;
        _updateSlot             = updateSlot;
    }

    public void Draw()
    {
        var design = _selector.Selected;
        if (design == null)
            return;

        using var child = ImRaii.Child("##panel", -Vector2.One, true);
        if (!child)
            return;

        if (ImGui.Button("TEST"))
        {
            var (id, data) = _objects.PlayerData;

            if (data.Valid && _state.GetOrCreate(id, data.Objects[0], out var state))
                _state.ApplyDesign(design, state);
        }

        _customizationDrawer.Draw(design.DesignData.Customize, design.WriteProtected());
    }
}
