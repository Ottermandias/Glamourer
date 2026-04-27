using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Glamourer.Api.Enums;
using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Designs.Special;
using Glamourer.Events;
using Glamourer.GameData;
using Glamourer.Gui.Tabs.AutomationTab;
using Glamourer.Interop.Material;
using Glamourer.Services;
using Glamourer.State;
using ImSharp;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Automation;

public sealed class AutomationTestCache : BasicCache, IReadOnlyList<AutomationTestCache.Change>
{
    private static readonly StringU8 False               = new("False"u8);
    private static readonly StringU8 True                = new("True"u8);
    private static readonly StringU8 UnchangedAttributes = new("All Unchanged Attributes"u8);
    private static readonly StringU8 AllModAssociations  = new("All Mod Associations"u8);
    private static readonly StringU8 AllAdvancedDyes     = new("Advanced Dyes"u8);
    private static readonly StringU8 ResetToGame         = new("Reset to Game State"u8);
    private static readonly StringU8 RedrawCharacter     = new("Redraw Character"u8);
    private static readonly StringU8 Forced              = new("Forced"u8);
    private static readonly StringU8 Removed             = new("Removed"u8);
    private static readonly StringU8 Inherited           = new("Inherited"u8);
    private static readonly StringU8 Enabled             = new("Enabled"u8);
    private static readonly StringU8 Disabled            = new("Disabled"u8);
    private static readonly StringU8 Random              = new("Randomly Selected"u8);
    private static readonly StringU8 SetConfiguration    = new("Set Configuration"u8);
    private static readonly StringU8 Legacy              = new("Legacy Value"u8);
    private static readonly StringU8 Dawntrail           = new("Dawntrail Value"u8);

    private readonly AutomationChanged   _automationChanged;
    private readonly AutomationSelection _selection;
    private readonly Configuration       _config;
    private readonly ItemManager         _items;

    private readonly List<Change>                _changes   = [];
    private readonly HashSet<StateIndex>         _state     = [];
    private readonly HashSet<MaterialValueIndex> _materials = [];
    private          bool                        _resetsAssociations;
    private          bool                        _resetsAdvanced;
    private          bool                        _forcesRedraw;

    public readonly struct Change(ulong type)
    {
        public Change(ulong type, Design? design)
            : this(type)
        {
            Design.SetTarget(design!);
        }

        public Change(StateIndex type, Design? design = null)
            : this(0x0000000100000000u | (uint)type.Value, design)
        { }

        public Change(MaterialValueIndex type, Design? design = null)
            : this(0x0000000200000000u | type.Key, design)
        { }

        public readonly ulong                 Type = type;
        public          StringU8              Slot   { get; init; }
        public          StringU8              Target { get; init; }
        public          StringU8              Source { get; init; }
        public readonly WeakReference<Design> Design = new(null!);


        public static Change CreateGameReset()
            => new(0u)
            {
                Slot   = UnchangedAttributes,
                Target = ResetToGame,
                Source = SetConfiguration,
            };

        public static Change CreateAssociationReset(StringU8? designName, Design? design)
            => new(3u, design)
            {
                Slot   = AllModAssociations,
                Target = Removed,
                Source = designName ?? SetConfiguration,
            };

        public static Change CreateForcedRedraw(StringU8 designName, Design? design)
            => new(4u, design)
            {
                Slot   = RedrawCharacter,
                Target = Forced,
                Source = designName,
            };

        public static Change CreateAdvancedReset(StringU8 designName, Design? design)
            => new(2u, design)
            {
                Slot   = AllAdvancedDyes,
                Target = ResetToGame,
                Source = designName,
            };
    }

    public JobId Job
    {
        get;
        set
        {
            if (field == value)
                return;

            field =  value;
            Dirty |= IManagedCache.DirtyFlags.Custom;
        }
    }

    public short GearSet
    {
        get;
        set
        {
            if (field == value)
                return;

            field =  value;
            Dirty |= IManagedCache.DirtyFlags.Custom;
        }
    }

