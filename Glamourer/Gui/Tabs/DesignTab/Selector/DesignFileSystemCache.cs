using Glamourer.Designs;
using Glamourer.Designs.History;
using Glamourer.Events;
using ImSharp;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFileSystemCache : FileSystemCache<DesignFileSystemCache.DesignData>
{
    public DesignFileSystemCache(DesignFileSystemDrawer parent)
        : base(parent)
    {
        parent.DesignChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignFileSystemSelector);
        parent.DesignColors.ColorChanged += OnColorChanged;
    }

    private void OnColorChanged()
    {
        foreach (var node in AllNodes.Values)
            node.Dirty = true;
    }

    private void OnDesignChanged(DesignChanged.Type type, Design design, ITransaction? _2)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
            case DesignChanged.Type.Deleted:
            case DesignChanged.Type.ReloadedAll:
            case DesignChanged.Type.Renamed:
            case DesignChanged.Type.ChangedDescription:
            case DesignChanged.Type.ChangedColor:
            case DesignChanged.Type.AddedTag:
            case DesignChanged.Type.RemovedTag:
            case DesignChanged.Type.ChangedTag:
            case DesignChanged.Type.AddedMod:
            case DesignChanged.Type.RemovedMod:
            case DesignChanged.Type.UpdatedMod:
            case DesignChanged.Type.ChangedLink:
            case DesignChanged.Type.Equip:
            case DesignChanged.Type.BonusItem:
            case DesignChanged.Type.Weapon:
                VisibleDirty = true;
                break;
        }

        if (design.Node is { } node && AllNodes.TryGetValue(node, out var cache))
            cache.Dirty = true;
    }

    private new DesignFileSystemDrawer Parent
        => (DesignFileSystemDrawer)base.Parent;

    public override void Update()
    {
        if (ColorsDirty)
        {
            CollapsedFolderColor =  ColorId.FolderCollapsed.Value().ToVector();
            ExpandedFolderColor  =  ColorId.FolderExpanded.Value().ToVector();
            LineColor            =  ColorId.FolderLine.Value().ToVector();
            Dirty                &= ~IManagedCache.DirtyFlags.Colors;
            OnColorChanged();
        }
    }

    protected override DesignData ConvertNode(in IFileSystemNode node)
        => new((IFileSystemData<Design>)node);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Parent.DesignChanged.Unsubscribe(OnDesignChanged);
        Parent.DesignColors.ColorChanged -= OnColorChanged;
    }

    public sealed class DesignData(IFileSystemData<Design> node) : BaseFileSystemNodeCache<DesignData>
    {
        public readonly IFileSystemData<Design> Node = node;
        public          Vector4                 Color;
        public          StringU8                Name      = new(node.Value.Name.Text);
        public          StringU8                Incognito = new(node.Value.Incognito);

        public override void Update(FileSystemCache cache, IFileSystemNode node)
        {
            var drawer = (DesignFileSystemDrawer)cache.Parent;
            Color = drawer.DesignColors.GetColor(Node.Value).ToVector();
            Name  = new StringU8(Node.Value.Name.Text);
        }

        protected override void DrawInternal(FileSystemCache<DesignData> cache, IFileSystemNode node)
        {
            var       c     = (DesignFileSystemCache)cache;
            using var color = ImGuiColor.Text.Push(Color);
            using var id    = Im.Id.Push(Node.Value.Index);
            var       flags = node.Selected ? TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Selected : TreeNodeFlags.NoTreePushOnOpen;
            Im.Tree.Leaf(c.Parent.Config.Ephemeral.IncognitoMode ? Incognito : Name, flags);
            CheckDoubleClick(c);
        }

        private void CheckDoubleClick(DesignFileSystemCache cache)
        {
            if (!cache.Parent.Config.AllowDoubleClickToApply)
                return;
            if (!Im.Item.Hovered())
                return;

            if (Im.Mouse.IsDoubleClicked(MouseButton.Left))
                cache.Parent.DesignApplier.ApplyToPlayer(Node.Value);
        }
    }
}
