using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Utility;
using ImSharp;
using Luna;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class HumanNpcCombo(DictBNpcNames bNpcNames, DictModelChara modelCharaDict, HumanModelList humans, DictBNpc bNpcs)
    : FilterComboBase<HumanNpcCombo.NpcCacheItem>(new NpcFilter())
{
    private NpcCacheItem _selection = new(string.Empty, ObjectKind.None, new HashSet<uint>());

    public NpcCacheItem Selection
        => _selection;

    public readonly struct NpcCacheItem(string name, ObjectKind kind, IReadOnlySet<uint> ids) : IComparable<NpcCacheItem>
    {
        public readonly StringPair         Name = new(name);
        public readonly ObjectKind         Kind = kind;
        public readonly IReadOnlySet<uint> Ids  = ids;

        /// <summary> Compare strings in a way that letters and numbers are sorted before any special symbols. </summary>
        public int CompareTo(NpcCacheItem other)
        {
            if (Name.Utf16.IsNullOrWhitespace() || other.Name.Utf16.IsNullOrWhitespace())
                return StringComparer.OrdinalIgnoreCase.Compare(Name.Utf16, other.Name.Utf16);

            var comp = (char.IsAsciiLetterOrDigit(Name.Utf16[0]), char.IsAsciiLetterOrDigit(other.Name.Utf16[0])) switch
            {
                (true, false) => -1,
                (false, true) => 1,
                _             => StringComparer.OrdinalIgnoreCase.Compare(Name.Utf16, other.Name.Utf16),
            };

            if (comp is not 0)
                return comp;

            return Comparer<ObjectKind>.Default.Compare(Kind, other.Kind);
        }
    }

    private sealed class NpcFilter : TextFilterBase<NpcCacheItem>
    {
        protected override string ToFilterString(in NpcCacheItem item, int globalIndex)
            => item.Name.Utf16;
    }

    protected override IEnumerable<NpcCacheItem> GetItems()
    {
        var bNpcDict = new SetDictionary<string, uint>(1024);
        var eNpcDict = new SetDictionary<string, uint>(1024);
        var bothSet  = new HashSet<string>(1024);
        for (var modelChara = 0u; modelChara < modelCharaDict.Count; ++modelChara)
        {
            if (!humans.IsHuman(modelChara))
                continue;

            var list = modelCharaDict[modelChara];
            if (list.Count is 0)
                continue;

            foreach (var (name, kind, id) in list.Where(t => !t.Name.IsNullOrWhitespace()))
            {
                string actualName;
                switch (kind)
                {
                    case ObjectKind.BattleNpc:
                        if (!bNpcNames.TryGetValue(id, out var nameIds))
                            continue;

                        foreach (var nameId in nameIds)
                        {
                            if (!bNpcs.TryGetValue(nameId, out var s))
                                continue;

                            if (bothSet.Contains(s))
                            {
                                actualName = $"{s} ({ObjectKind.BattleNpc.ToName()})";
                            }
                            else if (eNpcDict.Remove(s, out var values))
                            {
                                actualName = $"{s} ({ObjectKind.BattleNpc.ToName()})";
                                eNpcDict.TryAdd(actualName, values);
                                bothSet.Add(s);
                            }
                            else
                            {
                                actualName = s;
                            }

                            bNpcDict.TryAdd(actualName, nameId.Id);
                        }

                        break;
                    case ObjectKind.EventNpc:
                        if (bothSet.Contains(name))
                        {
                            actualName = $"{name} ({ObjectKind.EventNpc.ToName()})";
                        }
                        else if (bNpcDict.Remove(name, out var values))
                        {
                            actualName = $"{name} ({ObjectKind.EventNpc.ToName()})";
                            bNpcDict.TryAdd(actualName, values);
                            bothSet.Add(name);
                        }
                        else
                        {
                            actualName = name;
                        }

                        eNpcDict.TryAdd(actualName, id);
                        break;
                }
            }
        }

        return bNpcDict.Grouped.Select(p => new NpcCacheItem(p.Key, ObjectKind.BattleNpc, p.Value))
            .Concat(eNpcDict.Grouped.Select(p => new NpcCacheItem(p.Key, ObjectKind.EventNpc, p.Value)))
            .OrderBy(p => p);
    }


    protected override float ItemHeight
        => Im.Style.TextHeightWithSpacing;

    protected override bool DrawItem(in NpcCacheItem item, int globalIndex, bool selected)
    {
        var ret = Im.Selectable(item.Name.Utf8, selected);
        if (Im.Item.Hovered())
        {
            using var style = Im.Style.PushDefault();
            using var tt    = Im.Tooltip.Begin();
            foreach (var id in item.Ids)
                Im.Text($"{id}");
        }

        return ret;
    }

    public bool Draw(Utf8StringHandler<LabelStringHandlerBuffer> label, float width)
        => base.Draw(label, _selection.Kind is ObjectKind.None ? "Human Non-Player-Characters..."u8 : _selection.Name.Utf8, StringU8.Empty,
            width, out _selection);

    protected override bool IsSelected(NpcCacheItem item, int globalIndex)
        => item.Kind == _selection.Kind && item.Name.Utf16 == _selection.Name.Utf16;
}
