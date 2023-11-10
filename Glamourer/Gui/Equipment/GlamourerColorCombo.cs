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
using Penumbra.GameData.Data;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui.Equipment;

public sealed class GlamourerColorCombo : FilterComboColors
{
    private readonly FavoriteManager _favorites;

    public GlamourerColorCombo(float comboWidth, StainData stains, FavoriteManager favorites)
        : base(comboWidth, CreateFunc(stains, favorites), Glamourer.Log)
        => _favorites = favorites;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using (var space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 0)))
        {
            if (globalIdx == 0)
            {
                using var font = ImRaii.PushFont(UiBuilder.IconFont);
                ImGui.Dummy(ImGui.CalcTextSize(FontAwesomeIcon.Star.ToIconString()));
            }
            else
            {
                UiHelpers.DrawFavoriteStar(_favorites, (StainId)Items[globalIdx].Key);
            }

            ImGui.SameLine();
        }

        var       buttonWidth = ImGui.GetContentRegionAvail().X;
        var       totalWidth  = ImGui.GetContentRegionMax().X;
        using var style       = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(buttonWidth / 2 / totalWidth, 0.5f));

        return base.DrawSelectable(globalIdx, selected);
    }

    private static Func<IReadOnlyList<KeyValuePair<byte, (string Name, uint Color, bool Gloss)>>> CreateFunc(StainData stains,
        FavoriteManager favorites)
        => () => stains.Data.Select(kvp => (kvp, favorites.Contains((StainId)kvp.Key))).OrderBy(p => !p.Item2).Select(p => p.kvp)
            .Prepend(new KeyValuePair<byte, (string Name, uint Dye, bool Gloss)>(0, ("None", 0, false))).ToList();
}
