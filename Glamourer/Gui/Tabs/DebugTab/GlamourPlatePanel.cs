using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Designs;
using Glamourer.Services;
using Glamourer.State;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed unsafe class GlamourPlatePanel : IGameDataDrawer
{
    private readonly DesignManager      _design;
    private readonly ItemManager        _items;
    private readonly StateManager       _state;
    private readonly ActorObjectManager _objects;

    public ReadOnlySpan<byte> Label
        => "Glamour Plates"u8;

    public bool Disabled
        => false;

    public GlamourPlatePanel(IGameInteropProvider interop, ItemManager items, DesignManager design, StateManager state,
        ActorObjectManager objects)
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
            ImUtf8.Text("Address:"u8);
            ImUtf8.Text("Number of Glamour Plates:"u8);
            ImUtf8.Text("Glamour Plates Requested:"u8);
            ImUtf8.Text("Glamour Plates Loaded:"u8);
            ImUtf8.Text("Is Applying Glamour Plates:"u8);
        }

        ImGui.SameLine();
        using (ImRaii.Group())
        {
            ImUtf8.CopyOnClickSelectable($"0x{(ulong)manager:X}");
            ImUtf8.Text(manager == null ? "-" : manager->GlamourPlates.Length.ToString());
            ImUtf8.Text(manager == null ? "-" : manager->GlamourPlatesRequested.ToString());
            ImGui.SameLine();
            if (ImUtf8.SmallButton("Request Update"u8))
                RequestGlamour();
            ImUtf8.Text(manager == null ? "-" : manager->GlamourPlatesLoaded.ToString());
            ImUtf8.Text(manager == null ? "-" : manager->IsApplyingGlamourPlate.ToString());
        }

        if (manager == null)
            return;

        ActorState? state = null;
        var (identifier, data) = _objects.PlayerData;
        var enabled = data.Valid && _state.GetOrCreate(identifier, data.Objects[0], out state);

        for (var i = 0; i < manager->GlamourPlates.Length; ++i)
        {
            using var tree = ImUtf8.TreeNode($"Plate #{i + 1:D2}");
            if (!tree)
                continue;

            ref var plate = ref manager->GlamourPlates[i];
            if (ImUtf8.ButtonEx("Apply to Player"u8, ""u8, Vector2.Zero, !enabled))
            {
                var design = CreateDesign(plate);
                _state.ApplyDesign(state!, design, ApplySettings.Manual with { IsFinal = true });
            }

            using (ImRaii.Group())
            {
                foreach (var slot in EquipSlotExtensions.FullSlots)
                    ImUtf8.Text(slot.ToName());
            }

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                foreach (var (_, index) in EquipSlotExtensions.FullSlots.WithIndex())
                    ImUtf8.Text($"{plate.ItemIds[index]:D6}, {StainIds.FromGlamourPlate(plate, index)}");
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
