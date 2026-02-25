using Luna;
using Notification = Luna.Notification;

namespace Glamourer.Designs.Links;

public sealed class DesignLinkLoader(DesignStorage designStorage, MessageService messager)
    : DelayedReferenceLoader<Design, Design, LinkData>(messager), IService
{
    protected override bool TryGetObject(in LinkData data, [NotNullWhen(true)] out Design? obj)
    {
        var identity = data.Identity;
        return designStorage.FindFirst(d => d.Identifier == identity, out obj);
    }

    protected override bool SetObject(Design parent, Design child, in LinkData data, out string error)
        => LinkContainer.AddLink(parent, child, data.Type, data.Order, out error);

    protected override void HandleChildNotFound(Design parent, in LinkData data)
    {
        Messager.AddMessage(new Notification(
            $"Could not find the design {data.Identity}. If this design was deleted, please re-save {parent.Identifier}."));
    }

    protected override void HandleChildNotSet(Design parent, Design child, string error)
        => Messager.AddMessage(new Notification($"Could not link {child.Identifier} to {parent.Identifier}: {error}"));
}
