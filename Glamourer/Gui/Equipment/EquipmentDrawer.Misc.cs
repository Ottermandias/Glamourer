using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Glamourer.State;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Stain = Glamourer.Structs.Stain;

namespace Glamourer.Gui.Equipment;

public partial class EquipmentDrawer
{
    private static readonly IReadOnlyDictionary<StainId, Stain> Stains;
    private static readonly FilterStainCombo                    StainCombo;

    private sealed class FilterStainCombo : FilterComboBase<Stain>
    {
        private readonly float   _comboWidth;
        private          Vector2 _buttonSize;

        public FilterStainCombo(float comboWidth)
            : base(Stains.Values.ToArray(), false)
            => _comboWidth = comboWidth;

        protected override float GetFilterWidth()
            => _buttonSize.X + ImGui.GetStyle().ScrollbarSize;

        protected override void DrawList(float width, float itemHeight)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.WindowPadding, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            _buttonSize = new Vector2(_comboWidth * ImGuiHelpers.GlobalScale, 0);
            if (ImGui.GetScrollMaxY() > 0)
                _buttonSize.X += ImGui.GetStyle().ScrollbarSize;
            base.DrawList(width, itemHeight);
        }

        protected override string ToString(Stain obj)
            => obj.Name;

        protected override bool DrawSelectable(int globalIdx, bool selected)
        {
            var stain = Items[globalIdx];
            // Push the stain color to type and if it is too bright, turn the text color black.
            using var colors = ImRaii.PushColor(ImGuiCol.Button, stain.RgbaColor)
                .Push(ImGuiCol.Text,   0xFF101010, stain.Intensity > 127)
                .Push(ImGuiCol.Border, 0xFF2020D0, selected);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2f * ImGuiHelpers.GlobalScale, selected);
            return ImGui.Button(stain.Name, _buttonSize);
        }
    }

    private void DrawStainSelector()
    {
        var       foundIdx = StainCombo.Items.IndexOf(s => s.RowIndex.Equals(_currentArmor.Stain));
        var       stain    = foundIdx >= 0 ? StainCombo.Items[foundIdx] : default;
        using var color    = ImRaii.PushColor(ImGuiCol.FrameBg, stain.RgbaColor, foundIdx >= 0);
        var change = StainCombo.Draw("##stainSelector", string.Empty, ref foundIdx, ImGui.GetFrameHeight(), ImGui.GetFrameHeight(),
            ImGuiComboFlags.NoArrowButton);
        if (!change && (byte)_currentArmor.Stain != 0)
        {
            ImGuiUtil.HoverTooltip($"{stain.Name}\nRight-click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                change   = true;
                foundIdx = -1;
            }
        }

        if (change)
        {
            _currentArmor = new CharacterArmor(_currentArmor.Set, _currentArmor.Variant,
                foundIdx >= 0 ? StainCombo.Items[foundIdx].RowIndex : Stain.None.RowIndex);
            UpdateActors();
        }
    }

    private void DrawCheckbox(ref ApplicationFlags flags)
        => DrawCheckbox("##checkbox", "Enable writing this slot in this save.", ref flags, _currentSlot.ToApplicationFlag());

    private static void DrawCheckbox(string label, string tooltip, ref ApplicationFlags flags, ApplicationFlags flag)
    {
        var tmp = (uint)flags;
        if (ImGui.CheckboxFlags(label, ref tmp, (uint)flag))
            flags = (ApplicationFlags)tmp;

        ImGuiUtil.HoverTooltip(tooltip);
    }
}
