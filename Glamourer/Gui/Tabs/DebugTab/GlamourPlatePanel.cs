using FFXIVClientStructs.FFXIV.Client.Game;
using Glamourer.Designs;
using Glamourer.Services;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed unsafe class GlamourPlatePanel(
    HookManager hooks,
    ItemManager items,
    DesignManager design,
    StateManager state,
    ActorObjectManager objects)
    : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Glamour Plates"u8;

    public bool Disabled
        => false;

    public void Draw()
    {
        var manager = MirageManager.Instance();
        using (Im.Group())
        {
            Im.Text("Address:"u8);
            Im.Text("Number of Glamour Plates:"u8);
            Im.Text("Glamour Plates Requested:"u8);
            Im.Text("Glamour Plates Loaded:"u8);
            Im.Text("Is Applying Glamour Plates:"u8);
        }

        Im.Line.Same();
        using (Im.Group())
        {
            Glamourer.Dynamis.DrawPointer(manager);
            Im.Text(manager is null ? "-"u8 : $"{manager->GlamourPlates.Length}");
            Im.Text(manager is null ? "-"u8 : $"{manager->GlamourPlatesRequested}");
            Im.Line.Same();
            if (Im.SmallButton("Request Update"u8))
                RequestGlamour();
            Im.Text(manager is null ? "-"u8 : $"{manager->GlamourPlatesLoaded}");
            Im.Text(manager is null ? "-"u8 : $"{manager->IsApplyingGlamourPlate}");
        }

        if (manager is null)
            return;

        ActorState? state1 = null;
        var (identifier, data) = objects.PlayerData;
        var enabled = data.Valid && state.GetOrCreate(identifier, data.Objects[0], out state1);

        for (var i = 0; i < manager->GlamourPlates.Length; ++i)
        {
            using var tree = Im.Tree.Node($"Plate #{i + 1:D2}");
            if (!tree)
                continue;

            ref var plate = ref manager->GlamourPlates[i];
            if (ImEx.Button("Apply to Player"u8, Vector2.Zero, StringU8.Empty, !enabled))
            {
                var design = CreateDesign(plate);
                state.ApplyDesign(state1!, design, ApplySettings.Manual with { IsFinal = true });
            }

            using (Im.Group())
            {
                foreach (var slot in EquipSlotExtensions.FullSlots)
                    Im.Text(slot.ToNameU8());
            }

            Im.Line.Same();
            using (Im.Group())
            {
                foreach (var (index, _) in EquipSlotExtensions.FullSlots.Index())
                    Im.Text($"{plate.ItemIds[index]:D6}, {StainIds.FromGlamourPlate(plate, index)}");
            }
        }
    }

    private readonly delegate* unmanaged<MirageManager*, void> _requestUpdate =
        (delegate* unmanaged<MirageManager*, void>)hooks.SigScanner.ScanText(Sigs.RequestGlamourPlates);

    public void RequestGlamour()
    {
        var manager = MirageManager.Instance();
        if (manager is null)
            return;

        _requestUpdate(manager);
    }

    public DesignBase CreateDesign(in MirageManager.GlamourPlate plate)
    {
        var design1 = design.CreateTemporary();
        design1.Application = ApplicationCollection.None;
        foreach (var (index, slot) in EquipSlotExtensions.FullSlots.Index())
        {
            var itemId = plate.ItemIds[index];
            if (itemId is 0)
                continue;

            var item = items.Resolve(slot, itemId);
            if (!item.Valid)
                continue;

            design1.GetDesignDataRef().SetItem(slot, item);
            design1.GetDesignDataRef().SetStain(slot, StainIds.FromGlamourPlate(plate, index));
            design1.Application.Equip |= slot.ToBothFlags();
        }

        return design1;
    }
}
