using Glamourer.Config;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using Luna;

namespace Glamourer.Gui.Tabs.DesignTab;

public sealed class DesignFileSystemDrawer : FileSystemDrawer<DesignFileSystemCache.DesignData>, IDisposable
{
    internal readonly Configuration Config;
    internal readonly DesignApplier DesignApplier;
    internal readonly DesignChanged DesignChanged;
    internal readonly DesignColors  DesignColors;
    internal readonly DesignManager Manager;

    public DesignFileSystemDrawer(MessageService messager, DesignFileSystem fileSystem, DesignManager manager, DesignConverter converter,
        Configuration config,
        DesignApplier designApplier, DesignChanged designChanged, DesignColors designColors)
        : base(messager, fileSystem, new DesignFilter(config))
    {
        Manager       = manager;
        Config        = config;
        DesignApplier = designApplier;
        DesignChanged = designChanged;
        DesignColors  = designColors;
        Footer.Buttons.AddButton(new NewDesignButton(manager),                           1000);
        Footer.Buttons.AddButton(new ImportDesignButton(converter, manager),             900);
        Footer.Buttons.AddButton(new DuplicateDesignButton(fileSystem, manager),         800);
        Footer.Buttons.AddButton(new DeleteSelectionButton(fileSystem, manager, config), -100);

        SortMode = Config.SortMode;
        OnRenameChanged(Config.ShowRename, default);
        Config.OnRenameChanged += OnRenameChanged;
    }

    private void OnRenameChanged(RenameField newValue, RenameField _)
    {
        DataContext.RemoveButtons<MoveDesignInput>();
        DataContext.RemoveButtons<RenameDesignInput>();
        switch (newValue)
        {
            case RenameField.RenameSearchPath: DataContext.AddButton(new MoveDesignInput(this),   -1000); break;
            case RenameField.RenameData:       DataContext.AddButton(new RenameDesignInput(this), -1000); break;
            case RenameField.BothSearchPathPrio:
                DataContext.AddButton(new RenameDesignInput(this), -1000);
                DataContext.AddButton(new MoveDesignInput(this),   -1001);
                break;
            case RenameField.BothDataPrio:
                DataContext.AddButton(new RenameDesignInput(this), -1001);
                DataContext.AddButton(new MoveDesignInput(this),   -1000);
                break;
        }
    }

    public void Dispose()
    {
        Config.OnRenameChanged -= OnRenameChanged;
    }

    public override Vector4 ExpandedFolderColor
        => ColorId.FolderExpanded.Value().ToVector();

    public override Vector4 CollapsedFolderColor
        => ColorId.FolderCollapsed.Value().ToVector();

    public override Vector4 FolderLineColor
        => ColorId.FolderLine.Value().ToVector();

    public override IEnumerable<ISortMode> ValidSortModes
        => ISortMode.Valid.Values;

    public override ReadOnlySpan<byte> Id
        => "Designs"u8;

    protected override FileSystemCache<DesignFileSystemCache.DesignData> CreateCache()
        => new DesignFileSystemCache(this);
}
