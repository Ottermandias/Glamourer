using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Glamourer.GameData;

namespace Glamourer.Designs.History;

public interface ITransaction
{
    public ITransaction? Merge(ITransaction other);
    public void          Revert(IDesignEditor editor, object data);
}

public readonly record struct CustomizeTransaction(CustomizeIndex Slot, CustomizeValue Old, CustomizeValue New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is CustomizeTransaction other && Slot == other.Slot ? new CustomizeTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeCustomize(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct EntireCustomizeTransaction(CustomizeFlag Apply, CustomizeArray Old, CustomizeArray New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is EntireCustomizeTransaction other ? new EntireCustomizeTransaction(Apply | other.Apply, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeEntireCustomize(data, Old, Apply, ApplySettings.Manual);
}

public readonly record struct EquipTransaction(EquipSlot Slot, EquipItem Old, EquipItem New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is EquipTransaction other && Slot == other.Slot ? new EquipTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeItem(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct BonusItemTransaction(BonusItemFlag Slot, EquipItem Old, EquipItem New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is BonusItemTransaction other && Slot == other.Slot ? new BonusItemTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeBonusItem(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct WeaponTransaction(
    EquipItem OldMain,
    EquipItem OldOff,
    EquipItem OldGauntlets,
    EquipItem NewMain,
    EquipItem NewOff,
    EquipItem NewGauntlets)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is WeaponTransaction other
            ? new WeaponTransaction(other.OldMain, other.OldOff, other.OldGauntlets, NewMain, NewOff, NewGauntlets)
            : null;

    public void Revert(IDesignEditor editor, object data)
    {
        editor.ChangeItem(data, EquipSlot.MainHand, OldMain,      ApplySettings.Manual);
        editor.ChangeItem(data, EquipSlot.OffHand,  OldOff,       ApplySettings.Manual);
        editor.ChangeItem(data, EquipSlot.Hands,    OldGauntlets, ApplySettings.Manual);
    }
}

public readonly record struct StainTransaction(EquipSlot Slot, StainIds Old, StainIds New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is StainTransaction other && Slot == other.Slot ? new StainTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeStains(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct CrestTransaction(CrestFlag Slot, bool Old, bool New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is CrestTransaction other && Slot == other.Slot ? new CrestTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeCrest(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct ParameterTransaction(CustomizeParameterFlag Slot, CustomizeParameterValue Old, CustomizeParameterValue New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => older is ParameterTransaction other && Slot == other.Slot ? new ParameterTransaction(Slot, other.Old, New) : null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeCustomizeParameter(data, Slot, Old, ApplySettings.Manual);
}

public readonly record struct MetaTransaction(MetaIndex Slot, bool Old, bool New)
    : ITransaction
{
    public ITransaction? Merge(ITransaction older)
        => null;

    public void Revert(IDesignEditor editor, object data)
        => editor.ChangeMetaState(data, Slot, Old, ApplySettings.Manual);
}
