using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.GameData;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui.Debug;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using ObjectManager = Glamourer.Interop.ObjectManager;

namespace Glamourer.Gui.Tabs.DebugTab;

public unsafe class ModelEvaluationPanel(
    ObjectManager _objectManager,
    VisorService _visorService,
    UpdateSlotService _updateSlotService,
    ChangeCustomizeService _changeCustomizeService,
    CrestService _crestService) : IGameDataDrawer
{
    public string Label
        => "Model Evaluation";

    public bool Disabled
        => false;

    private int _gameObjectIndex;

    public void Draw()
    {
        ImGui.InputInt("Game Object Index", ref _gameObjectIndex, 0, 0);
        var       actor = _objectManager[_gameObjectIndex];
        var       model = actor.Model;
        using var table = ImRaii.Table("##evaluationTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TableHeader("Actor");
        ImGui.TableNextColumn();
        ImGui.TableHeader("Model");
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Address");
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(actor.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(model.ToString());
        ImGui.TableNextColumn();
        if (actor.IsCharacter)
        {
            ImGui.TextUnformatted(actor.AsCharacter->CharacterData.ModelCharaId.ToString());
            if (actor.AsCharacter->CharacterData.TransformationId != 0)
                ImGui.TextUnformatted($"Transformation Id: {actor.AsCharacter->CharacterData.TransformationId}");
            if (actor.AsCharacter->CharacterData.ModelCharaId_2 != -1)
                ImGui.TextUnformatted($"ModelChara2 {actor.AsCharacter->CharacterData.ModelCharaId_2}");
        }

        ImGuiUtil.DrawTableColumn("Mainhand");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetMainhand().ToString() : "No Character");

        var (mainhand, offhand, mainModel, offModel) = model.GetWeapons(actor);
        ImGuiUtil.DrawTableColumn(mainModel.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(mainhand.ToString());

        ImGuiUtil.DrawTableColumn("Offhand");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetOffhand().ToString() : "No Character");
        ImGuiUtil.DrawTableColumn(offModel.ToString());
        ImGui.TableNextColumn();
        ImGuiUtil.CopyOnClickSelectable(offhand.ToString());

        DrawVisor(actor, model);
        DrawHatState(actor, model);
        DrawWeaponState(actor, model);
        DrawWetness(actor, model);
        DrawEquip(actor, model);
        DrawCustomize(actor, model);
        DrawCrests(actor, model);
        DrawParameters(actor, model);

        ImGuiUtil.DrawTableColumn("Scale");
        ImGuiUtil.DrawTableColumn(actor.Valid ? actor.AsObject->Scale.ToString(CultureInfo.InvariantCulture) : "No Character");
        ImGuiUtil.DrawTableColumn(model.Valid ? model.AsDrawObject->Object.Scale.ToString() : "No Model");
        ImGuiUtil.DrawTableColumn(model.IsCharacterBase ? $"{*(float*)(model.Address + 0x270)} {*(float*)(model.Address + 0x274)}" : "No CharacterBase");
    }

    private void DrawParameters(Actor actor, Model model)
    {
        if (!model.IsHuman)
            return;

        var convert = model.GetParameterData();
        foreach (var flag in CustomizeParameterExtensions.AllFlags)
        {
            ImGuiUtil.DrawTableColumn(flag.ToString());
            ImGuiUtil.DrawTableColumn(string.Empty);
            ImGuiUtil.DrawTableColumn(convert[flag].InternalQuadruple.ToString());
            ImGui.TableNextColumn();
        }
    }

    private void DrawVisor(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Visor");
        ImGuiUtil.DrawTableColumn("Visor State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.AsCharacter->DrawData.IsVisorToggled.ToString() : "No Character");
        ImGuiUtil.DrawTableColumn(model.IsHuman ? VisorService.GetVisorState(model).ToString() : "No Human");
        ImGui.TableNextColumn();
        if (!model.IsHuman)
            return;

        if (ImGui.SmallButton("Set True"))
            _visorService.SetVisorState(model, true);
        ImGui.SameLine();
        if (ImGui.SmallButton("Set False"))
            _visorService.SetVisorState(model, false);
        ImGui.SameLine();
        if (ImGui.SmallButton("Toggle"))
            _visorService.SetVisorState(model, !VisorService.GetVisorState(model));
    }

    private void DrawHatState(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("HatState");
        ImGuiUtil.DrawTableColumn("Hat State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsHatHidden ? "Hidden" : actor.GetArmor(EquipSlot.Head).ToString()
            : "No Character");
        ImGuiUtil.DrawTableColumn(model.IsHuman
            ? model.AsHuman->Head.Value == 0 ? "No Hat" : model.GetArmor(EquipSlot.Head).ToString()
            : "No Human");
        ImGui.TableNextColumn();
        if (!model.IsHuman)
            return;

        if (ImGui.SmallButton("Hide"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, CharacterArmor.Empty);
        ImGui.SameLine();
        if (ImGui.SmallButton("Show"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head, actor.GetArmor(EquipSlot.Head));
        ImGui.SameLine();
        if (ImGui.SmallButton("Toggle"))
            _updateSlotService.UpdateSlot(model, EquipSlot.Head,
                model.AsHuman->Head.Value == 0 ? actor.GetArmor(EquipSlot.Head) : CharacterArmor.Empty);
    }

    private static void DrawWeaponState(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("WeaponState");
        ImGuiUtil.DrawTableColumn("Weapon State");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter
            ? actor.AsCharacter->DrawData.IsWeaponHidden ? "Hidden" : "Visible"
            : "No Character");
        string text;
        if (!model.IsHuman)
        {
            text = "No Model";
        }
        else if (model.AsDrawObject->Object.ChildObject == null)
        {
            text = "No Weapon";
        }
        else
        {
            var weapon = (DrawObject*)model.AsDrawObject->Object.ChildObject;
            text = (weapon->Flags & 0x09) == 0x09 ? "Visible" : "Hidden";
        }

        ImGuiUtil.DrawTableColumn(text);
        ImGui.TableNextColumn();
    }

    private static void DrawWetness(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Wetness");
        ImGuiUtil.DrawTableColumn("Wetness");
        ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.AsCharacter->IsGPoseWet ? "GPose" : "None" : "No Character");
        var modelString = model.IsCharacterBase
            ? $"{model.AsCharacterBase->SwimmingWetness:F4} Swimming\n"
          + $"{model.AsCharacterBase->WeatherWetness:F4} Weather\n"
          + $"{model.AsCharacterBase->ForcedWetness:F4} Forced\n"
          + $"{model.AsCharacterBase->WetnessDepth:F4} Depth\n"
            : "No CharacterBase";
        ImGuiUtil.DrawTableColumn(modelString);
        ImGui.TableNextColumn();
        if (!actor.IsCharacter)
            return;

        if (ImGui.SmallButton("GPose On"))
            actor.AsCharacter->IsGPoseWet = true;
        ImGui.SameLine();
        if (ImGui.SmallButton("GPose Off"))
            actor.AsCharacter->IsGPoseWet = false;
        ImGui.SameLine();
        if (ImGui.SmallButton("GPose Toggle"))
            actor.AsCharacter->IsGPoseWet = !actor.AsCharacter->IsGPoseWet;
    }

    private void DrawEquip(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Equipment");
        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            using var id2 = ImRaii.PushId((int)slot);
            ImGuiUtil.DrawTableColumn(slot.ToName());
            ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetArmor(slot).ToString() : "No Character");
            ImGuiUtil.DrawTableColumn(model.IsHuman ? model.GetArmor(slot).ToString() : "No Human");
            ImGui.TableNextColumn();
            if (!model.IsHuman)
                continue;

            if (ImGui.SmallButton("Change Piece"))
                _updateSlotService.UpdateArmor(model, slot,
                    new CharacterArmor((PrimaryId)(slot == EquipSlot.Hands ? 6064 : slot == EquipSlot.Head ? 6072 : 1), 1, 0));
            ImGui.SameLine();
            if (ImGui.SmallButton("Change Stain"))
                _updateSlotService.UpdateStain(model, slot, 5);
            ImGui.SameLine();
            if (ImGui.SmallButton("Reset"))
                _updateSlotService.UpdateSlot(model, slot, actor.GetArmor(slot));
        }
    }

    private void DrawCustomize(Actor actor, Model model)
    {
        using var id = ImRaii.PushId("Customize");
        var actorCustomize = actor.IsCharacter
            ? *(CustomizeArray*)&actor.AsCharacter->DrawData.CustomizeData
            : new CustomizeArray();
        var modelCustomize = model.IsHuman
            ? *(CustomizeArray*)model.AsHuman->Customize.Data
            : new CustomizeArray();
        foreach (var type in Enum.GetValues<CustomizeIndex>())
        {
            using var id2 = ImRaii.PushId((int)type);
            ImGuiUtil.DrawTableColumn(type.ToDefaultName());
            ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actorCustomize[type].Value.ToString("X2") : "No Character");
            ImGuiUtil.DrawTableColumn(model.IsHuman ? modelCustomize[type].Value.ToString("X2") : "No Human");
            ImGui.TableNextColumn();
            if (!model.IsHuman || type.ToFlag().RequiresRedraw())
                continue;

            if (ImGui.SmallButton("++"))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value + (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("--"))
            {
                var value = modelCustomize[type].Value;
                var (_, mask) = type.ToByteAndMask();
                var shift    = BitOperations.TrailingZeroCount(mask);
                var newValue = value - (1 << shift);
                modelCustomize.Set(type, (CustomizeValue)newValue);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("Reset"))
            {
                modelCustomize.Set(type, actorCustomize[type]);
                _changeCustomizeService.UpdateCustomize(model, modelCustomize);
            }
        }
    }

    private void DrawCrests(Actor actor, Model model)
    {
        using var id              = ImRaii.PushId("Crests");
        CrestFlag whichToggle     = 0;
        CrestFlag totalModelFlags = 0;
        foreach (var crestFlag in CrestExtensions.AllRelevantSet)
        {
            id.Push((int)crestFlag);
            var modelCrest = CrestService.GetModelCrest(actor, crestFlag);
            if (modelCrest)
                totalModelFlags |= crestFlag;
            ImGuiUtil.DrawTableColumn($"{crestFlag.ToLabel()} Crest");
            ImGuiUtil.DrawTableColumn(actor.IsCharacter ? actor.GetCrest(crestFlag).ToString() : "No Character");
            ImGuiUtil.DrawTableColumn(modelCrest.ToString());

            ImGui.TableNextColumn();
            if (model.IsHuman && ImGui.SmallButton("Toggle"))
                whichToggle = crestFlag;

            id.Pop();
        }

        if (whichToggle != 0)
            _crestService.UpdateCrests(actor, totalModelFlags ^ whichToggle);
    }
}
