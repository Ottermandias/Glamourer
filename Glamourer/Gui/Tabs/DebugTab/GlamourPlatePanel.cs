using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Designs;
using Glamourer.Interop;
using Glamourer.Services;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class GlamourPlatePanel : IGameDataDrawer
{
    private readonly DesignManager _design;
    private readonly ItemManager   _items;
    private readonly StateManager  _state;
    private readonly ObjectManager _objects;

    public string Label
        => "Glamour Plates";

    public bool Disabled
        => false;

    public GlamourPlatePanel(IGameInteropProvider interop, ItemManager items, DesignManager design, StateManager state, ObjectManager objects)
    {
        _items   = items;
        _design  = design;
        _state   = state;
        _objects = objects;
        interop.InitializeFromAttributes(this);
    }

    public void Draw()
    {
        var manager = MirageManager.Instance();
        using (ImRaii.Group())
        {
            ImGui.TextUnformatted("Address:");
            ImGui.TextUnformatted("Number of Glamour Plates:");
            ImGui.TextUnformatted("Glamour Plates Requested:");
            ImGui.TextUnformatted("Glamour Plates Loaded:");
            ImGui.TextUnformatted("Is Applying Glamour Plates:");
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            ImGuiUtil.CopyOnClickSelectable($"0x{(ulong)manager:X}");
            ImGui.TextUnformatted(manager == null ? "-" : manager->GlamourPlates.Length.ToString());
            ImGui.TextUnformatted(manager == null ? "-" : manager->GlamourPlatesRequested.ToString());
            ImGui.SameLine();
            if (ImGui.SmallButton("Request Update"))
                RequestGlamour();
            ImGui.TextUnformatted(manager == null ? "-" : manager->GlamourPlatesLoaded.ToString());
            ImGui.TextUnformatted(manager == null ? "-" : manager->IsApplyingGlamourPlate.ToString());
        }

        if (manager == null)
            return;

        ActorState? state = null;
        var (identifier, data) = _objects.PlayerData;
        var enabled = data.Valid && _state.GetOrCreate(identifier, data.Objects[0], out state);

        for (var i = 0; i < manager->GlamourPlates.Length; ++i)
        {
            using var tree = ImRaii.TreeNode($"Plate #{i + 1:D2}");
            if (!tree)
                continue;

            ref var plate = ref manager->GlamourPlates[i];
            if (ImGuiUtil.DrawDisabledButton("Apply to Player", Vector2.Zero, string.Empty, !enabled))
            {
                var design = CreateDesign(plate);
                _state.ApplyDesign(state!, design, ApplySettings.Manual with { IsFinal = true });
            }

            using (ImRaii.Group())
            {
                foreach (var slot in EquipSlotExtensions.FullSlots)
                    ImGui.TextUnformatted(slot.ToName());
            }

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                foreach (var (_, index) in EquipSlotExtensions.FullSlots.WithIndex())
                    ImGui.TextUnformatted($"{plate.ItemIds[index]:D6}, {StainIds.FromGlamourPlate(plate, index)}");
            }
        }
    }

    [Signature(Sigs.RequestGlamourPlates)]
    private readonly delegate* unmanaged<MirageManager*, void> _requestUpdate = null!;

    public void RequestGlamour()
    {
        var manager = MirageManager.Instance();
        if (manager == null)
            return;

        _requestUpdate(manager);
    }

    public DesignBase CreateDesign(in MirageManager.GlamourPlate plate)
    {
        var design = _design.CreateTemporary();
        design.Application = ApplicationCollection.None;
        foreach (var (slot, index) in EquipSlotExtensions.FullSlots.WithIndex())
        {
            var itemId = plate.ItemIds[index];
            if (itemId == 0)
                continue;

            var item = _items.Resolve(slot, itemId);
            if (!item.Valid)
                continue;

            design.GetDesignDataRef().SetItem(slot, item);
            design.GetDesignDataRef().SetStain(slot, StainIds.FromGlamourPlate(plate, index));
            design.Application.Equip |= slot.ToBothFlags();
        }

        return design;
    }
}
