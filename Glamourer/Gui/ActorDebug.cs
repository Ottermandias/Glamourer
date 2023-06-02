using Glamourer.Designs;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Enums;

namespace Glamourer.Gui;

public static class ActorDebug
{
    /// <summary> Draw the model data values as straight table data without evaluation. </summary>
    public static unsafe void Draw(in ModelData model)
    {
        using var table = ImRaii.Table("##drawObjectData", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGuiUtil.DrawTableColumn("Model ID");
        ImGuiUtil.DrawTableColumn(model.ModelId.ToString());
        ImGuiUtil.DrawTableColumn(model.ModelId.ToString("X8"));

        for (var i = 0; i < Penumbra.GameData.Structs.CustomizeData.Size; ++i)
        {
            ImGuiUtil.DrawTableColumn($"Customize[{i:D2}]");
            ImGuiUtil.DrawTableColumn(model.Customize.Data.Data[i].ToString());
            ImGuiUtil.DrawTableColumn(model.Customize.Data.Data[i].ToString("X2"));
        }
        ImGuiUtil.DrawTableColumn("Race");
        ImGuiUtil.DrawTableColumn(model.Customize.Race.ToString());
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Clan");
        ImGuiUtil.DrawTableColumn(model.Customize.Clan.ToString());
        ImGui.TableNextColumn();

        ImGuiUtil.DrawTableColumn("Gender");
        ImGuiUtil.DrawTableColumn(model.Customize.Gender.ToString());
        ImGui.TableNextColumn();

        for (var i = 0; i < 10; ++i)
        {
            var slot = EquipSlotExtensions.EqdpSlots[i];
            ImGuiUtil.DrawTableColumn($"Equipment[{i}] ({slot})");
            var armor = model.Armor(slot);
            ImGuiUtil.DrawTableColumn($"{armor.Set.Value}, {armor.Variant}, {armor.Stain.Value}");
            ImGuiUtil.DrawTableColumn(armor.Value.ToString("X8"));
        }

        ImGuiUtil.DrawTableColumn("Mainhand");
        ImGuiUtil.DrawTableColumn($"{model.MainHand.Set.Value}, {model.MainHand.Type.Value}, {model.MainHand.Variant}, {model.MainHand.Stain.Value}");
        ImGuiUtil.DrawTableColumn(model.MainHand.Value.ToString("X16"));

        ImGuiUtil.DrawTableColumn("Offhand");
        ImGuiUtil.DrawTableColumn($"{model.OffHand.Set.Value}, {model.OffHand.Type.Value}, {model.OffHand.Variant}, {model.OffHand.Stain.Value}");
        ImGuiUtil.DrawTableColumn(model.OffHand.Value.ToString("X16"));
    }
}
