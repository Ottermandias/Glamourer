using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Glamourer.Unlocks;
using ImGuiNET;
using OtterGui.Widgets;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class GlamourerColorCombo(float _comboWidth, DictStain _stains, FavoriteManager _favorites)
    : FilterComboColors(_comboWidth, CreateFunc(_stains, _favorites), Glamourer.Log)
{
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using (var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        {
            if (globalIdx == 0)
            {
                using var font = ImRaii.PushFont(UiBuilder.IconFont);
                ImGui.Dummy(ImGui.CalcTextSize(FontAwesomeIcon.Star.ToIconString()));
            }
            else
            {
                UiHelpers.DrawFavoriteStar(_favorites, Items[globalIdx].Key);
            }

            ImGui.SameLine();
        }

        var       buttonWidth = ImGui.GetContentRegionAvail().X;
        var       totalWidth  = ImGui.GetContentRegionMax().X;
        using var style       = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(buttonWidth / 2 / totalWidth, 0.5f));

        return base.DrawSelectable(globalIdx, selected);
    }

    private static Func<IReadOnlyList<KeyValuePair<byte, (string Name, uint Color, bool Gloss)>>> CreateFunc(DictStain stains,
        FavoriteManager favorites)
        => () => stains.Select(kvp => (kvp, favorites.Contains(kvp.Key))).OrderBy(p => !p.Item2).Select(p => p.kvp)
            .Prepend(new KeyValuePair<StainId, Stain>(Stain.None.RowIndex, Stain.None)).Select(kvp
                => new KeyValuePair<byte, (string, uint, bool)>(kvp.Key.Id, (kvp.Value.Name, kvp.Value.RgbaColor, kvp.Value.Gloss))).ToList();
}
