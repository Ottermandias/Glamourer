using Glamourer.Designs;
using Glamourer.Designs.History;

namespace Glamourer.Interop.CustomizePlus;

public record CustomizePlusActionTransaction(Guid Id, CustomizePlusAction Old, CustomizePlusAction New) : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is CustomizePlusActionTransaction other && Id == other.Id
            ? new CustomizePlusActionTransaction(Id, other.Old, New)
            : null;

    public void Revert(IDesignEditor editor, object data)
        => ((DesignManager)editor).SetCustomizePlusAction((Design)data, Id, Old);
}
