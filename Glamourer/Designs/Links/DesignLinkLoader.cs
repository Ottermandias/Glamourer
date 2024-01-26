using Dalamud.Interface.Internal.Notifications;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Services;

namespace Glamourer.Designs.Links;

public sealed class DesignLinkLoader(DesignStorage designStorage, MessageService messager)
    : DelayedReferenceLoader<Design, LinkData>(messager), IService
{
    protected override bool TryGetObject(LinkData data, [NotNullWhen(true)] out Design? obj)
        => designStorage.FindFirst(d => d.Identifier == data.Identity, out obj);

    protected override bool SetObject(Design parent, Design child, LinkData data, out string error)
        => LinkContainer.AddLink(parent, child, data.Type, data.Order, out error);

    protected override void HandleChildNotFound(Design parent, LinkData data)
    {
        Messager.AddMessage(new Notification(
            $"Could not find the design {data.Identity}. If this design was deleted, please re-save {parent.Identifier}.",
            NotificationType.Warning));
    }

    protected override void HandleChildNotSet(Design parent, Design child, string error)
        => Messager.AddMessage(new Notification($"Could not link {child.Identifier} to {parent.Identifier}: {error}",
            NotificationType.Warning));
}
