using Glamourer.Automation;
using Glamourer.Events;
using ImSharp;
using Luna;
using Penumbra.GameData.Interop;

namespace Glamourer.Gui.Tabs.AutomationTab;

public sealed class SetSelector(
    AutomationSelection selection,
    Configuration config,
    AutoDesignManager manager,
    AutomationFilter filter,
    ActorObjectManager objects,
    AutomationChanged automationChanged)
    : IPanel
{
    private readonly AutomationFilter  _filter            = filter;
    private readonly AutoDesignManager _manager           = manager;
    private readonly AutomationChanged _automationChanged = automationChanged;

    public ReadOnlySpan<byte> Id
        => "Automation Selector"u8;

    public void Draw()
    {
        Im.Cursor.Y += Im.Style.FramePadding.Y;
        var       cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache(this));
        using var clip  = new Im.ListClipper(cache.Count, cache.SelectableSize.Y);
        foreach (var item in clip.Iterate(cache))
        {
            Im.Cursor.X += Im.Style.FramePadding.X;
            using var id    = Im.Id.Push(item.Index);
            using var group = Im.Group();
            DrawSetSelectable(cache, item);
        }
    }

    private void DrawSetSelectable(Cache cache, in AutomationCacheItem item)
    {
        using (ImGuiColor.Text.Push(item.Set.Enabled ? cache.EnabledSet : cache.DisabledSet))
        {
            if (Im.Selectable(config.Ephemeral.IncognitoMode ? item.Incognito : item.Name.Utf8, item.Set == selection.Set,
                    SelectableFlags.None, cache.SelectableSize))
                selection.Update(item);
        }

        var lineEnd   = Im.Item.LowerRightCorner;
        var lineStart = new Vector2(Im.Item.UpperLeftCorner.X, lineEnd.Y);
        Im.Window.DrawList.Shape.Line(lineStart, lineEnd, cache.LineColor, Im.Style.GlobalScale);

        DrawDragDrop(cache, item);

        var identifier = config.Ephemeral.IncognitoMode ? item.IdentifierIncognito : item.IdentifierString;
        var textSize   = identifier.CalculateSize();
        var textColor  = item.Set.Identifiers.Any(objects.ContainsKey) ? cache.AutomationAvailable : cache.AutomationUnavailable;
        Im.Cursor.Position = new Vector2(Im.ContentRegion.Available.X - textSize.X - Im.Style.FramePadding.X, Im.Cursor.Y - Im.Style.TextHeightWithSpacing);
        Im.Text(identifier, textColor);
    }

    private void DrawDragDrop(Cache cache, in AutomationCacheItem item)
    {
        using (var target = Im.DragDrop.Target())
        {
            if (target.IsDropping("DesignSetDragDrop"u8))
            {
                if (cache.SetDragIndex >= 0)
                {
                    var idx = cache.SetDragIndex;
                    _manager.MoveSet(idx, item.Index);
                }

                cache.SetDragIndex = -1;
            }
            else if (target.IsDropping("DesignDragDrop"u8))
            {
                if (selection.DraggedDesignIndex >= 0)
                {
                    var idx     = selection.DraggedDesignIndex;
                    var setTo   = item.Set;
                    var setFrom = selection.Set!;
                    _manager.MoveDesignToSet(setFrom, idx, setTo);
                }

                selection.DraggedDesignIndex = -1;
            }
        }

        using (var source = Im.DragDrop.Source())
        {
            if (!source)
                return;

            Im.Text($"Moving design set {item.Name.Utf8} from position {item.Index + 1}...");
            if (source.SetPayload("DesignSetDragDrop"u8))
                cache.SetDragIndex = item.Index;
        }
    }


    private sealed class Cache : BasicFilterCache<AutomationCacheItem>
    {
        public Vector2 SelectableSize;
        public Vector4 EnabledSet;
        public Vector4 DisabledSet;
        public Vector4 AutomationAvailable;
        public Vector4 AutomationUnavailable;
        public Rgba32  LineColor;

        public           int         SetDragIndex = -1;
        private readonly SetSelector _parent;

        public Cache(SetSelector parent)
            : base(parent._filter)
        {
            _parent = parent;
            _parent._automationChanged.Subscribe(OnAutomationChanged, AutomationChanged.Priority.SetSelector);
        }

        protected override void Dispose(bool disposing)
        {
            _parent._automationChanged.Unsubscribe(OnAutomationChanged);
            base.Dispose(disposing);
        }

        private void OnAutomationChanged(AutomationChanged.Type type, AutoDesignSet? set, object? data)
        {
            switch (type)
            {
                case AutomationChanged.Type.DeletedSet:
                case AutomationChanged.Type.AddedSet:
                case AutomationChanged.Type.MovedSet:
                case AutomationChanged.Type.RenamedSet:
                case AutomationChanged.Type.ChangeIdentifier:
                case AutomationChanged.Type.ToggleSet:
                    Dirty |= IManagedCache.DirtyFlags.Custom;
                    break;
            }
        }

        protected override IEnumerable<AutomationCacheItem> GetItems()
            => _parent._manager.Index().Select(s => new AutomationCacheItem(s.Item, s.Index));

        public override void Update()
        {
            SelectableSize        = new Vector2(0, 2 * Im.Style.TextHeight + Im.Style.ItemSpacing.Y);
            EnabledSet            = ColorId.EnabledAutoSet.Value().ToVector();
            DisabledSet           = ColorId.DisabledAutoSet.Value().ToVector();
            AutomationAvailable   = ColorId.AutomationActorAvailable.Value().ToVector();
            AutomationUnavailable = ColorId.AutomationActorUnavailable.Value().ToVector();
            LineColor             = ImGuiColor.Border.Get();
            base.Update();
        }
    }
}
