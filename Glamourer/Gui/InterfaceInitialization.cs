using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Lumina.Excel.GeneratedSheets;

namespace Glamourer.Gui
{
    internal partial class Interface
    {
        private const float ColorButtonWidth = 22.5f;
        private const float ColorComboWidth  = 140f;
        private const float ItemComboWidth   = 350f;

        private static readonly Vector4 GreyVector = new(0.5f, 0.5f, 0.5f, 1);

        private static ComboWithFilter<Stain> CreateDefaultStainCombo(IReadOnlyList<Stain> stains)
            => new("##StainCombo", ColorComboWidth, ColorButtonWidth, stains,
                s => s.Name.ToString())
            {
                Flags = ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.HeightLarge,
                PreList = () =>
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,   Vector2.Zero);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                },
                PostList = () => { ImGui.PopStyleVar(3); },
                CreateSelectable = s =>
                {
                    var push = PushColor(s);
                    var ret = ImGui.Button($"{s.Name}##Stain{(byte) s.RowIndex}",
                        Vector2.UnitX * (ColorComboWidth - ImGui.GetStyle().ScrollbarSize));
                    ImGui.PopStyleColor(push);
                    return ret;
                },
                ItemsAtOnce = 12,
            };

        private ComboWithFilter<Item> CreateItemCombo(EquipSlot slot, IReadOnlyList<Item> items)
            => new($"{_equipSlotNames[slot]}##Equip", ItemComboWidth, ItemComboWidth, items, i => i.Name)
            {
                Flags = ImGuiComboFlags.HeightLarge,
                CreateSelectable = i =>
                {
                    var ret   = ImGui.Selectable(i.Name);
                    var setId = $"({(int) i.MainModel.id})";
                    var size  = ImGui.CalcTextSize(setId).X;
                    ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - size - ImGui.GetStyle().ItemInnerSpacing.X);
                    ImGui.TextColored(GreyVector, setId);
                    return ret;
                },
            };

        private (ComboWithFilter<Item>, ComboWithFilter<Stain>) CreateCombos(EquipSlot slot, IReadOnlyList<Item> items,
            ComboWithFilter<Stain> defaultStain)
            => (CreateItemCombo(slot, items), new ComboWithFilter<Stain>($"##{slot}Stain", defaultStain));

        private static ImGuiScene.TextureWrap? GetLegacyTattooIcon()
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamourer.LegacyTattoo.raw");
            if (resource != null)
            {
                var rawImage = new byte[resource.Length];
                resource.Read(rawImage, 0, (int) resource.Length);
                return Dalamud.PluginInterface.UiBuilder.LoadImageRaw(rawImage, 192, 192, 4);
            }

            return null;
        }

        private static Dictionary<EquipSlot, string> GetEquipSlotNames()
        {
            var sheet = Dalamud.GameData.GetExcelSheet<Addon>()!;
            var ret = new Dictionary<EquipSlot, string>(12)
            {
                [EquipSlot.MainHand] = sheet.GetRow(738)?.Text.ToString() ?? "Main Hand",
                [EquipSlot.OffHand]  = sheet.GetRow(739)?.Text.ToString() ?? "Off Hand",
                [EquipSlot.Head]     = sheet.GetRow(740)?.Text.ToString() ?? "Head",
                [EquipSlot.Body]     = sheet.GetRow(741)?.Text.ToString() ?? "Body",
                [EquipSlot.Hands]    = sheet.GetRow(742)?.Text.ToString() ?? "Hands",
                [EquipSlot.Legs]     = sheet.GetRow(744)?.Text.ToString() ?? "Legs",
                [EquipSlot.Feet]     = sheet.GetRow(745)?.Text.ToString() ?? "Feet",
                [EquipSlot.Ears]     = sheet.GetRow(746)?.Text.ToString() ?? "Ears",
                [EquipSlot.Neck]     = sheet.GetRow(747)?.Text.ToString() ?? "Neck",
                [EquipSlot.Wrists]   = sheet.GetRow(748)?.Text.ToString() ?? "Wrists",
                [EquipSlot.RFinger]  = sheet.GetRow(749)?.Text.ToString() ?? "Right Ring",
                [EquipSlot.LFinger]  = sheet.GetRow(750)?.Text.ToString() ?? "Left Ring",
            };
            return ret;
        }
    }
}
