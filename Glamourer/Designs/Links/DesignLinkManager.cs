using Glamourer.Automation;
using Glamourer.Events;
using Glamourer.Services;
using OtterGui.Services;

namespace Glamourer.Designs.Links;

public sealed class DesignLinkManager : IService, IDisposable
{
    private readonly DesignStorage _storage;
    private readonly DesignChanged _event;
    private readonly SaveService   _saveService;

    public DesignLinkManager(DesignStorage storage, DesignChanged @event, SaveService saveService)
    {
        _storage     = storage;
        _event       = @event;
        _saveService = saveService;

        _event.Subscribe(OnDesignChanged, DesignChanged.Priority.DesignLinkManager);
    }

    public void Dispose()
        => _event.Unsubscribe(OnDesignChanged);

    public void MoveDesignLink(Design parent, int idxFrom, LinkOrder orderFrom, int idxTo, LinkOrder orderTo)
    {
        if (!parent.Links.Reorder(idxFrom, orderFrom, idxTo, orderTo))
            return;

        parent.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(parent);
        Glamourer.Log.Debug($"Moved link from {orderFrom} {idxFrom} to {idxTo} {orderTo}.");
        _event.Invoke(DesignChanged.Type.ChangedLink, parent, null);
    }

    public void AddDesignLink(Design parent, Design child, LinkOrder order)
    {
        if (!LinkContainer.AddLink(parent, child, ApplicationType.All, order, out _))
            return;

        parent.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(parent);
        Glamourer.Log.Debug($"Added new {order} link to {child.Identifier} for {parent.Identifier}.");
        _event.Invoke(DesignChanged.Type.ChangedLink, parent, null);
    }

    public void RemoveDesignLink(Design parent, int idx, LinkOrder order)
    {
        if (!parent.Links.Remove(idx, order))
            return;

        parent.LastEdit = DateTimeOffset.UtcNow;
        _saveService.QueueSave(parent);
        Glamourer.Log.Debug($"Removed the {order} link at {idx} for {parent.Identifier}.");
        _event.Invoke(DesignChanged.Type.ChangedLink, parent, null);
    }

    private void OnDesignChanged(DesignChanged.Type type, Design deletedDesign, object? _)
    {
        if (type is not DesignChanged.Type.Deleted)
            return;

        foreach (var design in _storage)
        {
            if (design.Links.Remove(deletedDesign))
            {
                design.LastEdit = DateTimeOffset.UtcNow;
                Glamourer.Log.Debug($"Removed {deletedDesign.Identifier} from {design.Identifier} links due to deletion.");
                _saveService.QueueSave(design);
            }
        }
    }
}