    public AutomationTestCache(AutomationChanged automationChanged, AutomationSelection selection, Configuration config, ItemManager items,
        short gearSet, JobId job)
    {
        _automationChanged          =  automationChanged;
        _selection                  =  selection;
        _config                     =  config;
        _items                      =  items;
        Job                         =  job;
        GearSet                     =  gearSet;
        _selection.SelectionChanged += OnSelectionChanged;
        _automationChanged.Subscribe(OnAutomationChanged, AutomationChanged.Priority.AutoDesignCache);
    }

    protected override void Dispose(bool disposing)
    {
        _selection.SelectionChanged -= OnSelectionChanged;
        _automationChanged.Unsubscribe(OnAutomationChanged);
    }

    private void OnAutomationChanged(in AutomationChanged.Arguments arguments)
        => Dirty |= IManagedCache.DirtyFlags.Custom;

    private void OnSelectionChanged()
        => Dirty |= IManagedCache.DirtyFlags.Custom;

    private static StringU8 GetTarget(IDesignStandIn design, Func<StringU8> generator)
        => design switch
        {
            RevertDesign => ResetToGame,
            RandomDesign => Random,
            _            => generator.Invoke(),
        };

    public override void Update()
    {
        if (!CustomDirty)
            return;

        Dirty &= ~IManagedCache.DirtyFlags.Custom;
        _changes.Clear();
        _state.Clear();
        _materials.Clear();
        _resetsAdvanced     = false;
        _resetsAssociations = false;
        _forcesRedraw       = false;
        if (_selection.Set is not { } set)
            return;

        if (!set.Enabled)
            return;

        if (set.BaseState is AutoDesignSet.Base.Game)
            _changes.Add(Change.CreateGameReset());

        if (set.ResetTemporarySettings)
        {
            _changes.Add(Change.CreateAssociationReset(null, null));
            _resetsAssociations = true;
        }

        foreach (var design in set.Designs)
        {
            if (!design.Conditions.Match(Job, GearSet))
                continue;

            var designName = new StringU8(design.Design.ResolveName(_config.Ephemeral.IncognitoMode));
            foreach (var (link, flags, _) in design.Design.AllLinks(true, cond => cond.Match(Job, GearSet)))
            {
                var application = (design.Type & flags).ApplyWhat(link);
                if (!_resetsAssociations && link.ResetTemporarySettings)
                {
                    _changes.Add(Change.CreateAssociationReset(designName, link as Design));
                    _resetsAssociations = true;
                }

                if (!_forcesRedraw && link.ForcedRedraw)
                {
                    _changes.Add(Change.CreateForcedRedraw(designName, link as Design));
                    _forcesRedraw = true;
                }

                if (!_resetsAdvanced && link.ResetAdvancedDyes)
                {
                    _changes.Add(Change.CreateAdvancedReset(designName, link as Design));
                    _resetsAdvanced = true;
                }

                var data = link.GetDesignData(default);
                foreach (var slot in EquipSlotExtensions.FullSlots)
                {
                    var gearFlag = slot.ToFlag();
                    if (application.Equip.HasFlag(gearFlag) && _state.Add(gearFlag))
                        _changes.Add(new Change(gearFlag, link as Design)
                        {
                            Slot   = slot.ToNameU8(),
                            Source = designName,
                            Target = GetTarget(link, () => new StringU8(data.Item(slot).Name)),
                        });
                }

                foreach (var slot in EquipSlotExtensions.FullSlots)
                {
                    var stainFlag = slot.ToStainFlag();
                    if (!application.Equip.HasFlag(stainFlag) || !_state.Add(stainFlag))
                        continue;

                    var stains = data.Stain(slot);
                    _changes.Add(new Change(stainFlag, link as Design)
                    {
                        Slot   = new StringU8($"{slot.ToNameU8()} (Stains)"),
                        Source = designName,
                        Target = GetTarget(link, () => (_items.Stains.TryGetValue(stains.Stain1, out var s1),
                                _items.Stains.TryGetValue(stains.Stain2,                         out var s2)) switch
                            {
                                (true, true)  => new StringU8($"{s1.Name}, {s2.Name}"),
                                (true, false) => new StringU8($"{s1.Name}, {(stains.Stain2.Id is 0 ? "Nothing" : stains.Stain2)}"),
                                (false, true) => new StringU8($"{(stains.Stain1.Id is 0 ? "Nothing" : stains.Stain1)}, {s2.Name}"),
                                (false, false) => new StringU8(
                                    $"{(stains.Stain1.Id is 0 ? "Nothing" : stains.Stain1)}, {(stains.Stain2.Id is 0 ? "Nothing" : stains.Stain2)}"),
                            }),
                    });
                }

                foreach (var slot in BonusExtensions.AllFlags)
                {
                    if (application.BonusItem.HasFlag(slot) && _state.Add(slot))
                        _changes.Add(new Change(slot, link as Design)
                        {
                            Slot   = slot.ToNameU8(),
                            Source = designName,
                            Target = GetTarget(link, () => new StringU8(data.BonusItem(slot).Name)),
                        });
                }

                foreach (var customize in CustomizationExtensions.AllBasic)
                {
                    var flag = customize.ToFlag();
                    if (application.Customize.HasFlag(flag) && _state.Add(customize))
                        _changes.Add(new Change(customize, link as Design)
                        {
                            Slot   = customize.ToNameU8(),
                            Source = designName,
                            Target = GetTarget(link, () => customize switch
                            {
                                CustomizeIndex.Race   => data.Customize.Race.ToNameU8(),
                                CustomizeIndex.Clan   => data.Customize.Clan.ToNameU8(),
                                CustomizeIndex.Gender => data.Customize.Gender.ToNameU8(),
                                _                     => new StringU8($"{data.Customize[customize].Value}"),
                            }),
                        });
                }

                foreach (var advancedCustomize in CustomizeParameterExtensions.AllFlags)
                {
                    if (application.Parameters.HasFlag(advancedCustomize) && _state.Add(advancedCustomize))
                        _changes.Add(new Change(advancedCustomize, link as Design)
                        {
                            Slot   = advancedCustomize.ToNameU8(),
                            Source = designName,
                            Target = GetTarget(link, () => new StringU8($"{data.Parameters[advancedCustomize]}")),
                        });
                }

                foreach (var meta in MetaExtensions.AllRelevant)
                {
                    var flag = meta.ToFlag();
                    if (application.Meta.HasFlag(flag) && _state.Add(meta))
                        _changes.Add(new Change(meta, link as Design)
                        {
                            Slot   = meta.ToNameU8(),
                            Source = designName,
                            Target = GetTarget(link, () => data.GetMeta(meta) ? True : False),
                        });
                }

                foreach (var crest in CrestExtensions.AllRelevantSet)
                {
                    if (application.Crest.HasFlag(crest) && _state.Add(crest))
                        _changes.Add(new Change(crest, link as Design)
                        {
                            Slot   = new StringU8($"{crest.ToLabelU8()} Crest"),
                            Source = designName,
                            Target = GetTarget(link, () => data.Crest(crest) ? True : False),
                        });
                }

                foreach (var (key, advancedDye) in link.GetMaterialData().Where(p => p.Item2.Enabled))
                {
                    var index = MaterialValueIndex.FromKey(key);
                    if (_materials.Add(index))
                        _changes.Add(new Change(index, link as Design)
                        {
                            Slot   = new StringU8($"{index}"),
                            Source = designName,
                            Target = GetTarget(link,
                                () => advancedDye.Revert ? ResetToGame : advancedDye.Mode is ColorRow.Mode.Legacy ? Legacy : Dawntrail),
                        });
                }

                if (link is Design d)
                    foreach (var (mod, settings) in d.AssociatedMods)
                    {
                        _changes.Add(new Change(0x0000000400000000u, d)
                        {
                            Slot   = new StringU8(mod.Name),
                            Source = designName,
                            Target = settings.Remove ? Removed : settings.ForceInherit ? Inherited : settings.Enabled ? Enabled : Disabled,
                        });
                    }
            }
        }

        _changes.Sort((lhs, rhs) => lhs.Type.CompareTo(rhs.Type));
    }

    public IEnumerator<Change> GetEnumerator()
        => _changes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _changes.Count;

    public Change this[int index]
        => _changes[index];
}
