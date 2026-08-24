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

    private void HandleDotFolder(ref string path)
    {
        // We only care about paths that are exactly '.'.
        if (path is not ".")
            return;

        // We ignore this feature if there is an object called '.' in root.
        if (HasDotObject)
            return;

        // If there is a single design selected, take its parent folder as path.
        if (Selection.Selection is { } design)
        {
            path = design.Parent?.FullPath ?? string.Empty;
        }
        else if (Selection.OrderedNodes.Count > 0)
        {
            // If there are multiple objects selected, take the first selected object's path or parent path.
            var parent = Selection.OrderedNodes[0] switch
            {
                IFileSystemFolder f => f.FullPath,
                { } n               => (n.Parent ?? Root).FullPath,
            };
            // Find the topmost shared folder in all selected objects.
            foreach (var node in Selection.Folders)
                parent = parent[..node.FullPath.AsSpan().CommonPrefixLength(parent)];
            foreach (var node in Selection.OrderedNodes)
                parent = parent[..(node.Parent ?? Root).FullPath.AsSpan().CommonPrefixLength(parent)];
            if (Find(parent, out var desiredParent) && desiredParent is IFileSystemFolder)
                path = parent;
        }
        else
        {
            // Else use the root.
            path = string.Empty;
        }
    }

    private void OnDesignChanged(in DesignChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case DesignChanged.Type.ReloadedAll: _saver.Load(); break;
            case DesignChanged.Type.Created:
                var parent = Root;
                var folder = (arguments.Transaction as CreationTransaction)?.Path ?? arguments.Design.Path.Folder;
                HandleDotFolder(ref folder);
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
        }
    }

    public void Dispose()
    {
        _navigation.Design -= OnTabSelected;
        _designChanged.Unsubscribe(OnDesignChanged);
    }
}
