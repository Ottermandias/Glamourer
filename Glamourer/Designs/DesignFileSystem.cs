using Dalamud.Interface.ImGuiNotification;
using Glamourer.Designs.History;
using Glamourer.Events;
using Glamourer.Services;
using Luna;

namespace Glamourer.Designs;

public sealed class DesignFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly DesignFileSystemSaver _saver;
    private readonly DesignChanged         _designChanged;

    public DesignFileSystem(Logger log, SaveService saveService, DesignStorage designs, DesignChanged designChanged)
        : base("DesignFileSystem", log, true)
    {
        _designChanged = designChanged;
        _saver         = new DesignFileSystemSaver(log, this, saveService, designs);

        _saver.Load();
        _designChanged.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignFileSystem);
    }

    private void OnDesignChanged(DesignChanged.Type type, Design design, ITransaction? _)
    {
        switch (type)
        {
            case DesignChanged.Type.ReloadedAll: _saver.Load(); break;
            case DesignChanged.Type.Created:
                var parent = Root;
                if (design.Path.Folder.Length > 0)
                    try
                    {
                        parent = FindOrCreateAllFolders(design.Path.Folder);
                    }
                    catch (Exception ex)
                    {
                        Glamourer.Messager.NotificationMessage(ex,
                            $"Could not move design to {design.Path} because the folder could not be created.",
                            NotificationType.Error);
                    }

                var (data, _) = CreateDuplicateDataNode(parent, design.Path.SortName ?? design.Name, design);
                Selection.Select(data);
                break;
            case DesignChanged.Type.Deleted:
                if (design.Node is { } node)
                {
                    if (node.Selected)
                        Selection.UnselectAll();
                    Delete(node);
                }

                break;
            case DesignChanged.Type.Renamed when design.Path.SortName is null:
                RenameWithDuplicates(design.Node!, design.Path.GetIntendedName(design.Name.Text));
                break;
            // TODO: Maybe add path changes?
        }
    }

    public void Dispose()
    {
        _designChanged.Unsubscribe(OnDesignChanged);
    }
}
