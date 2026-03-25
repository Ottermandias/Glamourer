using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using ImSharp;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Tabs.DebugTab;

public sealed unsafe class ModelEvaluationPanel(
    ActorObjectManager objectManager,
    VisorService visorService,
    VieraEarService vieraEarService,
    UpdateSlotService updateSlotService,
    ChangeCustomizeService changeCustomizeService,
    CrestService crestService,
    DictBonusItems bonusItems) : IGameDataDrawer
{
    public ReadOnlySpan<byte> Label
        => "Model Evaluation"u8;

    public bool Disabled
        => false;

    private int _gameObjectIndex;

    public void Draw()
    {
        Im.Input.Scalar("Game Object Index"u8, ref _gameObjectIndex);
        var       actor = objectManager.Objects[_gameObjectIndex];
        var       model = actor.Model;
        using var table = Im.Table.Begin("##evaluationTable"u8, 4, TableFlags.SizingFixedFit | TableFlags.RowBackground);
        table.NextColumn();
        table.NextColumn();
        table.Header("Actor"u8);
        table.NextColumn();
        table.Header("Model"u8);
        table.NextColumn();

        table.DrawColumn("Address"u8);
        table.NextColumn();

        Glamourer.Dynamis.DrawPointer(actor);
        table.NextColumn();
        Glamourer.Dynamis.DrawPointer(model);
        table.NextColumn();
        if (actor.IsCharacter)
        {
            Im.Text($"{actor.AsCharacter->ModelContainer.ModelCharaId}");
            if (actor.AsCharacter->CharacterData.TransformationId is not 0)
                Im.Text($"Transformation Id: {actor.AsCharacter->CharacterData.TransformationId}");
            if (actor.AsCharacter->ModelContainer.ModelCharaId_2 is not -1)
                Im.Text($"ModelChara2 {actor.AsCharacter->ModelContainer.ModelCharaId_2}");

            table.DrawDataPair("Character Mode"u8, actor.AsCharacter->Mode);
            table.NextColumn();
            table.NextColumn();

            table.DrawDataPair("Animation"u8, ((ushort*)&actor.AsCharacter->Timeline)[0x78]);
            table.NextColumn();
            table.NextColumn();
        }


        table.DrawColumn("Mainhand"u8);
        table.DrawColumn(actor.IsCharacter ? $"{actor.GetMainhand()}" : "No Character"u8);

        var (mainhand, offhand, mainModel, offModel) = model.GetWeapons(actor);
        table.DrawColumn($"{mainModel}");
        table.NextColumn();
        Glamourer.Dynamis.DrawPointer(mainhand);

        table.DrawColumn("Offhand"u8);
        table.DrawColumn(actor.IsCharacter ? $"{actor.GetOffhand()}" : "No Character"u8);
        table.DrawColumn($"{offModel}");
        table.NextColumn();
        Glamourer.Dynamis.DrawPointer(offhand);

        DrawVisor(table, actor, model);
        DrawVieraEars(table, actor, model);
        DrawHatState(table, actor, model);
        DrawWeaponState(table, actor, model);
        DrawWetness(table, actor, model);
        DrawEquip(table, actor, model);
        DrawCustomize(table, actor, model);
        DrawCrests(table, actor, model);
        DrawParameters(table, actor, model);

        table.DrawColumn("Scale"u8);
        table.DrawColumn(actor.Valid ? actor.AsObject->Scale.ToString(CultureInfo.InvariantCulture) : "No Character"u8);
        table.DrawColumn(model.Valid ? $"{model.AsDrawObject->Object.Scale}" : "No Model"u8);
    }

    private static void DrawParameters(in Im.TableDisposable table, Actor actor, Model model)
    {
        if (!model.IsHuman)
            return;

        var convert = model.GetParameterData();
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            using var id = Im.Id.Push((int)flag);
            table.DrawColumn(flag.ToNameU8());
            table.DrawColumn(StringU8.Empty);
            var value = convert[flag].InternalQuadruple;
            table.DrawColumn($"{value.X:F2} | {value.Y:F2} | {value.Z:F2} | {value.W:F2}");
            table.NextColumn();
        }
    }

    private void DrawVisor(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("Visor"u8);
        table.DrawColumn("Visor State"u8);
        table.DrawColumn(actor.IsCharacter ? $"{actor.AsCharacter->DrawData.IsVisorToggled}" : "No Character"u8);
        table.DrawColumn(model.IsHuman ? $"{VisorService.GetVisorState(model)}" : "No Human"u8);
        table.NextColumn();
        if (!model.IsHuman)
            return;

        if (Im.SmallButton("Set True"u8))
            visorService.SetVisorState(model, true);
        Im.Line.Same();
        if (Im.SmallButton("Set False"u8))
            visorService.SetVisorState(model, false);
        Im.Line.Same();
        if (Im.SmallButton("Toggle"u8))
            visorService.SetVisorState(model, !VisorService.GetVisorState(model));
    }

    private void DrawVieraEars(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("Viera Ears"u8);
        table.DrawColumn("Viera Ears"u8);
        table.DrawColumn(actor.IsCharacter ? $"{actor.ShowVieraEars}" : "No Character"u8);
        table.DrawColumn(model.IsHuman ? $"{model.VieraEarsVisible}" : "No Human"u8);
        table.NextColumn();
        if (!model.IsHuman)
            return;

        if (Im.SmallButton("Set True"u8))
            vieraEarService.SetVieraEarState(model, true);
        Im.Line.Same();
        if (Im.SmallButton("Set False"u8))
            vieraEarService.SetVieraEarState(model, false);
        Im.Line.Same();
        if (Im.SmallButton("Toggle"u8))
            vieraEarService.SetVieraEarState(model, !model.VieraEarsVisible);
    }

    private void DrawHatState(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("HatState"u8);
        table.DrawColumn("Hat State"u8);
        table.DrawColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsHatHidden ? "Hidden"u8 : $"{actor.GetArmor(EquipSlot.Head)}"
            : "No Character"u8);
        table.DrawColumn(model.IsHuman
            ? model.AsHuman->Head.Value is 0 ? "No Hat"u8 : $"{model.GetArmor(EquipSlot.Head)}"
            : "No Human"u8);
        table.NextColumn();
        if (!model.IsHuman)
            return;

        if (Im.SmallButton("Hide"u8))
            updateSlotService.UpdateEquipSlot(model, EquipSlot.Head, CharacterArmor.Empty);
        Im.Line.Same();
        if (Im.SmallButton("Show"u8))
            updateSlotService.UpdateEquipSlot(model, EquipSlot.Head, actor.GetArmor(EquipSlot.Head));
        Im.Line.Same();
        if (Im.SmallButton("Toggle"u8))
            updateSlotService.UpdateEquipSlot(model, EquipSlot.Head,
                model.AsHuman->Head.Value == 0 ? actor.GetArmor(EquipSlot.Head) : CharacterArmor.Empty);
    }

    private static void DrawWeaponState(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("WeaponState"u8);
        table.DrawColumn("Weapon State"u8);
        table.DrawColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsWeaponHidden ? "Hidden"u8 : "Visible"u8
            : "No Character"u8);
        ReadOnlySpan<byte> text;
        if (!model.IsHuman)
        {
            text = "No Model"u8;
        }
        else if (model.AsDrawObject->Object.ChildObject is null)
        {
            text = "No Weapon"u8;
        }
        else
        {
            var weapon = (DrawObject*)model.AsDrawObject->Object.ChildObject;
            text = (weapon->Flags & 0x09) is 0x09 ? "Visible"u8 : "Hidden"u8;
        }

        table.DrawColumn(text);
        table.NextColumn();
    }

    private static void DrawWetness(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("Wetness"u8);
        table.DrawColumn("Wetness"u8);
        table.DrawColumn(actor.IsCharacter ? actor.IsGPoseWet ? "GPose"u8 : "None"u8 : "No Character"u8);
        table.DrawColumn(model.IsCharacterBase
            ? $"{model.AsCharacterBase->SwimmingWetness:F4} Swimming\n"
          + $"{model.AsCharacterBase->WeatherWetness:F4} Weather\n"
          + $"{model.AsCharacterBase->ForcedWetness:F4} Forced\n"
          + $"{model.AsCharacterBase->WetnessDepth:F4} Depth\n"
            : "No CharacterBase"u8);
        table.NextColumn();
        if (!actor.IsCharacter)
            return;

        if (Im.SmallButton("GPose On"u8))
            actor.IsGPoseWet = true;
        Im.Line.Same();
        if (Im.SmallButton("GPose Off"u8))
            actor.IsGPoseWet = false;
        Im.Line.Same();
        if (Im.SmallButton("GPose Toggle"u8))
            actor.IsGPoseWet = !actor.IsGPoseWet;
    }

    private void DrawEquip(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("Equipment"u8);
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            id.Push((int)slot);
            table.DrawColumn(slot.ToNameU8());
            table.DrawColumn(actor.IsCharacter ? $"{actor.GetArmor(slot)}" : "No Character"u8);
            table.DrawColumn(model.IsHuman ? $"{model.GetArmor(slot)}" : "No Human"u8);
            table.NextColumn();
            if (!model.IsHuman)
                continue;

            if (Im.SmallButton("Change Piece"u8))
                updateSlotService.UpdateArmor(model, slot,
                    new CharacterArmor(slot switch
                    {
                        EquipSlot.Hands => 6064,
                        EquipSlot.Head  => 6072,
                        _               => 1,
                    }, 1, StainIds.None));
            Im.Line.Same();
            if (Im.SmallButton("Change Stain"u8))
                updateSlotService.UpdateStain(model, slot, new StainIds(5, 7));
            Im.Line.Same();
            if (Im.SmallButton("Reset"u8))
                updateSlotService.UpdateEquipSlot(model, slot, actor.GetArmor(slot));
            id.Pop();
        }

        foreach (var slot in BonusExtensions.AllFlags)
        {
            id.Push((int)slot.ToModelIndex());
            table.DrawColumn(slot.ToNameU8());
            if (!actor.IsCharacter)
            {
                table.DrawColumn("No Character"u8);
            }
            else
            {
                var glassesId = actor.GetBonusItem(slot);
                if (bonusItems.TryGetValue(glassesId, out var glasses))
                    table.DrawColumn($"{glasses.PrimaryId.Id},{glasses.Variant.Id} ({glassesId})");
                else
                    table.DrawColumn($"{glassesId}");
            }

            table.DrawColumn(model.IsHuman ? $"{model.GetBonus(slot)}" : "No Human"u8);
            table.NextColumn();
            if (Im.SmallButton("Change Piece"u8))
            {
                var data = model.GetBonus(slot);
                updateSlotService.UpdateBonusSlot(model, slot, data with { Variant = (Variant)((data.Variant.Id + 1) % 12) });
            }

            id.Pop();
        }
    }

    private void DrawCustomize(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id = Im.Id.Push("Customize"u8);
        var actorCustomize = actor.IsCharacter
            ? *(CustomizeArray*)&actor.AsCharacter->DrawData.CustomizeData
            : new CustomizeArray();
        var modelCustomize = model.IsHuman
            ? *(CustomizeArray*)&model.AsHuman->Customize
            : new CustomizeArray();
        foreach (var type in CustomizeIndex.Values)
        {
            id.Push((int)type);

            table.DrawColumn(type.ToNameU8());
            table.DrawColumn(actor.IsCharacter ? $"{actorCustomize[type].Value:X2}" : "No Character"u8);
            table.DrawColumn(model.IsHuman ? $"{modelCustomize[type].Value}" : "No Human"u8);
            table.NextColumn();
            if (!model.IsHuman || type.ToFlag().RequiresRedraw())
                continue;

            if (Im.SmallButton("++"u8))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value + (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }

            Im.Line.Same();
            if (Im.SmallButton("--"u8))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value - (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }

            Im.Line.Same();
            if (Im.SmallButton("Reset"u8))
            {
                modelCustomize.Set(type, actorCustomize[type]);
                changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }

            id.Pop();
        }
    }

    private void DrawCrests(in Im.TableDisposable table, Actor actor, Model model)
    {
        using var id              = Im.Id.Push("Crests"u8);
        CrestFlag whichToggle     = 0;
        CrestFlag totalModelFlags = 0;
        foreach (var crestFlag in CrestExtensions.AllRelevantSet)
        {
            id.Push((int)crestFlag);
            var modelCrest = CrestService.GetModelCrest(actor, crestFlag);
            if (modelCrest)
                totalModelFlags |= crestFlag;
            table.DrawColumn($"{crestFlag.ToLabel()} Crest");
            table.DrawColumn(actor.IsCharacter ? $"{actor.GetCrest(crestFlag)}" : "No Character"u8);
            table.DrawColumn($"{modelCrest}");

            table.NextColumn();
            if (model.IsHuman && Im.SmallButton("Toggle"u8))
                whichToggle = crestFlag;

            id.Pop();
        }

        if (whichToggle is not 0)
            crestService.UpdateCrests(actor, totalModelFlags ^ whichToggle);
    }
}
