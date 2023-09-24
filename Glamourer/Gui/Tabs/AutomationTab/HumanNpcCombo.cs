using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Utility;
using Glamourer.Services;
using ImGuiNET;
using OtterGui.Custom;
using OtterGui.Widgets;
using Penumbra.GameData.Data;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class HumanNpcCombo : FilterComboCache<(string Name, ObjectKind Kind, uint[] Ids)>
{
    private readonly string _label;

    public HumanNpcCombo(string label, IdentifierService service, HumanModelList humans)
        : base(() => CreateList(service, humans))
        => _label = label;

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
        => Draw(_label, CurrentSelection.Name.IsNullOrEmpty() ? "Human Non-Player-Characters..." : CurrentSelection.Name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());


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

    private static IReadOnlyList<(string Name, ObjectKind Kind, uint[] Ids)> CreateList(IdentifierService service, HumanModelList humans)
    {
        var ret = new List<(string Name, ObjectKind Kind, uint Id)>(1024);
        for (var modelChara = 0u; modelChara < service.AwaitedService.NumModelChara; ++modelChara)
        {
            if (!humans.IsHuman(modelChara))
                continue;

            var list = service.AwaitedService.ModelCharaNames(modelChara);
            if (list.Count == 0)
                continue;

            foreach (var (name, kind, id) in list.Where(t => !t.Name.IsNullOrWhitespace()))
            {
                switch (kind)
                {
                    case ObjectKind.BattleNpc:
                        var nameIds = service.AwaitedService.GetBnpcNames(id);
                        ret.AddRange(nameIds.Select(nameId => (service.AwaitedService.Name(ObjectKind.BattleNpc, nameId), kind, nameId.Id)));
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
