using Glamourer.GameData;
using Glamourer.Designs;
using Glamourer.State;
using ImSharp;
using Luna;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed class ActiveStatePanel(StateManager stateManager, ActorObjectManager objectManager) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => new StringU8($"Active Actors ({stateManager.Count})###Active Actors");

    public bool Disabled
        => false;

    public void Draw()
    {
        foreach (var (identifier, actors) in objectManager)
        {
            using var id = Im.Id.Push(actors.Label);
            if (ImEx.Icon.Button(LunaStyle.DeleteIcon, StringU8.Empty, !stateManager.ContainsKey(identifier)))
                stateManager.DeleteState(identifier);

            Im.Line.Same();
            using var t = Im.Tree.Node(actors.Label);
            if (!t)
                continue;

            if (stateManager.GetOrCreate(identifier, actors.Objects[0], out var state))
                DrawState(stateManager, actors, state);
            else
                Im.Text("Invalid actor."u8);
        }
    }

    public static void DrawState(StateManager stateManager, ActorData data, ActorState state)
    {
        using var table = Im.Table.Begin("##state"u8, 7, TableFlags.RowBackground | TableFlags.SizingFixedFit);
        if (!table)
            return;

        table.DrawDataPair("Name"u8, state.Identifier);
        table.NextColumn();
        if (Im.Button("Reset"u8))
            stateManager.ResetState(state, StateSource.Manual);

        table.NextRow();

        PrintRow("Model ID"u8, state.BaseData.ModelId, state.ModelData.ModelId, state.Sources[MetaIndex.ModelId]);
        table.NextRow();
        PrintRow("Wetness"u8, state.BaseData.IsWet(), state.ModelData.IsWet(), state.Sources[MetaIndex.Wetness]);
        table.NextRow();

        if (state.BaseData.IsHuman && state.ModelData.IsHuman)
        {
            PrintRow("Hat Visible"u8, state.BaseData.IsHatVisible(), state.ModelData.IsHatVisible(), state.Sources[MetaIndex.HatState]);
            table.NextRow();
            PrintRow("Visor Toggled"u8, state.BaseData.IsVisorToggled(), state.ModelData.IsVisorToggled(),
                state.Sources[MetaIndex.VisorState]);
            table.NextRow();
            PrintRow("Viera Ears Visible"u8, state.BaseData.AreEarsVisible(), state.ModelData.AreEarsVisible(),
                state.Sources[MetaIndex.EarState]);
            table.NextRow();
            PrintRow("Weapon Visible"u8, state.BaseData.IsWeaponVisible(), state.ModelData.IsWeaponVisible(),
                state.Sources[MetaIndex.WeaponState]);
            table.NextRow();
            foreach (var slot in EquipSlotExtensions.EqdpSlots.Prepend(EquipSlot.OffHand).Prepend(EquipSlot.MainHand))
            {
                PrintRow(slot.ToNameU8(), ItemString(state.BaseData, slot), ItemString(state.ModelData, slot), state.Sources[slot, false]);
                table.DrawColumn($"{state.BaseData.Stain(slot)}");
                table.DrawColumn($"{state.ModelData.Stain(slot)}");
                table.DrawColumn($"{state.Sources[slot, true]}");
            }

            foreach (var slot in BonusExtensions.AllFlags)
            {
                PrintRow(slot.ToNameU8(), BonusItemString(state.BaseData, slot), BonusItemString(state.ModelData, slot), state.Sources[slot]);
                table.NextRow();
            }

            foreach (var type in CustomizeIndex.Values)
            {
                PrintRow(type.ToNameU8(), state.BaseData.Customize[type].Value, state.ModelData.Customize[type].Value,
                    state.Sources[type]);
                table.NextRow();
            }

            foreach (var crest in CrestExtensions.AllRelevantSet)
            {
                PrintRow(crest.ToLabelU8(), state.BaseData.Crest(crest), state.ModelData.Crest(crest), state.Sources[crest]);
                table.NextRow();
            }

            foreach (var flag in CustomizeParameterExtensions.AllFlags)
            {
                PrintRow(flag.ToNameU8(), state.BaseData.Parameters[flag], state.ModelData.Parameters[flag], state.Sources[flag]);
                table.NextRow();
            }
        }
        else
        {
            table.DrawColumn(StringU8.Join((byte)' ', state.BaseData.GetCustomizeBytes().Select(b => b.ToString("X2"))));
            table.DrawColumn(StringU8.Join((byte)' ', state.ModelData.GetCustomizeBytes().Select(b => b.ToString("X2"))));
            table.NextRow();
            table.DrawColumn(StringU8.Join((byte)' ', state.BaseData.GetEquipmentBytes().Select(b => b.ToString("X2"))));
            table.DrawColumn(StringU8.Join((byte)' ', state.ModelData.GetEquipmentBytes().Select(b => b.ToString("X2"))));
        }

        return;

        static StringU8 ItemString(in DesignData data, EquipSlot slot)
        {
            var item = data.Item(slot);
            return
                new StringU8(
                    $"{item.Name} ({item.Id.ToDiscriminatingString()} {item.PrimaryId.Id}{(item.SecondaryId != 0 ? $"-{item.SecondaryId.Id}" : string.Empty)}-{item.Variant})");
        }

        static StringU8 BonusItemString(in DesignData data, BonusItemFlag slot)
        {
            var item = data.BonusItem(slot);
            return
                new StringU8(
                    $"{item.Name} ({item.Id.ToDiscriminatingString()} {item.PrimaryId.Id}{(item.SecondaryId != 0 ? $"-{item.SecondaryId.Id}" : string.Empty)}-{item.Variant})");
        }

        static void PrintRow<T>(ReadOnlySpan<byte> label, T actor, T model, StateSource source) where T : notnull
        {
            Im.Table.DrawColumn(label);
            Im.Table.DrawColumn($"{actor}");
            Im.Table.DrawColumn($"{model}");
            Im.Table.DrawColumn(source.ToNameU8());
        }
    }
}
