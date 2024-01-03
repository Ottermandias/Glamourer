using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Custom;
using OtterGui.Log;
using OtterGui.Widgets;
using Penumbra.GameData.DataContainers;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class HumanNpcCombo(
    string label,
    DictModelChara modelCharaDict,
    DictBNpcNames bNpcNames,
    DictBNpc bNpcs,
    HumanModelList humans,
    Logger log)
    : FilterComboCache<(string Name, ObjectKind Kind, uint[] Ids)>(() => CreateList(modelCharaDict, bNpcNames, bNpcs, humans), log)
{
    protected override string ToString((string Name, ObjectKind Kind, uint[] Ids) obj)
        => obj.Name;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (name, kind, ids) = Items[globalIdx];
        if (globalIdx > 0 && Items[globalIdx - 1].Name == name || globalIdx + 1 < Items.Count && Items[globalIdx + 1].Name == name)
            name = $"{name} ({kind.ToName()})";
        var ret = ImGui.Selectable(name, selected);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(string.Join('\n', ids.Select(i => i.ToString())));

        return ret;
    }

    public bool Draw(float width)
        => Draw(label, CurrentSelection.Name.IsNullOrEmpty() ? "Human Non-Player-Characters..." : CurrentSelection.Name, string.Empty, width,
            ImGui.GetTextLineHeightWithSpacing());


    /// <summary> Compare strings in a way that letters and numbers are sorted before any special symbols. </summary>
    private class NameComparer : IComparer<(string, ObjectKind)>
    {
        public int Compare((string, ObjectKind) x, (string, ObjectKind) y)
        {
            if (x.Item1.IsNullOrWhitespace() || y.Item1.IsNullOrWhitespace())
                return StringComparer.OrdinalIgnoreCase.Compare(x.Item1, y.Item1);

            var comp = (char.IsAsciiLetterOrDigit(x.Item1[0]), char.IsAsciiLetterOrDigit(y.Item1[0])) switch
            {
                (true, false) => -1,
                (false, true) => 1,
                _             => StringComparer.OrdinalIgnoreCase.Compare(x.Item1, y.Item1),
            };

            if (comp != 0)
                return comp;

            return Comparer<ObjectKind>.Default.Compare(x.Item2, y.Item2);
        }
    }

    private static IReadOnlyList<(string Name, ObjectKind Kind, uint[] Ids)> CreateList(DictModelChara modelCharaDict, DictBNpcNames bNpcNames,
        DictBNpc bNpcs, HumanModelList humans)
    {
        var ret = new List<(string Name, ObjectKind Kind, uint Id)>(1024);
        for (var modelChara = 0u; modelChara < modelCharaDict.Count; ++modelChara)
        {
            if (!humans.IsHuman(modelChara))
                continue;

            var list = modelCharaDict[modelChara];
            if (list.Count == 0)
                continue;

            foreach (var (name, kind, id) in list.Where(t => !t.Name.IsNullOrWhitespace()))
            {
                switch (kind)
                {
                    case ObjectKind.BattleNpc:
                        if (!bNpcNames.TryGetValue(id, out var nameIds))
                            continue;

                        ret.AddRange(nameIds.SelectWhere(nameId => (bNpcs.TryGetValue(nameId, out var s), (s!, kind, nameId.Id))));
                        break;
                    case ObjectKind.EventNpc:
                        ret.Add((name, kind, id));
                        break;
                }
            }
        }

        return ret.GroupBy(t => (t.Name, t.Kind))
            .OrderBy(g => g.Key, Comparer)
            .Select(g => (g.Key.Name, g.Key.Kind, g.Select(g => g.Id).Distinct().ToArray()))
            .ToList();
    }

    private static readonly NameComparer Comparer = new();
}
