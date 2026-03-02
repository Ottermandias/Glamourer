using Dalamud.Interface.ImGuiNotification;
using Glamourer.Events;
using Glamourer.Services;
using Luna;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly DesignFileSystemSaver _saver;
    private readonly DesignChanged         _designChanged;
    private readonly TabSelected           _tabSelected;

    public DesignFileSystem(Logger log, SaveService saveService, DesignStorage designs, DesignChanged designChanged, TabSelected tabSelected)
        : base("DesignFileSystem", log, true)
    {
        _designChanged = designChanged;
        _tabSelected   = tabSelected;
        _saver         = new DesignFileSystemSaver(log, this, saveService, designs);

        _saver.Load();
        _designChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignFileSystem);
        _tabSelected.Subscribe(OnTabSelected, TabSelected.Priority.DesignSelector);
    }

    private void OnTabSelected(in TabSelected.Arguments arguments)
    {
        if (arguments.Design?.Node is { } node)
            Selection.Select(node, true);
    }

    private void OnDesignChanged(in DesignChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case DesignChanged.Type.ReloadedAll: _saver.Load(); break;
            case DesignChanged.Type.Created:
                var parent = Root;
                if (arguments.Design.Path.Folder.Length > 0)
                    try
                    {
                        parent = FindOrCreateAllFolders(arguments.Design.Path.Folder);
                    }
                    catch (Exception ex)
                    {
                        Glamourer.Messager.NotificationMessage(ex,
                            $"Could not move design to {arguments.Design.Path} because the folder could not be created.",
                            NotificationType.Error);
                    }

                var (data, _) = CreateDuplicateDataNode(parent, arguments.Design.Path.SortName ?? arguments.Design.Name, arguments.Design);
                Selection.Select(data, true);
                break;
            case DesignChanged.Type.Deleted:
                if (arguments.Design.Node is { } node)
                {
                    if (node.Selected)
                        Selection.UnselectAll();
                    Delete(node);
                }

                break;
            case DesignChanged.Type.Renamed when arguments.Design.Path.SortName is null:
                RenameWithDuplicates(arguments.Design.Node!, arguments.Design.Path.GetIntendedName(arguments.Design.Name));
                break;
            // TODO: Maybe add path changes?
        }
    }

    public void Dispose()
    {
        _tabSelected.Unsubscribe(OnTabSelected);
        _designChanged.Unsubscribe(OnDesignChanged);
    }
}
