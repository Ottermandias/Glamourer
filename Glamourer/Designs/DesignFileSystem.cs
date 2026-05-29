using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs.History;
using Glamourer.Events;
using Glamourer.Gui;
using Glamourer.Services;
using Luna;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly DesignFileSystemSaver _saver;
    private readonly DesignChanged         _designChanged;
    private readonly NavigationService     _navigation;

    public DesignFileSystem(LunaLogger log, SaveService saveService, DesignStorage designs, DesignChanged designChanged,
        NavigationService navigation)
        : base("DesignFileSystem", log, true)
    {
        _designChanged = designChanged;
        _navigation    = navigation;
        _saver         = new DesignFileSystemSaver(log, this, saveService, designs);

        _saver.Load();
        _designChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignFileSystem);
        _navigation.Design += OnTabSelected;
    }

    private void OnTabSelected(Design? design)
    {
        if (design?.Node is { } node)
            Selection.Select(node, true);
    }

    private void OnDesignChanged(in DesignChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case DesignChanged.Type.ReloadedAll: _saver.Load(); break;
            case DesignChanged.Type.Created:
                var parent = Root;
                var folder = (arguments.Transaction as CreationTransaction)?.Path ?? arguments.Design.Path.Folder;
                if (folder.Length > 0)
                    try
                    {
                        parent = FindOrCreateAllFolders(folder);
                    }
                    catch (Exception ex)
                    {
                        Glamourer.Messager.NotificationMessage(ex,
                            $"Could not move design to {folder} because the folder could not be created.",
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
        _navigation.Design -= OnTabSelected;
        _designChanged.Unsubscribe(OnDesignChanged);
    }
}
